using UnityEngine;

namespace HyperArcade
{
    /// <summary>Muestra un texto de marca (título del juego, empresa, eslogan) y aplica la fuente del skin.</summary>
    [RequireComponent(typeof(TMPro.TMP_Text))]
    public class SkinnedText : MonoBehaviour
    {
        [SerializeField] private BrandTextSlot slot;

        private void OnEnable()
        {
            var text = GetComponent<TMPro.TMP_Text>();
            string value = SkinManager.GetText(slot);
            if (!string.IsNullOrEmpty(value)) text.text = value;
            if (SkinManager.Font != null) text.font = SkinManager.Font;
        }
    }
}
