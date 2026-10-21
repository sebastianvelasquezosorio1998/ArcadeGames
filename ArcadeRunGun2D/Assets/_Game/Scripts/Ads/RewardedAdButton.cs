using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HyperArcade
{
    /// <summary>
    /// Botón "Ver anuncio para obtener X". Arrastrar a cualquier Button del menú o la tienda.
    ///  - Diamonds / Coins: entrega la recompensa automáticamente (cantidades de EconomyConfig).
    ///  - Custom: solo dispara onRewarded (revivir, duplicar monedas...).
    /// Se desactiva solo cuando no hay anuncio cargado, hay cooldown o se alcanzó el límite diario.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class RewardedAdButton : MonoBehaviour
    {
        public enum RewardKind { Diamonds, Coins, Custom }

        [SerializeField] private AdPlacement placement = AdPlacement.MenuDiamonds;
        [SerializeField] private RewardKind reward = RewardKind.Diamonds;
        [Tooltip("0 = usar la cantidad de EconomyConfig.")]
        [SerializeField] private int amountOverride;
        [SerializeField] private TMP_Text label;
        [Tooltip("{0} = cantidad. Ej: 'Ver anuncio\\n+{0} Diamantes'")]
        [SerializeField] private string labelFormat = "";
        [Tooltip("Mostrar el motivo (cooldown/límite) en el texto cuando no está disponible.")]
        [SerializeField] private bool showUnavailableReason = true;
        [SerializeField] private float refreshInterval = 0.5f;

        public UnityEvent onRewarded = new UnityEvent();

        private Button _button;
        private float _refreshTimer;

        public AdPlacement Placement { get => placement; set => placement = value; }

        public int Amount
        {
            get
            {
                if (amountOverride > 0) return amountOverride;
                var eco = GameConfig.Instance != null ? GameConfig.Instance.economy : null;
                if (eco == null) return 0;
                return reward == RewardKind.Diamonds ? eco.adRewardDiamonds : reward == RewardKind.Coins ? eco.adRewardCoins : 0;
            }
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);
            if (string.IsNullOrEmpty(labelFormat))
                labelFormat = reward == RewardKind.Diamonds ? Texts.AdDiamondsFormat
                    : reward == RewardKind.Coins ? Texts.AdCoinsFormat : (label != null ? label.text : "");
        }

        /// <summary>Configura texto y cantidad en tiempo de ejecución (ej. "x2 monedas (+350)").</summary>
        public void Configure(string format, int amount)
        {
            labelFormat = format;
            amountOverride = amount;
            if (_button != null) Refresh();
        }

        private void OnEnable() => Refresh();

        private void Update()
        {
            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer <= 0f) Refresh();
        }

        public void Refresh()
        {
            _refreshTimer = refreshInterval;
            var ads = AdsManager.Instance;
            string reason = null;
            bool available = ads != null && ads.CanShow(placement, out reason);
            _button.interactable = available;

            if (label == null) return;
            if (!available && showUnavailableReason && !string.IsNullOrEmpty(reason) && reward != RewardKind.Custom)
                label.text = reason;
            else
                label.text = string.Format(labelFormat, Amount);
        }

        private void OnClick()
        {
            if (AdsManager.Instance == null) return;
            AudioManager.Play(SfxId.ButtonClick);
            _button.interactable = false;
            AdsManager.Instance.ShowRewarded(placement, GrantReward, Refresh);
        }

        private void GrantReward()
        {
            switch (reward)
            {
                case RewardKind.Diamonds:
                    Wallet.Add(CurrencyType.Diamonds, Amount, "ad_" + placement);
                    Toast.Show(string.Format(Texts.RewardDiamondsFormat, Amount));
                    AudioManager.Play(SfxId.Purchase);
                    break;
                case RewardKind.Coins:
                    Wallet.Add(CurrencyType.Coins, Amount, "ad_" + placement);
                    Toast.Show(string.Format(Texts.RewardCoinsFormat, Amount));
                    AudioManager.Play(SfxId.Purchase);
                    break;
            }
            onRewarded.Invoke();
            Refresh();
        }
    }
}
