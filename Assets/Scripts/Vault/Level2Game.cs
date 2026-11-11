using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LibraryEscape
{
    /// <summary>
    /// Правила второго уровня: собрать 8 стопок денег, не попасться камерам
    /// и уйти через вентиляцию в правом углу. Есть пауза, жизни и экраны победы/поражения.
    /// </summary>
    public class Level2Game : LevelGameBase
    {
        private const float HoldSeconds = 1.1f;
        private const float RoundSeconds = 60f;

        private PlayerMover2D _player;
        private SecurityCamera2D[] _cameras;
        private MoneyTable2D[] _tables;
        private HidingZone2D[] _zones;
        private VaultVent2D _vent;

        private int _totalMoney;
        private int _collected;

        private bool _hidden;
        private float _invulnerableUntil;
        private float _timeLeft;

        private MoneyTable2D _holdTarget;
        private float _hold;

        private Text _timerText;
        private Text _moneyText;
        private Text _promptText;
        private GameObject _holdBar;
        private Image _holdFill;

        private AudioClip _collectClip;
        private AudioClip _alarmClip;

        protected override string DefeatHint { get { return "R — попробовать снова\nEsc — в главное меню"; } }

        public bool PlayerHidden { get { return _hidden; } }
        public bool Invulnerable { get { return Time.time < _invulnerableUntil; } }
        public bool AllCollected { get { return _collected >= _totalMoney; } }
        public int Collected { get { return _collected; } }
        public int TotalMoney { get { return _totalMoney; } }

        public Vector2 PlayerPosition
        {
            get { return _player != null ? (Vector2)_player.transform.position : Vector2.zero; }
        }

        public void Initialize(PlayerMover2D player, SecurityCamera2D[] cameras, MoneyTable2D[] tables,
            HidingZone2D[] zones, VaultVent2D vent, int startingLives)
        {
            _player = player;
            _cameras = cameras != null ? cameras : new SecurityCamera2D[0];
            _tables = tables != null ? tables : new MoneyTable2D[0];
            _zones = zones != null ? zones : new HidingZone2D[0];
            _vent = vent;

            InitializeBase(startingLives);
            _timeLeft = RoundSeconds;

            _totalMoney = 0;
            for (int i = 0; i < _tables.Length; i++)
            {
                if (_tables[i] != null) _totalMoney += _tables[i].stacks;
            }
            _collected = 0;

            if (_player != null) _player.game = this;

            for (int i = 0; i < _cameras.Length; i++)
            {
                if (_cameras[i] != null) _cameras[i].game = this;
            }

            if (_vent != null) _vent.game = this;

            BuildHud();
            BuildAudio();
            SetVentReady(false);
            UpdateHud();
            ShowToast("Собери 8 стопок денег и уходи в вентиляцию справа", 4f);
        }

        // ---------------------------------------------------------------- Update

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (_state != State.Running)
            {
                if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                {
                    Restart();
                    return;
                }

                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                {
                    LoadMenu();
                    return;
                }

                UpdateToast();
                UpdateFlash();
                return;
            }

            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                Restart();
                return;
            }

            if (_paused)
            {
                if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ResumeGame();
                return;
            }

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                PauseGame();
                return;
            }

            UpdateTimer();
            if (!IsRunning) return;

            UpdateHiding();
            UpdateDetection();
            UpdateCollection();
            UpdateHud();
            UpdateToast();
            UpdateFlash();
        }

        private void UpdateTimer()
        {
            _timeLeft -= Time.deltaTime;

            if (_timeLeft > 0f) return;

            _timeLeft = 0f;
            UpdateHud();
            Lose("Время вышло");
        }

        private void UpdateHiding()
        {
            _hidden = false;
            Vector2 position = PlayerPosition;

            for (int i = 0; i < _zones.Length; i++)
            {
                HidingZone2D zone = _zones[i];
                if (zone == null) continue;

                bool inside = zone.Contains(position);
                zone.SetActive(inside);
                if (inside) _hidden = true;
            }
        }

        /// <summary>Луч камеры попал в игрока — урон сразу, без накопления тревоги.</summary>
        private void UpdateDetection()
        {
            if (Invulnerable) return;

            for (int i = 0; i < _cameras.Length; i++)
            {
                if (_cameras[i] != null && _cameras[i].SeesPlayer)
                {
                    OnAlarm();
                    return;
                }
            }
        }

        private void OnAlarm()
        {
            if (!IsRunning) return;

            _invulnerableUntil = Time.time + 2.5f;
            ApplyDamage("Камера засекла!  −1 жизнь", 2.5f, _player);
            Play(_alarmClip);
        }

        private void UpdateCollection()
        {
            MoneyTable2D nearest = FindNearestTable();

            if (nearest == null)
            {
                _hold = 0f;
                _holdTarget = null;
                HideHoldBar();

                if (AllCollected)
                {
                    SetPrompt("Деньги собраны — вентиляция справа", new Color(0.62f, 1f, 0.72f));
                }
                else
                {
                    ClearPrompt();
                }

                return;
            }

            if (_holdTarget != nearest)
            {
                _holdTarget = nearest;
                _hold = 0f;
            }

            SetPrompt("Зажми E — забрать деньги  (осталось " + nearest.stacks + ")", Color.white);

            Keyboard keyboard = Keyboard.current;
            bool holding = keyboard != null && keyboard.eKey.isPressed;

            if (holding)
            {
                _hold += Time.deltaTime;
                ShowHoldBar(_hold / HoldSeconds);

                if (_hold >= HoldSeconds)
                {
                    _hold = 0f;
                    Collect(nearest);
                }
            }
            else
            {
                _hold = 0f;
                HideHoldBar();
            }
        }

        private MoneyTable2D FindNearestTable()
        {
            if (_player == null) return null;

            Vector2 position = PlayerPosition;
            MoneyTable2D best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < _tables.Length; i++)
            {
                MoneyTable2D table = _tables[i];
                if (table == null || !table.HasMoney) continue;

                float distance = Vector2.Distance(position, table.transform.position);
                if (distance > table.InteractRadius) continue;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = table;
                }
            }

            return best;
        }

        private void Collect(MoneyTable2D table)
        {
            if (table == null || !table.CollectOne()) return;

            _collected++;
            Play(_collectClip);
            ShowToast("Деньги: " + _collected + " / " + _totalMoney, 1.4f);

            if (AllCollected)
            {
                SetVentReady(true);
                ShowToast("Все деньги собраны! Вентиляция справа — уходи", 4.5f);
            }

            UpdateHud();
        }

        private void SetVentReady(bool ready)
        {
            if (_vent != null) _vent.SetReady(ready);
        }

        /// <summary>Вызывается триггером вентиляции при входе игрока.</summary>
        public void OnVentEntered()
        {
            if (!IsRunning) return;

            if (!AllCollected)
            {
                ShowToast("Сначала собери все деньги: " + _collected + " / " + _totalMoney, 2.4f);
                return;
            }

            Win();
        }

        private void Win()
        {
            if (_state != State.Running) return;

            ClearPause();
            _state = State.Victory;
            Play(_winClip);

            ShowEnd("ОГРАБЛЕНИЕ УДАЛОСЬ",
                "Деньги: " + _collected + " / " + _totalMoney + "\n\nR — сыграть снова\nEsc — в главное меню");
        }

        // ---------------------------------------------------------------- HUD

        private void BuildHud()
        {
            Transform root = BuildHudCanvas();

            _timerText = CreateText("Timer", root, new Vector2(0f, 1f), new Vector2(36f, -22f),
                new Vector2(900f, 60f), 42, TextAnchor.UpperLeft, new Color(1f, 0.94f, 0.78f));

            _moneyText = CreateText("Money", root, new Vector2(0f, 1f), new Vector2(36f, -80f),
                new Vector2(900f, 48f), 34, TextAnchor.UpperLeft, new Color(0.74f, 1f, 0.78f));

            BuildHearts(root, new Vector2(36f, -134f), 36f, 46f, new Color(0.95f, 0.26f, 0.33f));

            _promptText = CreateText("Prompt", root, new Vector2(0.5f, 0f), new Vector2(0f, 108f),
                new Vector2(1500f, 40f), 28, TextAnchor.LowerCenter, new Color(0.90f, 0.92f, 0.96f));

            // Полоска удержания E.
            RectTransform hold = CreateRect("Hold Bar", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 78f), new Vector2(360f, 16f));
            Image holdBackground = hold.gameObject.AddComponent<Image>();
            holdBackground.color = new Color(0.10f, 0.11f, 0.14f, 0.9f);
            holdBackground.raycastTarget = false;
            _holdBar = hold.gameObject;

            RectTransform holdFill = CreateRect("Fill", hold, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(0f, 16f));
            _holdFill = holdFill.gameObject.AddComponent<Image>();
            _holdFill.color = new Color(0.42f, 0.92f, 0.52f, 0.95f);
            _holdFill.raycastTarget = false;

            _holdBar.SetActive(false);

            CreateText("Hint", root, new Vector2(0.5f, 0f), new Vector2(0f, 22f),
                new Vector2(1500f, 36f), 24, TextAnchor.LowerCenter, new Color(0.72f, 0.75f, 0.82f)).text =
                "WASD / стрелки — движение      Зажми E — собрать деньги      Esc — пауза";

            BuildToast(root);
            BuildFlash(root);
            BuildEndOverlay(root);
            BuildPausePanel(root);
        }

        protected override void RefreshHud()
        {
            UpdateHud();
        }

        protected override void OnDefeat()
        {
            _hold = 0f;
            ClearPrompt();
            HideHoldBar();
        }

        private void UpdateHud()
        {
            if (_moneyText != null)
            {
                _moneyText.text = "ДЕНЬГИ  " + _collected + " / " + _totalMoney;
                _moneyText.color = AllCollected ? new Color(0.62f, 1f, 0.72f) : new Color(0.74f, 1f, 0.78f);
            }

            if (_timerText != null)
            {
                _timerText.text = "ВРЕМЯ  " + Mathf.CeilToInt(_timeLeft);
                _timerText.color = _timeLeft <= 10f
                    ? new Color(1f, 0.36f, 0.30f)
                    : new Color(1f, 0.94f, 0.78f);
            }

            UpdateHearts(new Color(0.95f, 0.26f, 0.33f), new Color(0.23f, 0.23f, 0.26f, 0.65f));
        }

        private void SetPrompt(string message, Color color)
        {
            if (_promptText == null) return;

            _promptText.text = message;
            color.a = 1f;
            _promptText.color = color;
        }

        private void ClearPrompt()
        {
            if (_promptText != null) _promptText.text = "";
        }

        private void ShowHoldBar(float progress)
        {
            if (_holdBar != null && !_holdBar.activeSelf) _holdBar.SetActive(true);
            if (_holdFill != null)
            {
                RectTransform rect = (RectTransform)_holdFill.transform;
                rect.sizeDelta = new Vector2(360f * Mathf.Clamp01(progress), 16f);
            }
        }

        private void HideHoldBar()
        {
            if (_holdBar != null && _holdBar.activeSelf) _holdBar.SetActive(false);
        }

        // ---------------------------------------------------------------- Звук

        private void BuildAudio()
        {
            BuildAudioBase();
            _collectClip = SoundKit.Tone("Collect", 880f, 0.10f, 0.30f);
            _alarmClip = SoundKit.Tone("Alarm", 240f, 0.35f, 0.35f);
        }

        // ---------------------------------------------------------------- UI-утилиты

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
