using System;
using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Fachada de lectura/escritura del progreso: personajes, armas, consumibles, niveles y récords.
    /// Las UIs y la tienda nunca tocan SaveData directamente.
    /// </summary>
    public static class PlayerProgress
    {
        /// <summary>Se dispara cuando cambia algo de inventario/progreso (para refrescar UIs).</summary>
        public static event Action Changed;

        private static SaveData D => SaveSystem.Data;
        private static GameConfig Config => GameConfig.Instance;

        // ---------------- Personajes ----------------

        public static bool IsCharacterUnlocked(string id) => D != null && D.unlockedCharacters.Contains(id);

        public static void UnlockCharacter(string id)
        {
            if (D == null || IsCharacterUnlocked(id)) return;
            D.unlockedCharacters.Add(id);
            Commit();
        }

        public static CharacterData SelectedCharacter
        {
            get
            {
                var c = D != null ? Config.GetCharacter(D.selectedCharacterId) : null;
                return c != null ? c : (Config.characters.Count > 0 ? Config.characters[0] : null);
            }
        }

        public static void SelectCharacter(string id)
        {
            if (!IsCharacterUnlocked(id)) return;
            D.selectedCharacterId = id;
            Commit();
        }

        // ---------------- Armas ----------------

        public static WeaponProgress GetWeapon(string weaponId)
        {
            if (D == null) return new WeaponProgress { weaponId = weaponId };
            var wp = D.weapons.Find(w => w.weaponId == weaponId);
            if (wp == null)
            {
                wp = new WeaponProgress { weaponId = weaponId };
                D.weapons.Add(wp);
            }
            return wp;
        }

        public static bool IsWeaponUnlocked(string weaponId) => GetWeapon(weaponId).unlocked;

        public static void UnlockWeapon(string weaponId)
        {
            GetWeapon(weaponId).unlocked = true;
            Commit();
        }

        public static WeaponData SelectedWeapon
        {
            get
            {
                var w = D != null ? Config.GetWeapon(D.selectedWeaponId) : null;
                if (w != null) return w;
                var character = SelectedCharacter;
                return character != null && character.defaultWeapon != null
                    ? character.defaultWeapon
                    : (Config.weapons.Count > 0 ? Config.weapons[0] : null);
            }
        }

        public static void SelectWeapon(string weaponId)
        {
            if (!IsWeaponUnlocked(weaponId)) return;
            D.selectedWeaponId = weaponId;
            Commit();
        }

        public static void IncreaseWeaponDamage(string weaponId) { GetWeapon(weaponId).damageLevel++; Commit(); }
        public static void IncreaseWeaponSpeed(string weaponId) { GetWeapon(weaponId).delayLevel++; Commit(); }

        // ---------------- Consumibles ----------------

        public static int GetConsumableCount(ConsumableType type)
        {
            if (D == null) return 0;
            var item = D.consumables.Find(c => c.id == type.ToString());
            return item != null ? item.count : 0;
        }

        public static void AddConsumable(ConsumableType type, int amount)
        {
            if (D == null || amount <= 0) return;
            string key = type.ToString();
            var item = D.consumables.Find(c => c.id == key);
            if (item == null) D.consumables.Add(new ItemCount { id = key, count = amount });
            else item.count += amount;
            Commit();
        }

        public static bool TryUseConsumable(ConsumableType type)
        {
            if (D == null) return false;
            var item = D.consumables.Find(c => c.id == type.ToString());
            if (item == null || item.count <= 0) return false;
            item.count--;
            Commit();
            return true;
        }

        // ---------------- Niveles ----------------

        public static int GetStars(string levelId)
        {
            if (D == null) return 0;
            var lp = D.levels.Find(l => l.levelId == levelId);
            return lp != null ? lp.stars : 0;
        }

        public static int TotalStars
        {
            get
            {
                int total = 0;
                if (D != null) foreach (var l in D.levels) total += l.stars;
                return total;
            }
        }

        /// <summary>Un nivel se desbloquea al obtener al menos 1★ en el anterior.</summary>
        public static bool IsLevelUnlocked(int levelIndex)
        {
            if (levelIndex <= 0) return true;
            if (levelIndex >= Config.levels.Count) return false;
            return GetStars(Config.levels[levelIndex - 1].id) > 0;
        }

        /// <summary>Registra el resultado conservando el mejor. Devuelve true si es un nuevo récord de estrellas.</summary>
        public static bool RecordLevelResult(string levelId, int stars, int coins)
        {
            if (D == null) return false;
            var lp = D.levels.Find(l => l.levelId == levelId);
            if (lp == null)
            {
                lp = new LevelProgress { levelId = levelId };
                D.levels.Add(lp);
            }
            bool improved = stars > lp.stars;
            lp.stars = Mathf.Max(lp.stars, Mathf.Clamp(stars, 0, 3));
            lp.bestCoins = Mathf.Max(lp.bestCoins, coins);
            Commit();
            return improved;
        }

        // ---------------- Infinito ----------------

        public static int EndlessBestScore => D != null ? D.endlessBestScore : 0;

        /// <summary>Devuelve true si es un nuevo récord.</summary>
        public static bool RecordEndlessResult(int score, float seconds)
        {
            if (D == null) return false;
            bool record = score > D.endlessBestScore;
            if (record) D.endlessBestScore = score;
            D.endlessBestTime = Mathf.Max(D.endlessBestTime, seconds);
            Commit();
            return record;
        }

        public static void RegisterMatchPlayed()
        {
            if (D == null) return;
            D.totalMatches++;
            Commit();
        }

        private static void Commit()
        {
            SaveSystem.Save();
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Changed = null;
    }
}
