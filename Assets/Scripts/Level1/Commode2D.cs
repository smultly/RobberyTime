using UnityEngine;

namespace LibraryEscape
{
    /// <summary>
    /// Один комод. Нарисованные комоды двусторонние: у каждой модели есть лицевая
    /// (зона снизу) и тыльная (зона сверху) половины, поэтому одна модель — это два
    /// независимых комода. Взломать можно только стоя в зелёной зоне на своей стороне:
    /// яркая пульсирующая — ещё не взломан, приглушённая — уже открыт.
    /// </summary>
    public class Commode2D : MonoBehaviour
    {
        public int ordinal = 1;
        public int digit = 0;
        public bool hasPaper = true;
        [Tooltip("0 — лицевая половина (зона снизу), 1 — тыльная (зона сверху).")]
        public int side;
        public Level1Game game;

        /// <summary>Размер зелёной зоны на полу.</summary>
        public static readonly Vector2 ZoneSize = new Vector2(2.4f, 0.8f);
        /// <summary>Расстояние от центра комода до центра зелёной зоны.</summary>
        public const float ZoneDistance = 1.75f;

        private bool _locked = true;
        private SpriteRenderer _lockIcon;
        private SpriteRenderer _paper;
        private SpriteRenderer _zone;
        private SpriteRenderer _drawer;
        private Color _zoneLocked;
        private Color _zoneUnlocked;
        private Vector3 _paperBase;
        private float _revealTimer;
        private float _pulse;

        public bool IsLocked { get { return _locked; } }
        public bool IsBack { get { return side != 0; } }
        public float ZoneY { get { return IsBack ? ZoneDistance : -ZoneDistance; } }

        public void Configure(LevelArt art, int commodeOrdinal, int commodeDigit, bool commodeHasPaper,
            Level1Game owner, int commodeSide, bool withBody)
        {
            ordinal = commodeOrdinal;
            digit = commodeDigit;
            hasPaper = commodeHasPaper;
            game = owner;
            side = commodeSide;

            _zoneLocked = art.zoneLockedColor;
            _zoneUnlocked = art.zoneUnlockedColor;

            BuildVisual(art, withBody);
        }

        private void BuildVisual(LevelArt art, bool withBody)
        {
            if (withBody)
            {
                GameObject bodyGo = new GameObject("Body");
                bodyGo.transform.SetParent(transform, false);
                SpriteRenderer body = bodyGo.AddComponent<SpriteRenderer>();
                body.sprite = art.CommodeSprite;
                body.color = art.commode != null ? Color.white : art.commodeColor;
                body.sortingOrder = 6;
                PlaceholderArt.FitToSize(bodyGo.transform, body.sprite, new Vector2(2.6f, 2.45f));

                BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(2.52f, 2.2f);
                gameObject.AddComponent<Obstacle2D>();
            }

            // Выдвинутый ящик — появляется после взлома этой половины.
            _drawer = PlaceholderArt.AddSprite("Open Drawer", transform, PlaceholderArt.Square,
                new Color(0.80f, 0.62f, 0.42f, 1f),
                new Vector2(0f, IsBack ? 1.06f : -1.06f), new Vector2(2.05f, 0.30f), 7);
            _drawer.enabled = false;

            // Замок-огонёк на своей половине.
            GameObject lockGo = new GameObject("Lock");
            lockGo.transform.SetParent(transform, false);
            lockGo.transform.localPosition = new Vector3(1.0f, IsBack ? 0.62f : -0.62f, 0f);
            _lockIcon = lockGo.AddComponent<SpriteRenderer>();
            _lockIcon.sprite = PlaceholderArt.Disc;
            _lockIcon.color = art.lockColor;
            _lockIcon.sortingOrder = 9;
            PlaceholderArt.FitToSize(lockGo.transform, _lockIcon.sprite, new Vector2(0.30f, 0.30f));

            // Зона статуса на полу со своей стороны.
            _zone = PlaceholderArt.AddSprite("Status Zone", transform, PlaceholderArt.Square, _zoneLocked,
                new Vector2(0f, ZoneY), ZoneSize, 2);

            GameObject paperGo = new GameObject("Paper");
            paperGo.transform.SetParent(transform, false);
            paperGo.transform.localPosition = new Vector3(0f, IsBack ? 0.55f : -0.55f, 0f);
            _paper = paperGo.AddComponent<SpriteRenderer>();
            _paper.sprite = art.PaperSprite;
            _paper.color = art.paper != null ? Color.white : art.paperColor;
            _paper.sortingOrder = 11;
            PlaceholderArt.FitToSize(paperGo.transform, _paper.sprite, new Vector2(0.8f, 1.0f));
            _paperBase = paperGo.transform.localPosition;
            paperGo.SetActive(false);
        }

        /// <summary>Стоит ли игрок в зелёной зоне именно этого комода.</summary>
        public bool PlayerInZone(Vector2 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            float halfW = ZoneSize.x * 0.5f + 0.1f;
            float halfH = ZoneSize.y * 0.5f + 0.1f;
            return Mathf.Abs(local.x) <= halfW && Mathf.Abs(local.y - ZoneY) <= halfH;
        }

        /// <summary>Открывает комод, показывает листок и передаёт цифру в игру.</summary>
        public void Unlock()
        {
            if (!_locked) return;

            _locked = false;

            if (_lockIcon != null) _lockIcon.enabled = false;
            if (_zone != null) _zone.color = _zoneUnlocked;
            if (_drawer != null) _drawer.enabled = true;

            if (hasPaper)
            {
                if (_paper != null)
                {
                    _paper.gameObject.SetActive(true);
                    Color color = _paper.color;
                    color.a = 1f;
                    _paper.color = color;
                    _paper.transform.localPosition = _paperBase;
                    _revealTimer = 0.9f;
                }

                if (game != null) game.CollectPaperData(ordinal, digit);
            }
            else if (game != null)
            {
                game.ShowToast("Комод пуст — листка здесь нет", 2f);
            }
        }

        private void Update()
        {
            if (_locked && _zone != null)
            {
                _pulse += Time.deltaTime * 2.4f;
                Color color = _zoneLocked;
                color.a = _zoneLocked.a * (0.72f + 0.28f * Mathf.Sin(_pulse));
                _zone.color = color;
            }

            if (_revealTimer <= 0f) return;

            _revealTimer -= Time.deltaTime;

            if (_paper != null && _paper.gameObject.activeSelf)
            {
                float progress = 1f - Mathf.Clamp01(_revealTimer / 0.9f);
                float direction = IsBack ? 1f : -1f;
                _paper.transform.localPosition = _paperBase + new Vector3(0f, direction * 0.8f * progress, 0f);

                Color color = _paper.color;
                color.a = Mathf.Clamp01(_revealTimer / 0.45f);
                _paper.color = color;

                if (_revealTimer <= 0f) _paper.gameObject.SetActive(false);
            }
        }
    }
}
