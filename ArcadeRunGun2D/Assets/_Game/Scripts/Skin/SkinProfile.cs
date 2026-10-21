using System.Collections.Generic;
using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// TODO el contenido visual y sonoro de una versión del juego (marca blanca).
    /// La lógica nunca referencia sprites: solo claves que se resuelven aquí.
    ///
    /// Reskin para un cliente = duplicar este asset, arrastrar los sprites/sonidos nuevos
    /// y asignarlo en GameConfig.activeSkin. Lo que no se rellene se toma del skin de respaldo.
    /// </summary>
    [CreateAssetMenu(menuName = "HyperArcade/Skin Profile", fileName = "Skin_")]
    public class SkinProfile : ScriptableObject
    {
        [Header("Marca")]
        public string gameTitle = "Arcade Blaster";
        public string companyName = "Tu Estudio";
        public string slogan = "¡Dispara, salta y gana!";
        public TMPro.TMP_FontAsset font;

        [Header("Paleta de colores de la UI")]
        public List<PaletteColor> palette = new List<PaletteColor>();

        [Header("Sprites de interfaz")]
        public List<UiSprite> uiSprites = new List<UiSprite>();

        [Header("Entidades (claves = visualKey de los datos)")]
        public List<VisualSet> characters = new List<VisualSet>();
        public List<VisualSet> enemies = new List<VisualSet>();
        [Tooltip("Proyectiles, monedas, escudo y efectos (fx_shield, fx_hit, pickup_coin...).")]
        public List<VisualSet> props = new List<VisualSet>();
        [Tooltip("Iconos de armas para la tienda (clave = WeaponData.iconKey).")]
        public List<VisualSet> weaponIcons = new List<VisualSet>();

        [Header("Escenarios (clave = LevelData.environmentKey)")]
        public List<EnvironmentSet> environments = new List<EnvironmentSet>();

        [Header("Audio")]
        public List<MusicEntry> music = new List<MusicEntry>();
        public List<SfxEntry> sfx = new List<SfxEntry>();

        // ---------------- Búsquedas (con caché) ----------------

        private Dictionary<string, VisualSet> _visuals;
        private Dictionary<string, EnvironmentSet> _environments;

        public VisualSet FindVisual(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_visuals == null)
            {
                _visuals = new Dictionary<string, VisualSet>();
                AddAll(characters); AddAll(enemies); AddAll(props); AddAll(weaponIcons);
            }
            return _visuals.TryGetValue(key, out var v) ? v : null;
        }

        public EnvironmentSet FindEnvironment(string key)
        {
            if (_environments == null)
            {
                _environments = new Dictionary<string, EnvironmentSet>();
                foreach (var e in environments)
                    if (e != null && !string.IsNullOrEmpty(e.key)) _environments[e.key] = e;
            }
            return key != null && _environments.TryGetValue(key, out var env) ? env : null;
        }

        public Sprite FindUiSprite(UiSlot slot)
        {
            var entry = uiSprites.Find(u => u.slot == slot);
            return entry != null ? entry.sprite : null;
        }

        public bool TryGetColor(PaletteSlot slot, out Color color)
        {
            var entry = palette.Find(p => p.slot == slot);
            color = entry != null ? entry.color : Color.white;
            return entry != null;
        }

        public SfxEntry FindSfx(SfxId id) => sfx.Find(s => s.id == id && s.clips != null && s.clips.Length > 0);
        public MusicEntry FindMusic(MusicId id) => music.Find(m => m.id == id && m.clip != null);

        public string GetText(BrandTextSlot slot)
        {
            switch (slot)
            {
                case BrandTextSlot.CompanyName: return companyName;
                case BrandTextSlot.Slogan: return slogan;
                default: return gameTitle;
            }
        }

        private void AddAll(List<VisualSet> list)
        {
            foreach (var v in list)
                if (v != null && !string.IsNullOrEmpty(v.key) && v.idle != null && v.idle.IsValid) _visuals[v.key] = v;
        }

        /// <summary>Invalida las cachés de búsqueda (tras editar las listas por código).</summary>
        public void ClearCache()
        {
            _visuals = null;
            _environments = null;
        }

        private void OnValidate() => ClearCache();
    }
}
