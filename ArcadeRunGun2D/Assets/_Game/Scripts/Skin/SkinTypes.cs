using System;
using UnityEngine;

namespace HyperArcade
{
    /// <summary>Animación por fotogramas: basta con arrastrar los sprites (sin Animator).</summary>
    [Serializable]
    public class SpriteClip
    {
        public Sprite[] frames;
        public float fps = 10f;
        public bool loop = true;

        public bool IsValid => frames != null && frames.Length > 0 && frames[0] != null;
    }

    /// <summary>
    /// Aspecto completo de una entidad (personaje, enemigo, proyectil, moneda, FX).
    /// Solo "idle" es obligatorio; el resto cae en cascada a idle si está vacío.
    /// </summary>
    [Serializable]
    public class VisualSet
    {
        [Tooltip("Clave usada por los datos de lógica (CharacterData.visualKey, EnemyData.visualKey...).")]
        public string key;
        [Tooltip("Retrato / icono para tienda y HUD.")]
        public Sprite icon;
        public SpriteClip idle = new SpriteClip();
        public SpriteClip run = new SpriteClip();
        public SpriteClip jump = new SpriteClip();
        public SpriteClip fall = new SpriteClip();
        public SpriteClip attack = new SpriteClip();
        public SpriteClip hurt = new SpriteClip();
        public SpriteClip death = new SpriteClip();

        [Tooltip("Escala visual (no afecta colisiones).")]
        public Vector2 scale = Vector2.one;
        [Tooltip("Desplazamiento del sprite respecto al pivote lógico.")]
        public Vector2 offset;
        [Tooltip("Desmarcar si el arte original mira hacia la izquierda.")]
        public bool facesRight = true;
        public Color tint = Color.white;

        public SpriteClip Get(AnimState state)
        {
            switch (state)
            {
                case AnimState.Run: return Pick(run, idle);
                case AnimState.Jump: return Pick(jump, idle);
                case AnimState.Fall: return Pick(fall, Pick(jump, idle));
                case AnimState.Attack: return Pick(attack, idle);
                case AnimState.Hurt: return Pick(hurt, idle);
                case AnimState.Death: return Pick(death, Pick(hurt, idle));
                default: return idle;
            }
        }

        public Sprite FirstSprite => idle != null && idle.IsValid ? idle.frames[0] : icon;

        private static SpriteClip Pick(SpriteClip preferred, SpriteClip fallback) =>
            preferred != null && preferred.IsValid ? preferred : fallback;
    }

    [Serializable]
    public class ParallaxLayer
    {
        public Sprite sprite;
        [Tooltip("0 = se mueve con el mundo, 1 = fijo a la cámara (muy lejano).")]
        [Range(0f, 1f)] public float parallax = 0.5f;
        public float yOffset;
        public int sortingOrder = -100;
        [Tooltip("Ancho en unidades del mosaico horizontal.")]
        public float tiledWidth = 120f;
    }

    /// <summary>Escenario: capas de fondo con parallax, suelo en mosaico y color de cámara.</summary>
    [Serializable]
    public class EnvironmentSet
    {
        public string key = "env_city";
        public ParallaxLayer[] layers = new ParallaxLayer[0];
        public Sprite groundSprite;
        public Color cameraBackground = new Color(0.45f, 0.75f, 1f);
    }

    [Serializable]
    public class UiSprite
    {
        public UiSlot slot;
        public Sprite sprite;
    }

    [Serializable]
    public class PaletteColor
    {
        public PaletteSlot slot;
        public Color color = Color.white;
    }

    [Serializable]
    public class SfxEntry
    {
        public SfxId id;
        [Tooltip("Si hay varios clips se elige uno al azar (variedad).")]
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0f, 0.5f)] public float pitchVariation = 0.05f;

        public AudioClip RandomClip =>
            clips == null || clips.Length == 0 ? null : clips[UnityEngine.Random.Range(0, clips.Length)];
    }

    [Serializable]
    public class MusicEntry
    {
        public MusicId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.8f;
    }
}
