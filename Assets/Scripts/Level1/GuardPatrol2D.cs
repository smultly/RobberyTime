using UnityEngine;

namespace LibraryEscape
{
    /// <summary>
    /// Охранник: ходит по маршруту (если он задан) и замечает игрока в конусе зрения.
    /// Если маршрута нет — стоит на месте, оглядывается и охраняет комнату.
    /// </summary>
    public class GuardPatrol2D : MonoBehaviour
    {
        [Header("Патруль")]
        public float moveSpeed = 2.925f;
        public float waitAtWaypoint = 0.45f;
        public float viewDistance = 7f;
        public float viewAngle = 80f;
        [Range(4, 64)] public int coneSegments = 28;
        [Tooltip("Направление взгляда, если маршрута нет.")]
        public Vector2 startFacing = Vector2.down;

        [Header("Оглядывание")]
        [Tooltip("Минимальный интервал между оглядываниями, секунды.")]
        public float lookIntervalMin = 3f;
        [Tooltip("Максимальный интервал между оглядываниями, секунды.")]
        public float lookIntervalMax = 6f;
        [Tooltip("Длительность одного оглядывания, секунды.")]
        public float lookDuration = 1.3f;
        [Tooltip("На сколько градусов охранник поворачивает голову в каждую сторону.")]
        public float lookSweepAngle = 48f;

        public Level1Game game;

        private Transform _player;
        private Vector2[] _route;
        private int _routeIndex;
        private float _waitTimer;
        private Vector2 _facing = Vector2.down;
        private Vector2 _moveFacing = Vector2.down;
        private float _lookOffset;
        private bool _looking;
        private float _lookTimer;
        private float _lookCooldown;
        private Transform _body;
        private Transform _cone;
        private MeshRenderer _coneRenderer;
        private Mesh _coneMesh;
        private Vector3[] _coneVertices;
        private int _coneSegmentCount;
        private Material _coneMaterial;
        private Color _calmColor;
        private Color _alertColor;
        private bool _alert;
        private SpriteFrameAnimator _animator;
        private bool _walking;

        public bool SeesPlayer { get; private set; }
        public bool HasRoute { get { return _route != null && _route.Length > 0; } }

        public void Configure(LevelArt art, Vector2[] route, Transform playerTarget, Level1Game owner)
        {
            _player = playerTarget;
            game = owner;
            _route = route;
            _calmColor = art.visionColor;
            _alertColor = art.alertColor;

            BuildVisual(art);

            if (HasRoute)
            {
                transform.position = new Vector3(_route[0].x, _route[0].y, 0f);
                _routeIndex = _route.Length > 1 ? 1 : 0;
                Vector2 direction = _route[_routeIndex] - _route[0];
                _moveFacing = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.down;
            }
            else
            {
                _routeIndex = 0;
                _moveFacing = startFacing.sqrMagnitude > 0.0001f ? startFacing.normalized : Vector2.down;
            }

            _facing = _moveFacing;
            ResetLookAround();
            UpdateVisuals();
        }

        private void BuildVisual(LevelArt art)
        {
            GameObject bodyGo = new GameObject("Body");
            _body = bodyGo.transform;
            _body.SetParent(transform, false);

            SpriteRenderer bodyRenderer = bodyGo.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = art.GuardSprite;
            bodyRenderer.color = art.guard != null ? Color.white : art.guardColor;
            bodyRenderer.sortingOrder = 9;
            PlaceholderArt.FitToSize(_body, bodyRenderer.sprite, new Vector2(1.9f, 1.9f));

            _animator = bodyGo.AddComponent<SpriteFrameAnimator>();
            _animator.target = bodyRenderer;
            _animator.SetFrames(art.GuardWalkFrames, 8f);
            _animator.playing = false;

            GameObject coneGo = new GameObject("Vision Cone");
            _cone = coneGo.transform;
            _cone.SetParent(transform, false);

            MeshFilter filter = coneGo.AddComponent<MeshFilter>();
            _coneSegmentCount = Mathf.Clamp(coneSegments, 3, 128);
            _coneMesh = PlaceholderArt.CreateConeMesh(viewDistance, viewAngle, _coneSegmentCount);
            _coneVertices = new Vector3[_coneSegmentCount + 2];
            filter.sharedMesh = _coneMesh;

            _coneMaterial = PlaceholderArt.CreateVisionMaterial(_calmColor);
            _coneRenderer = coneGo.AddComponent<MeshRenderer>();
            _coneRenderer.sharedMaterial = _coneMaterial;
            _coneRenderer.sortingOrder = 2;
        }

