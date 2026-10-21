using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using A = HyperArcade.EditorTools.PlaceholderArt;
using UB = HyperArcade.EditorTools.EditorUIBuilder;

namespace HyperArcade.EditorTools
{
    /// <summary>
    /// Asistente "un clic": crea capas, arte de marcador de posición, skin por defecto,
    /// contenido (personajes, armas, enemigos, niveles, consumibles), prefabs y escenas.
    /// Menú: Tools > HyperArcade.
    /// </summary>
    public static partial class HyperArcadeSetup
    {
        public const string Root = "Assets/_Game";
        private const string SkinDir = Root + "/Skins/Default";
        private const string ArtDir = SkinDir + "/Sprites";
        private const string AudioDir = SkinDir + "/Audio";
        private const string DataDir = Root + "/Data";
        private const string PrefabDir = Root + "/Prefabs";
        private const string SceneDir = Root + "/Scenes";
        private const string ResourcesDir = Root + "/Resources";

        private static readonly string[] Layers = { "Ground", "Player", "Enemy", "Pickup", "Projectile" };

        [MenuItem("Tools/HyperArcade/1. Crear contenido por defecto (todo)", priority = 0)]
        public static void CreateEverything()
        {
            if (!EditorUtility.DisplayDialog("HyperArcade",
                    "Se crearán (o sobrescribirán) en Assets/_Game: arte y sonidos de prueba, skin por defecto, " +
                    "datos de juego, prefabs y las escenas MainMenu y Game.\n\n¿Continuar?", "Crear", "Cancelar"))
                return;

            if (AssetDatabase.FindAssets("t:TMP_Settings").Length == 0)
            {
                EditorUtility.DisplayDialog("TextMeshPro",
                    "Primero importa los recursos esenciales de TextMeshPro:\n" +
                    "Window > TextMeshPro > Import TMP Essential Resources\n\nLuego vuelve a ejecutar este asistente.", "OK");
                return;
            }

            // El asistente crea escenas nuevas: guarda (o descarta, si el usuario lo decide) la escena abierta.
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);

            try
            {
                Progress("Capas", 0.05f);
                EnsureFolders();
                EnsureLayers();

                Progress("Arte y audio de prueba", 0.15f);
                var skin = CreateDefaultSkin();

                Progress("Datos de juego", 0.45f);
                var content = CreateContent();

                Progress("Prefabs", 0.6f);
                var prefabs = CreateGameplayPrefabs(skin);

                Progress("GameConfig", 0.7f);
                var config = CreateGameConfig(skin, content, prefabs);

                Progress("Escenas", 0.8f);
                var ui = CreateUIPrefabs(skin);
                string game = BuildGameScene(config);
                string menu = BuildMenuScene(config, ui);

                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(menu, true),
                    new EditorBuildSettingsScene(game, true)
                };
                AssetDatabase.SaveAssets();
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(menu);
                Debug.Log("[HyperArcade] ¡Listo! Pulsa Play en la escena MainMenu.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void Progress(string step, float t) => EditorUtility.DisplayProgressBar("HyperArcade", step, t);

        private static void EnsureFolders()
        {
            foreach (var d in new[] { ArtDir, AudioDir, DataDir + "/Characters", DataDir + "/Weapons", DataDir + "/Enemies",
                         DataDir + "/Levels", DataDir + "/Consumables", DataDir + "/Config", PrefabDir + "/UI", SceneDir, ResourcesDir })
                Directory.CreateDirectory(d);
            AssetDatabase.Refresh();
        }

        // ================================================================ Capas

        private static void EnsureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            foreach (string name in Layers)
            {
                bool exists = false;
                for (int i = 0; i < layers.arraySize; i++)
                    if (layers.GetArrayElementAtIndex(i).stringValue == name) { exists = true; break; }
                if (exists) continue;
                for (int i = 8; i < layers.arraySize; i++)
                {
                    var p = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(p.stringValue)) { p.stringValue = name; break; }
                }
            }
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static int Layer(string name) => Mathf.Max(0, LayerMask.NameToLayer(name));

        // ================================================================ Skin por defecto

        private static Sprite Spr(string name, A.Canvas c, int ppu, A.Pivot pivot = A.Pivot.Center, Vector4 border = default, bool fullRect = false) =>
            A.SaveSprite(c, $"{ArtDir}/{name}.png", ppu, pivot, border, fullRect);

        private static SpriteClip Clip(float fps, bool loop, params Sprite[] frames) =>
            new SpriteClip { frames = frames, fps = fps, loop = loop };

        private static VisualSet CharacterSet(string key, Color body, Color accent, bool angry = false, float width = 1f)
        {
            var i0 = Spr(key + "_idle0", A.Character(body, accent, 0, angry, width), 96, A.Pivot.BottomCenter);
            var i1 = Spr(key + "_idle1", A.Character(body, accent, 2, angry, width), 96, A.Pivot.BottomCenter);
            var r0 = Spr(key + "_run0", A.Character(body, accent, -3, angry, width), 96, A.Pivot.BottomCenter);
            var r1 = Spr(key + "_run1", A.Character(body, accent, 3, angry, width), 96, A.Pivot.BottomCenter);
            return new VisualSet
            {
                key = key, icon = i0,
                idle = Clip(3, true, i0, i1),
                run = Clip(10, true, r0, r1),
                jump = Clip(1, true, r1),
                death = Clip(1, false, r0),
                tint = Color.white,
                scale = Vector2.one
            };
        }

