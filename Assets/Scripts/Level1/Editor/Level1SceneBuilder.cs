using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibraryEscape;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LibraryEscape.EditorTools
{
    /// <summary>
    /// Один раз создаёт сцену Level1.unity с камерой и bootstrap-объектом,
    /// добавляет её в Build Settings и открывает. Пункты меню — Tools > Library Escape.
    /// </summary>
    [InitializeOnLoad]
    public static class Level1SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Level1.unity";
        private const string ArtFolder = "Assets/Level1";
        private const string ArtPath = "Assets/Level1/LevelArt.asset";
        private const string AutoBuildKey = "LibraryEscape.Level1.AutoBuilt.v1";

        static Level1SceneBuilder()
        {
            EditorApplication.delayCall += AutoBuild;
        }

        private static void AutoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (EditorPrefs.GetBool(AutoBuildKey, false)) return;

            if (File.Exists(ScenePath))
            {
                EditorPrefs.SetBool(AutoBuildKey, true);
                return;
            }

            BuildScene(true);
            EditorPrefs.SetBool(AutoBuildKey, true);
        }

        [MenuItem("Tools/Library Escape/Rebuild Level 1 Scene")]
        public static void Rebuild()
        {
            BuildScene(true);
        }

        [MenuItem("Tools/Library Escape/Open Level 1 Scene")]
        public static void OpenLevel1()
        {
            if (File.Exists(ScenePath)) OpenScene();
        }

        [MenuItem("Tools/Library Escape/Create Level Art Asset")]
        public static void CreateArtAsset()
        {
            LevelArt art = EnsureArt();
            Selection.activeObject = art;
            EditorGUIUtility.PingObject(art);
        }

        private static void BuildScene(bool openAfter)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");

            LevelArt art = EnsureArt();
            EditorSceneManager.SaveOpenScenes();

            // NewSceneMode.Single закрывает текущие сцены, поэтому путь можно перезаписать.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";

            Camera gameCamera = cameraGo.AddComponent<Camera>();
            gameCamera.orthographic = true;
            gameCamera.orthographicSize = 6f;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = art.backgroundColor;
            gameCamera.transform.position = new Vector3(0f, -19f, -10f);
            cameraGo.AddComponent<AudioListener>();

            FollowCamera2D follow = cameraGo.AddComponent<FollowCamera2D>();
            follow.orthographicSize = 6f;
            follow.bounds = new Rect(-12.5f, -25f, 25f, 50f);

            GameObject levelGo = new GameObject("Level 1");
            Level1Bootstrap bootstrap = levelGo.AddComponent<Level1Bootstrap>();
            bootstrap.art = art;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.Refresh();

            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            if (openAfter) OpenScene();
        }

        private static LevelArt EnsureArt()
        {
            LevelArt art = AssetDatabase.LoadAssetAtPath<LevelArt>(ArtPath);
            if (art != null) return art;

            if (!AssetDatabase.IsValidFolder(ArtFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Level1"))
                {
                    AssetDatabase.CreateFolder("Assets", "Level1");
                }
            }

            art = ScriptableObject.CreateInstance<LevelArt>();
            AssetDatabase.CreateAsset(art, ArtPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return art;
        }

        private static void OpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.GetActiveScene().path == ScenePath) return;

            EditorSceneManager.SaveOpenScenes();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
    }
}
