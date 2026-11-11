using UnityEngine;

namespace LibraryEscape
{
    /// <summary>
    /// Камера наблюдения: широкий конус зрения, который медленно качается влево-вправо.
    /// Замечает игрока, если он не спрятался в зоне укрытия. Лучи конуса упираются в стены.
    /// </summary>
    public class SecurityCamera2D : MonoBehaviour
    {
        [Header("Обзор")]
        public float viewDistance = 9f;
        public float viewAngle = 105f;
        [Range(4, 96)] public int coneSegments = 28;

        [Header("Оглядывание")]
        public float sweepAmplitude = 35f;
        public float sweepPeriod = 8f;
        public float sweepPhase = 0f;
        public Vector2 baseFacing = Vector2.down;
        [Tooltip("Непрерывное вращение на 360° вместо качания.")]
        public bool continuousRotation;
        [Tooltip("Скорость вращения, градусов в секунду.")]
        public float rotateSpeed = 20f;

        [Header("Цвета")]
        public Color calmColor = new Color(0.98f, 0.80f, 0.32f, 0.13f);
        public Color alertColor = new Color(1f, 0.24f, 0.18f, 0.42f);

        public Level2Game game;

        private Transform _body;
        private Transform _cone;
        private MeshRenderer _coneRenderer;
        private Mesh _coneMesh;
        private Vector3[] _coneVertices;
        private int _segmentCount;
        private Material _coneMaterial;
        private bool _alert;
        private Vector2 _facing = Vector2.down;
        private float _time;
        private float _bodyOffset;

        public bool SeesPlayer { get; private set; }

        public void Configure(Level2Game owner, Vector2 baseDirection, float amplitude, float period, float phase,
            float distance, float angle, bool withBody = true, bool continuous = false, float speed = 0f,
            float bodyOffset = 0f)
        {
            game = owner;
            baseFacing = baseDirection.sqrMagnitude > 0.0001f ? baseDirection.normalized : Vector2.down;
            sweepAmplitude = amplitude;
            sweepPeriod = Mathf.Max(1f, period);
            sweepPhase = phase;
            viewDistance = distance;
            viewAngle = angle;
            continuousRotation = continuous;
            if (speed > 0f) rotateSpeed = speed;

            BuildVisual(withBody, bodyOffset);
        }

        private void BuildVisual(bool withBody, float bodyOffset)
        {
            if (withBody)
            {
                GameObject bodyGo = new GameObject("Body");
                _body = bodyGo.transform;
                _body.SetParent(transform, false);
                // Корпус выносится вперёд (позиция обновляется в UpdateVisuals),
                // чтобы камера сидела на грани колонны и поворачивалась вместе с лучом.
                _bodyOffset = bodyOffset;

                SpriteRenderer bodyRenderer = bodyGo.AddComponent<SpriteRenderer>();
                bodyRenderer.sprite = VaultArt.CameraBody;
                bodyRenderer.color = Color.white;
                bodyRenderer.sortingOrder = 8;
                PlaceholderArt.FitToSize(_body, bodyRenderer.sprite, new Vector2(1.0f, 0.6f));
            }

            GameObject coneGo = new GameObject("Vision Cone");
            _cone = coneGo.transform;
            _cone.SetParent(transform, false);

            _segmentCount = Mathf.Clamp(coneSegments, 4, 96);
            MeshFilter filter = coneGo.AddComponent<MeshFilter>();
            _coneMesh = PlaceholderArt.CreateConeMesh(viewDistance, viewAngle, _segmentCount);
            _coneVertices = new Vector3[_segmentCount + 2];
            filter.sharedMesh = _coneMesh;

            _coneMaterial = PlaceholderArt.CreateVisionMaterial(calmColor);
            _coneRenderer = coneGo.AddComponent<MeshRenderer>();
            _coneRenderer.sharedMaterial = _coneMaterial;
            _coneRenderer.sortingOrder = 2;
        }

