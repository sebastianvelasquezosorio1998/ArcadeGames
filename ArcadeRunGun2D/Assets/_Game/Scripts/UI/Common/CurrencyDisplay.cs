using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HyperArcade
{
    /// <summary>Contador de Monedas o Diamantes (barra superior). Anima el número al cambiar.</summary>
    public class CurrencyDisplay : MonoBehaviour
    {
        [SerializeField] private CurrencyType currency;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private Image icon;
        [Tooltip("Botón '+' opcional: abre la tienda.")]
        [SerializeField] private Button addButton;

        private int _shown;
        private Coroutine _routine;

        private void Awake()
        {
            if (addButton != null)
                addButton.onClick.AddListener(() =>
                {
                    if (UIScreenManager.Instance != null) UIScreenManager.Instance.Show(ScreenIds.Shop);
                });
        }

        private void OnEnable()
        {
            if (icon != null)
            {
                var sprite = SkinManager.GetUiSprite(currency == CurrencyType.Coins ? UiSlot.CoinIcon : UiSlot.DiamondIcon);
                if (sprite != null) icon.sprite = sprite;
            }
            _shown = Wallet.Get(currency);
            amountText.text = Format(_shown);
            Wallet.Changed += OnChanged;
        }

        private void OnDisable() => Wallet.Changed -= OnChanged;

        private void OnChanged(CurrencyType type, int value, int delta)
        {
            if (type != currency) return;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(CountTo(value));
        }

        private IEnumerator CountTo(int target)
        {
            int start = _shown;
            const float duration = 0.4f;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                _shown = Mathf.RoundToInt(Mathf.Lerp(start, target, t / duration));
                amountText.text = Format(_shown);
                transform.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(t / duration * Mathf.PI));
                yield return null;
            }
            _shown = target;
            amountText.text = Format(_shown);
            transform.localScale = Vector3.one;
        }

        public static string Format(int value)
        {
            if (value >= 1000000) return (value / 1000000f).ToString("0.#") + "M";
            if (value >= 10000) return (value / 1000f).ToString("0.#") + "K";
            return value.ToString("N0");
        }
    }
}
