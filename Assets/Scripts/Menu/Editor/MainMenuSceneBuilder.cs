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
    /// Создаёт сцену MainMenu.unity с главным меню и умеет пересобрать все сцены
    /// в правильном порядке: меню → уровень 1 → уровень 2.
    /// </summary>
    public static class MainMenuSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/MainMenu.unity";

        [MenuItem("Tools/Library Escape/Rebuild Main Menu Scene")]
        public static void Rebuild()
        {
            BuildScene(true);
        }

        public static void BuildScene(bool openAfter)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");

            EditorSceneManager.SaveOpenScenes();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";

            Camera gameCamera = cameraGo.AddComponent<Camera>();
            gameCamera.orthographic = true;
            gameCamera.orthographicSize = 5f;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = new Color(0.055f, 0.060f, 0.078f);
            gameCamera.transform.position = new Vector3(0f, 0f, -10f);
            cameraGo.AddComponent<AudioListener>();

            GameObject menuGo = new GameObject("Main Menu");
            menuGo.AddComponent<MainMenuGame>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.Refresh();

            AddToBuildSettings(ScenePath);

            if (openAfter) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Tools/Library Escape/Rebuild All Scenes")]
        public static void RebuildAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            // Уровень 1 (открывает свою сцену), затем уровень 2 и меню.
            Level1SceneBuilder.Rebuild();
            Level2SceneBuilder.BuildScene(false);
            BuildScene(false);

            ApplyBuildOrder();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void AddToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(s => s.path == scenePath);
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ApplyBuildOrder()
        {
            string[] order =
            {
                ScenePath,
                "Assets/Scenes/Level1.unity",
                Level2SceneBuilder.ScenePath
            };

            List<EditorBuildSettingsScene> result = new List<EditorBuildSettingsScene>();

            for (int i = 0; i < order.Length; i++)
            {
                if (File.Exists(order[i])) result.Add(new EditorBuildSettingsScene(order[i], true));
            }

            // Остальные сцены проекта — в конец, чтобы ничего не потерять.
            for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                EditorBuildSettingsScene scene = EditorBuildSettings.scenes[i];
                if (result.Any(s => s.path == scene.path)) continue;
                result.Add(scene);
            }

            EditorBuildSettings.scenes = result.ToArray();
        }
    }
}