        private static VisualSet SimpleSet(string key, float fps, params Sprite[] frames) =>
            new VisualSet { key = key, icon = frames[0], idle = Clip(fps, true, frames), scale = Vector2.one, tint = Color.white };

        private static SkinProfile CreateDefaultSkin()
        {
            string path = SkinDir + "/Skin_Default.asset";
            var skin = AssetDatabase.LoadAssetAtPath<SkinProfile>(path);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<SkinProfile>();
                AssetDatabase.CreateAsset(skin, path);
            }

            skin.gameTitle = "Arcade Blaster";
            skin.companyName = "Tu Estudio";
            skin.slogan = "¡Dispara, salta y gana!";

            var dark = new Color(0.08f, 0.08f, 0.12f);
            skin.palette = new List<PaletteColor>
            {
                new PaletteColor { slot = PaletteSlot.Primary, color = new Color(1f, 0.55f, 0.1f) },
                new PaletteColor { slot = PaletteSlot.Secondary, color = new Color(0.2f, 0.5f, 0.95f) },
                new PaletteColor { slot = PaletteSlot.Accent, color = new Color(1f, 0.85f, 0.15f) },
                new PaletteColor { slot = PaletteSlot.Text, color = Color.white },
                new PaletteColor { slot = PaletteSlot.Background, color = new Color(0.12f, 0.14f, 0.22f) },
                new PaletteColor { slot = PaletteSlot.Danger, color = new Color(0.9f, 0.25f, 0.25f) },
            };

            // --- Personajes
            skin.characters = new List<VisualSet>
            {
                CharacterSet("player_hero", new Color(0.2f, 0.5f, 0.95f), new Color(1f, 0.85f, 0.15f)),
                CharacterSet("player_ninja", new Color(0.22f, 0.22f, 0.28f), new Color(0.95f, 0.2f, 0.25f), false, 0.85f),
                CharacterSet("player_tank", new Color(0.85f, 0.5f, 0.2f), new Color(0.4f, 0.4f, 0.45f), false, 1.25f),
            };

            // --- Enemigos
            var bat0 = Spr("enemy_bat_0", A.Bat(0), 96);
            var bat1 = Spr("enemy_bat_1", A.Bat(1), 96);
            var gun0 = Spr("enemy_gunner_0", A.Gunner(0), 96, A.Pivot.BottomCenter);
            var gun1 = Spr("enemy_gunner_1", A.Gunner(2), 96, A.Pivot.BottomCenter);
            var boss0 = Spr("enemy_boss_0", A.Boss(0), 96, A.Pivot.BottomCenter);
            var boss1 = Spr("enemy_boss_1", A.Boss(3), 96, A.Pivot.BottomCenter);
            var bossSet = SimpleSet("enemy_boss", 4, boss0, boss1);
            bossSet.scale = new Vector2(2f, 2f);
            skin.enemies = new List<VisualSet>
            {
                CharacterSet("enemy_grunt", new Color(0.9f, 0.25f, 0.25f), new Color(0.3f, 0.1f, 0.1f), true),
                CharacterSet("enemy_runner", new Color(1f, 0.6f, 0.15f), new Color(0.4f, 0.2f, 0.05f), true, 0.7f),
                SimpleSet("enemy_bat", 8, bat0, bat1),
                SimpleSet("enemy_gunner", 3, gun0, gun1),
                bossSet,
            };

            // --- Proyectiles, monedas, efectos
            var coinFrames = new[]
            {
                Spr("pickup_coin_0", A.Coin(1f), 128), Spr("pickup_coin_1", A.Coin(0.6f), 128),
                Spr("pickup_coin_2", A.Coin(0.2f), 128), Spr("pickup_coin_3", A.Coin(0.6f), 128)
            };
            skin.props = new List<VisualSet>
            {
                SimpleSet("proj_basic", 1, Spr("proj_basic", A.Projectile(new Color(1f, 0.95f, 0.5f), new Color(1f, 0.7f, 0.1f)), 100)),
                SimpleSet("proj_special", 1, Spr("proj_special", A.Orb(new Color(0.8f, 1f, 1f), new Color(0.2f, 0.8f, 1f), 48), 80)),
                SimpleSet("proj_enemy", 1, Spr("proj_enemy", A.Orb(new Color(1f, 0.8f, 0.8f), new Color(1f, 0.2f, 0.2f), 32), 100)),
                SimpleSet("pickup_coin", 10, coinFrames),
                SimpleSet("fx_shield", 1, Spr("fx_shield", A.ShieldBubble(), 100)),
            };
            skin.props[4].offset = new Vector2(0f, 0.7f);

