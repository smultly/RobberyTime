using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LibraryEscape
{
    /// <summary>Правила уровня 1: таймер, жизни, сбор листков, код и финиш.</summary>
    public class Level1Game : LevelGameBase
    {
        private LevelArt _art;
        private PlayerMover2D _player;
        private GuardPatrol2D _guard;
        private Commode2D[] _commodes;
        private SecretDoor _door;

        private int _totalPapers = 5;
        private int _papersFound;
        private readonly int[] _digits = new int[5];
        private readonly bool[] _paperCollected = new bool[5];

        private float _roundSeconds = 60f;
        private float _timeLeft = 60f;

        private Text _timerText;
        private Text _livesLabel;
        private Text _codeText;
        private Text _hintText;

        private Text _promptText;
        private Commode2D _pickTarget;

        private bool _lockpickOpen;
        private GameObject _lockpickPanel;
        private RectTransform _lockpickZoneRect;
        private RectTransform _lockpickNeedleRect;
        private Image _lockpickTrackImage;
        private Text _lockpickStatusText;
        private Text _lockpickProgressText;
        private float _needlePhase;
        private float _needleSpeed;
        private float _zoneCenter;
        private float _zoneHalfWidth;
        private int _minigameHits;
        private int _minigameMisses;
        private float _minigameFlash;

        private const float LockpickBarWidth = 720f;
        private const int LockpickHitsRequired = 3;
        private const int LockpickMissesAllowed = 3;

        private bool _codePanelOpen;
        private GameObject _codePanel;
        private Text _codePanelHint;
        private Text _codePanelStatus;
        private readonly Text[] _slotTexts = new Text[5];
        private readonly RectTransform[] _slotRects = new RectTransform[5];
        private readonly Image[] _slotImages = new Image[5];
        private readonly int[] _entered = new int[5];
        private int _inputCount;
        private readonly RectTransform[] _keyRects = new RectTransform[10];
        private RectTransform _clearRect;
        private RectTransform _confirmRect;
        private RectTransform _closeRect;

        private static readonly Key[] CodeDigitKeys =
        {
            Key.Digit1, Key.Numpad1, Key.Digit2, Key.Numpad2, Key.Digit3, Key.Numpad3,
            Key.Digit4, Key.Numpad4, Key.Digit5, Key.Numpad5, Key.Digit6, Key.Numpad6,
            Key.Digit7, Key.Numpad7, Key.Digit8, Key.Numpad8, Key.Digit9, Key.Numpad9,
            Key.Digit0, Key.Numpad0
        };

        private static readonly int[] CodeDigitValues =
        {
            1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 0, 0
        };

        private AudioClip _clickClip;
        private AudioClip _missClip;

        public override bool MovementBlocked { get { return _codePanelOpen || _lockpickOpen; } }
        public bool CodePanelOpen { get { return _codePanelOpen; } }
        public bool LockpickOpen { get { return _lockpickOpen; } }
        public bool CodeComplete { get { return _totalPapers > 0 && _papersFound >= _totalPapers; } }
        public float TimeLeft { get { return _timeLeft; } }
        public int PapersFound { get { return _papersFound; } }

        public string Code
        {
            get
            {
                if (!CodeComplete) return string.Empty;
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < _totalPapers; i++)
                {
                    if (i > 0) builder.Append(' ');
                    builder.Append(_digits[i]);
                }
                return builder.ToString();
            }
        }

        public void Initialize(LevelArt art, PlayerMover2D player, GuardPatrol2D guard, Commode2D[] commodes,
            SecretDoor door, int startingLives, float roundSeconds)
        {
            _art = art != null ? art : ScriptableObject.CreateInstance<LevelArt>();
            _player = player;
            _guard = guard;
            _commodes = commodes != null ? commodes : new Commode2D[0];
            _door = door;
            InitializeBase(startingLives);
            _roundSeconds = Mathf.Max(5f, roundSeconds);
            _timeLeft = _roundSeconds;

            _totalPapers = 0;
            for (int i = 0; i < _commodes.Length; i++)
            {
                if (_commodes[i] != null && _commodes[i].hasPaper) _totalPapers++;
            }
            _totalPapers = Mathf.Min(_totalPapers, _digits.Length);

            for (int i = 0; i < _digits.Length; i++)
            {
                _digits[i] = -1;
                _paperCollected[i] = false;
            }

            if (_player != null) _player.game = this;
            if (_guard != null) _guard.game = this;
            if (_door != null) _door.game = this;
            for (int i = 0; i < _commodes.Length; i++)
            {
                if (_commodes[i] != null) _commodes[i].game = this;
            }

            BuildHud();
            BuildAudio();
            UpdateHud();
            ShowToast("Взломай 5 комодов из 8 (E): зелёная зона — комод ещё не открыт", 4f);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                Restart();
                return;
            }

            // Пауза: Esc ставит игру на паузу и снимает её.
            if (_paused)
            {
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ResumeGame();
                return;
            }

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame
                && _state == State.Running && !_codePanelOpen && !_lockpickOpen)
            {
                PauseGame();
                return;
            }

            if (_codePanelOpen)
            {
                HideLockpick();
                UpdateCodePanelInput();
                UpdateToast();
                UpdateFlash();
                return;
            }

            if (_state == State.Running)
            {
                // Во время взлома игра стоит на паузе: таймер не идёт.
                if (!_lockpickOpen)
                {
                    _timeLeft -= Time.deltaTime;
                    if (_timeLeft <= 0f)
                    {
                        _timeLeft = 0f;
                        UpdateHud();
                        Lose("Время вышло");
                    }
                    else
                    {
                        UpdateHud();
                    }
                }
            }
            else if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
            {
                if (_state == State.Victory) LoadLevel2();
                else Restart();
                return;
            }

            if (_state == State.Running)
            {
                if (_lockpickOpen) UpdateMinigame();
                else UpdateLockpick();
            }
            else
            {
                HideLockpick();
                CloseLockpick(false);
            }

            UpdateToast();
            UpdateFlash();
        }

        public void CollectPaper(PaperPickup paper)
        {
            if (!IsRunning || paper == null || paper.Collected) return;

            paper.Collect();
            CollectPaperData(paper.ordinal, paper.digit);
        }

        /// <summary>Забирает листок с номером и цифрой (используется комодами и лежащими листками).</summary>
        public void CollectPaperData(int ordinal, int digit)
        {
            if (!IsRunning) return;

            int index = Mathf.Clamp(ordinal - 1, 0, _digits.Length - 1);
            if (_paperCollected[index]) return;

            _paperCollected[index] = true;
            _digits[index] = digit;
            _papersFound++;

            if (CodeComplete)
            {
                ShowToast("Код собран! Беги к двери в секретную комнату", 4f);
            }
            else
            {
                ShowToast("Лист №" + ordinal + ": цифра " + digit, 2.2f);
            }

            UpdateHud();
        }

        private void UpdateLockpick()
        {
            Commode2D nearest = FindNearestLockedCommode(true);

            if (nearest == null)
            {
                _pickTarget = null;
                HideLockpick();

                // Игрок стоит у комода, но не в зелёной зоне — подсказываем.
                if (FindNearestLockedCommode(false) != null)
                {
                    SetPrompt("Встань в зелёную зону у комода, чтобы взломать",
                        new Color(0.72f, 0.86f, 0.72f));
                }

                return;
            }

            _pickTarget = nearest;
            SetPrompt("Нажми  E  — взломать замок отмычкой", new Color(0.88f, 0.90f, 0.96f));

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                OpenLockpick(nearest);
            }
        }

        /// <summary>Ближайший невзломанный комод. requireZone — только если игрок стоит в его зелёной зоне.</summary>
        private Commode2D FindNearestLockedCommode(bool requireZone)
        {
            if (_player == null) return null;

            Vector2 playerPosition = _player.transform.position;
            Commode2D best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < _commodes.Length; i++)
            {
                Commode2D commode = _commodes[i];
                if (commode == null || !commode.IsLocked) continue;

                float distance = Vector2.Distance(playerPosition, commode.transform.position);
                if (distance > 3.6f) continue;
                if (requireZone && !commode.PlayerInZone(playerPosition)) continue;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = commode;
                }
            }

            return best;
        }

        private void SetPrompt(string message, Color color)
        {
            if (_promptText == null) return;

            _promptText.text = message;
            color.a = 1f;
            _promptText.color = color;
        }

        private void HideLockpick()
        {
            if (_promptText != null) _promptText.text = "";
        }

        private void OpenLockpick(Commode2D target)
        {
            if (target == null || !target.IsLocked || _lockpickPanel == null) return;
            if (_lockpickOpen || _codePanelOpen) return;

            _pickTarget = target;
            _lockpickOpen = true;
            _minigameHits = 0;
            _minigameMisses = 0;
            _minigameFlash = 0f;
            _needlePhase = 0f;
            _needleSpeed = 0.65f;
            _zoneHalfWidth = 0.12f;

            if (_lockpickTrackImage != null)
            {
                _lockpickTrackImage.color = new Color(0.10f, 0.12f, 0.17f, 0.98f);
            }

            ChooseLockpickZone();
            RefreshLockpickVisuals();

            if (_lockpickStatusText != null)
            {
                _lockpickStatusText.text = "Нажми E, когда бегунок в зелёной зоне";
                _lockpickStatusText.color = new Color(0.80f, 0.86f, 0.95f);
            }

            _lockpickPanel.SetActive(true);
            HideLockpick();
        }

        /// <summary>Закрывает мини-игру взлома. keepTarget = false сбрасывает выбранный комод.</summary>
        private void CloseLockpick(bool keepTarget)
        {
            if (!_lockpickOpen) return;

            _lockpickOpen = false;
            _minigameFlash = 0f;
            if (_lockpickTrackImage != null)
            {
                _lockpickTrackImage.color = new Color(0.10f, 0.12f, 0.17f, 0.98f);
            }
            if (!keepTarget) _pickTarget = null;
            if (_lockpickPanel != null) _lockpickPanel.SetActive(false);
            HideLockpick();
        }

        /// <summary>Двигает бегунок и обрабатывает нажатие E / Space.</summary>
        private void UpdateMinigame()
        {
            _needlePhase += _needleSpeed * Time.deltaTime;
            float needle = Mathf.PingPong(_needlePhase, 1f);

            if (_lockpickNeedleRect != null)
            {
                _lockpickNeedleRect.anchoredPosition = new Vector2(needle * LockpickBarWidth, 0f);
            }

            if (_minigameFlash > 0f)
            {
                _minigameFlash = Mathf.Max(0f, _minigameFlash - Time.deltaTime * 4f);
                if (_lockpickTrackImage != null)
                {
                    Color calm = new Color(0.10f, 0.12f, 0.17f, 0.98f);
                    Color miss = new Color(0.72f, 0.16f, 0.16f, 1f);
                    _lockpickTrackImage.color = Color.Lerp(calm, miss, _minigameFlash);
                }
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                CloseLockpick(false);
                return;
            }

            if (!keyboard.eKey.wasPressedThisFrame && !keyboard.spaceKey.wasPressedThisFrame) return;

            // Нажатие засчитывается только если бегунок внутри зелёной зоны.
            if (Mathf.Abs(needle - _zoneCenter) <= _zoneHalfWidth) OnLockpickHit();
            else OnLockpickMiss();
        }

        private void OnLockpickHit()
        {
            Play(_clickClip);
            _minigameHits++;

            if (_minigameHits >= LockpickHitsRequired)
            {
                Commode2D target = _pickTarget;
                CloseLockpick(true);
                if (target != null) target.Unlock();
                return;
            }

            _needleSpeed += 0.15f;
            _zoneHalfWidth = Mathf.Max(0.07f, _zoneHalfWidth - 0.02f);
            _needlePhase = 0f;

            ChooseLockpickZone();
            RefreshLockpickVisuals();

            if (_lockpickStatusText != null)
            {
                _lockpickStatusText.text = "Есть! Продолжай…";
                _lockpickStatusText.color = new Color(0.55f, 0.95f, 0.62f);
            }
        }

        private void OnLockpickMiss()
        {
            Play(_missClip);
            _minigameMisses++;
            _minigameFlash = 1f;

            if (_minigameMisses >= LockpickMissesAllowed)
            {
                CloseLockpick(true);
                ApplyDamage("Отмычка сломалась!  −1 жизнь", 0f, _player);
                return;
            }

            if (_lockpickStatusText != null)
            {
                _lockpickStatusText.text = "Промах! Осталось попыток: " + (LockpickMissesAllowed - _minigameMisses);
                _lockpickStatusText.color = new Color(0.98f, 0.50f, 0.45f);
            }

            RefreshLockpickVisuals();
        }

        private void ChooseLockpickZone()
        {
            float margin = _zoneHalfWidth + 0.03f;
            _zoneCenter = Random.Range(margin, 1f - margin);
        }

        private void RefreshLockpickVisuals()
        {
            if (_lockpickZoneRect != null)
            {
                _lockpickZoneRect.sizeDelta = new Vector2(_zoneHalfWidth * 2f * LockpickBarWidth, 44f);
                _lockpickZoneRect.anchoredPosition = new Vector2(_zoneCenter * LockpickBarWidth, 0f);
            }

            if (_lockpickProgressText != null)
            {
                string hits = "";
                for (int i = 0; i < LockpickHitsRequired; i++)
                {
                    hits += i < _minigameHits ? "● " : "○ ";
                }

                _lockpickProgressText.text = "ВЗЛОМ  " + hits + "    ПРОМАХИ  " +
                    _minigameMisses + " / " + LockpickMissesAllowed;
            }
        }

        public void OnPlayerSpotted()
        {
            if (!IsRunning || _player == null || _player.IsInvulnerable) return;

            if (_lockpickOpen) CloseLockpick(false);

            ApplyDamage("Вас заметил охранник!  −1 жизнь, щит на 2 секунды", 2.2f, _player);
        }

        protected override void RefreshHud()
        {
            UpdateHud();
        }

        protected override void OnDefeat()
        {
            CloseCodePanel();
            if (_lockpickOpen) CloseLockpick(false);
        }

        public void Win()
        {
            if (_state != State.Running) return;

            ClearPause();
            _state = State.Victory;
            Play(_winClip);
            ShowEnd("УРОВЕНЬ 1 ПРОЙДЕН", "Код: " + Code + "\n\nEnter — уровень 2\nR — сыграть снова");
        }

        /// <summary>Открывает меню ввода кода у двери. Игра при этом ставится на паузу.</summary>
        public void OpenCodePanel()
        {
            if (_codePanel == null || !IsRunning || !CodeComplete) return;
            if (_codePanelOpen) return;

            _codePanelOpen = true;
            _codePanel.SetActive(true);

            _inputCount = 0;
            for (int i = 0; i < _entered.Length; i++) _entered[i] = 0;
            RefreshCodeSlots();

            if (_codePanelHint != null) _codePanelHint.text = "Собранный код:  " + Code;
            if (_codePanelStatus != null)
            {
                _codePanelStatus.text = "";
                _codePanelStatus.color = new Color(0.95f, 0.45f, 0.40f);
            }

            UpdateHud();
        }

        public void CloseCodePanel()
        {
            if (!_codePanelOpen) return;

            _codePanelOpen = false;
            if (_codePanel != null) _codePanel.SetActive(false);
        }

        private void UpdateCodePanelInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                if (TryHandleCodePanelPoint(mouse.position.ReadValue())) return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                CloseCodePanel();
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                SubmitCode();
                return;
            }

            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                BackspaceCode();
                return;
            }

            for (int i = 0; i < CodeDigitKeys.Length; i++)
            {
                if (keyboard[CodeDigitKeys[i]].wasPressedThisFrame)
                {
                    AddCodeDigit(CodeDigitValues[i]);
                    return;
                }
            }
        }

        /// <summary>Обрабатывает клик по панели кода. Возвращает true, если клик попал в элемент панели.</summary>
        private bool TryHandleCodePanelPoint(Vector2 point)
        {
            for (int i = 0; i < _keyRects.Length; i++)
            {
                if (Contains(_keyRects[i], point))
                {
                    AddCodeDigit(i);
                    return true;
                }
            }

            if (Contains(_clearRect, point))
            {
                BackspaceCode();
                return true;
            }

            if (Contains(_confirmRect, point))
            {
                SubmitCode();
                return true;
            }

            if (Contains(_closeRect, point))
            {
                CloseCodePanel();
                return true;
            }

            return false;
        }

        /// <summary>Добавляет одну цифру в вводимый код (не больше пяти).</summary>
        private void AddCodeDigit(int digit)
        {
            if (!_codePanelOpen || _inputCount >= _entered.Length) return;

            _entered[_inputCount] = digit;
            _inputCount++;

            if (_codePanelStatus != null) _codePanelStatus.text = "";
            RefreshCodeSlots();
        }

        private void BackspaceCode()
        {
            if (!_codePanelOpen || _inputCount <= 0) return;

            _inputCount--;
            _entered[_inputCount] = 0;

            if (_codePanelStatus != null) _codePanelStatus.text = "";
            RefreshCodeSlots();
        }

        /// <summary>Обновляет окошки кода: введённые цифры и точки-заглушки.</summary>
        private void RefreshCodeSlots()
        {
            for (int i = 0; i < _entered.Length; i++)
            {
                bool filled = i < _inputCount;

                if (_slotTexts[i] != null)
                {
                    _slotTexts[i].text = filled ? _entered[i].ToString() : "·";
                    _slotTexts[i].color = filled ? Color.white : new Color(0.55f, 0.62f, 0.74f);
                }

                if (_slotImages[i] != null)
                {
                    _slotImages[i].color = filled
                        ? new Color(0.22f, 0.42f, 0.62f, 0.98f)
                        : new Color(0.17f, 0.21f, 0.30f, 0.98f);
                }
            }
        }

        private void SubmitCode()
        {
            if (!_codePanelOpen) return;

            if (_inputCount < _entered.Length)
            {
                if (_codePanelStatus != null)
                {
                    _codePanelStatus.text = "Введите все 5 цифр кода";
                    _codePanelStatus.color = new Color(0.95f, 0.72f, 0.36f);
                }
                return;
            }

            bool correct = true;
            for (int i = 0; i < _digits.Length; i++)
            {
                if (_entered[i] != _digits[i])
                {
                    correct = false;
                    break;
                }
            }

            if (correct)
            {
                CloseCodePanel();
                if (_door != null) _door.OpenDoor();
                else Win();
                return;
            }

            _inputCount = 0;
            for (int i = 0; i < _entered.Length; i++) _entered[i] = 0;
            RefreshCodeSlots();

            if (_codePanelStatus != null)
            {
                _codePanelStatus.text = "Неверный код!  −1 жизнь";
                _codePanelStatus.color = new Color(0.98f, 0.42f, 0.36f);
            }

            ApplyDamage("Неверный код!  −1 жизнь", 0f, _player);
        }

        private static bool Contains(RectTransform rect, Vector2 screenPoint)
        {
            return rect != null && rect.gameObject.activeInHierarchy &&
                   RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, null);
        }

        /// <summary>Переход на второй уровень (если он добавлен в Build Settings).</summary>
        private void LoadLevel2()
        {
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded("Level2")) SceneManager.LoadScene("Level2");
            else Restart();
        }

        private void UpdateHud()
        {
            if (_timerText != null)
            {
                _timerText.text = "ВРЕМЯ  " + Mathf.CeilToInt(_timeLeft);
                _timerText.color = _timeLeft <= 10f
                    ? new Color(1f, 0.36f, 0.30f)
                    : new Color(1f, 0.94f, 0.78f);
            }

            UpdateHearts(_art != null ? _art.heartColor : Color.red, new Color(0.23f, 0.23f, 0.26f, 0.65f));

            if (_codeText != null)
            {
                _codeText.text = CodeComplete
                    ? "КОД:  " + Code + "\nЛИСТКИ: " + _papersFound + " / " + _totalPapers
                    : "КОД:  • • • • •\nЛИСТКИ: " + _papersFound + " / " + _totalPapers;
            }
        }

        private void BuildHud()
        {
            Transform root = BuildHudCanvas();

            _timerText = CreateText("Timer", root, new Vector2(0f, 1f), new Vector2(36f, -24f),
                new Vector2(560f, 64f), 46, TextAnchor.UpperLeft, new Color(1f, 0.94f, 0.78f));

            BuildHearts(root, new Vector2(36f, -96f), 38f, 46f, _art.heartColor);

            _livesLabel = CreateText("Lives", root, new Vector2(0f, 1f), new Vector2(36f, -138f),
                new Vector2(300f, 32f), 22, TextAnchor.UpperLeft, new Color(0.75f, 0.78f, 0.85f));
            _livesLabel.text = "ЖИЗНИ";

            _codeText = CreateText("Code", root, new Vector2(1f, 1f), new Vector2(-36f, -24f),
                new Vector2(760f, 110f), 40, TextAnchor.UpperRight, new Color(0.86f, 0.94f, 1f));

            _hintText = CreateText("Hint", root, new Vector2(0.5f, 0f), new Vector2(0f, 22f),
                new Vector2(1400f, 36f), 24, TextAnchor.LowerCenter, new Color(0.72f, 0.75f, 0.82f));
            _hintText.text = "WASD / стрелки — движение      E — взломать комод      R — рестарт";

            _promptText = CreateText("Prompt", root, new Vector2(0.5f, 0f), new Vector2(0f, 108f),
                new Vector2(1500f, 40f), 28, TextAnchor.LowerCenter, new Color(0.88f, 0.90f, 0.96f));

            BuildToast(root);
            BuildFlash(root);

            BuildLockpickPanel(root);
            BuildCodePanel(root);
            BuildEndOverlay(root);
            BuildPausePanel(root);
        }

        private void BuildLockpickPanel(Transform root)
        {
            RectTransform overlayRect = CreateRect("Lockpick Panel", root, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image dim = overlayRect.gameObject.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.03f, 0.05f, 0.55f);
            dim.raycastTarget = false;

            RectTransform panel = CreateRect("Panel", overlayRect, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(840f, 430f));
            Image panelBackground = panel.gameObject.AddComponent<Image>();
            panelBackground.color = new Color(0.10f, 0.12f, 0.17f, 0.98f);
            panelBackground.raycastTarget = false;

            CreateText("Title", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f),
                new Vector2(780f, 60f), 48, TextAnchor.MiddleCenter, new Color(0.90f, 0.94f, 1f)).text =
                "ВЗЛОМ КОМОДА";

            CreateText("Subtitle", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, 100f),
                new Vector2(780f, 34f), 24, TextAnchor.MiddleCenter, new Color(0.72f, 0.79f, 0.90f)).text =
                "Нажми E, когда бегунок в зелёной зоне — 3 раза";

            RectTransform trackRect = CreateRect("Track", panel, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 35f), new Vector2(LockpickBarWidth, 48f));
            _lockpickTrackImage = trackRect.gameObject.AddComponent<Image>();
            _lockpickTrackImage.color = new Color(0.10f, 0.12f, 0.17f, 0.98f);
            _lockpickTrackImage.raycastTarget = false;

            _lockpickZoneRect = CreateRect("Zone", trackRect, new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 44f));
            Image zone = _lockpickZoneRect.gameObject.AddComponent<Image>();
            zone.color = new Color(0.32f, 0.84f, 0.42f, 0.65f);
            zone.raycastTarget = false;

            _lockpickNeedleRect = CreateRect("Needle", trackRect, new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 52f));
            Image needle = _lockpickNeedleRect.gameObject.AddComponent<Image>();
            needle.color = new Color(1f, 0.93f, 0.45f, 1f);
            needle.raycastTarget = false;

            _lockpickProgressText = CreateText("Progress", panel, new Vector2(0.5f, 0.5f),
                new Vector2(0f, -45f), new Vector2(780f, 34f), 26, TextAnchor.MiddleCenter,
                new Color(0.85f, 0.89f, 0.96f));

            _lockpickStatusText = CreateText("Status", panel, new Vector2(0.5f, 0.5f),
                new Vector2(0f, -95f), new Vector2(780f, 40f), 28, TextAnchor.MiddleCenter,
                new Color(0.80f, 0.86f, 0.95f));

            CreateText("Tip", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, -160f),
                new Vector2(780f, 30f), 22, TextAnchor.MiddleCenter, new Color(0.62f, 0.68f, 0.78f)).text =
                "Время остановлено        Esc — отойти от комода";

            _lockpickPanel = overlayRect.gameObject;
            _lockpickPanel.SetActive(false);
        }

        private void BuildCodePanel(Transform root)
        {
            RectTransform overlayRect = CreateRect("Code Panel", root, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image dim = overlayRect.gameObject.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.03f, 0.05f, 0.72f);
            dim.raycastTarget = false;

            RectTransform panel = CreateRect("Panel", overlayRect, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 860f));
            Image panelBackground = panel.gameObject.AddComponent<Image>();
            panelBackground.color = new Color(0.10f, 0.12f, 0.17f, 0.98f);
            panelBackground.raycastTarget = false;

            CreateText("Title", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, 380f),
                new Vector2(680f, 60f), 46, TextAnchor.MiddleCenter, new Color(0.90f, 0.94f, 1f)).text = "ВВЕДИТЕ КОД";

            _codePanelHint = CreateText("Hint", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, 320f),
                new Vector2(680f, 36f), 26, TextAnchor.MiddleCenter, new Color(0.78f, 0.86f, 0.96f));

            // Окошки ввода: по одному на каждую цифру кода.
            for (int i = 0; i < _slotRects.Length; i++)
            {
                float x = (i - 2) * 104f;
                Image slot = CreateImage("Slot " + (i + 1), panel, new Vector2(0.5f, 0.5f),
                    new Vector2(x, 225f), new Vector2(88f, 108f), new Color(0.17f, 0.21f, 0.30f, 0.98f), null);
                _slotImages[i] = slot;
                _slotRects[i] = (RectTransform)slot.transform;

                Text digit = CreateText("Digit", slot.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(88f, 108f), 56, TextAnchor.MiddleCenter, Color.white);
                digit.text = "·";
                _slotTexts[i] = digit;
            }

            // Клавиатура 3×4: 1–9, стереть, 0, подтвердить.
            for (int k = 0; k < 12; k++)
            {
                int col = k % 3;
                int row = k / 3;
                float x = (col - 1) * 124f;
                float y = 90f - row * 112f;

                bool isClear = k == 9;
                bool isConfirm = k == 11;
                int digit = k == 10 ? 0 : k + 1;

                Color background = isClear
                    ? new Color(0.40f, 0.27f, 0.27f, 0.98f)
                    : isConfirm
                        ? new Color(0.20f, 0.55f, 0.85f, 0.98f)
                        : new Color(0.20f, 0.24f, 0.32f, 0.98f);

                Image key = CreateImage("Key " + (k + 1), panel, new Vector2(0.5f, 0.5f),
                    new Vector2(x, y), new Vector2(108f, 84f), background, null);

                string label = isClear ? "<" : isConfirm ? "OK" : digit.ToString();
                CreateText("Label", key.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(108f, 84f), isConfirm ? 34 : 42, TextAnchor.MiddleCenter, Color.white).text = label;

                if (isClear) _clearRect = (RectTransform)key.transform;
                else if (isConfirm) _confirmRect = (RectTransform)key.transform;
                else _keyRects[digit] = (RectTransform)key.transform;
            }

            CreateText("Tip", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, -320f),
                new Vector2(680f, 32f), 22, TextAnchor.MiddleCenter, new Color(0.62f, 0.68f, 0.78f)).text =
                "Нажимайте цифры на панели        Esc — закрыть";

            Image close = CreateImage("Close", panel, new Vector2(0.5f, 0.5f),
                new Vector2(0f, -385f), new Vector2(240f, 56f), new Color(0.24f, 0.26f, 0.33f, 0.98f), null);
            _closeRect = (RectTransform)close.transform;
            CreateText("Close Label", close.transform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(240f, 56f), 26, TextAnchor.MiddleCenter, new Color(0.85f, 0.87f, 0.92f)).text = "ЗАКРЫТЬ";

            _codePanelStatus = CreateText("Status", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, -278f),
                new Vector2(680f, 36f), 26, TextAnchor.MiddleCenter, new Color(0.98f, 0.42f, 0.36f));

            _codePanel = overlayRect.gameObject;
            _codePanel.SetActive(false);
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

        private void BuildAudio()
        {
            BuildAudioBase();
            _clickClip = SoundKit.Tone("Lock Click", 950f, 0.05f, 0.22f);
            _missClip = SoundKit.Tone("Lock Miss", 170f, 0.14f, 0.30f);
        }
    }
}
