using System.Collections.Generic;
using UnityEngine;

namespace HyperArcade
{
    public struct ProjectileSpec
    {
        public Team team;
        public float damage;
        public float speed;
        public float lifetime;
        public int pierce;
        public float scale;
        public string visualKey;
    }

    /// <summary>
    /// Proyectil genérico reutilizable (pool) para jugador y enemigos.
    /// Prefab: Rigidbody2D + CircleCollider2D (trigger) + hijo "Visual" con SkinnedVisual.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private SkinnedVisual visual;
        [SerializeField] private LayerMask obstacleMask;

        private Rigidbody2D _rb;
        private ProjectileSpec _spec;
        private float _despawnAt;
        private int _pierceLeft;
        private readonly HashSet<int> _alreadyHit = new HashSet<int>();

        /// <summary>Dispara un proyectil desde el pool usando el prefab de GameConfig.</summary>
        public static Projectile Fire(Vector2 position, Vector2 direction, ProjectileSpec spec)
        {
            var prefab = GameConfig.Instance != null ? GameConfig.Instance.projectilePrefab : null;
            if (prefab == null) return null;
            var p = PoolManager.Spawn(prefab, position, Quaternion.identity);
            p.Launch(direction, spec);
            return p;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            if (obstacleMask == 0) obstacleMask = LayerMask.GetMask("Ground");
            if (visual == null) visual = GetComponentInChildren<SkinnedVisual>();
        }

        public void Launch(Vector2 direction, ProjectileSpec spec)
        {
            _spec = spec;
            _pierceLeft = spec.pierce;
            _alreadyHit.Clear();
            _despawnAt = Time.time + Mathf.Max(0.05f, spec.lifetime);

            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            transform.localScale = Vector3.one * (spec.scale > 0f ? spec.scale : 1f);
            _rb.SetVelocity(direction * spec.speed);

            if (visual != null) visual.SetKey(spec.visualKey);
        }

        private void Update()
        {
            if (Time.time >= _despawnAt) PoolManager.Despawn(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (((1 << other.gameObject.layer) & obstacleMask) != 0)
            {
                PoolManager.Despawn(gameObject);
                return;
            }

            var target = other.GetComponentInParent<IDamageable>();
            if (target == null || target.Team == _spec.team || !target.IsAlive) return;

            int id = ((Component)target).GetInstanceID();
            if (!_alreadyHit.Add(id)) return;

            target.TakeDamage(new DamageInfo(_spec.damage, _spec.team, transform.position, _rb.GetVelocity().normalized));

            if (_pierceLeft-- <= 0) PoolManager.Despawn(gameObject);
        }
    }
}
