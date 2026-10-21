using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Guardado local en JSON.
    ///  - Móvil / PC: archivo en Application.persistentDataPath con copia de respaldo (.bak).
    ///  - WebGL: JSON dentro de PlayerPrefs (no hay sistema de archivos persistente fiable).
    /// Incluye un hash con sal para detectar ediciones manuales del archivo (anti-trampas básico).
    /// </summary>
    public static class SaveSystem
    {
        private const string FileName = "save.json";
        private const string BackupFileName = "save.bak";
        private const string PrefsKey = "HA_SAVE_V1";
        // Cambia la sal en cada versión de marca blanca para que los guardados no sean intercambiables.
        private const string Salt = "HyperArcade::cambia-esta-sal";

        [Serializable]
        private class SaveEnvelope
        {
            public int version;
            public string payload;
            public string hash;
        }

        public static SaveData Data { get; private set; }
        public static bool IsLoaded => Data != null;

        /// <summary>Se dispara tras cada guardado exitoso.</summary>
        public static event Action Saved;

        private static string MainPath => Path.Combine(Application.persistentDataPath, FileName);
        private static string BackupPath => Path.Combine(Application.persistentDataPath, BackupFileName);

        private static bool UsePlayerPrefs
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public static void Load()
        {
            SaveData loaded;
            if (UsePlayerPrefs)
            {
                loaded = Decode(PlayerPrefs.GetString(PrefsKey, null));
            }
            else
            {
                loaded = Decode(ReadFile(MainPath));
                if (loaded == null)
                {
                    loaded = Decode(ReadFile(BackupPath));
                    if (loaded != null) Debug.LogWarning("[Save] Guardado principal dañado: se restauró la copia de respaldo.");
                }
            }

            Data = loaded ?? CreateNew();
            Migrate(Data);
            EnsureDefaults(Data);
        }

        public static void Save()
        {
            if (Data == null) return;
            try
            {
                string encoded = Encode(Data);
                if (UsePlayerPrefs)
                {
                    PlayerPrefs.SetString(PrefsKey, encoded);
                    PlayerPrefs.Save();
                }
                else
                {
                    // Escritura segura: tmp -> respaldo del anterior -> reemplazo.
                    string tmp = MainPath + ".tmp";
                    File.WriteAllText(tmp, encoded, Encoding.UTF8);
                    if (File.Exists(MainPath))
                    {
                        File.Copy(MainPath, BackupPath, true);
                        File.Delete(MainPath);
                    }
                    File.Move(tmp, MainPath);
                }
                Saved?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError("[Save] Error al guardar: " + e.Message);
            }
        }

        /// <summary>Borra el progreso (botón de depuración / ajustes).</summary>
        public static void DeleteAll()
        {
            if (UsePlayerPrefs) PlayerPrefs.DeleteKey(PrefsKey);
            else
            {
                if (File.Exists(MainPath)) File.Delete(MainPath);
                if (File.Exists(BackupPath)) File.Delete(BackupPath);
            }
            Data = CreateNew();
            EnsureDefaults(Data);
            Save();
        }

        // ---------------------------------------------------------------------

        private static SaveData CreateNew()
        {
            var data = new SaveData();
            var config = GameConfig.Instance;
            if (config != null && config.economy != null)
            {
                data.coins = config.economy.startingCoins;
                data.diamonds = config.economy.startingDiamonds;
            }
            return data;
        }

        /// <summary>
        /// Garantiza contenido por defecto aunque se añadan personajes/armas en actualizaciones.
        /// </summary>
        private static void EnsureDefaults(SaveData data)
        {
            var config = GameConfig.Instance;
            if (config == null) return;

            foreach (var c in config.characters)
                if (c != null && c.unlockedByDefault && !data.unlockedCharacters.Contains(c.id))
                    data.unlockedCharacters.Add(c.id);

            foreach (var w in config.weapons)
            {
                if (w == null) continue;
                var wp = data.weapons.Find(x => x.weaponId == w.id);
                if (wp == null)
                {
                    wp = new WeaponProgress { weaponId = w.id };
                    data.weapons.Add(wp);
                }
                if (w.unlockedByDefault) wp.unlocked = true;
            }

            if (string.IsNullOrEmpty(data.selectedCharacterId) || !data.unlockedCharacters.Contains(data.selectedCharacterId))
                data.selectedCharacterId = data.unlockedCharacters.Count > 0 ? data.unlockedCharacters[0] : null;

            var selectedWeapon = data.weapons.Find(x => x.weaponId == data.selectedWeaponId);
            if (selectedWeapon == null || !selectedWeapon.unlocked)
            {
                var firstUnlocked = data.weapons.Find(x => x.unlocked);
                data.selectedWeaponId = firstUnlocked != null ? firstUnlocked.weaponId : null;
            }
        }

        private static void Migrate(SaveData data)
        {
            // Ejemplo de migración futura:
            // if (data.version < 2) { ...; data.version = 2; }
            data.version = SaveData.CurrentVersion;
            if (data.ads == null) data.ads = new AdCounters();
            if (data.settings == null) data.settings = new SettingsData();
        }

        private static string Encode(SaveData data)
        {
            string json = JsonUtility.ToJson(data);
            var envelope = new SaveEnvelope { version = data.version, payload = json, hash = Hash(json) };
            return JsonUtility.ToJson(envelope, true);
        }

        private static SaveData Decode(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            try
            {
                var envelope = JsonUtility.FromJson<SaveEnvelope>(raw);
                if (envelope == null || string.IsNullOrEmpty(envelope.payload)) return null;
                if (envelope.hash != Hash(envelope.payload))
                {
                    Debug.LogWarning("[Save] Hash inválido: el guardado fue modificado o está dañado.");
                    return null;
                }
                return JsonUtility.FromJson<SaveData>(envelope.payload);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] No se pudo leer el guardado: " + e.Message);
                return null;
            }
        }

        private static string ReadFile(string path)
        {
            try { return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null; }
            catch (Exception) { return null; }
        }

        private static string Hash(string text)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text + Salt));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
