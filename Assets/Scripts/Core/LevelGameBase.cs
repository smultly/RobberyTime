using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LibraryEscape
{
    /// <summary>
    /// База для игровых уровней: жизни, урон, пауза, тосты, вспышки, экран конца,
    /// перезапуск, выход в меню и общий набор звуков. Конкретные уровни наследуются
    /// и добавляют свои правила (код и комоды на первом, деньги и камеры на втором).
    /// </summary>
    public abstract class LevelGameBase : MonoBehaviour, ILevelFlow
    {
        protected enum State
        {
            Running,
            Victory,
            Defeat
        }

        protected State _state = State.Running;

        protected int _startingLives = 3;
        protected int _lives = 3;
        protected bool _paused;

        protected Text _toastText;
        protected float _toastTimer;
        protected float _toastDuration = 1f;

        protected Image _flash;
        protected float _flashTimer;

        protected GameObject _overlay;
        protected Text _overlayTitle;
        protected Text _overlaySubtitle;

        protected GameObject _pausePanel;
        protected Image[] _hearts;

        protected AudioSource _audio;
        protected AudioClip _hurtClip;
        protected AudioClip _winClip;
        protected AudioClip _loseClip;

        public bool IsRunning { get { return _state == State.Running; } }
        public bool IsPaused { get { return _paused; } }
        public virtual bool MovementBlocked { get { return false; } }
        public int Lives { get { return _lives; } }

        /// <summary>Текст под заголовком «ПОРАЖЕНИЕ» — уровни могут дописать свои подсказки.</summary>
        protected virtual string DefeatHint { get { return "R — попробовать снова"; } }

        // ------------------------------------------------------------ старт

        protected void InitializeBase(int startingLives)
        {
            _startingLives = Mathf.Max(1, startingLives);
            _lives = _startingLives;
            _state = State.Running;
            _paused = false;
            Time.timeScale = 1f;
        }

        // ------------------------------------------------------------ HUD

        protected Transform BuildHudCanvas()
        {
            return UiKit.CreateHudCanvas("HUD", transform);
        }

        protected void BuildHearts(Transform root, Vector2 start, float size, float spacing, Color color)
        {
            _hearts = new Image[_startingLives];
            for (int i = 0; i < _hearts.Length; i++)
            {
                _hearts[i] = UiKit.MakeImage("Heart " + (i + 1), root, new Vector2(0f, 1f),
                    new Vector2(start.x + i * spacing, start.y), new Vector2(size, size), color,
                    PlaceholderArt.Heart);
            }
        }

        protected void UpdateHearts(Color alive, Color lost)
        {
            if (_hearts == null) return;

            for (int i = 0; i < _hearts.Length; i++)
            {
                if (_hearts[i] == null) continue;
                _hearts[i].color = i < _lives ? alive : lost;
            }
        }

        protected void BuildToast(Transform root)
        {
            _toastText = UiKit.MakeText("Toast", root, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f),
                new Vector2(1500f, 80f), 42, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.55f, 0f));
        }

        protected void BuildFlash(Transform root)
        {
            _flash = UiKit.MakeImage("Hit Flash", root, Vector2.zero, Vector2.zero, Vector2.zero,
                new Color(0.85f, 0.05f, 0.05f, 0f), null);
            RectTransform rect = (RectTransform)_flash.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        protected void BuildPausePanel(Transform root)
        {
            RectTransform overlay = UiKit.FullScreenLayer("Pause Panel", root, new Color(0.02f, 0.02f, 0.03f, 0.72f));

            UiKit.MakeText("Title", overlay, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f),
                new Vector2(1600f, 120f), 78, TextAnchor.MiddleCenter, Color.white).text = "ПАУЗА";

            UiKit.MakeText("Hint", overlay, new Vector2(0.5f, 0.5f), new Vector2(0f, -80f),
                new Vector2(1600f, 140f), 34, TextAnchor.MiddleCenter, new Color(0.85f, 0.88f, 0.95f)).text =
                "Esc — продолжить\nR — начать заново";

            _pausePanel = overlay.gameObject;
            _pausePanel.SetActive(false);
        }

        protected void BuildEndOverlay(Transform root)
        {
            RectTransform overlay = UiKit.FullScreenLayer("End Overlay", root, new Color(0.02f, 0.02f, 0.03f, 0.80f));

            _overlayTitle = UiKit.MakeText("Title", overlay, new Vector2(0.5f, 0.5f), new Vector2(0f, 80f),
                new Vector2(1600f, 120f), 78, TextAnchor.MiddleCenter, Color.white);
            _overlaySubtitle = UiKit.MakeText("Subtitle", overlay, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f),
                new Vector2(1600f, 220f), 34, TextAnchor.MiddleCenter, new Color(0.85f, 0.88f, 0.95f));

            _overlay = overlay.gameObject;
            _overlay.SetActive(false);
        }

        protected void ShowEnd(string title, string subtitle)
        {
            if (_overlay != null) _overlay.SetActive(true);
            if (_overlayTitle != null) _overlayTitle.text = title;
            if (_overlaySubtitle != null) _overlaySubtitle.text = subtitle;
        }

        public void ShowToast(string message, float duration = 2.5f)
        {
            if (_toastText == null) return;

            _toastText.text = message;
            _toastTimer = Mathf.Max(0.4f, duration);
            _toastText.color = new Color(1f, 0.88f, 0.55f, 1f);
        }

        protected void UpdateToast()
        {
            if (_toastText == null || _toastTimer <= 0f) return;

            _toastTimer -= Time.deltaTime;
            float alpha = Mathf.Clamp01(_toastTimer / 0.5f);

            Color color = _toastText.color;
            color.a = alpha;
            _toastText.color = color;
        }

        protected void UpdateFlash()
        {
            if (_flash == null || _flashTimer <= 0f) return;

            _flashTimer -= Time.deltaTime;
            Color color = _flash.color;
            color.a = Mathf.Clamp01(_flashTimer / 0.55f) * 0.45f;
            _flash.color = color;
        }

        // ------------------------------------------------------------ урон и поражение

        /// <summary>Обновление HUD конкретного уровня (переопределяется наследниками).</summary>
        protected virtual void RefreshHud()
        {
        }

        /// <summary>Отнимает жизнь. Возвращает false, если жизнь была последней.</summary>
        protected bool ApplyDamage(string toast, float invulnerability, PlayerMover2D player)
        {
            if (!IsRunning) return false;

            _lives--;
            _flashTimer = 0.45f;
            Play(_hurtClip);

            if (_lives <= 0)
            {
                _lives = 0;
                RefreshHud();
                Lose("Жизни закончились");
                return false;
            }

            if (invulnerability > 0f && player != null) player.SetInvulnerable(invulnerability);

            ShowToast(toast, 2.4f);
            RefreshHud();
            return true;
        }

        protected void Lose(string reason)
        {
            if (_state != State.Running) return;

            OnDefeat();
            ClearPause();
            _state = State.Defeat;
            Play(_loseClip);
            ShowEnd("ПОРАЖЕНИЕ", reason + "\n\n" + DefeatHint);
        }

        /// <summary>Хук для уровня: закрыть открытые панели и т.п. перед поражением.</summary>
        protected virtual void OnDefeat()
        {
        }

        // ------------------------------------------------------------ пауза

        public void PauseGame()
        {
            if (_paused || _state != State.Running) return;

            _paused = true;
            Time.timeScale = 0f;
            if (_pausePanel != null) _pausePanel.SetActive(true);
        }

        public void ResumeGame()
        {
            if (!_paused) return;
            ClearPause();
        }

        protected void ClearPause()
        {
            _paused = false;
            Time.timeScale = 1f;
            if (_pausePanel != null) _pausePanel.SetActive(false);
        }

        // ------------------------------------------------------------ сцены

        protected void Restart()
        {
            Time.timeScale = 1f;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else SceneManager.LoadScene(scene.name);
        }

        protected void LoadMenu()
        {
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded("MainMenu")) SceneManager.LoadScene("MainMenu");
            else Restart();
        }

        // ------------------------------------------------------------ звук

        protected void BuildAudioBase()
        {
            _audio = gameObject.GetComponent<AudioSource>();
            if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;

            _hurtClip = SoundKit.Tone("Hurt", 180f, 0.30f, 0.45f);
            _winClip = SoundKit.Tone("Win", 660f, 0.45f, 0.40f);
            _loseClip = SoundKit.Tone("Lose", 120f, 0.60f, 0.45f);

            if (FindAnyObjectByType<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }
        }

        protected void Play(AudioClip clip)
        {
            if (_audio != null && clip != null) _audio.PlayOneShot(clip);
        }

        protected virtual void OnDestroy()
        {
            if (Time.timeScale == 0f) Time.timeScale = 1f;
        }
    }
}
