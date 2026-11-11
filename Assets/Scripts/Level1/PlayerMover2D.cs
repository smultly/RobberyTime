using UnityEngine;
using UnityEngine.InputSystem;

namespace LibraryEscape
{
    /// <summary>Движение игрока: WASD или стрелки, вид сверху.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMover2D : MonoBehaviour
    {
        public float moveSpeed = 5f;
        [System.NonSerialized] public ILevelFlow game;
        public Transform facingPivot;
        public SpriteRenderer shieldRenderer;
        public SpriteFrameAnimator animator;

        private Rigidbody2D _body;
        private Vector2 _input;
        private float _invulnerableUntil;
        private bool _shieldCaptured;
        private Vector3 _shieldBaseScale = Vector3.one;
        private float _shieldBaseAlpha = 0.75f;

        public bool IsInvulnerable { get { return Time.time < _invulnerableUntil; } }
        public Vector2 Facing { get; private set; } = Vector2.down;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.gravityScale = 0f;
            _body.freezeRotation = true;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            ApplyFacingRotation();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Vector2 input = Vector2.zero;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            }

            _input = input.sqrMagnitude > 1f ? input.normalized : input;

            bool canMove = game == null ||
                           (game.IsRunning && !game.IsPaused && !game.MovementBlocked);

            if (canMove && _input.sqrMagnitude > 0.01f)
            {
                Facing = _input.normalized;
                ApplyFacingRotation();
            }

            if (animator != null) animator.playing = canMove && _input.sqrMagnitude > 0.01f;

            UpdateShield();
        }

        /// <summary>Поворачивает спрайт по направлению движения. Спрайт игрока нарисован лицом вверх.</summary>
        private void ApplyFacingRotation()
        {
            if (facingPivot == null) return;
            float angle = Mathf.Atan2(Facing.y, Facing.x) * Mathf.Rad2Deg - 90f;
            facingPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void FixedUpdate()
        {
            bool canMove = game == null ||
                           (game.IsRunning && !game.IsPaused && !game.MovementBlocked);
            _body.linearVelocity = canMove ? _input * moveSpeed : Vector2.zero;
        }

        public void Teleport(Vector2 position)
        {
            _body.linearVelocity = Vector2.zero;
            _body.position = position;
            transform.position = new Vector3(position.x, position.y, 0f);
            _body.WakeUp();
            Physics2D.SyncTransforms();
        }

        public void SetInvulnerable(float seconds)
        {
            _invulnerableUntil = Time.time + seconds;
        }

        private void UpdateShield()
        {
            if (shieldRenderer == null) return;

            if (!_shieldCaptured)
            {
                _shieldBaseScale = shieldRenderer.transform.localScale;
                _shieldBaseAlpha = shieldRenderer.color.a;
                _shieldCaptured = true;
                shieldRenderer.enabled = false;
            }

            bool active = IsInvulnerable;
            if (shieldRenderer.enabled != active)
            {
                shieldRenderer.enabled = active;
                shieldRenderer.transform.localScale = _shieldBaseScale;
            }

            if (!active) return;

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 13f);
            Color color = shieldRenderer.color;
            color.a = _shieldBaseAlpha * Mathf.Lerp(0.55f, 1f, pulse);
            shieldRenderer.color = color;

            float scale = 1f + 0.05f * Mathf.Sin(Time.time * 9f);
            shieldRenderer.transform.localScale = _shieldBaseScale * scale;
        }
    }
}