        private void Update()
        {
            bool paused = game != null && (!game.IsRunning || game.IsPaused || game.CodePanelOpen || game.LockpickOpen);
            if (paused)
            {
                if (_animator != null) _animator.playing = false;
                return;
            }

            UpdateLookAround();

            bool walking = false;

            if (HasRoute)
            {
                Vector2 position = transform.position;
                Vector2 target = _route[_routeIndex];

                if (_waitTimer > 0f)
                {
                    _waitTimer -= Time.deltaTime;
                    if (_waitTimer <= 0f)
                    {
                        _routeIndex = (_routeIndex + 1) % _route.Length;
                    }
                }
                else
                {
                    Vector2 direction = target - position;
                    if (direction.sqrMagnitude > 0.0001f) _moveFacing = direction.normalized;

                    Vector2 next = Vector2.MoveTowards(position, target, moveSpeed * Time.deltaTime);
                    transform.position = new Vector3(next.x, next.y, 0f);
                    walking = true;

                    if ((next - target).sqrMagnitude < 0.0016f)
                    {
                        _waitTimer = waitAtWaypoint;
                    }
                }
            }

            _walking = walking;
            if (_animator != null) _animator.playing = walking;

            ApplyLookOffset();
            UpdateVisuals();
            UpdateConeMesh();
            ScanForPlayer();
        }

        /// <summary>Перестраивает видимый конус: лучи упираются в стены и препятствия.</summary>
        private void UpdateConeMesh()
        {
            if (_coneMesh == null || _coneVertices == null) return;

            int segments = _coneSegmentCount;
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

        /// <summary>Ближайшее препятствие вдоль луча, но не дальше максимальной дистанции.</summary>
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

        /// <summary>Периодически запускает оглядывание: голова плавно поворачивается влево-вправо.</summary>
        private void UpdateLookAround()
        {
            if (_looking)
            {
                _lookTimer -= Time.deltaTime;
                float progress = 1f - Mathf.Clamp01(_lookTimer / Mathf.Max(0.01f, lookDuration));
                _lookOffset = Mathf.Sin(progress * Mathf.PI * 2f) * lookSweepAngle;

                if (_lookTimer <= 0f)
                {
                    _looking = false;
                    _lookOffset = 0f;
                    _lookCooldown = Random.Range(lookIntervalMin, lookIntervalMax);
                }
            }
            else
            {
                _lookCooldown -= Time.deltaTime;
                if (_lookCooldown <= 0f)
                {
                    _looking = true;
                    _lookTimer = lookDuration;
                }
            }
        }

        /// <summary>Применяет текущий угол оглядывания к направлению взгляда.</summary>
        private void ApplyLookOffset()
        {
            if (Mathf.Abs(_lookOffset) < 0.01f)
            {
                _facing = _moveFacing;
                return;
            }

            _facing = Quaternion.Euler(0f, 0f, _lookOffset) * (Vector3)_moveFacing;
            if (_facing.sqrMagnitude > 0.0001f) _facing.Normalize();
        }

        private void ResetLookAround()
        {
            _looking = false;
            _lookTimer = 0f;
            _lookOffset = 0f;
            _lookCooldown = Random.Range(lookIntervalMin, lookIntervalMax);
        }

        private void UpdateVisuals()
        {
            float angle = Mathf.Atan2(_facing.y, _facing.x) * Mathf.Rad2Deg;

            if (_cone != null) _cone.localRotation = Quaternion.Euler(0f, 0f, angle);

            // На ходу спрайт смотрит по направлению движения, а конус продолжает
            // оглядываться влево-вправо. Стоя на месте охранник поворачивается всем телом.
            // Спрайт нарисован лицом вниз, поэтому к углу направления добавляем +90°.
            Vector2 bodyFacing = _walking ? _moveFacing : _facing;
            float bodyAngle = Mathf.Atan2(bodyFacing.y, bodyFacing.x) * Mathf.Rad2Deg;
            if (_body != null) _body.localRotation = Quaternion.Euler(0f, 0f, bodyAngle + 90f);
        }

        private void ScanForPlayer()
        {
            SeesPlayer = false;
            if (_player == null) return;

            Vector2 origin = transform.position;
            Vector2 toPlayer = (Vector2)_player.position - origin;
            float distance = toPlayer.magnitude;

            if (distance > viewDistance + 0.3f)
            {
                SetAlert(false);
                return;
            }

            float angle = Vector2.Angle(_facing, toPlayer);
            if (distance > 1.2f && angle > viewAngle * 0.5f)
            {
                SetAlert(false);
                return;
            }

            if (IsBlocked(origin, _player.position))
            {
                SetAlert(false);
                return;
            }

            SeesPlayer = true;
            SetAlert(true);

            if (game != null) game.OnPlayerSpotted();
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
            if (_coneMaterial != null) _coneMaterial.color = alert ? _alertColor : _calmColor;
        }

        /// <summary>Возвращает охранника в начало маршрута (после того как игрока поймали).</summary>
        public void ResetPatrol()
        {
            _walking = false;

            if (!HasRoute)
            {
                _facing = _moveFacing;
                ResetLookAround();
                SeesPlayer = false;
                SetAlert(false);
                UpdateVisuals();
                return;
            }

            transform.position = new Vector3(_route[0].x, _route[0].y, 0f);
            _routeIndex = _route.Length > 1 ? 1 : 0;
            _waitTimer = 0f;

            Vector2 direction = _route[_routeIndex] - _route[0];
            if (direction.sqrMagnitude > 0.0001f) _moveFacing = direction.normalized;
            _facing = _moveFacing;
            ResetLookAround();

            SeesPlayer = false;
            SetAlert(false);
            UpdateVisuals();
        }

        private void OnDestroy()
        {
            if (_coneMaterial != null) Destroy(_coneMaterial);
        }
    }
}
