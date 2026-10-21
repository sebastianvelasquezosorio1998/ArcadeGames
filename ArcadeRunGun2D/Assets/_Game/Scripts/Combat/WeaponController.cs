using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Sistema de disparo: ataque básico con Attack Delay + habilidad especial con cooldown.
    /// Las estadísticas salen de WeaponData + niveles de mejora guardados.
    /// </summary>
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private Transform muzzle;
        [SerializeField] private Team team = Team.Player;

        public WeaponData Data { get; private set; }
        public float Damage { get; private set; }
        public float AttackDelay { get; private set; }

        private float _nextFireTime;
        private float _nextSpecialTime;

        /// <summary>0 = especial listo, 1 = recién usado.</summary>
        public float SpecialCooldownNormalized =>
            Data == null || Data.specialCooldown <= 0f ? 0f : Mathf.Clamp01((_nextSpecialTime - Time.time) / Data.specialCooldown);

        public bool SpecialReady => Time.time >= _nextSpecialTime;

        public void Setup(WeaponData data, int damageLevel, int delayLevel, float damageMultiplier)
        {
            Data = data;
            Damage = data.GetDamage(damageLevel) * damageMultiplier;
            AttackDelay = data.GetAttackDelay(delayLevel);
            _nextFireTime = 0f;
            _nextSpecialTime = 0f;
        }

        public bool TryFire(Vector2 direction)
        {
            if (Data == null || Time.time < _nextFireTime) return false;
            _nextFireTime = Time.time + AttackDelay;

            int count = Mathf.Max(1, Data.projectilesPerShot);
            FireFan(direction, count, Data.spreadAngle, Damage, Data.projectileSpeed, Data.pierce, 1f, Data.projectileVisualKey);
            AudioManager.Play(SfxId.Shoot);
            return true;
        }

        public bool TrySpecial(Vector2 direction)
        {
            if (Data == null || !SpecialReady) return false;
            _nextSpecialTime = Time.time + Data.specialCooldown;

            float dmg = Damage * Data.specialDamageMultiplier;
            switch (Data.special)
            {
                case SpecialAbilityType.SpreadBurst:
                    // Abanico de 7 proyectiles
                    FireFan(direction, 7, 60f, dmg, Data.projectileSpeed, 0, 1.2f, Data.specialVisualKey);
                    break;
                case SpecialAbilityType.PiercingShot:
                    // Disparo grande que atraviesa a todos
                    FireFan(direction, 1, 0f, dmg * 2f, Data.projectileSpeed * 1.3f, 999, 2.2f, Data.specialVisualKey);
                    break;
                case SpecialAbilityType.Nova:
                    // Anillo de 16 proyectiles alrededor del jugador
                    FireFan(Vector2.right, 16, 360f - 360f / 16f, dmg, Data.projectileSpeed * 0.8f, 1, 1.2f, Data.specialVisualKey);
                    break;
            }
            AudioManager.Play(SfxId.Special);
            CameraFollow2D.Shake(0.15f, 0.15f);
            return true;
        }

        private void FireFan(Vector2 direction, int count, float totalAngle, float damage, float speed, int pierce, float scale, string visualKey)
        {
            // El cañón se refleja según hacia dónde se dispara (el root del jugador no se voltea).
            Vector2 origin = transform.position;
            if (muzzle != null)
            {
                Vector2 local = muzzle.localPosition;
                if (direction.x < -0.01f) local.x = -local.x;
                origin += local;
            }
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float step = count > 1 ? totalAngle / (count - 1) : 0f;
            float start = baseAngle - totalAngle * 0.5f;

            var spec = new ProjectileSpec
            {
                team = team,
                damage = damage,
                speed = speed,
                lifetime = Data.projectileLifetime,
                pierce = pierce,
                scale = scale,
                visualKey = visualKey
            };

            for (int i = 0; i < count; i++)
            {
                float a = (count > 1 ? start + step * i : baseAngle) * Mathf.Deg2Rad;
                Projectile.Fire(origin, new Vector2(Mathf.Cos(a), Mathf.Sin(a)), spec);
            }
        }
    }
}
