using System.IO;
using UnityEditor;
using UnityEngine;

namespace LibraryEscape.EditorTools
{
    /// <summary>
    /// Настраивает импорт нарисованных PNG (PPU 20, Point, без сжатия) и раскладывает
    /// спрайты по слотам LevelArt. Запускается автоматически при первом импорте,
    /// вручную — Tools > Library Escape > Setup Level 1 Art.
    /// </summary>
    [InitializeOnLoad]
    public static class Level1ArtImporter
    {
        private const string ArtFolder = "Assets/Art/Level1";
        private const string ArtPath = "Assets/Level1/LevelArt.asset";
        private const string SetupKey = "LibraryEscape.Level1ArtSetup.v1";
        private const float PixelsPerUnit = 20f;

        private static readonly string[] Textures =
        {
            "floor40x40.png",
            "paper16x20.png",
            "player1.png",
            "player2.png",
            "player3.png",
            "security1.png",
            "security2.png",
            "security3.png",
            "commode.png",
            "room1_base.png",
            "room1_2_base.png"
        };

        static Level1ArtImporter()
        {
            EditorApplication.delayCall += AutoSetup;
        }

        private static void AutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (EditorPrefs.GetBool(SetupKey, false)) return;
            if (!File.Exists(ArtFolder + "/commode.png")) return;

            Setup();
            EditorPrefs.SetBool(SetupKey, true);
        }

        [MenuItem("Tools/Library Escape/Setup Level 1 Art")]
        public static void SetupMenu()
        {
            Setup();
            EditorPrefs.SetBool(SetupKey, true);
        }

        private static void Setup()
        {
            AssetDatabase.Refresh();

            for (int i = 0; i < Textures.Length; i++)
            {
                ConfigureTexture(Textures[i]);
            }

            AssetDatabase.Refresh();
            AssignSprites();
            Debug.Log("Library Escape: спрайты уровня 1 импортированы (PPU 20, Point) и привязаны к LevelArt.");
        }

        private static void ConfigureTexture(string fileName)
        {
            string path = ArtFolder + "/" + fileName;
            if (!File.Exists(path)) return;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }
            if (importer == null) return;

            Vector2 pivot;
            bool customPivot = TryGetCustomPivot(fileName, out pivot);

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spritePixelsPerUnit = PixelsPerUnit;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.filterMode = FilterMode.Point;
            settings.mipmapEnabled = false;
            settings.alphaIsTransparency = true;
            settings.npotScale = TextureImporterNPOTScale.None;
            settings.wrapMode = NeedsRepeat(fileName) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            settings.spriteGenerateFallbackPhysicsShape = false;

            if (customPivot)
            {
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
            }
            else
            {
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePivot = new Vector2(0.5f, 0.5f);
            }

            importer.SetTextureSettings(settings);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.isReadable = false;

            TextureImporterPlatformSettings platform = importer.GetDefaultPlatformTextureSettings();
            platform.maxTextureSize = 2048;
            platform.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(platform);

            importer.SaveAndReimport();
        }

        private static bool TryGetCustomPivot(string fileName, out Vector2 pivot)
        {
            // Пивот в центре видимых пикселей, а не пустого холста.
            if (fileName.StartsWith("player"))
            {
                pivot = new Vector2(0.5f, 0.233f);
                return true;
            }

            if (fileName.StartsWith("security"))
            {
                pivot = new Vector2(0.5f, 0.75f);
                return true;
            }

            pivot = new Vector2(0.5f, 0.5f);
            return false;
        }

        private static bool NeedsRepeat(string fileName)
        {
            return fileName.StartsWith("floor");
        }

        private static void AssignSprites()
        {
            LevelArt art = AssetDatabase.LoadAssetAtPath<LevelArt>(ArtPath);
            if (art == null) return;

            art.floor = Load("floor40x40.png");
            art.paper = Load("paper16x20.png");
            art.commode = Load("commode.png");
            art.roomClosed = Load("room1_base.png");
            art.roomOpened = Load("room1_2_base.png");

            Sprite player1 = Load("player1.png");
            Sprite player2 = Load("player2.png");
            Sprite player3 = Load("player3.png");
            art.player = player1;
            art.playerWalk = new[] { player1, player2, player3 };

            Sprite guard1 = Load("security1.png");
            Sprite guard2 = Load("security2.png");
            Sprite guard3 = Load("security3.png");
            art.guard = guard1;
            art.guardWalk = new[] { guard1, guard2, guard3 };

            // Цвета-заглушки для нарисованных спрайтов не нужны.
            art.playerColor = Color.white;
            art.guardColor = Color.white;
            art.paperColor = Color.white;
            art.floorColor = Color.white;

            EditorUtility.SetDirty(art);
            AssetDatabase.SaveAssets();
        }

        private static Sprite Load(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + "/" + fileName);
        }
    }
}
