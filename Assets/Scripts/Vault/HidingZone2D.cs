using UnityEngine;

namespace LibraryEscape
{
    /// <summary>
    /// Зона укрытия: пока игрок стоит внутри, камеры его не видят.
    /// На полу — тёмный коврик с бирюзовой рамкой, внутри он подсвечивается.
    /// </summary>
    public class HidingZone2D : MonoBehaviour
    {
        public Vector2 size = new Vector2(2.6f, 2.2f);

        private SpriteRenderer _fill;
        private SpriteRenderer[] _edges;
        private Color _fillCalm;
        private Color _fillActive;
        private Color _edgeCalm;
        private Color _edgeActive;
        private bool _active;

        public void Configure(Vector2 worldSize)
        {
            size = worldSize;
            BuildVisual();
        }

        private void BuildVisual()
        {
            _fillCalm = new Color(0.16f, 0.42f, 0.46f, 0.30f);
            _fillActive = new Color(0.26f, 0.72f, 0.78f, 0.42f);
            _edgeCalm = new Color(0.35f, 0.78f, 0.86f, 0.55f);
            _edgeActive = new Color(0.55f, 0.95f, 1f, 0.95f);

            _fill = PlaceholderArt.AddSprite("Hiding Fill", transform, PlaceholderArt.Square, _fillCalm,
                Vector2.zero, size, 3);

            float halfW = size.x * 0.5f;
            float halfH = size.y * 0.5f;
            const float edge = 0.09f;

            _edges = new SpriteRenderer[4];
            _edges[0] = PlaceholderArt.AddSprite("Edge Top", transform, PlaceholderArt.Square, _edgeCalm,
                new Vector2(0f, halfH - edge * 0.5f), new Vector2(size.x, edge), 4);
            _edges[1] = PlaceholderArt.AddSprite("Edge Bottom", transform, PlaceholderArt.Square, _edgeCalm,
                new Vector2(0f, -halfH + edge * 0.5f), new Vector2(size.x, edge), 4);
            _edges[2] = PlaceholderArt.AddSprite("Edge Left", transform, PlaceholderArt.Square, _edgeCalm,
                new Vector2(-halfW + edge * 0.5f, 0f), new Vector2(edge, size.y), 4);
            _edges[3] = PlaceholderArt.AddSprite("Edge Right", transform, PlaceholderArt.Square, _edgeCalm,
                new Vector2(halfW - edge * 0.5f, 0f), new Vector2(edge, size.y), 4);
        }

        public bool Contains(Vector2 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            return Mathf.Abs(local.x) <= size.x * 0.5f && Mathf.Abs(local.y) <= size.y * 0.5f;
        }

        /// <summary>Подсвечивает зону, пока в ней стоит игрок.</summary>
        public void SetActive(bool active)
        {
            if (_active == active) return;
            _active = active;

            if (_fill != null) _fill.color = active ? _fillActive : _fillCalm;
            if (_edges == null) return;

            for (int i = 0; i < _edges.Length; i++)
            {
                if (_edges[i] != null) _edges[i].color = active ? _edgeActive : _edgeCalm;
            }
        }
    }
}