            // --- Iconos de armas (tienda)
            skin.weaponIcons = new List<VisualSet>
            {
                SimpleSet("weapon_blaster", 1, Spr("weapon_blaster", A.Icon(new Color(1f, 0.6f, 0.15f), c => c.RoundRect(22, 40, 74, 56, 6, dark)), 96)),
                SimpleSet("weapon_shotgun", 1, Spr("weapon_shotgun", A.Icon(new Color(0.85f, 0.3f, 0.3f), c =>
                { c.Circle(48, 66, 9, dark); c.Circle(34, 40, 9, dark); c.Circle(62, 40, 9, dark); }), 96)),
                SimpleSet("weapon_laser", 1, Spr("weapon_laser", A.Icon(new Color(0.3f, 0.75f, 1f), c => c.RoundRect(14, 44, 82, 52, 4, Color.white)), 96)),
            };

            // --- UI
            var panel = Spr("ui_panel", A.Panel(Color.white, 96, 28), 100, A.Pivot.Center, new Vector4(32, 32, 32, 32));
            var button = Spr("ui_button", A.Panel(Color.white, 96, 34), 100, A.Pivot.Center, new Vector4(36, 36, 36, 36));
            Vector2[] up = { new Vector2(48, 74), new Vector2(72, 38), new Vector2(24, 38) };
            skin.uiSprites = new List<UiSprite>
            {
                Ui(UiSlot.Logo, Spr("ui_logo", A.Logo(), 100)),
                Ui(UiSlot.MenuBackground, Spr("ui_menu_bg", A.Gradient(16, 256, new Color(0.12f, 0.1f, 0.3f), new Color(0.35f, 0.7f, 1f)), 100, A.Pivot.Center, default, true)),
                Ui(UiSlot.PanelBackground, panel),
                Ui(UiSlot.ButtonBackground, button),
                Ui(UiSlot.CoinIcon, coinFrames[0]),
                Ui(UiSlot.DiamondIcon, Spr("ui_diamond", A.Diamond(), 100)),
                Ui(UiSlot.StarFull, Spr("ui_star_full", A.StarIcon(new Color(1f, 0.85f, 0.15f), dark), 100)),
                Ui(UiSlot.StarEmpty, Spr("ui_star_empty", A.StarIcon(new Color(0.35f, 0.35f, 0.4f), dark), 100)),
                Ui(UiSlot.LockIcon, Spr("ui_lock", A.Lock(), 100)),
                Ui(UiSlot.ShieldIcon, Spr("ui_shield", A.Icon(new Color(0.3f, 0.7f, 1f), c => c.Ring(48, 48, 24, 8, Color.white)), 100)),
                Ui(UiSlot.MultiplierIcon, Spr("ui_multiplier", A.Icon(new Color(1f, 0.75f, 0.1f), c =>
                { c.Circle(38, 44, 16, new Color(1f, 0.95f, 0.5f)); c.Circle(58, 54, 16, Color.white); }), 100)),
                Ui(UiSlot.ConverterIcon, Spr("ui_converter", A.Icon(new Color(0.6f, 0.35f, 0.9f), c =>
                { c.Ring(48, 48, 26, 6, Color.white); c.Circle(48, 48, 12, new Color(1f, 0.85f, 0.15f)); }), 100)),
                Ui(UiSlot.JoystickBase, Spr("ui_joy_base", JoyBase(), 100)),
                Ui(UiSlot.JoystickKnob, Spr("ui_joy_knob", A.Orb(Color.white, new Color(1f, 1f, 1f), 128), 100)),
                Ui(UiSlot.JumpButton, Spr("ui_btn_jump", A.Icon(new Color(0.25f, 0.75f, 0.35f), c => c.Polygon(up, Color.white)), 100)),
                Ui(UiSlot.AttackButton, Spr("ui_btn_attack", A.Icon(new Color(0.9f, 0.3f, 0.25f), c =>
                { c.Ring(48, 48, 22, 6, Color.white); c.Circle(48, 48, 6, Color.white); }), 100)),
                Ui(UiSlot.SpecialButton, Spr("ui_btn_special", A.Icon(new Color(0.6f, 0.35f, 0.9f), c => c.Polygon(A.Star(48, 46, 30, 13), Color.white)), 100)),
                Ui(UiSlot.PauseButton, Spr("ui_btn_pause", A.Icon(new Color(0.3f, 0.3f, 0.4f), c =>
                { c.RoundRect(32, 28, 44, 68, 3, Color.white); c.RoundRect(52, 28, 64, 68, 3, Color.white); }), 100)),
            };

