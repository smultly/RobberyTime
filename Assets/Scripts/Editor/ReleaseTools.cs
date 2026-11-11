using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LibraryEscape.EditorTools
{
    /// <summary>
    /// Подготовка релиза: имя игры, версия, окно, порядок сцен и сборка Windows x64.
    /// Меню: Tools > Library Escape.
    /// </summary>
    public static class ReleaseTools
    {
        private const string ProductName = "RobberyTime";
        private const string CompanyName = "smultly";
        private const string Version = "1.0.0";

        private const int WindowWidth = 1600;
        private const int WindowHeight = 900;

        private const string MenuScene = "Assets/Scenes/MainMenu.unity";
        private const string Level1Scene = "Assets/Scenes/Level1.unity";
        private const string Level2Scene = "Assets/Scenes/Level2.unity";

        private const string BuildFolder = "Builds/Win64";
        private const string BuildName = "RobberyTime.exe";

        [MenuItem("Tools/Library Escape/1. Prepare Release Settings")]
        public static void PrepareRelease()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = Version;

            // Окно 1600×900, можно растягивать, Alt+Enter работает.
            PlayerSettings.defaultScreenWidth = WindowWidth;
            PlayerSettings.defaultScreenHeight = WindowHeight;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.allowFullscreenSwitch = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.forceSingleInstance = true;

            DisableNativeResolution();

            // Только три игровые сцены, в правильном порядке.
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScene, true),
                new EditorBuildSettingsScene(Level1Scene, true),
                new EditorBuildSettingsScene(Level2Scene, true)
            };

            QualitySettings.vSyncCount = 1;

            AssetDatabase.SaveAssets();
            Debug.Log("[RobberyTime] Настройки релиза применены: " + ProductName + " " + Version + ".");
        }

        [MenuItem("Tools/Library Escape/2. Build Windows x64")]
        public static void BuildWindows()
        {
            PrepareRelease();

            Directory.CreateDirectory(BuildFolder);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = BuildFolder + "/" + BuildName,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
            {
                float megabytes = report.summary.totalSize / (1024f * 1024f);
                Debug.Log("[RobberyTime] Сборка готова: " + Path.GetFullPath(options.locationPathName) +
                          " (" + megabytes.ToString("F1") + " МБ)");

                if (!Application.isBatchMode) EditorUtility.RevealInFinder(Path.GetFullPath(BuildFolder));
            }
            else
            {
                Debug.LogError("[RobberyTime] Сборка не удалась: " + report.summary.result);
            }
        }

        /// <summary>Для запуска из командной строки: -executeMethod LibraryEscape.EditorTools.ReleaseTools.PrepareReleaseAndBuild</summary>
        public static void PrepareReleaseAndBuild()
        {
            PrepareRelease();
            BuildWindows();
        }

        /// <summary>Отключает «родное» разрешение окна — иначе окно откроется во весь монитор.</summary>
        private static void DisableNativeResolution()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;

            SerializedObject settings = new SerializedObject(assets[0]);
            SerializedProperty property = settings.FindProperty("defaultIsNativeResolution");
            if (property == null) return;

            property.boolValue = false;
            settings.ApplyModifiedProperties();
        }
    }
}
