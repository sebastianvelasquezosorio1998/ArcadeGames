using UnityEngine;

namespace HyperArcade
{
    /// <summary>Cámara que sigue al jugador con anticipación, límites de arena y sacudida.</summary>
    public class CameraFollow2D : MonoBehaviour
    {
        private static CameraFollow2D _instance;

        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private float lookAhead = 1.5f;
        [SerializeField] private Vector2 offset = new Vector2(0f, 1.5f);
        [Tooltip("Límites del centro de la cámara (x = min, y = max).")]
        [SerializeField] private Vector2 xLimits = new Vector2(-20f, 20f);
        [SerializeField] private Vector2 yLimits = new Vector2(0f, 6f);

        private Vector3 _velocity;
        private Vector3 _lastShake;
        private PlayerController _player;
        private float _shakeUntil;
        private float _shakeIntensity;

        public static void Shake(float intensity, float duration)
        {
            if (_instance == null) return;
            _instance._shakeIntensity = Mathf.Max(_instance._shakeIntensity, intensity);
            _instance._shakeUntil = Mathf.Max(_instance._shakeUntil, Time.time + duration);
        }

        public void SetTarget(Transform t)
        {
            target = t;
            _player = t != null ? t.GetComponent<PlayerController>() : null;
            if (t != null) transform.position = GetDesired();
        }

        private void Awake()
        {
            _instance = this;
            if (target != null) SetTarget(target);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 pos = Vector3.SmoothDamp(transform.position - _lastShake, GetDesired(), ref _velocity, smoothTime);

            Vector3 shake = Vector3.zero;
            if (Time.time < _shakeUntil) shake = (Vector3)(Random.insideUnitCircle * _shakeIntensity);
            else _shakeIntensity = 0f;

            _lastShake = shake;
            transform.position = pos + shake;
        }

        private Vector3 GetDesired()
        {
            float facing = _player == null || _player.FacingRight ? 1f : -1f;

            float x = Mathf.Clamp(target.position.x + offset.x + facing * lookAhead, xLimits.x, xLimits.y);
            float y = Mathf.Clamp(target.position.y + offset.y, yLimits.x, yLimits.y);
            return new Vector3(x, y, transform.position.z);
        }
    }
}
