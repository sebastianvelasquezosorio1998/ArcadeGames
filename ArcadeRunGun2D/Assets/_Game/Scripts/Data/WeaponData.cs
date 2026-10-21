using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Arma mejorable. Dos ejes de mejora: Damage (daño) y Attack Delay (tiempo entre disparos).
    /// </summary>
    [CreateAssetMenu(menuName = "HyperArcade/Weapon", fileName = "Weapon_")]
    public class WeaponData : ScriptableObject
    {
        public string id = "blaster";
        public string displayName = "Blaster";
        [TextArea] public string description;
        [Tooltip("Clave del VisualSet del proyectil en el SkinProfile.")]
        public string projectileVisualKey = "proj_basic";
        [Tooltip("Clave del icono de tienda en el SkinProfile (VisualSet.icon).")]
        public string iconKey = "weapon_blaster";

        [Header("Daño")]
        public float baseDamage = 10f;
        public float damagePerLevel = 3f;
        public int maxDamageLevel = 10;

        [Header("Attack Delay (segundos entre disparos)")]
        public float baseAttackDelay = 0.35f;
        public float delayReductionPerLevel = 0.02f;
        public float minAttackDelay = 0.1f;
        public int maxDelayLevel = 10;

        [Header("Proyectil")]
        public float projectileSpeed = 16f;
        public float projectileLifetime = 1.2f;
        public int projectilesPerShot = 1;
        public float spreadAngle = 0f;
        public int pierce = 0;

        [Header("Habilidad especial (botón derecho)")]
        public SpecialAbilityType special = SpecialAbilityType.SpreadBurst;
        public float specialCooldown = 6f;
        public float specialDamageMultiplier = 2.5f;
        public string specialVisualKey = "proj_special";

        [Header("Economía")]
        public bool unlockedByDefault;
        public Price unlockPrice = new Price(CurrencyType.Coins, 1500);
        [Tooltip("Costo de la mejora nivel 0 -> 1. Crece con upgradeCostGrowth.")]
        public int baseUpgradeCost = 100;
        public float upgradeCostGrowth = 1.45f;

        public float GetDamage(int level) => baseDamage + damagePerLevel * Mathf.Clamp(level, 0, maxDamageLevel);

        public float GetAttackDelay(int level) =>
            Mathf.Max(minAttackDelay, baseAttackDelay - delayReductionPerLevel * Mathf.Clamp(level, 0, maxDelayLevel));

        /// <summary>Costo (en monedas) para pasar de "currentLevel" a "currentLevel + 1".</summary>
        public int GetUpgradeCost(int currentLevel) =>
            Mathf.RoundToInt(baseUpgradeCost * Mathf.Pow(upgradeCostGrowth, currentLevel) / 10f) * 10;
    }
}
