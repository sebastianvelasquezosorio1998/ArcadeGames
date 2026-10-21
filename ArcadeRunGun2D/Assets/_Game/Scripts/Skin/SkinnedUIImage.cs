using UnityEngine;
using UnityEngine.UI;

namespace HyperArcade
{
    /// <summary>Asigna a un Image el sprite de marca de la ranura indicada (logo, iconos, botones...).</summary>
    [RequireComponent(typeof(Image))]
    public class SkinnedUIImage : MonoBehaviour
    {
        [SerializeField] private UiSlot slot;
        [Tooltip("Mantener la proporción del sprite nuevo (útil para logos de clientes).")]
        [SerializeField] private bool preserveAspect = true;

        private void OnEnable()
        {
            var sprite = SkinManager.GetUiSprite(slot);
            if (sprite == null) return;
            var image = GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
        }
    }
}
