using UnityEngine;

namespace LibraryEscape
{
    /// <summary>Камера следует за игроком и не выходит за пределы комнаты.</summary>
    public class FollowCamera2D : MonoBehaviour
    {
        public Transform target;
        public float smoothTime = 0.16f;
        public float orthographicSize = 6f;
        public Rect bounds = new Rect(-12.5f, -25f, 25f, 50f);

        private Camera _camera;
        private Vector3 _velocity;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            ApplySize();
        }

        public void SetTarget(Transform followTarget, Rect roomBounds)
        {
            target = followTarget;
            bounds = roomBounds;
            ApplySize();
            SnapNow();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float halfHeight = orthographicSize;
            float aspect = _camera != null ? _camera.aspect : 16f / 9f;
            float halfWidth = halfHeight * aspect;

            float minX = bounds.xMin + halfWidth;
            float maxX = bounds.xMax - halfWidth;
            float minY = bounds.yMin + halfHeight;
            float maxY = bounds.yMax - halfHeight;

            float x = minX <= maxX ? Mathf.Clamp(target.position.x, minX, maxX) : bounds.center.x;
            float y = minY <= maxY ? Mathf.Clamp(target.position.y, minY, maxY) : bounds.center.y;

            Vector3 desired = new Vector3(x, y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);
        }

        private void SnapNow()
        {
            if (target == null) return;

            float halfHeight = orthographicSize;
            float aspect = _camera != null ? _camera.aspect : 16f / 9f;
            float halfWidth = halfHeight * aspect;

            float x = Mathf.Clamp(target.position.x, bounds.xMin + halfWidth, bounds.xMax - halfWidth);
            float y = Mathf.Clamp(target.position.y, bounds.yMin + halfHeight, bounds.yMax - halfHeight);

            transform.position = new Vector3(x, y, transform.position.z);
            _velocity = Vector3.zero;
        }

        private void ApplySize()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera != null)
            {
                _camera.orthographic = true;
                _camera.orthographicSize = orthographicSize;
            }
        }
    }
}
