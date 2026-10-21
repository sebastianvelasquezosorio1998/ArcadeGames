using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Conecta los botones de Ataque y Especial con el WeaponController.
    /// Incluye auto-apuntado suave al enemigo más cercano frente al jugador (ayuda en táctil).
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(WeaponController))]
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private bool autoAim = true;
        [SerializeField] private float autoAimRange = 9f;
        [Tooltip("Ángulo máximo (grados) respecto a la dirección de apuntado para corregir.")]
        [SerializeField] private float autoAimAngle = 35f;

        public WeaponController Weapon { get; private set; }
        private PlayerController _controller;
        private PlayerHealth _health;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _health = GetComponent<PlayerHealth>();
            Weapon = GetComponent<WeaponController>();
        }

        public void Setup(WeaponData weapon, float damageMultiplier)
        {
            var progress = PlayerProgress.GetWeapon(weapon.id);
            Weapon.Setup(weapon, progress.damageLevel, progress.delayLevel, damageMultiplier);
        }

        private void Update()
        {
            var input = PlayerInputHub.Instance;
            if (input == null || !_controller.ControlsEnabled || (_health != null && !_health.IsAlive)) return;

            if (input.SpecialButton != null) input.SpecialButton.SetCooldown(Weapon.SpecialCooldownNormalized);

            Vector2 aim = GetAim();
            bool wantsFire = input.AttackHeld || (input.AutoFire && EnemyRegistry.Count > 0);
            if (wantsFire && Weapon.TryFire(aim)) _controller.PlayAttackAnim();
            if (input.ConsumeSpecial() && Weapon.TrySpecial(aim)) _controller.PlayAttackAnim(0.3f);
        }

        private Vector2 GetAim()
        {
            Vector2 aim = _controller.AimDirection;
            if (!autoAim) return aim;

            var target = EnemyRegistry.FindNearest(transform.position, autoAimRange, aim, autoAimAngle);
            if (target == null) return aim;
            Vector2 toTarget = (Vector2)target.AimPoint - (Vector2)transform.position;
            return toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : aim;
        }
    }
}
