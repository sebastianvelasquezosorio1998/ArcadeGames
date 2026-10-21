using System.Collections.Generic;
using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Configuración raíz del juego. Debe existir en Resources/GameConfig.asset.
    /// Para crear una versión de marca blanca basta con cambiar "activeSkin"
    /// (y opcionalmente los IDs de anuncios en AdsConfig).
    /// </summary>
    [CreateAssetMenu(menuName = "HyperArcade/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Marca blanca")]
        [Tooltip("Skin (sprites, fondos, sonidos, colores) usado en esta build.")]
        public SkinProfile activeSkin;
        [Tooltip("Skin de respaldo: se usa para cualquier recurso que el skin activo no defina.")]
        public SkinProfile fallbackSkin;

        [Header("Configuraciones")]
        public EconomyConfig economy;
        public AdsConfig ads;
        public EndlessConfig endless;

        [Header("Contenido (solo lógica / estadísticas)")]
        public List<CharacterData> characters = new List<CharacterData>();
        public List<WeaponData> weapons = new List<WeaponData>();
        public List<ConsumableData> consumables = new List<ConsumableData>();
        [Tooltip("En orden de progresión del mapa.")]
        public List<LevelData> levels = new List<LevelData>();

        [Header("Prefabs genéricos (sin arte: el arte lo pone el skin)")]
        public PlayerController playerPrefab;
        public EnemyController enemyPrefab;
        public Projectile projectilePrefab;
        public Coin coinPrefab;

        private static GameConfig _instance;

        public static GameConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GameConfig>("GameConfig");
                    if (_instance == null)
                        Debug.LogError("[HyperArcade] Falta Resources/GameConfig.asset. Usa Tools > HyperArcade > 1. Crear contenido por defecto.");
                }
                return _instance;
            }
        }

        public CharacterData GetCharacter(string id) => characters.Find(c => c != null && c.id == id);
        public WeaponData GetWeapon(string id) => weapons.Find(w => w != null && w.id == id);
        public ConsumableData GetConsumable(ConsumableType type) => consumables.Find(c => c != null && c.type == type);
        public LevelData GetLevel(string id) => levels.Find(l => l != null && l.id == id);
        public int GetLevelIndex(LevelData level) => levels.IndexOf(level);

        public LevelData GetNextLevel(LevelData level)
        {
            int i = levels.IndexOf(level);
            return i >= 0 && i + 1 < levels.Count ? levels[i + 1] : null;
        }
    }
}
