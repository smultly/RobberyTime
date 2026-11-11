using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LibraryEscape
{
    /// <summary>
    /// Главное меню: название игры, кнопки «Играть» и «Выйти».
    /// Строится в рантайме в стиле первого уровня — тёмный фон, пергаментный текст,
    /// книжная полка снизу и синий акцент у главной кнопки.
    /// </summary>
    public class MainMenuGame : MonoBehaviour
    {
        private const string FirstLevelScene = "Level1";

        private RectTransform _playRect;
        private RectTransform _quitRect;
        private Image _playBackground;
        private Image _quitBackground;
        private Text _playLabel;
        private Text _quitLabel;
        private Text _title;
        private float _time;

        private void Start()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            BuildUi();
        }

        private void Update()
        {
            _time += Time.unscaledDeltaTime;

            Vector2 mousePosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            bool overPlay = Contains(_playRect, mousePosition);
            bool overQuit = Contains(_quitRect, mousePosition);

            if (_playBackground != null)
            {
                _playBackground.color = overPlay
                    ? new Color(0.30f, 0.68f, 0.98f, 1f)
                    : new Color(0.20f, 0.55f, 0.85f, 0.98f);
            }

            if (_quitBackground != null)
            {
                _quitBackground.color = overQuit
                    ? new Color(0.34f, 0.37f, 0.45f, 1f)
                    : new Color(0.24f, 0.26f, 0.33f, 0.98f);
            }

            if (_title != null)
            {
                Color color = _title.color;
                color.a = 0.92f + 0.08f * Mathf.Sin(_time * 1.6f);
                _title.color = color;
            }

            Keyboard keyboard = Keyboard.current;

            if (mousePosition != Vector2.zero && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (overPlay)
                {
                    Play();
                    return;
                }

                if (overQuit)
                {
                    Quit();
                    return;
                }
            }

            if (keyboard != null)
            {
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                {
                    Play();
                    return;
                }

                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    Quit();
                }
            }
        }

        private void Play()
        {
            if (Application.CanStreamedLevelBeLoaded(FirstLevelScene)) SceneManager.LoadScene(FirstLevelScene);
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void BuildUi()
        {
            Transform root = UiKit.CreateHudCanvas("Menu Canvas", transform);

            // Фон.
            CreateImage("Background", root, Vector2.zero, Vector2.zero, new Vector2(2400f, 1400f),
                new Color(0.055f, 0.060f, 0.078f, 1f), null);

            // Книжная полка внизу — отсылка к первому уровню.
            BuildBookshelf(root);

            // Название и подзаголовок.
            CreateText("Title Shadow", root, new Vector2(0.5f, 0.5f), new Vector2(5f, 195f),
                new Vector2(1500f, 190f), 132, TextAnchor.MiddleCenter, new Color(0.16f, 0.08f, 0.05f)).text =
                "RobberyTime";

            _title = CreateText("Title", root, new Vector2(0.5f, 0.5f), new Vector2(0f, 200f),
                new Vector2(1500f, 190f), 132, TextAnchor.MiddleCenter, new Color(1f, 0.93f, 0.78f));
            _title.text = "RobberyTime";

            CreateImage("Title Line", root, new Vector2(0.5f, 0.5f), new Vector2(0f, 108f),
                new Vector2(420f, 5f), new Color(0.72f, 0.58f, 0.28f, 0.95f), null);

            CreateText("Subtitle", root, new Vector2(0.5f, 0.5f), new Vector2(0f, 66f),
                new Vector2(1200f, 40f), 30, TextAnchor.MiddleCenter, new Color(0.70f, 0.74f, 0.82f)).text =
                "БИБЛИОТЕКА · ХРАНИЛИЩЕ";

            // Кнопки.
            Image play = CreateImage("Play", root, new Vector2(0.5f, 0.5f), new Vector2(0f, -90f),
                new Vector2(440f, 88f), new Color(0.20f, 0.55f, 0.85f, 0.98f), null);
            _playBackground = play;
            _playRect = (RectTransform)play.transform;
            _playLabel = CreateText("Play Label", play.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(440f, 88f), 40, TextAnchor.MiddleCenter, Color.white);
            _playLabel.text = "ИГРАТЬ";

            Image quit = CreateImage("Quit", root, new Vector2(0.5f, 0.5f), new Vector2(0f, -200f),
                new Vector2(440f, 76f), new Color(0.24f, 0.26f, 0.33f, 0.98f), null);
            _quitBackground = quit;
            _quitRect = (RectTransform)quit.transform;
            _quitLabel = CreateText("Quit Label", quit.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(440f, 76f), 32, TextAnchor.MiddleCenter, new Color(0.85f, 0.87f, 0.92f));
            _quitLabel.text = "ВЫЙТИ";
        }

        private static void BuildBookshelf(Transform root)
        {
            Color[] palette =
            {
                new Color(0.42f, 0.27f, 0.17f),
                new Color(0.44f, 0.18f, 0.17f),
                new Color(0.33f, 0.35f, 0.18f),
                new Color(0.30f, 0.22f, 0.36f),
                new Color(0.17f, 0.31f, 0.27f),
                new Color(0.55f, 0.47f, 0.22f),
                new Color(0.26f, 0.22f, 0.20f)
            };

            float[] heights = { 96f, 116f, 104f, 128f, 88f, 120f, 100f, 110f };

            // Полка.
            CreateImage("Shelf", root, new Vector2(0.5f, 0f), new Vector2(0f, 78f),
                new Vector2(1980f, 12f), new Color(0.28f, 0.19f, 0.12f), null);

            int index = 0;
            for (float x = -930f; x <= 930f; x += 46f)
            {
                float width = 30f + (index % 3) * 6f;
                float height = heights[index % heights.Length];
                Color color = palette[index % palette.Length];
                color.a = 0.85f;

                CreateImage("Book " + index, root, new Vector2(0.5f, 0f), new Vector2(x, 90f),
                    new Vector2(width, height), color, null);

                // Светлая полоска-корешок.
                CreateImage("Book Line " + index, root, new Vector2(0.5f, 0f), new Vector2(x, 90f + height * 0.72f),
                    new Vector2(width - 6f, 4f), new Color(1f, 0.95f, 0.80f, 0.20f), null);

                index++;
            }
        }

        private static bool Contains(RectTransform rect, Vector2 screenPoint)
        {
            return UiKit.Contains(rect, screenPoint);
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 pivot,
            Vector2 position, Vector2 size)
        {
            return UiKit.Rect(name, parent, anchor, pivot, position, size);
        }

        private static Text CreateText(string name, Transform parent, Vector2 anchor, Vector2 position,
            Vector2 size, int fontSize, TextAnchor alignment, Color color)
        {
            return UiKit.MakeText(name, parent, anchor, position, size, fontSize, alignment, color);
        }

        private static Image CreateImage(string name, Transform parent, Vector2 anchor, Vector2 position,
            Vector2 size, Color color, Sprite sprite)
        {
            return UiKit.MakeImage(name, parent, anchor, position, size, color, sprite);
        }
    }
}
