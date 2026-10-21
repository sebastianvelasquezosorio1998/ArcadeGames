using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HyperArcade
{
    /// <summary>Fila de consumible en la pre-partida: icono, cantidad, interruptor "llevar" y botón comprar.</summary>
    public class ConsumableSlotUI : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Toggle equipToggle;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Image priceCurrencyIcon;

        private ConsumableData _data;
        private Action<ConsumableData> _onBuy;
        private bool _refreshing;

        private void Awake()
        {
            equipToggle.onValueChanged.AddListener(OnToggle);
            buyButton.onClick.AddListener(() => _onBuy?.Invoke(_data));
        }

        public void Setup(ConsumableData data, Action<ConsumableData> onBuy)
        {
            _data = data;
            _onBuy = onBuy;
            var sprite = SkinManager.GetUiSprite(data.icon);
            if (icon != null && sprite != null) icon.sprite = sprite;
            nameText.text = data.displayName;
            priceText.text = data.price.amount.ToString();
            if (priceCurrencyIcon != null)
            {
                var cur = SkinManager.GetUiSprite(data.price.currency == CurrencyType.Coins ? UiSlot.CoinIcon : UiSlot.DiamondIcon);
                if (cur != null) priceCurrencyIcon.sprite = cur;
            }
            Refresh();
        }

        public void Refresh()
        {
            if (_data == null) return;
            _refreshing = true;
            int owned = PlayerProgress.GetConsumableCount(_data.type);
            countText.text = string.Format(Texts.OwnedFormat, owned);
            if (owned <= 0) GameSession.EquippedConsumables.Remove(_data.type);
            equipToggle.interactable = owned > 0;
            equipToggle.isOn = GameSession.EquippedConsumables.Contains(_data.type);
            if (priceText != null) priceText.color = Wallet.CanAfford(_data.price) ? Color.white : new Color(1f, 0.45f, 0.45f);
            _refreshing = false;
        }

        private void OnToggle(bool on)
        {
            if (_refreshing || _data == null) return;
            if (on && PlayerProgress.GetConsumableCount(_data.type) > 0) GameSession.EquippedConsumables.Add(_data.type);
            else GameSession.EquippedConsumables.Remove(_data.type);
            AudioManager.Play(SfxId.ButtonClick);
        }
    }
}