            // --- Escenario
            skin.environments = new List<EnvironmentSet>
            {
                new EnvironmentSet
                {
                    key = "env_city",
                    cameraBackground = new Color(0.45f, 0.75f, 1f),
                    groundSprite = Spr("env_ground", A.GroundTile(), 64, A.Pivot.Center, default, true),
                    layers = new[]
                    {
                        new ParallaxLayer { sprite = Spr("env_sky", A.Gradient(16, 512, new Color(0.65f, 0.88f, 1f), new Color(0.25f, 0.5f, 0.95f)), 32, A.Pivot.Center, default, true), parallax = 1f, yOffset = 4f, sortingOrder = -100, tiledWidth = 160f },
                        new ParallaxLayer { sprite = Spr("env_hills_far", A.HillsLayer(512, 256, 150, 40, 2, 0.5f, new Color(0.55f, 0.7f, 0.95f)), 64, A.Pivot.Center, default, true), parallax = 0.8f, yOffset = 1f, sortingOrder = -90, tiledWidth = 160f },
                        new ParallaxLayer { sprite = Spr("env_hills_near", A.HillsLayer(512, 256, 110, 35, 3, 2f, new Color(0.35f, 0.65f, 0.4f)), 64, A.Pivot.Center, default, true), parallax = 0.5f, yOffset = 0f, sortingOrder = -80, tiledWidth = 160f },
                    }
                }
            };

            // --- Audio sintetizado
            skin.sfx = new List<SfxEntry>
            {
                Sfx(SfxId.ButtonClick, A.SaveTone(AudioDir + "/click.wav", 0.05f, t => 900f, 0.35f, true), 0.6f),
                Sfx(SfxId.Purchase, A.SaveTone(AudioDir + "/purchase.wav", 0.3f, t => t < 0.33f ? 660f : t < 0.66f ? 880f : 1320f, 0.4f, true)),
                Sfx(SfxId.Error, A.SaveTone(AudioDir + "/error.wav", 0.25f, t => 180f, 0.4f, true)),
                Sfx(SfxId.Shoot, A.SaveTone(AudioDir + "/shoot.wav", 0.08f, t => Mathf.Lerp(1400f, 500f, t), 0.25f, true), 0.5f, 0.12f),
                Sfx(SfxId.Special, A.SaveTone(AudioDir + "/special.wav", 0.35f, t => Mathf.Lerp(300f, 1200f, t), 0.4f, true, 0.2f)),
                Sfx(SfxId.Jump, A.SaveTone(AudioDir + "/jump.wav", 0.15f, t => Mathf.Lerp(400f, 900f, t), 0.3f), 0.6f),
                Sfx(SfxId.PlayerHit, A.SaveTone(AudioDir + "/hurt.wav", 0.2f, t => Mathf.Lerp(500f, 150f, t), 0.5f, true, 0.3f)),
                Sfx(SfxId.PlayerDie, A.SaveTone(AudioDir + "/die.wav", 0.6f, t => Mathf.Lerp(600f, 80f, t), 0.5f, true)),
                Sfx(SfxId.EnemyHit, A.SaveTone(AudioDir + "/enemy_hit.wav", 0.06f, t => 300f, 0.3f, false, 0.6f), 0.4f, 0.15f),
                Sfx(SfxId.EnemyDie, A.SaveTone(AudioDir + "/explode.wav", 0.3f, t => Mathf.Lerp(200f, 40f, t), 0.5f, false, 0.85f), 0.7f, 0.1f),
                Sfx(SfxId.Coin, A.SaveTone(AudioDir + "/coin.wav", 0.12f, t => t < 0.4f ? 988f : 1319f, 0.3f, true), 0.45f, 0.03f),
                Sfx(SfxId.Shield, A.SaveTone(AudioDir + "/shield.wav", 0.4f, t => 500f + Mathf.Sin(t * 40f) * 120f, 0.4f)),
                Sfx(SfxId.Converter, A.SaveTone(AudioDir + "/converter.wav", 0.5f, t => Mathf.Lerp(1500f, 300f, t), 0.4f, true)),
                Sfx(SfxId.BossAppear, A.SaveTone(AudioDir + "/boss.wav", 0.9f, t => 90f + Mathf.Sin(t * 30f) * 20f, 0.6f, true)),
                Sfx(SfxId.Victory, A.SaveTone(AudioDir + "/victory.wav", 0.8f, t => t < 0.25f ? 523f : t < 0.5f ? 659f : t < 0.75f ? 784f : 1047f, 0.4f, true)),
                Sfx(SfxId.Defeat, A.SaveTone(AudioDir + "/defeat.wav", 0.8f, t => t < 0.33f ? 392f : t < 0.66f ? 330f : 262f, 0.4f, true)),
                Sfx(SfxId.Star, A.SaveTone(AudioDir + "/star.wav", 0.2f, t => Mathf.Lerp(1200f, 1800f, t), 0.35f)),
            };
            float C = 262f, D = 294f, E = 330f, G = 392f, Am = 440f;
            skin.music = new List<MusicEntry>
            {
                new MusicEntry { id = MusicId.Menu, volume = 0.6f, clip = A.SaveMelody(AudioDir + "/music_menu.wav", new[] { C, E, G, E, D, G, C, 0f }, 0.3f) },
                new MusicEntry { id = MusicId.Gameplay, volume = 0.5f, clip = A.SaveMelody(AudioDir + "/music_game.wav", new[] { Am, Am, G, E, Am, Am, G, D, C, C, D, E, G, E, D, 0f }, 0.18f) },
            };

