using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Estadísticas de un personaje. NO contiene sprites: el aspecto visual se resuelve
    /// con "visualKey" en el SkinProfile activo (marca blanca).
    /// </summary>
    [CreateAssetMenu(menuName = "HyperArcade/Character", fileName = "Character_")]
    public class CharacterData : ScriptableObject
    {
        public string id = "hero";
        public string displayName = "Héroe";
        [TextArea] public string description;
        [Tooltip("Clave del VisualSet en el SkinProfile (ej. 'player_hero').")]
        public string visualKey = "player_hero";

        [Header("Estadísticas")]
        public float maxHealth = 100f;
        public float moveSpeed = 7f;
        public float jumpForce = 14f;
        [Range(0, 2)] public int extraJumps = 1;
        [Tooltip("Multiplica el daño de cualquier arma que use.")]
        public float damageMultiplier = 1f;

        [Header("Desbloqueo")]
        public bool unlockedByDefault;
        public Price unlockPrice = new Price(CurrencyType.Coins, 1000);
        [Tooltip("Arma con la que empieza si el jugador no tiene otra equipada.")]
        public WeaponData defaultWeapon;
    }
}
