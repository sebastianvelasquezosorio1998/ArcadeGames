using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Vida del jugador con invulnerabilidad tras recibir daño, escudo temporal (potenciador)
    /// y revivir (anuncio recompensado).
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] private float invulnerabilityAfterHit = 0.8f;
        [SerializeField] private float knockbackForce = 6f;
        [Tooltip("Hijo con SkinnedVisual (clave 'fx_shield') que se muestra con el escudo activo.")]
        [SerializeField] private SkinnedVisual shieldVisual;

        public Team Team => Team.Player;
        public bool IsAlive => Current > 0f;
        public float Current { get; private set; }
        public float Max { get; private set; } = 100f;
        public float Normalized => Max > 0f ? Current / Max : 0f;
        public bool ShieldActive => Time.time < _shieldUntil;
        /// <summary>Daño total recibido en la partida (para estadísticas).</summary>
        public float DamageTaken { get; private set; }

        private float _invulnerableUntil;
        private float _shieldUntil;
        private Rigidbody2D _rb;
        private PlayerController _controller;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _controller = GetComponent<PlayerController>();
            if (shieldVisual != null) shieldVisual.gameObject.SetActive(false);
        }

        public void Setup(float maxHealth)
        {
            Max = maxHealth;
            Current = maxHealth;
            DamageTaken = 0f;
            GameEvents.RaisePlayerHealthChanged(Current, Max);
        }

        public void ActivateShield(float seconds)
        {
            _shieldUntil = Mathf.Max(_shieldUntil, Time.time + seconds);
            if (shieldVisual != null)
            {
                shieldVisual.gameObject.SetActive(true);
                shieldVisual.SetKey("fx_shield");
            }
            AudioManager.Play(SfxId.Shield);
        }

        public void TakeDamage(DamageInfo info)
        {
            if (!IsAlive || info.sourceTeam == Team.Player) return;
            if (ShieldActive || Time.time < _invulnerableUntil) return;

            Current = Mathf.Max(0f, Current - info.amount);
            DamageTaken += info.amount;
            _invulnerableUntil = Time.time + invulnerabilityAfterHit;
            GameEvents.RaisePlayerHealthChanged(Current, Max);

            var visual = _controller != null ? _controller.Visual : null;
            if (visual != null) visual.Flash(Color.red, 0.1f);
            CameraFollow2D.Shake(0.2f, 0.2f);
            Vibrate();

            if (_rb != null)
            {
                float dir = Mathf.Sign(transform.position.x - info.hitPoint.x);
                if (dir == 0f) dir = 1f;
                _rb.SetVelocity(new Vector2(dir * knockbackForce, knockbackForce * 0.6f));
            }

            if (Current <= 0f) Die();
            else AudioManager.Play(SfxId.PlayerHit);
        }

        /// <summary>Daño letal directo (caer al vacío).</summary>
        public void Kill()
        {
            if (!IsAlive) return;
            Current = 0f;
            GameEvents.RaisePlayerHealthChanged(Current, Max);
            Die();
        }

        public void Revive(float healthPercent, float invulnerableSeconds = 2.5f)
        {
            Current = Mathf.Max(1f, Max * healthPercent);
            _invulnerableUntil = Time.time + invulnerableSeconds;
            if (_controller != null)
            {
                _controller.ControlsEnabled = true;
                if (_controller.Visual != null) _controller.Visual.ForceState(AnimState.Idle);
            }
            GameEvents.RaisePlayerHealthChanged(Current, Max);
            GameEvents.RaisePlayerRevived();
        }

        private void Die()
        {
            AudioManager.Play(SfxId.PlayerDie);
            if (_controller != null)
            {
                _controller.ControlsEnabled = false;
                if (_controller.Visual != null) _controller.Visual.ForceState(AnimState.Death);
            }
            GameEvents.RaisePlayerDied();
        }

        private void Update()
        {
            var visual = _controller != null ? _controller.Visual : null;
            if (visual != null) visual.SetBlink(IsAlive && !ShieldActive && Time.time < _invulnerableUntil);
            if (shieldVisual != null && shieldVisual.gameObject.activeSelf && !ShieldActive)
                shieldVisual.gameObject.SetActive(false);
        }

        private static void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (SaveSystem.Data != null && SaveSystem.Data.settings.vibration) Handheld.Vibrate();
#endif
        }
    }
}
