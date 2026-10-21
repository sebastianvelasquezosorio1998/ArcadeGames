using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HyperArcade
{
    /// <summary>
    /// Controlador de la Tienda (pestañas Personajes / Armas / Potenciadores).
    /// Construye las tarjetas desde GameConfig, delega las compras en ShopService
    /// y se refresca solo cuando cambian el saldo o el inventario.
    /// Los botones "Ver anuncio +10 Diamantes / +100 Monedas" son RewardedAdButton dentro de la pantalla.
    /// Los botones de compra siguen activos aunque falte saldo: al pulsarlos se sugiere ver un anuncio.
    /// </summary>
    public class ShopController : UIScreen
    {
        private enum Tab { Characters, Weapons, Consumables }

        [Header("Pestañas")]
        [SerializeField] private Button charactersTab;
        [SerializeField] private Button weaponsTab;
        [SerializeField] private Button consumablesTab;
        [SerializeField] private GameObject charactersPanel;
        [SerializeField] private GameObject weaponsPanel;
        [SerializeField] private GameObject consumablesPanel;

        [Header("Contenido")]
        [SerializeField] private RectTransform charactersContainer;
        [SerializeField] private RectTransform weaponsContainer;
        [SerializeField] private RectTransform consumablesContainer;
        [SerializeField] private ShopItemUI itemPrefab;
        [SerializeField] private Button backButton;
        [Tooltip("Botón de anuncio a resaltar cuando falta dinero (opcional).")]
        [SerializeField] private RectTransform adButtonHighlight;

        private readonly Dictionary<Object, ShopItemUI> _items = new Dictionary<Object, ShopItemUI>();
        private Tab _tab = Tab.Characters;
        private static ShopController _instance;

        private void Awake()
        {
            _instance = this;
            charactersTab.onClick.AddListener(() => SelectTab(Tab.Characters));
            weaponsTab.onClick.AddListener(() => SelectTab(Tab.Weapons));
            consumablesTab.onClick.AddListener(() => SelectTab(Tab.Consumables));
            if (backButton != null) backButton.onClick.AddListener(() => UIScreenManager.Instance.Back());
        }

        protected override void OnShow()
        {
            Wallet.Changed += OnWalletChanged;
            PlayerProgress.Changed += RefreshAll;
            SelectTab(_tab, false);
        }

        protected override void OnHide()
        {
            Wallet.Changed -= OnWalletChanged;
            PlayerProgress.Changed -= RefreshAll;
        }

        private void OnWalletChanged(CurrencyType type, int value, int delta) => RefreshAll();

        private void SelectTab(Tab tab, bool sound = true)
        {
            if (sound) AudioManager.Play(SfxId.ButtonClick);
            _tab = tab;
            charactersPanel.SetActive(tab == Tab.Characters);
            weaponsPanel.SetActive(tab == Tab.Weapons);
            consumablesPanel.SetActive(tab == Tab.Consumables);
            charactersTab.interactable = tab != Tab.Characters;
            weaponsTab.interactable = tab != Tab.Weapons;
            consumablesTab.interactable = tab != Tab.Consumables;
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (!IsVisible) return;
            var config = GameConfig.Instance;
            switch (_tab)
            {
                case Tab.Characters: foreach (var c in config.characters) if (c != null) RefreshCharacter(c); break;
                case Tab.Weapons: foreach (var w in config.weapons) if (w != null) RefreshWeapon(w); break;
                case Tab.Consumables: foreach (var c in config.consumables) if (c != null) RefreshConsumable(c); break;
            }
        }

        private ShopItemUI GetItem(Object key, RectTransform container)
        {
            if (!_items.TryGetValue(key, out var item) || item == null)
            {
                item = Instantiate(itemPrefab, container);
                _items[key] = item;
            }
            return item;
        }

        // ------------------------------------------------------------ Personajes

        private void RefreshCharacter(CharacterData c)
        {
            var item = GetItem(c, charactersContainer);
            bool unlocked = PlayerProgress.IsCharacterUnlocked(c.id);
            bool selected = PlayerProgress.SelectedCharacter == c;
            string stats = string.Format(Texts.CharacterStatsFormat, c.maxHealth, c.moveSpeed, c.damageMultiplier.ToString("0.0#"));

            item.SetInfo(SkinManager.GetIcon(c.visualKey), c.displayName, c.description + "\n" + stats,
                selected ? Texts.Selected : null, selected);
            item.HideAction(1);
            item.HideAction(2);

            if (!unlocked)
                item.SetAction(0, c.unlockPrice.amount.ToString(), true,
                    () => Handle(ShopService.UnlockCharacter(c), c.unlockPrice.currency, Texts.Unlocked), c.unlockPrice);
            else
                item.SetAction(0, selected ? Texts.Selected : Texts.Select, !selected, () =>
                {
                    PlayerProgress.SelectCharacter(c.id);
                    AudioManager.Play(SfxId.ButtonClick);
                });
        }

        // ------------------------------------------------------------ Armas

        private void RefreshWeapon(WeaponData w)
        {
            var item = GetItem(w, weaponsContainer);
            var progress = PlayerProgress.GetWeapon(w.id);
            bool selected = PlayerProgress.SelectedWeapon == w;

            string stats = string.Format(Texts.WeaponStatsFormat,
                w.GetDamage(progress.damageLevel).ToString("0"), progress.damageLevel, w.maxDamageLevel,
                w.GetAttackDelay(progress.delayLevel).ToString("0.00"), progress.delayLevel, w.maxDelayLevel);
            item.SetInfo(SkinManager.GetIcon(w.iconKey), w.displayName, w.description + "\n" + stats,
                selected ? Texts.Equipped : null, selected);

            if (!progress.unlocked)
            {
                item.SetAction(0, w.unlockPrice.amount.ToString(), true,
                    () => Handle(ShopService.UnlockWeapon(w), w.unlockPrice.currency, Texts.Unlocked), w.unlockPrice);
                item.HideAction(1);
                item.HideAction(2);
                return;
            }

            item.SetAction(0, selected ? Texts.Equipped : Texts.Equip, !selected, () =>
            {
                PlayerProgress.SelectWeapon(w.id);
                AudioManager.Play(SfxId.ButtonClick);
            });

            bool damageMax = progress.damageLevel >= w.maxDamageLevel;
            var damagePrice = ShopService.GetDamageUpgradePrice(w);
            item.SetAction(1, damageMax ? Texts.MaxLevel : string.Format(Texts.UpgradeDamageFormat, damagePrice.amount),
                !damageMax,
                () => Handle(ShopService.UpgradeDamage(w), CurrencyType.Coins, Texts.Upgraded), damageMax ? (Price?)null : damagePrice);

            bool speedMax = progress.delayLevel >= w.maxDelayLevel || w.GetAttackDelay(progress.delayLevel) <= w.minAttackDelay;
            var speedPrice = ShopService.GetSpeedUpgradePrice(w);
            item.SetAction(2, speedMax ? Texts.MaxLevel : string.Format(Texts.UpgradeSpeedFormat, speedPrice.amount),
                !speedMax,
                () => Handle(ShopService.UpgradeAttackSpeed(w), CurrencyType.Coins, Texts.Upgraded), speedMax ? (Price?)null : speedPrice);
        }

        // ------------------------------------------------------------ Consumibles

        private void RefreshConsumable(ConsumableData c)
        {
            var item = GetItem(c, consumablesContainer);
            int owned = PlayerProgress.GetConsumableCount(c.type);
            item.SetInfo(SkinManager.GetUiSprite(c.icon), c.displayName, c.description, string.Format(Texts.OwnedFormat, owned), false);
            item.SetAction(0, c.price.amount.ToString(), true,
                () => Handle(ShopService.BuyConsumable(c), c.price.currency, Texts.Purchased), c.price);
            item.HideAction(1);
            item.HideAction(2);
        }

        // ------------------------------------------------------------ Resultado

        private void Handle(PurchaseResult result, CurrencyType currency, string successMessage)
        {
            if (result == PurchaseResult.Success) Toast.Show(successMessage);
            ReportResult(result, currency, false);
            RefreshAll();
        }

        /// <summary>Feedback común de compra (usado también por la pre-partida).</summary>
        public static void ReportResult(PurchaseResult result, CurrencyType currency, bool toastOnSuccess = true)
        {
            switch (result)
            {
                case PurchaseResult.Success:
                    AudioManager.Play(SfxId.Purchase);
                    if (toastOnSuccess) Toast.Show(Texts.Purchased);
                    break;
                case PurchaseResult.NotEnoughCurrency:
                    AudioManager.Play(SfxId.Error);
                    // Empuje suave hacia el anuncio recompensado: principal vía de monetización.
                    Toast.Show(currency == CurrencyType.Coins ? Texts.NotEnoughCoins : Texts.NotEnoughDiamonds);
                    if (_instance != null && _instance.IsVisible) _instance.PulseAdButton();
                    break;
                case PurchaseResult.MaxLevel:
                    Toast.Show(Texts.MaxLevel);
                    break;
                default:
                    AudioManager.Play(SfxId.Error);
                    break;
            }
        }

        private void PulseAdButton()
        {
            if (adButtonHighlight != null) StartCoroutine(Pulse(adButtonHighlight));
        }

        private static System.Collections.IEnumerator Pulse(RectTransform target)
        {
            for (float t = 0; t < 0.6f; t += Time.unscaledDeltaTime)
            {
                target.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(t / 0.6f * Mathf.PI * 3f));
                yield return null;
            }
            target.localScale = Vector3.one;
        }
    }
}
