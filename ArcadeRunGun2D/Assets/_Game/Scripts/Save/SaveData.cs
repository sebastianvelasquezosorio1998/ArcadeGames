using System;
using System.Collections.Generic;

namespace HyperArcade
{
    /// <summary>
    /// Estado persistente del jugador. Serializable con JsonUtility
    /// (por eso se usan listas en lugar de diccionarios).
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;

        // Economía dual
        public int coins;
        public int diamonds;

        // Personajes y armas
        public string selectedCharacterId;
        public List<string> unlockedCharacters = new List<string>();
        public string selectedWeaponId;
        public List<WeaponProgress> weapons = new List<WeaponProgress>();

        // Consumibles en inventario
        public List<ItemCount> consumables = new List<ItemCount>();

        // Progresión
        public List<LevelProgress> levels = new List<LevelProgress>();
        public int endlessBestScore;
        public float endlessBestTime;
        public int totalMatches;

        // Anuncios (límites diarios)
        public AdCounters ads = new AdCounters();

        public SettingsData settings = new SettingsData();
    }

    [Serializable]
    public class WeaponProgress
    {
        public string weaponId;
        public bool unlocked;
        public int damageLevel;
        public int delayLevel;
    }

    [Serializable]
    public class ItemCount
    {
        public string id;
        public int count;
    }

    [Serializable]
    public class LevelProgress
    {
        public string levelId;
        public int stars;
        public int bestCoins;
    }

    [Serializable]
    public class AdCounters
    {
        /// <summary>Fecha (yyyy-MM-dd, hora local) a la que corresponden los contadores.</summary>
        public string day;
        public List<ItemCount> watchedToday = new List<ItemCount>();
        public List<AdTimestamp> lastWatched = new List<AdTimestamp>();
        public int totalWatched;
        /// <summary>Niveles ganados desde el último video entre niveles (intersticial).</summary>
        public int levelsSinceInterstitial;
        public int totalInterstitials;
    }

    [Serializable]
    public class AdTimestamp
    {
        public string id;
        public long utcTicks;
    }

    [Serializable]
    public class SettingsData
    {
        public float musicVolume = 0.7f;
        public float sfxVolume = 1f;
        public bool vibration = true;
    }
}
