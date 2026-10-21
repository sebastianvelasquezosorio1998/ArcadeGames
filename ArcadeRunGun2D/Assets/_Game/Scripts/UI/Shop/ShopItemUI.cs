using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HyperArcade
{
    /// <summary>
    /// Tarjeta genérica de tienda con hasta 3 botones de acción.
    /// Personajes: [Desbloquear/Elegir]. Armas: [Desbloquear/Equipar] [Daño +] [Cadencia +].
    /// Consumibles: [Comprar].
    /// </summary>
    public class ShopItemUI : MonoBehaviour
    {
        [Serializable]
        public class ActionSlot
        {
            public Button button;
            public TMP_Text label;
            [Tooltip("Icono de moneda/diamante junto al precio (opcional).")]
            public Image currencyIcon;
            [NonSerialized] public Action onClick;
        }

        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text infoText;
        [SerializeField] private TMP_Text badgeText;
        [SerializeField] private GameObject selectedHighlight;
        [SerializeField] private ActionSlot[] actions = new ActionSlot[3];

        private void Awake()
        {
            foreach (var a in actions)
            {
                if (a == null || a.button == null) continue;
                var slot = a;
                slot.button.onClick.AddListener(() => slot.onClick?.Invoke());
            }
        }

        public void SetInfo(Sprite sprite, string title, string info, string badge, bool highlighted)
        {
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
                icon.preserveAspect = true;
            }
            nameText.text = title;
            if (infoText != null) infoText.text = info;
            if (badgeText != null)
            {
                badgeText.gameObject.SetActive(!string.IsNullOrEmpty(badge));
                badgeText.text = badge;
            }
            if (selectedHighlight != null) selectedHighlight.SetActive(highlighted);
        }

        /// <param name="price">null = botón sin precio.</param>
        public void SetAction(int index, string label, bool interactable, Action onClick, Price? price = null)
        {
            if (index < 0 || index >= actions.Length || actions[index] == null || actions[index].button == null) return;
            var a = actions[index];
            a.button.gameObject.SetActive(true);
            a.button.interactable = interactable;
            a.onClick = onClick;
            if (a.label != null) a.label.text = label;
            if (a.currencyIcon != null)
            {
                a.currencyIcon.gameObject.SetActive(price.HasValue);
                if (price.HasValue)
                {
                    var s = SkinManager.GetUiSprite(price.Value.currency == CurrencyType.Coins ? UiSlot.CoinIcon : UiSlot.DiamondIcon);
                    if (s != null) a.currencyIcon.sprite = s;
                }
            }
        }

        public void HideAction(int index)
        {
            if (index >= 0 && index < actions.Length && actions[index] != null && actions[index].button != null)
                actions[index].button.gameObject.SetActive(false);
        }
    }
}
