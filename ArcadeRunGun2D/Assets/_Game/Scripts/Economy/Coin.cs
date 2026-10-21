using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Moneda soltada por enemigos. Salta, rebota en el suelo y, tras un instante,
    /// vuela sola hacia el jugador (en hipercasual nunca se pierden monedas).
    /// Prefab: Rigidbody2D + CircleCollider2D (capa "Pickup", colisiona solo con Ground) + hijo Visual.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Coin : MonoBehaviour
    {
        [SerializeField] private SkinnedVisual visual;
        [SerializeField] private float magnetDelay = 0.6f;
        [SerializeField] private float magnetSpeed = 18f;
        [SerializeField] private float collectDistance = 0.6f;
        [SerializeField] private float maxLifetime = 12f;

        private Rigidbody2D _rb;
        private int _value;
        private float _spawnTime;
        private bool _magnet;
        private bool _collected;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.freezeRotation = true;
            if (visual == null) visual = GetComponentInChildren<SkinnedVisual>();
        }

        public void Init(int value, Vector2 popVelocity)
        {
            _value = value;
            _spawnTime = Time.time;
            _magnet = false;
            _collected = false;
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = 2.5f;
            _rb.SetVelocity(popVelocity);
            if (visual != null) visual.SetKey("pickup_coin");
        }

        private void Update()
        {
            if (_collected) return;
            var player = MatchController.Instance != null ? MatchController.Instance.Player : null;
            if (player == null) return;

            Vector2 target = (Vector2)player.transform.position + Vector2.up * 0.6f;
            if (!_magnet && Time.time - _spawnTime >= magnetDelay)
            {
                _magnet = true;
                _rb.bodyType = RigidbodyType2D.Kinematic;
                _rb.SetVelocity(Vector2.zero);
            }

            if (_magnet)
            {
                float speed = magnetSpeed * (1f + (Time.time - _spawnTime - magnetDelay));
                transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);
            }

            if (Vector2.Distance(transform.position, target) <= collectDistance || Time.time - _spawnTime > maxLifetime)
                Collect();
        }

        public void Collect()
        {
            if (_collected) return;
            _collected = true;
            if (MatchController.Instance != null) MatchController.Instance.AddMatchCoins(_value);
            AudioManager.Play(SfxId.Coin);
            PoolManager.Despawn(gameObject);
        }
    }
}
