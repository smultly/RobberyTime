using System.Collections.Generic;
using UnityEngine;

namespace LibraryEscape
{
    /// <summary>Стол с деньгами в хранилище. Игрок зажимает E рядом, чтобы забрать стопку.</summary>
    public class MoneyTable2D : MonoBehaviour
    {
        public int stacks = 2;
        public float interactRadius = 2.4f;
        public Level2Game game;

        private readonly List<Transform> _stackVisuals = new List<Transform>();

        public bool HasMoney { get { return stacks > 0; } }
        public float InteractRadius { get { return interactRadius; } }

        public void Configure(Level2Game owner, int stackCount)
        {
            game = owner;
            stacks = Mathf.Max(1, stackCount);
            BuildVisual();
        }

        private void BuildVisual()
        {
            // Стол: тёмная столешница с более светлой поверхностью.
            PlaceholderArt.AddSprite("Table", transform, PlaceholderArt.Square,
                new Color(0.24f, 0.16f, 0.11f), Vector2.zero, new Vector2(2.4f, 1.0f), 5);
            PlaceholderArt.AddSprite("Top", transform, PlaceholderArt.Square,
                new Color(0.42f, 0.30f, 0.19f), Vector2.zero, new Vector2(2.2f, 0.82f), 6);
            PlaceholderArt.AddSprite("Edge", transform, PlaceholderArt.Square,
                new Color(0.50f, 0.36f, 0.22f), new Vector2(0f, 0.34f), new Vector2(2.2f, 0.10f), 7);

            // Стопки денег.
            for (int i = 0; i < stacks; i++)
            {
                float x = (i - (stacks - 1) * 0.5f) * 0.66f;
                SpriteRenderer stack = PlaceholderArt.AddSprite("Money " + (i + 1), transform, VaultArt.Money,
                    Color.white, new Vector2(x, 0.06f), new Vector2(0.62f, 0.46f), 8);
                _stackVisuals.Add(stack.transform);
            }

            // Коллайдер для игрока (камера смотрит поверх стола).
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(2.4f, 1.0f);
        }

        /// <summary>Забирает одну стопку. Возвращает false, если стол уже пуст.</summary>
        public bool CollectOne()
        {
            if (stacks <= 0) return false;

            stacks--;

            if (_stackVisuals.Count > 0)
            {
                Transform visual = _stackVisuals[_stackVisuals.Count - 1];
                _stackVisuals.RemoveAt(_stackVisuals.Count - 1);
                if (visual != null) Destroy(visual.gameObject);
            }

            return true;
        }
    }
}