            skin.ClearCache();
            EditorUtility.SetDirty(skin);
            return skin;
        }

        private static A.Canvas JoyBase()
        {
            var c = new A.Canvas(256, 256);
            c.Circle(128, 128, 126, new Color(0f, 0f, 0f, 0.25f));
            c.Ring(128, 128, 120, 8, new Color(1f, 1f, 1f, 0.6f));
            return c;
        }

        private static UiSprite Ui(UiSlot slot, Sprite s) => new UiSprite { slot = slot, sprite = s };

        private static SfxEntry Sfx(SfxId id, AudioClip clip, float volume = 0.8f, float pitch = 0.05f) =>
            new SfxEntry { id = id, clips = new[] { clip }, volume = volume, pitchVariation = pitch };

        // ================================================================ Datos

        private class Content
        {
            public List<CharacterData> characters = new List<CharacterData>();
            public List<WeaponData> weapons = new List<WeaponData>();
            public List<ConsumableData> consumables = new List<ConsumableData>();
            public List<LevelData> levels = new List<LevelData>();
            public EconomyConfig economy;
            public AdsConfig ads;
            public EndlessConfig endless;
        }

        private static T Asset<T>(string path, Action<T> init) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            bool isNew = asset == null;
            if (isNew) asset = ScriptableObject.CreateInstance<T>();
            init(asset);
            if (isNew) AssetDatabase.CreateAsset(asset, path);
            else EditorUtility.SetDirty(asset);
            return asset;
        }

        private static Content CreateContent()
        {
            var c = new Content();

            // --- Armas
            var blaster = Asset<WeaponData>(DataDir + "/Weapons/Weapon_Blaster.asset", w =>
            {
                w.id = "blaster"; w.displayName = "Blaster"; w.description = "Equilibrada y fiable.";
                w.iconKey = "weapon_blaster"; w.projectileVisualKey = "proj_basic"; w.specialVisualKey = "proj_special";
                w.baseDamage = 10; w.damagePerLevel = 3; w.maxDamageLevel = 10;
                w.baseAttackDelay = 0.3f; w.delayReductionPerLevel = 0.015f; w.minAttackDelay = 0.12f; w.maxDelayLevel = 10;
                w.projectileSpeed = 18; w.projectileLifetime = 1.1f; w.projectilesPerShot = 1; w.spreadAngle = 0; w.pierce = 0;
                w.special = SpecialAbilityType.SpreadBurst; w.specialCooldown = 6; w.specialDamageMultiplier = 2.5f;
                w.unlockedByDefault = true; w.unlockPrice = new Price(CurrencyType.Coins, 0);
                w.baseUpgradeCost = 100; w.upgradeCostGrowth = 1.45f;
            });
            var shotgun = Asset<WeaponData>(DataDir + "/Weapons/Weapon_Shotgun.asset", w =>
            {
                w.id = "shotgun"; w.displayName = "Escopeta"; w.description = "3 balas en abanico. Especial: Nova.";
                w.iconKey = "weapon_shotgun"; w.projectileVisualKey = "proj_basic"; w.specialVisualKey = "proj_special";
                w.baseDamage = 7; w.damagePerLevel = 2; w.maxDamageLevel = 10;
                w.baseAttackDelay = 0.55f; w.delayReductionPerLevel = 0.025f; w.minAttackDelay = 0.25f; w.maxDelayLevel = 10;
                w.projectileSpeed = 16; w.projectileLifetime = 0.6f; w.projectilesPerShot = 3; w.spreadAngle = 22; w.pierce = 0;
                w.special = SpecialAbilityType.Nova; w.specialCooldown = 8; w.specialDamageMultiplier = 2f;
                w.unlockedByDefault = false; w.unlockPrice = new Price(CurrencyType.Coins, 1500);
                w.baseUpgradeCost = 150; w.upgradeCostGrowth = 1.45f;
            });
            var laser = Asset<WeaponData>(DataDir + "/Weapons/Weapon_Laser.asset", w =>
            {
                w.id = "laser"; w.displayName = "Láser"; w.description = "Atraviesa enemigos. Especial: rayo perforante.";
                w.iconKey = "weapon_laser"; w.projectileVisualKey = "proj_special"; w.specialVisualKey = "proj_special";
                w.baseDamage = 16; w.damagePerLevel = 4; w.maxDamageLevel = 10;
                w.baseAttackDelay = 0.42f; w.delayReductionPerLevel = 0.02f; w.minAttackDelay = 0.18f; w.maxDelayLevel = 10;
                w.projectileSpeed = 26; w.projectileLifetime = 0.9f; w.projectilesPerShot = 1; w.spreadAngle = 0; w.pierce = 2;
                w.special = SpecialAbilityType.PiercingShot; w.specialCooldown = 7; w.specialDamageMultiplier = 3f;
                w.unlockedByDefault = false; w.unlockPrice = new Price(CurrencyType.Diamonds, 60);
                w.baseUpgradeCost = 200; w.upgradeCostGrowth = 1.5f;
            });
            c.weapons.AddRange(new[] { blaster, shotgun, laser });

            // --- Personajes
            c.characters.Add(Asset<CharacterData>(DataDir + "/Characters/Character_Hero.asset", ch =>
            {
                ch.id = "hero"; ch.displayName = "Max"; ch.description = "Todoterreno."; ch.visualKey = "player_hero";
                ch.maxHealth = 100; ch.moveSpeed = 7; ch.jumpForce = 14; ch.extraJumps = 1; ch.damageMultiplier = 1f;
                ch.unlockedByDefault = true; ch.unlockPrice = new Price(CurrencyType.Coins, 0); ch.defaultWeapon = blaster;
            }));
            c.characters.Add(Asset<CharacterData>(DataDir + "/Characters/Character_Ninja.asset", ch =>
            {
                ch.id = "ninja"; ch.displayName = "Kira"; ch.description = "Rápida y con triple salto."; ch.visualKey = "player_ninja";
                ch.maxHealth = 80; ch.moveSpeed = 8.5f; ch.jumpForce = 15; ch.extraJumps = 2; ch.damageMultiplier = 1.1f;
                ch.unlockedByDefault = false; ch.unlockPrice = new Price(CurrencyType.Coins, 2000); ch.defaultWeapon = blaster;
            }));
            c.characters.Add(Asset<CharacterData>(DataDir + "/Characters/Character_Tank.asset", ch =>
            {
                ch.id = "tank"; ch.displayName = "Bruno"; ch.description = "Mucha vida y daño extra."; ch.visualKey = "player_tank";
                ch.maxHealth = 160; ch.moveSpeed = 6; ch.jumpForce = 13; ch.extraJumps = 1; ch.damageMultiplier = 1.25f;
                ch.unlockedByDefault = false; ch.unlockPrice = new Price(CurrencyType.Diamonds, 80); ch.defaultWeapon = blaster;
            }));

            // --- Enemigos
            EnemyData Enemy(string id, string name, EnemyBehavior b, float hp, float speed, float contact, Vector2 size, int minC, int maxC, int score, Action<EnemyData> extra = null) =>
                Asset<EnemyData>($"{DataDir}/Enemies/Enemy_{id}.asset", e =>
                {
                    e.id = id; e.displayName = name; e.visualKey = "enemy_" + id; e.behavior = b; e.isBoss = b == EnemyBehavior.Boss;
                    e.maxHealth = hp; e.moveSpeed = speed; e.contactDamage = contact; e.colliderSize = size;
                    e.minCoins = minC; e.maxCoins = maxC; e.scoreValue = score; e.projectileVisualKey = "proj_enemy";
                    extra?.Invoke(e);
                });

            var grunt = Enemy("grunt", "Soldado", EnemyBehavior.Walker, 30, 2.5f, 10, new Vector2(0.7f, 1.2f), 1, 3, 10);
            var runner = Enemy("runner", "Corredor", EnemyBehavior.Walker, 18, 4.5f, 8, new Vector2(0.55f, 1.1f), 1, 2, 10);
            var bat = Enemy("bat", "Murciélago", EnemyBehavior.Flyer, 20, 3f, 8, new Vector2(0.8f, 0.5f), 1, 3, 12);
            var gunner = Enemy("gunner", "Tirador", EnemyBehavior.Shooter, 35, 2f, 10, new Vector2(0.7f, 1.2f), 2, 4, 15, e =>
            {
                e.shootDelay = 2.2f; e.projectileDamage = 8; e.projectileSpeed = 8; e.preferredDistance = 6;
            });
            var boss = Enemy("boss", "Jefe Bruto", EnemyBehavior.Boss, 900, 2f, 20, new Vector2(1.5f, 2.5f), 40, 60, 300, e =>
            {
                e.shootDelay = 1f; e.projectileDamage = 12; e.projectileSpeed = 9;
            });

            // --- Consumibles
            c.consumables.Add(Asset<ConsumableData>(DataDir + "/Consumables/Consumable_CoinMultiplier.asset", x =>
            {
                x.type = ConsumableType.CoinMultiplier; x.displayName = "x2 Monedas"; x.description = "Duplica las monedas de toda la partida.";
                x.icon = UiSlot.MultiplierIcon; x.price = new Price(CurrencyType.Coins, 250); x.value = 2; x.amountPerPurchase = 1;
            }));
            c.consumables.Add(Asset<ConsumableData>(DataDir + "/Consumables/Consumable_Shield.asset", x =>
            {
                x.type = ConsumableType.Shield; x.displayName = "Escudo"; x.description = "Invulnerable durante 6 segundos.";
                x.icon = UiSlot.ShieldIcon; x.price = new Price(CurrencyType.Coins, 150); x.duration = 6; x.amountPerPurchase = 1;
            }));
            c.consumables.Add(Asset<ConsumableData>(DataDir + "/Consumables/Consumable_Converter.asset", x =>
            {
                x.type = ConsumableType.EnemyToCoins; x.displayName = "Convertidor"; x.description = "Convierte a los enemigos en pantalla en monedas.";
                x.icon = UiSlot.ConverterIcon; x.price = new Price(CurrencyType.Diamonds, 5); x.amountPerPurchase = 1;
            }));

            // --- Niveles (2–3 minutos cada uno)
            WaveDefinition W(float delay, float interval, params (EnemyData e, int n)[] groups)
            {
                var w = new WaveDefinition { delayBeforeWave = delay, spawnInterval = interval };
                foreach (var g in groups) w.groups.Add(new SpawnGroup { enemy = g.e, count = g.n });
                return w;
            }

            LevelData Level(int n, MissionType m, int target, float time, float hp, int maxAlive, int reward, WaveDefinition[] waves, EnemyData levelBoss = null, float bossTime = 40f) =>
                Asset<LevelData>($"{DataDir}/Levels/Level_{n:00}.asset", l =>
                {
                    l.id = $"level_{n:00}"; l.displayName = $"Nivel {n}"; l.environmentKey = "env_city";
                    l.mission = m; l.missionTarget = target; l.timeLimit = time;
                    l.waves = new List<WaveDefinition>(waves); l.loopWaves = true;
                    l.enemyHealthMultiplier = hp; l.maxEnemiesAlive = maxAlive;
                    l.boss = levelBoss; l.bossSpawnTime = bossTime;
                    l.twoStarsMinHealth = 0.4f; l.threeStarsMinHealth = 0.75f; l.threeStarsMaxTime = time * 0.75f;
                    l.completionCoins = reward;
                });

            c.levels.Add(Level(1, MissionType.KillEnemies, 15, 150, 1f, 6, 50, new[] { W(1.5f, 1.4f, (grunt, 6)), W(2f, 1.1f, (grunt, 8)) }));
            c.levels.Add(Level(2, MissionType.KillEnemies, 25, 150, 1f, 8, 60, new[] { W(1.5f, 1.1f, (grunt, 6), (runner, 3)), W(2f, 0.9f, (grunt, 6), (runner, 6)) }));
            c.levels.Add(Level(3, MissionType.Survive, 60, 60, 1f, 8, 70, new[] { W(1f, 1f, (bat, 4), (grunt, 4)), W(1.5f, 0.8f, (bat, 6), (runner, 4)) }));
            c.levels.Add(Level(4, MissionType.KillEnemies, 30, 160, 1.1f, 9, 80, new[] { W(1.5f, 1f, (grunt, 6), (gunner, 2)), W(2f, 0.9f, (runner, 5), (gunner, 3), (bat, 3)) }));
            c.levels.Add(Level(5, MissionType.DefeatBoss, 1, 170, 1f, 6, 150, new[] { W(1.5f, 1.3f, (grunt, 5), (bat, 3)) }, boss, 25f));
            c.levels.Add(Level(6, MissionType.Survive, 90, 90, 1.2f, 10, 100, new[] { W(1f, 0.8f, (grunt, 6), (bat, 4)), W(1f, 0.7f, (runner, 6), (gunner, 3)) }));
            c.levels.Add(Level(7, MissionType.KillEnemies, 40, 170, 1.3f, 11, 120, new[] { W(1f, 0.8f, (grunt, 6), (runner, 4), (gunner, 3)), W(1.5f, 0.7f, (bat, 6), (gunner, 4)) }));
            c.levels.Add(Level(8, MissionType.Survive, 120, 120, 1.4f, 12, 140, new[] { W(1f, 0.7f, (grunt, 8), (bat, 5)), W(1f, 0.6f, (runner, 8), (gunner, 4), (bat, 4)) }));
            c.levels.Add(Level(9, MissionType.DefeatBoss, 1, 180, 1.5f, 8, 250, new[] { W(1f, 1f, (grunt, 5), (gunner, 3), (bat, 3)) }, boss, 35f));

            // --- Configs
            c.economy = Asset<EconomyConfig>(DataDir + "/Config/EconomyConfig.asset", e => { });
            c.ads = Asset<AdsConfig>(DataDir + "/Config/AdsConfig.asset", a => { });
            c.endless = Asset<EndlessConfig>(DataDir + "/Config/EndlessConfig.asset", e =>
            {
                e.environmentKey = "env_city";
                e.enemies = new List<EndlessEnemyEntry>
                {
                    new EndlessEnemyEntry { enemy = grunt, availableFromSecond = 0, weight = 3 },
                    new EndlessEnemyEntry { enemy = runner, availableFromSecond = 20, weight = 2 },
                    new EndlessEnemyEntry { enemy = bat, availableFromSecond = 40, weight = 2 },
                    new EndlessEnemyEntry { enemy = gunner, availableFromSecond = 60, weight = 1.5f },
                };
                e.bosses = new List<EnemyData> { boss };
            });
            return c;
        }

        // ================================================================ Prefabs de juego

        private class Prefabs
        {
            public PlayerController player;
            public EnemyController enemy;
            public Projectile projectile;
            public Coin coin;
        }

        private static PhysicsMaterial2D Material2D(string path, float friction, float bounciness)
        {
            var m = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (m == null)
            {
                m = new PhysicsMaterial2D(Path.GetFileNameWithoutExtension(path));
                AssetDatabase.CreateAsset(m, path);
            }
            m.friction = friction;
            m.bounciness = bounciness;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static GameObject SavePrefab(GameObject go, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        /// <param name="previewKey">Solo para ver el prefab en el editor; en juego el sprite lo pone el skin.</param>
        private static SkinnedVisual AddVisual(GameObject parent, int sortingOrder, SkinProfile skin, string previewKey, string name = "Visual")
        {
            var v = new GameObject(name);
            v.transform.SetParent(parent.transform, false);
            v.layer = parent.layer;
            var sr = v.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;
            var preview = skin != null ? skin.FindVisual(previewKey) : null;
            if (preview != null) sr.sprite = preview.FirstSprite;
            return v.AddComponent<SkinnedVisual>();
        }

        private static Prefabs CreateGameplayPrefabs(SkinProfile skin)
        {
            var p = new Prefabs();
            var frictionless = Material2D(PrefabDir + "/NoFriction.physicsMaterial2D", 0f, 0f);
            var bouncy = Material2D(PrefabDir + "/CoinBounce.physicsMaterial2D", 0.4f, 0.45f);
            int groundMask = 1 << Layer("Ground");

            // --- Jugador
            {
                var go = new GameObject("Player") { layer = Layer("Player") };
                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 3f; rb.freezeRotation = true;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.sharedMaterial = frictionless;
                var col = go.AddComponent<CapsuleCollider2D>();
                col.size = new Vector2(0.7f, 1.3f); col.offset = new Vector2(0f, 0.65f);

                var visual = AddVisual(go, 10, skin, "player_hero");
                var groundCheck = new GameObject("GroundCheck").transform;
                groundCheck.SetParent(go.transform, false);
                groundCheck.localPosition = new Vector3(0f, 0.02f, 0f);
                var muzzle = new GameObject("Muzzle").transform;
                muzzle.SetParent(go.transform, false);
                muzzle.localPosition = new Vector3(0.45f, 0.7f, 0f);
                var shield = AddVisual(go, 11, skin, "fx_shield", "Shield");
                shield.gameObject.SetActive(false);

                var controller = go.AddComponent<PlayerController>();
                var health = go.AddComponent<PlayerHealth>();
                var weapon = go.AddComponent<WeaponController>();
                go.AddComponent<PlayerCombat>();

                UB.Set(controller, "visual", visual);
                UB.Set(controller, "groundCheck", groundCheck);
                UB.SetValue(controller, "groundMask", groundMask);
                UB.Set(health, "shieldVisual", shield);
                UB.Set(weapon, "muzzle", muzzle);
                p.player = SavePrefab(go, PrefabDir + "/Player.prefab").GetComponent<PlayerController>();
            }

            // --- Enemigo genérico
            {
                var go = new GameObject("Enemy") { layer = Layer("Enemy") };
                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 3f; rb.freezeRotation = true;
                rb.sharedMaterial = frictionless;
                go.AddComponent<BoxCollider2D>();
                var visual = AddVisual(go, 5, skin, "enemy_grunt");
                var enemy = go.AddComponent<EnemyController>();
                UB.Set(enemy, "visual", visual);
                p.enemy = SavePrefab(go, PrefabDir + "/Enemy.prefab").GetComponent<EnemyController>();
            }

            // --- Proyectil genérico
            {
                var go = new GameObject("Projectile") { layer = Layer("Projectile") };
                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true; col.radius = 0.15f;
                var visual = AddVisual(go, 20, skin, "proj_basic");
                var proj = go.AddComponent<Projectile>();
                UB.Set(proj, "visual", visual);
                UB.SetValue(proj, "obstacleMask", groundMask);
                p.projectile = SavePrefab(go, PrefabDir + "/Projectile.prefab").GetComponent<Projectile>();
            }

            // --- Moneda
            {
                var go = new GameObject("Coin") { layer = Layer("Pickup") };
                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 2.5f; rb.freezeRotation = true;
                rb.sharedMaterial = bouncy;
                var col = go.AddComponent<CircleCollider2D>();
                col.radius = 0.18f;
                var visual = AddVisual(go, 15, skin, "pickup_coin");
                var coin = go.AddComponent<Coin>();
                UB.Set(coin, "visual", visual);
                p.coin = SavePrefab(go, PrefabDir + "/Coin.prefab").GetComponent<Coin>();
            }
            return p;
        }

        private static GameConfig CreateGameConfig(SkinProfile skin, Content c, Prefabs p) =>
            Asset<GameConfig>(ResourcesDir + "/GameConfig.asset", g =>
            {
                g.activeSkin = skin;
                g.fallbackSkin = skin;
                g.economy = c.economy;
                g.ads = c.ads;
                g.endless = c.endless;
                g.characters = c.characters;
                g.weapons = c.weapons;
                g.consumables = c.consumables;
                g.levels = c.levels;
                g.playerPrefab = p.player;
                g.enemyPrefab = p.enemy;
                g.projectilePrefab = p.projectile;
                g.coinPrefab = p.coin;
            });
    }
}