        private void Update()
        {
            if (game == null) return;

            if (!game.IsRunning || game.IsPaused)
            {
                SeesPlayer = false;
                SetAlert(false);
                return;
            }

            _time += Time.deltaTime;

            float offset = continuousRotation
                ? _time * rotateSpeed + sweepPhase
                : Mathf.Sin(_time / sweepPeriod * Mathf.PI * 2f + sweepPhase) * sweepAmplitude;

            _facing = Quaternion.Euler(0f, 0f, offset) * (Vector3)baseFacing;
            if (_facing.sqrMagnitude > 0.0001f) _facing.Normalize();

            UpdateVisuals();
            UpdateConeMesh();
            ScanForPlayer();
        }

        private void UpdateVisuals()
        {
            float angle = Mathf.Atan2(_facing.y, _facing.x) * Mathf.Rad2Deg;

            if (_cone != null) _cone.localRotation = Quaternion.Euler(0f, 0f, angle);
            // Спрайт камеры нарисован объективом вниз.
            if (_body != null)
            {
                _body.localPosition = new Vector3(_facing.x * _bodyOffset, _facing.y * _bodyOffset, 0f);
                _body.localRotation = Quaternion.Euler(0f, 0f, angle + 90f);
            }
        }

        private void UpdateConeMesh()
        {
            if (_coneMesh == null || _coneVertices == null) return;

            int segments = _segmentCount;
            float half = viewAngle * 0.5f * Mathf.Deg2Rad;
            float baseAngle = Mathf.Atan2(_facing.y, _facing.x) * Mathf.Rad2Deg;
            Vector2 origin = transform.position;

            _coneVertices[0] = Vector3.zero;

            for (int i = 0; i <= segments; i++)
            {
                float angle = -half + 2f * half * i / segments;
                Vector2 direction = Quaternion.Euler(0f, 0f, baseAngle + angle * Mathf.Rad2Deg) * Vector3.right;
                float distance = ClearDistance(origin, direction, viewDistance);
                _coneVertices[i + 1] = new Vector3(Mathf.Cos(angle) * distance, Mathf.Sin(angle) * distance, 0f);
            }

            _coneMesh.vertices = _coneVertices;
            _coneMesh.RecalculateBounds();
        }

        private float ClearDistance(Vector2 origin, Vector2 direction, float maxDistance)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, maxDistance);
            float nearest = maxDistance;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i].collider;
                if (hit == null) continue;
                if (hit.GetComponent<Obstacle2D>() == null) continue;
                if (hits[i].distance < nearest) nearest = hits[i].distance;
            }

            return Mathf.Max(0.05f, nearest - 0.05f);
        }

        private void ScanForPlayer()
        {
            SeesPlayer = false;

            if (game.PlayerHidden || game.Invulnerable)
            {
                SetAlert(false);
                return;
            }

            Vector2 origin = transform.position;
            Vector2 playerPosition = game.PlayerPosition;
            Vector2 toPlayer = playerPosition - origin;
            float distance = toPlayer.magnitude;

            if (distance > viewDistance) return;
            if (distance > 0.8f && Vector2.Angle(_facing, toPlayer) > viewAngle * 0.5f) return;
            if (IsBlocked(origin, playerPosition)) return;

            SeesPlayer = true;
            SetAlert(true);
        }

        private bool IsBlocked(Vector2 from, Vector2 to)
        {
            RaycastHit2D[] hits = Physics2D.LinecastAll(from, to);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i].collider;
                if (hit == null) continue;
                if (hit.GetComponent<Obstacle2D>() != null) return true;
            }
            return false;
        }

        private void SetAlert(bool alert)
        {
            if (_alert == alert) return;
            _alert = alert;
            if (_coneMaterial != null) _coneMaterial.color = alert ? alertColor : calmColor;
        }

        private void OnDestroy()
        {
            if (_coneMaterial != null) Destroy(_coneMaterial);
        }
    }
}
