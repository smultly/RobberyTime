using UnityEngine;

namespace LibraryEscape
{
    /// <summary>
    /// Вентиляция в углу хранилища. Когда все деньги собраны, вход в зону у вентиляции
    /// завершает уровень победой.
    /// </summary>
    public class VaultVent2D : MonoBehaviour
    {
        public Level2Game game;

        private SpriteRenderer _grate;
        private SpriteRenderer _glow;
        private bool _ready;

        public void Configure(Level2Game owner, SpriteRenderer grate)
        {
            game = owner;
            _grate = grate;

            _glow = PlaceholderArt.AddSprite("Vent Glow", transform, PlaceholderArt.Disc,
                new Color(0.35f, 1f, 0.65f, 0f), Vector2.zero, new Vector2(2.6f, 2.6f), 3);
        }

        public void SetReady(bool ready)
        {
            _ready = ready;

            if (_grate != null)
            {
                _grate.color = ready ? new Color(0.75f, 1f, 0.85f, 1f) : Color.white;
            }

            if (_glow != null && !ready)
            {
                Color color = _glow.color;
                color.a = 0f;
                _glow.color = color;
            }
        }

        private void Update()
        {
            if (!_ready || _glow == null) return;

            Color color = _glow.color;
            color.a = 0.18f + 0.14f * Mathf.Sin(Time.time * 3.4f);
            _glow.color = color;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (game == null) return;
            if (other.GetComponent<PlayerMover2D>() == null) return;
            game.OnVentEntered();
        }
    }
}
