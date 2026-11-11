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
    /// Создаёт сцену Level2.unity (банковское хранилище) и добавляет её в Build Settings.
    /// </summary>
    public static class Level2SceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Level2.unity";
        private const string ArtPath = "Assets/Level1/LevelArt.asset";
        private const float CameraSize = 7f;

        [MenuItem("Tools/Library Escape/Rebuild Level 2 Scene")]
        public static void Rebuild()
        {
            BuildScene(true);
        }

        public static void BuildScene(bool openAfter)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");

            LevelArt art = AssetDatabase.LoadAssetAtPath<LevelArt>(ArtPath);
            EditorSceneManager.SaveOpenScenes();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            GameObject cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";

            Camera gameCamera = cameraGo.AddComponent<Camera>();
            gameCamera.orthographic = true;
            gameCamera.orthographicSize = CameraSize;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = new Color(0.045f, 0.050f, 0.065f);
            gameCamera.transform.position = new Vector3(0f, 0f, -10f);
            cameraGo.AddComponent<AudioListener>();

            FollowCamera2D follow = cameraGo.AddComponent<FollowCamera2D>();
            follow.orthographicSize = CameraSize;
            follow.bounds = new Rect(-Level2Bootstrap.RoomWidth * 0.5f, -Level2Bootstrap.RoomHeight * 0.5f,
                Level2Bootstrap.RoomWidth, Level2Bootstrap.RoomHeight);

            GameObject levelGo = new GameObject("Level 2");
            Level2Bootstrap bootstrap = levelGo.AddComponent<Level2Bootstrap>();
            bootstrap.art = art;
            bootstrap.cameraSize = CameraSize;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.Refresh();

            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            if (openAfter) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
    }
}
