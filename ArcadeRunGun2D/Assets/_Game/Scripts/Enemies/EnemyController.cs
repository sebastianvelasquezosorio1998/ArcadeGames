using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Enemigo genérico (un único prefab para todos). El comportamiento, las estadísticas
    /// y la clave visual vienen de EnemyData:
    ///  - Walker: camina hacia el jugador.
    ///  - Flyer: vuela hacia el jugador con ondulación.
    ///  - Shooter: mantiene distancia y dispara.
    ///  - Boss: alterna persecución y ráfagas; se enfurece por debajo del 50% de vida.
    /// Prefab: Rigidbody2D (Dynamic, Freeze Rotation) + BoxCollider2D + hijo "Visual" (SkinnedVisual).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        [SerializeField] private SkinnedVisual visual;
        [SerializeField] private float deathDespawnDelay = 0.45f;

        public EnemyData Data { get; private set; }
        public Team Team => Team.Enemy;
        public bool IsAlive => _health > 0f && _active;
        public float Health => _health;
        public float MaxHealth => _maxHealth;
        public Vector3 AimPoint => transform.position + Vector3.up * (Data != null ? Data.colliderSize.y * 0.5f : 0.5f);

        private Rigidbody2D _rb;
        private BoxCollider2D _collider;
        private float _health;
        private float _maxHealth;
        private float _speedMultiplier = 1f;
        private bool _active;
        private float _nextShotTime;
        private float _nextContactTime;
        private float _despawnAt = -1f;
        private float _spawnTime;
        private bool _facingRight = true;

        // Jefe
        private float _phaseEndTime;
        private bool _bossBarrage;
        private bool _enraged;

        private PlayerHealth _player;
        private Collider2D _playerCollider;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.freezeRotation = true;
            _collider = GetComponent<BoxCollider2D>();
            if (visual == null) visual = GetComponentInChildren<SkinnedVisual>();
            if (visual != null) visual.ClipFinished += OnClipFinished;
        }

        /// <summary>Llamado por EnemySpawner justo después de sacarlo del pool.</summary>
        public void Init(EnemyData data, float healthMultiplier, float speedMultiplier, PlayerHealth player)
        {
            Data = data;
            _maxHealth = data.maxHealth * Mathf.Max(0.1f, healthMultiplier);
            _health = _maxHealth;
            _speedMultiplier = speedMultiplier;
            _player = player;
            _playerCollider = player != null ? player.GetComponent<Collider2D>() : null;
            _active = true;
            _despawnAt = -1f;
            _spawnTime = Time.time;
            _nextShotTime = Time.time + data.shootDelay * Random.Range(0.6f, 1.2f);
            _phaseEndTime = Time.time + 3f;
            _bossBarrage = false;
            _enraged = false;

            _collider.enabled = true;
            _collider.size = data.colliderSize;
            _collider.offset = new Vector2(0f, data.colliderSize.y * 0.5f);
            _rb.simulated = true;
            _rb.gravityScale = data.behavior == EnemyBehavior.Flyer ? 0f : 3f;
            _rb.SetVelocity(Vector2.zero);

            if (visual != null)
            {
                visual.SetKey(data.visualKey);
                visual.ForceState(AnimState.Run);
            }

            EnemyRegistry.Register(this);
            if (data.isBoss)
            {
                GameEvents.RaiseBossSpawned(this);
                GameEvents.RaiseBossHealthChanged(_health, _maxHealth);
                AudioManager.Play(SfxId.BossAppear);
                CameraFollow2D.Shake(0.3f, 0.5f);
            }
        }

        private void Update()
        {
            if (!_active)
            {
                if (_despawnAt > 0f && Time.time >= _despawnAt) Despawn();
                return;
            }
            if (_player == null || !_player.IsAlive)
            {
                _rb.SetVelocity(new Vector2(0f, _rb.GetVelocity().y));
                return;
            }

            switch (Data.behavior)
            {
                case EnemyBehavior.Walker: UpdateWalker(1f); break;
                case EnemyBehavior.Flyer: UpdateFlyer(); break;
                case EnemyBehavior.Shooter: UpdateShooter(); break;
                case EnemyBehavior.Boss: UpdateBoss(); break;
            }

            CheckContactDamage();
            if (visual != null) visual.SetFacing(_facingRight);
        }

        // ------------------------------------------------------------ IA

        private Vector2 ToPlayer => (Vector2)(_player.transform.position - transform.position);

        private void UpdateWalker(float speedScale)
        {
            float dir = Mathf.Sign(ToPlayer.x);
            _facingRight = dir > 0f;
            float speed = Data.moveSpeed * _speedMultiplier * speedScale;
            _rb.SetVelocity(new Vector2(dir * speed, _rb.GetVelocity().y));
        }

        private void UpdateFlyer()
        {
            Vector2 to = ToPlayer + Vector2.up * 0.8f;
            _facingRight = to.x > 0f;
            Vector2 v = to.normalized * Data.moveSpeed * _speedMultiplier;
            v.y += Mathf.Sin((Time.time - _spawnTime) * 4f) * 1.2f;
            _rb.SetVelocity(v);
        }

        private void UpdateShooter()
        {
            Vector2 to = ToPlayer;
            _facingRight = to.x > 0f;
            float dist = Mathf.Abs(to.x);
            float move = 0f;
            if (dist > Data.preferredDistance + 1f) move = Mathf.Sign(to.x);
            else if (dist < Data.preferredDistance - 1f) move = -Mathf.Sign(to.x);
            _rb.SetVelocity(new Vector2(move * Data.moveSpeed * _speedMultiplier, _rb.GetVelocity().y));

            if (Time.time >= _nextShotTime && dist < Data.preferredDistance * 2f)
            {
                _nextShotTime = Time.time + Data.shootDelay;
                Shoot(to, 1, 0f);
            }
        }

        private void UpdateBoss()
        {
            if (!_enraged && _health <= _maxHealth * 0.5f)
            {
                _enraged = true;
                _speedMultiplier *= 1.4f;
                if (visual != null) visual.Flash(new Color(1f, 0.4f, 0.4f), 0.4f);
            }

            if (Time.time >= _phaseEndTime)
            {
                _bossBarrage = !_bossBarrage;
                _phaseEndTime = Time.time + (_bossBarrage ? 2.2f : 3.5f);
                _nextShotTime = Time.time + 0.3f;
            }

            if (_bossBarrage)
            {
                _rb.SetVelocity(new Vector2(0f, _rb.GetVelocity().y));
                _facingRight = ToPlayer.x > 0f;
                if (Time.time >= _nextShotTime)
                {
                    float delay = Data.shootDelay * (_enraged ? 0.3f : 0.45f);
                    _nextShotTime = Time.time + delay;
                    Shoot(ToPlayer + Vector2.up * 0.5f, _enraged ? 5 : 3, 40f);
                    if (visual != null) visual.SetState(AnimState.Attack);
                }
            }
            else
            {
                UpdateWalker(1f);
                if (visual != null) visual.SetState(AnimState.Run);
            }
        }

        private void Shoot(Vector2 direction, int count, float spread)
        {
            var spec = new ProjectileSpec
            {
                team = Team.Enemy,
                damage = Data.projectileDamage,
                speed = Data.projectileSpeed,
                lifetime = 3f,
                pierce = 0,
                scale = Data.isBoss ? 1.4f : 1f,
                visualKey = Data.projectileVisualKey
            };
            Vector2 origin = AimPoint;
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float step = count > 1 ? spread / (count - 1) : 0f;
            for (int i = 0; i < count; i++)
            {
                float a = (baseAngle - spread * 0.5f + step * i) * Mathf.Deg2Rad;
                if (count == 1) a = baseAngle * Mathf.Deg2Rad;
                Projectile.Fire(origin, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), spec);
            }
        }

        /// <summary>Daño por contacto sin depender de la matriz de colisiones (comparación de bounds).</summary>
        private void CheckContactDamage()
        {
            if (_playerCollider == null || Time.time < _nextContactTime) return;
            if (!_collider.bounds.Intersects(_playerCollider.bounds)) return;
            _nextContactTime = Time.time + 0.5f;
            _player.TakeDamage(new DamageInfo(Data.contactDamage, Team.Enemy, transform.position, ToPlayer.normalized));
        }

        // ------------------------------------------------------------ Daño

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive || info.sourceTeam == Team.Enemy) return;
            _health -= info.amount;
            if (visual != null) visual.Flash(new Color(1f, 0.55f, 0.55f), 0.07f);
            AudioManager.Play(SfxId.EnemyHit);

            // Pequeño empuje (no en jefes)
            if (!Data.isBoss && Data.behavior != EnemyBehavior.Flyer)
                _rb.SetVelocity(new Vector2(info.direction.x * 2.5f, _rb.GetVelocity().y));

            if (Data.isBoss) GameEvents.RaiseBossHealthChanged(Mathf.Max(0f, _health), _maxHealth);
            if (_health <= 0f) Die(true);
        }

        /// <summary>Potenciador "Convertidor": el enemigo se transforma en monedas (no aplica a jefes).</summary>
        public bool ConvertToCoins()
        {
            if (!IsAlive || Data.isBoss) return false;
            _health = 0f;
            Die(true, bonusCoins: 2);
            return true;
        }

        private void Die(bool giveRewards, int bonusCoins = 0)
        {
            _active = false;
            EnemyRegistry.Unregister(this);
            _collider.enabled = false;
            _rb.SetVelocity(Vector2.zero);
            _rb.simulated = false;

            if (giveRewards)
            {
                int coins = Random.Range(Data.minCoins, Data.maxCoins + 1) + bonusCoins;
                CoinDropper.Drop(AimPoint, coins);
                AudioManager.Play(SfxId.EnemyDie);
                GameEvents.RaiseEnemyKilled(this);
                if (Data.isBoss)
                {
                    CameraFollow2D.Shake(0.5f, 0.6f);
                    GameEvents.RaiseBossDefeated(this);
                }
            }

            if (visual != null) visual.ForceState(AnimState.Death);
            _despawnAt = Time.time + deathDespawnDelay;
        }

        private void OnClipFinished(AnimState state)
        {
            if (state == AnimState.Death && !_active) _despawnAt = Mathf.Min(_despawnAt, Time.time + 0.1f);
            else if (state == AnimState.Attack && _active && visual != null) visual.SetState(AnimState.Run);
        }

        /// <summary>Retira al enemigo sin recompensas (fin de partida, caída al vacío, limpieza).</summary>
        public void Despawn()
        {
            if (_active) Die(false);
            _active = false;
            EnemyRegistry.Unregister(this);
            PoolManager.Despawn(gameObject);
        }

        private void OnDisable()
        {
            _active = false;
            EnemyRegistry.Unregister(this);
        }
    }
}
