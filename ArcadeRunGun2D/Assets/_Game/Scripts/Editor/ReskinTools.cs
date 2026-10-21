using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build;
#endif

namespace HyperArcade.EditorTools
{
    /// <summary>
    /// Flujo de reventa / marca blanca (objetivo: reskin completo en menos de 1 hora):
    ///   1) "Crear skin de cliente": genera un SkinProfile vacío + carpetas + lista de recursos.
    ///   2) El artista arrastra sprites/sonidos a las ranuras (lo vacío hereda del skin por defecto).
    ///   3) "Validar skin activo": informa de lo que falta o se está heredando.
    ///   4) Cambiar IDs de anuncios en AdsConfig, nombre/icono en Player Settings y compilar.
    /// </summary>
    public static class ReskinTools
    {
        private static GameConfig LoadConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(HyperArcadeSetup.Root + "/Resources/GameConfig.asset");
            if (config == null)
                EditorUtility.DisplayDialog("HyperArcade", "No existe GameConfig. Ejecuta primero 'Tools > HyperArcade > 1. Crear contenido por defecto'.", "OK");
            return config;
        }

        // ------------------------------------------------------------ Validación

        [MenuItem("Tools/HyperArcade/2. Validar skin activo", priority = 20)]
        public static void ValidateActiveSkin()
        {
            var config = LoadConfig();
            if (config == null) return;
            var active = config.activeSkin;
            var fallback = config.fallbackSkin;
            if (active == null) { EditorUtility.DisplayDialog("HyperArcade", "GameConfig no tiene activeSkin asignado.", "OK"); return; }
            active.ClearCache();
            if (fallback != null) fallback.ClearCache();

            var missing = new List<string>();
            var inherited = new List<string>();

            void CheckVisual(string key, string owner)
            {
                if (string.IsNullOrEmpty(key)) return;
                if (active.FindVisual(key) != null) return;
                if (fallback != null && fallback != active && fallback.FindVisual(key) != null) inherited.Add($"{key}  ({owner})");
                else missing.Add($"{key}  ({owner})");
            }

            foreach (var c in config.characters.Where(x => x != null)) CheckVisual(c.visualKey, "Personaje " + c.displayName);
            foreach (var w in config.weapons.Where(x => x != null))
            {
                CheckVisual(w.iconKey, "Icono arma " + w.displayName);
                CheckVisual(w.projectileVisualKey, "Proyectil " + w.displayName);
                CheckVisual(w.specialVisualKey, "Especial " + w.displayName);
            }
            var enemies = new HashSet<EnemyData>();
            foreach (var l in config.levels.Where(x => x != null))
            {
                foreach (var wave in l.waves) foreach (var g in wave.groups) if (g.enemy != null) enemies.Add(g.enemy);
                if (l.boss != null) enemies.Add(l.boss);
            }
            if (config.endless != null)
            {
                foreach (var e in config.endless.enemies) if (e.enemy != null) enemies.Add(e.enemy);
                foreach (var b in config.endless.bosses) if (b != null) enemies.Add(b);
            }
            foreach (var e in enemies)
            {
                CheckVisual(e.visualKey, "Enemigo " + e.displayName);
                if (e.behavior == EnemyBehavior.Shooter || e.isBoss) CheckVisual(e.projectileVisualKey, "Proyectil de " + e.displayName);
            }
            CheckVisual("pickup_coin", "Moneda");
            CheckVisual("fx_shield", "Escudo");

            foreach (UiSlot slot in System.Enum.GetValues(typeof(UiSlot)))
            {
                if (active.FindUiSprite(slot) != null) continue;
                if (fallback != null && fallback != active && fallback.FindUiSprite(slot) != null) inherited.Add("UI " + slot);
                else missing.Add("UI " + slot);
            }

            foreach (SfxId id in System.Enum.GetValues(typeof(SfxId)))
                if (active.FindSfx(id) == null && (fallback == null || fallback.FindSfx(id) == null)) missing.Add("Sonido " + id);

            var envKeys = new HashSet<string>(config.levels.Where(l => l != null).Select(l => l.environmentKey));
            if (config.endless != null) envKeys.Add(config.endless.environmentKey);
            foreach (var key in envKeys)
            {
                if (active.FindEnvironment(key) != null) continue;
                if (fallback != null && fallback != active && fallback.FindEnvironment(key) != null) inherited.Add("Escenario " + key);
                else missing.Add("Escenario " + key);
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Skin activo: {active.name}  ·  Título: {active.gameTitle}");
            sb.AppendLine(missing.Count == 0 ? "✔ No falta ningún recurso." : $"✖ Faltan {missing.Count} recursos (se verán vacíos):");
            foreach (var m in missing) sb.AppendLine("   - " + m);
            if (inherited.Count > 0)
            {
                sb.AppendLine($"ℹ {inherited.Count} recursos se heredan del skin de respaldo ({(fallback != null ? fallback.name : "-")}):");
                foreach (var i in inherited) sb.AppendLine("   - " + i);
            }
            Debug.Log("[HyperArcade] Validación de skin\n" + sb);
            EditorUtility.DisplayDialog("Validación de skin",
                missing.Count == 0
                    ? $"Todo correcto. {inherited.Count} recursos heredados del skin por defecto.\n(Detalle en la consola)"
                    : $"Faltan {missing.Count} recursos. {inherited.Count} heredados.\n(Detalle en la consola)", "OK");
        }

        // ------------------------------------------------------------ Nuevo cliente

        [MenuItem("Tools/HyperArcade/3. Crear skin de cliente (plantilla)", priority = 21)]
        public static void CreateClientSkin()
        {
            var config = LoadConfig();
            if (config == null) return;
            string path = EditorUtility.SaveFilePanelInProject("Nuevo skin de cliente", "Skin_Cliente", "asset",
                "Elige carpeta y nombre (ej. Assets/_Game/Skins/Pizzeria/Skin_Pizzeria.asset)", HyperArcadeSetup.Root + "/Skins");
            if (string.IsNullOrEmpty(path)) return;

            var template = config.fallbackSkin != null ? config.fallbackSkin : config.activeSkin;
            var skin = ScriptableObject.CreateInstance<SkinProfile>();
            skin.gameTitle = "Nombre del Cliente";
            skin.companyName = "Cliente";
            skin.slogan = template != null ? template.slogan : "";

            if (template != null)
            {
                skin.palette = template.palette.Select(p => new PaletteColor { slot = p.slot, color = p.color }).ToList();
                skin.uiSprites = template.uiSprites.Select(u => new UiSprite { slot = u.slot }).ToList();
                skin.characters = template.characters.Select(EmptyCopy).ToList();
                skin.enemies = template.enemies.Select(EmptyCopy).ToList();
                skin.props = template.props.Select(EmptyCopy).ToList();
                skin.weaponIcons = template.weaponIcons.Select(EmptyCopy).ToList();
                skin.sfx = template.sfx.Select(s => new SfxEntry { id = s.id, volume = s.volume, pitchVariation = s.pitchVariation, clips = new AudioClip[0] }).ToList();
                skin.music = template.music.Select(m => new MusicEntry { id = m.id, volume = m.volume }).ToList();
                // Los escenarios se heredan; añade uno con la misma clave para reemplazarlo.
            }

            string folder = Path.GetDirectoryName(path);
            Directory.CreateDirectory(Path.Combine(folder, "Sprites"));
            Directory.CreateDirectory(Path.Combine(folder, "Audio"));
            File.WriteAllText(Path.Combine(folder, "LEEME_RECURSOS.txt"), BuildChecklist(template));
            AssetDatabase.CreateAsset(skin, path);
            AssetDatabase.Refresh();

            if (EditorUtility.DisplayDialog("HyperArcade", "Skin creado. ¿Usarlo ahora como skin activo de la build?", "Sí", "No"))
            {
                config.activeSkin = skin;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = skin;
            EditorGUIUtility.PingObject(skin);
        }

        private static VisualSet EmptyCopy(VisualSet v) => new VisualSet
        {
            key = v.key, scale = v.scale, offset = v.offset, facesRight = true, tint = Color.white,
            idle = new SpriteClip { fps = v.idle.fps, loop = true, frames = new Sprite[0] },
            run = new SpriteClip { fps = v.run.fps, loop = true, frames = new Sprite[0] },
            jump = new SpriteClip { fps = 1, loop = true, frames = new Sprite[0] },
            fall = new SpriteClip { fps = 1, loop = true, frames = new Sprite[0] },
            attack = new SpriteClip { fps = 12, loop = false, frames = new Sprite[0] },
            hurt = new SpriteClip { fps = 1, loop = false, frames = new Sprite[0] },
            death = new SpriteClip { fps = 8, loop = false, frames = new Sprite[0] },
        };

        private static string BuildChecklist(SkinProfile template)
        {
            var sb = new StringBuilder();
            sb.AppendLine("RECURSOS PARA EL RESKIN (lo que no rellenes se hereda del skin por defecto)");
            sb.AppendLine("=========================================================================");
            sb.AppendLine("Formato: PNG con transparencia. Personajes/enemigos con pivote 'Bottom Center'.");
            sb.AppendLine("Tamaños orientativos (Pixels Per Unit = 96 en personajes):");
            sb.AppendLine("  Personaje/enemigo: 96x128 px por fotograma (jefe: igual, se escala x2)");
            sb.AppendLine("  Proyectiles: 32-64 px · Moneda: 48 px (4 fotogramas girando)");
            sb.AppendLine("  Fondos: 512x256 px por capa (Mesh Type = Full Rect) · Suelo: 64x64 en mosaico");
            sb.AppendLine("  Logo: 512x256 · Iconos UI: 96x96 · Paneles/botones: 9-slice");
            sb.AppendLine();
            if (template == null) return sb.ToString();
            sb.AppendLine("CLAVES DE ENTIDADES (VisualSet.key)");
            foreach (var v in template.characters.Concat(template.enemies).Concat(template.props).Concat(template.weaponIcons))
                sb.AppendLine("  - " + v.key);
            sb.AppendLine();
            sb.AppendLine("RANURAS DE UI");
            foreach (var u in template.uiSprites) sb.AppendLine("  - " + u.slot);
            sb.AppendLine();
            sb.AppendLine("SONIDOS");
            foreach (var s in template.sfx) sb.AppendLine("  - " + s.id);
            foreach (var m in template.music) sb.AppendLine("  - Música " + m.id);
            sb.AppendLine();
            sb.AppendLine("ESCENARIOS (añadir un EnvironmentSet con la misma clave para reemplazar)");
            foreach (var e in template.environments) sb.AppendLine("  - " + e.key);
            sb.AppendLine();
            sb.AppendLine("CHECKLIST DE BUILD");
            sb.AppendLine("  [ ] Título, empresa y eslogan en el SkinProfile");
            sb.AppendLine("  [ ] Player Settings: Company Name, Product Name, Package Name, icono");
            sb.AppendLine("  [ ] AdsConfig: IDs reales del cliente (AdMob / Unity Ads) y testMode = false");
            sb.AppendLine("  [ ] Cambiar la sal de SaveSystem (opcional)");
            sb.AppendLine("  [ ] Tools > HyperArcade > 2. Validar skin activo");
            return sb.ToString();
        }

        [MenuItem("Tools/HyperArcade/4. Usar skin seleccionado", priority = 22)]
        public static void UseSelectedSkin()
        {
            var skin = Selection.activeObject as SkinProfile;
            var config = LoadConfig();
            if (config == null) return;
            if (skin == null) { EditorUtility.DisplayDialog("HyperArcade", "Selecciona un SkinProfile en la ventana Project.", "OK"); return; }
            config.activeSkin = skin;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log("[HyperArcade] Skin activo: " + skin.name);
        }

        [MenuItem("Tools/HyperArcade/4. Usar skin seleccionado", true)]
        private static bool UseSelectedSkinValidate() => Selection.activeObject is SkinProfile;

        // ------------------------------------------------------------ Utilidades

        [MenuItem("Tools/HyperArcade/Borrar partida guardada", priority = 40)]
        public static void DeleteSave()
        {
            foreach (var f in new[] { "save.json", "save.bak", "save.json.tmp" })
            {
                string p = Path.Combine(Application.persistentDataPath, f);
                if (File.Exists(p)) File.Delete(p);
            }
            PlayerPrefs.DeleteKey("HA_SAVE_V1");
            Debug.Log("[HyperArcade] Guardado borrado: " + Application.persistentDataPath);
        }

        [MenuItem("Tools/HyperArcade/Anuncios/Activar AdMob (HA_ADMOB)", priority = 60)]
        private static void EnableAdMob() => ToggleDefine("HA_ADMOB", true, "HA_UNITY_ADS");

        [MenuItem("Tools/HyperArcade/Anuncios/Activar Unity Ads (HA_UNITY_ADS)", priority = 61)]
        private static void EnableUnityAds() => ToggleDefine("HA_UNITY_ADS", true, "HA_ADMOB");

        [MenuItem("Tools/HyperArcade/Anuncios/Desactivar SDKs (solo Mock)", priority = 62)]
        private static void DisableAds()
        {
            ToggleDefine("HA_ADMOB", false, null);
            ToggleDefine("HA_UNITY_ADS", false, null);
        }

        private static void ToggleDefine(string symbol, bool enable, string removeOther)
        {
            foreach (var group in new[] { BuildTargetGroup.Android, BuildTargetGroup.iOS })
            {
                var defines = GetDefines(group).Split(';').Where(d => !string.IsNullOrWhiteSpace(d)).ToList();
                defines.Remove(symbol);
                if (removeOther != null) defines.Remove(removeOther);
                if (enable) defines.Add(symbol);
                SetDefines(group, string.Join(";", defines));
            }
            Debug.Log($"[HyperArcade] Símbolo {symbol} {(enable ? "activado" : "desactivado")} en Android/iOS.");
        }

        private static string GetDefines(BuildTargetGroup group)
        {
#if UNITY_2021_2_OR_NEWER
            return PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group));
#else
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
#endif
        }

        private static void SetDefines(BuildTargetGroup group, string defines)
        {
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group), defines);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, defines);
#endif
        }
    }
}
