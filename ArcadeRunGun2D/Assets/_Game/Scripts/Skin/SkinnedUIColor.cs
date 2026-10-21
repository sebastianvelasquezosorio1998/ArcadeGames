using UnityEngine;
using UnityEngine.UI;

namespace HyperArcade
{
    /// <summary>Tiñe cualquier Graphic (Image, texto) con un color de la paleta de marca.</summary>
    [RequireComponent(typeof(Graphic))]
    public class SkinnedUIColor : MonoBehaviour
    {
        [SerializeField] private PaletteSlot slot;
        [SerializeField] private bool keepAlpha = true;

        private void OnEnable()
        {
            if (!SkinManager.TryGetColor(slot, out var color)) return;
            var graphic = GetComponent<Graphic>();
            if (keepAlpha) color.a = graphic.color.a;
            graphic.color = color;
        }
    }
}
