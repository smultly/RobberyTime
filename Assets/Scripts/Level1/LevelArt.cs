using UnityEngine;

namespace LibraryEscape
{
    /// <summary>
    /// Набор спрайтов и цветов для уровня.
    /// Создаётся как ассет (Tools > Library Escape > Create Level Art Asset).
    /// Пустые слоты заменяются процедурными заглушками, поэтому игра работает сразу.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelArt", menuName = "Library Escape/Level Art")]
    public class LevelArt : ScriptableObject
    {
        [Header("Комната")]
        [Tooltip("Тайл пола (40×40 px, PPU 20) — рисуется в тайловом режиме.")]
        public Sprite floor;
        [Tooltip("План комнаты до ввода кода (500×1000 px).")]
        public Sprite roomClosed;
        [Tooltip("План комнаты после ввода кода: шкаф отодвинут, дверь видна.")]
        public Sprite roomOpened;

        [Header("Спрайты (пусто = заглушка)")]
        public Sprite wall;
        public Sprite shelf;
        public Sprite table;
        public Sprite player;
        public Sprite[] playerWalk;
        public Sprite guard;
        public Sprite[] guardWalk;
        public Sprite paper;
        public Sprite door;
        public Sprite heart;
        public Sprite shield;
        public Sprite commode;

        [Header("Цвета заглушек")]
        public Color floorColor = new Color(0.21f, 0.22f, 0.27f);
        public Color floorLineColor = new Color(1f, 1f, 1f, 0.07f);
        public Color wallColor = new Color(0.43f, 0.35f, 0.28f);
        public Color shelfColor = new Color(0.56f, 0.41f, 0.27f);
        public Color tableColor = new Color(0.45f, 0.33f, 0.22f);
        public Color playerColor = new Color(0.31f, 0.76f, 0.97f);
        public Color guardColor = new Color(0.91f, 0.28f, 0.26f);
        public Color paperColor = new Color(1f, 0.98f, 0.90f);
        public Color doorColor = new Color(0.58f, 0.28f, 0.70f);
        public Color heartColor = new Color(0.95f, 0.26f, 0.33f);
        public Color shieldColor = new Color(0.40f, 0.86f, 1f, 0.75f);
        public Color commodeColor = new Color(0.50f, 0.34f, 0.20f);
        public Color commodeOpenColor = new Color(0.63f, 0.46f, 0.29f);
        public Color lockColor = new Color(0.94f, 0.77f, 0.30f);
        public Color backgroundColor = new Color(0.06f, 0.07f, 0.09f);
        public Color visionColor = new Color(1f, 0.82f, 0.20f, 0.26f);
        public Color alertColor = new Color(1f, 0.22f, 0.12f, 0.48f);

        [Header("Зона перед комодом")]
        [Tooltip("Ярко-зелёная зона: комод ещё не взломан.")]
        public Color zoneLockedColor = new Color(0.25f, 1f, 0.40f, 0.38f);
        [Tooltip("Приглушённая зона: комод уже взломан.")]
        public Color zoneUnlockedColor = new Color(0.38f, 0.55f, 0.42f, 0.14f);

        public Sprite FloorSprite { get { return floor != null ? floor : PlaceholderArt.Square; } }
        public Sprite WallSprite { get { return wall != null ? wall : PlaceholderArt.Square; } }
        public Sprite ShelfSprite { get { return shelf != null ? shelf : PlaceholderArt.Square; } }
        public Sprite TableSprite { get { return table != null ? table : PlaceholderArt.Square; } }
        public Sprite PlayerSprite { get { return player != null ? player : PlaceholderArt.Disc; } }
        public Sprite GuardSprite { get { return guard != null ? guard : PlaceholderArt.Disc; } }
        public Sprite PaperSprite { get { return paper != null ? paper : PlaceholderArt.Paper; } }
        public Sprite DoorSprite { get { return door != null ? door : PlaceholderArt.Square; } }
        public Sprite HeartSprite { get { return heart != null ? heart : PlaceholderArt.Heart; } }
        public Sprite ShieldSprite { get { return shield != null ? shield : PlaceholderArt.Shield; } }
        public Sprite CommodeSprite { get { return commode != null ? commode : PlaceholderArt.Square; } }

        /// <summary>Кадры ходьбы игрока. Если массив пуст — используется один статичный спрайт.</summary>
        public Sprite[] PlayerWalkFrames
        {
            get
            {
                if (playerWalk != null && playerWalk.Length > 0) return playerWalk;
                return new[] { PlayerSprite };
            }
        }

        /// <summary>Кадры ходьбы охранника. Если массив пуст — используется один статичный спрайт.</summary>
        public Sprite[] GuardWalkFrames
        {
            get
            {
                if (guardWalk != null && guardWalk.Length > 0) return guardWalk;
                return new[] { GuardSprite };
            }
        }
    }
}
