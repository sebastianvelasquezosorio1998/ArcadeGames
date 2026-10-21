using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Tipo de enemigo. Todos usan el mismo prefab genérico; esto define
    /// comportamiento, estadísticas y la clave visual del skin.
    /// </summary>
    [CreateAssetMenu(menuName = "HyperArcade/Enemy", fileName = "Enemy_")]
    public class EnemyData : ScriptableObject
    {
        public string id = "grunt";
        public string displayName = "Soldado";
        public string visualKey = "enemy_grunt";
        public EnemyBehavior behavior = EnemyBehavior.Walker;
        public bool isBoss;

        [Header("Estadísticas")]
        public float maxHealth = 30f;
        public float moveSpeed = 2.5f;
        public float contactDamage = 10f;
        [Tooltip("Tamaño del collider en unidades de mundo.")]
        public Vector2 colliderSize = new Vector2(0.8f, 1f);

        [Header("Disparo (Shooter / Boss)")]
        public float shootDelay = 2f;
        public float projectileDamage = 8f;
        public float projectileSpeed = 8f;
        public float preferredDistance = 5f;
        public string projectileVisualKey = "proj_enemy";

        [Header("Recompensas")]
        public int minCoins = 1;
        public int maxCoins = 3;
        public int scoreValue = 10;
    }
}
