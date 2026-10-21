using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Punto único de acceso a los recursos visuales/sonoros.
    /// Busca primero en el skin activo y, si falta, en el de respaldo (reskins parciales).
    /// </summary>
    public static class SkinManager
    {
        public static SkinProfile Active
        {
            get
            {
                var cfg = GameConfig.Instance;
                if (cfg == null) return null;
                return cfg.activeSkin != null ? cfg.activeSkin : cfg.fallbackSkin;
            }
        }

        private static SkinProfile Fallback
        {
            get
            {
                var cfg = GameConfig.Instance;
                return cfg != null && cfg.fallbackSkin != Active ? cfg.fallbackSkin : null;
            }
        }

        public static VisualSet GetVisual(string key)
        {
            var v = Active != null ? Active.FindVisual(key) : null;
            if (v == null && Fallback != null) v = Fallback.FindVisual(key);
            if (v == null && !string.IsNullOrEmpty(key)) Debug.LogWarning($"[Skin] No hay VisualSet para la clave '{key}'.");
            return v;
        }

        public static Sprite GetIcon(string key)
        {
            var v = GetVisual(key);
            return v == null ? null : (v.icon != null ? v.icon : v.FirstSprite);
        }

        public static EnvironmentSet GetEnvironment(string key)
        {
            var e = Active != null ? Active.FindEnvironment(key) : null;
            if (e == null && Fallback != null) e = Fallback.FindEnvironment(key);
            if (e == null && Active != null && Active.environments.Count > 0) e = Active.environments[0];
            return e;
        }

        public static Sprite GetUiSprite(UiSlot slot)
        {
            var s = Active != null ? Active.FindUiSprite(slot) : null;
            if (s == null && Fallback != null) s = Fallback.FindUiSprite(slot);
            return s;
        }

        public static bool TryGetColor(PaletteSlot slot, out Color color)
        {
            if (Active != null && Active.TryGetColor(slot, out color)) return true;
            if (Fallback != null && Fallback.TryGetColor(slot, out color)) return true;
            color = Color.white;
            return false;
        }

        public static SfxEntry GetSfx(SfxId id)
        {
            var s = Active != null ? Active.FindSfx(id) : null;
            if (s == null && Fallback != null) s = Fallback.FindSfx(id);
            return s;
        }

        public static MusicEntry GetMusic(MusicId id)
        {
            var m = Active != null ? Active.FindMusic(id) : null;
            if (m == null && Fallback != null) m = Fallback.FindMusic(id);
            return m;
        }

        public static string GetText(BrandTextSlot slot) => Active != null ? Active.GetText(slot) : string.Empty;

        public static TMPro.TMP_FontAsset Font
        {
            get
            {
                if (Active != null && Active.font != null) return Active.font;
                return Fallback != null ? Fallback.font : null;
            }
        }
    }
}
