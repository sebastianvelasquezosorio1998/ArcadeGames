using UnityEngine;

namespace HyperArcade
{
    /// <summary>Aplica solo la fuente de marca a un texto existente (poner en todos los textos de la UI).</summary>
    [RequireComponent(typeof(TMPro.TMP_Text))]
    public class SkinnedFont : MonoBehaviour
    {
        private void OnEnable()
        {
            if (SkinManager.Font != null) GetComponent<TMPro.TMP_Text>().font = SkinManager.Font;
        }
    }
}
