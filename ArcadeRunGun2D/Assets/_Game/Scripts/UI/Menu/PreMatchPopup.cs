using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HyperArcade
{
    /// <summary>
    /// Tienda Pre-Partida: muestra la misión, el personaje/arma equipados y permite
    /// comprar y equipar potenciadores justo antes de jugar (momento de mayor conversión).
    /// </summary>
    public class PreMatchPopup : UIScreen
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text missionText;
        [SerializeField] private StarsDisplay bestStars;
        [SerializeField] private Image characterIcon;
        [SerializeField] private TMP_Text loadoutText;
        [SerializeField] private RectTransform consumablesContainer;
        [SerializeField] private ConsumableSlotUI consumableSlotPrefab;
        [SerializeField] private Button playButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button closeButton;

        private readonly List<ConsumableSlotUI> _slots = new List<ConsumableSlotUI>();
        private LevelData _level;
        private bool _endless;

        private void Awake()
        {
            playButton.onClick.AddListener(Play);
            if (shopButton != null) shopButton.onClick.AddListener(() => UIScreenManager.Instance.Show(ScreenIds.Shop));
            if (closeButton != null) closeButton.onClick.AddListener(() => UIScreenManager.Instance.Back());
        }

        public void SetupLevel(LevelData level)
        {
            _level = level;
            _endless = false;
        }

        public void SetupEndless()
        {
            _level = null;
            _endless = true;
        }

        protected override void OnShow()
        {
            if (_endless)
            {
                titleText.text = "Modo Infinito";
                missionText.text = "Sobrevive el mayor tiempo posible.\nRécord: " + PlayerProgress.EndlessBestScore;
                if (bestStars != null) bestStars.gameObject.SetActive(false);
            }
            else if (_level != null)
            {
                titleText.text = _level.displayName;
                missionText.text = DescribeMission(_level);
                if (bestStars != null)
                {
                    bestStars.gameObject.SetActive(true);
                    bestStars.SetStars(PlayerProgress.GetStars(_level.id));
                }
            }

            var character = PlayerProgress.SelectedCharacter;
            var weapon = PlayerProgress.SelectedWeapon;
            if (characterIcon != null && character != null)
            {
                var icon = SkinManager.GetIcon(character.visualKey);
                if (icon != null) characterIcon.sprite = icon;
            }
            if (loadoutText != null && character != null && weapon != null)
                loadoutText.text = character.displayName + "  ·  " + weapon.displayName;

            BuildSlots();
            Wallet.Changed += OnWalletChanged;
        }

        protected override void OnHide() => Wallet.Changed -= OnWalletChanged;

        private void OnWalletChanged(CurrencyType type, int value, int delta) => RefreshSlots();

        private void BuildSlots()
        {
            var consumables = GameConfig.Instance.consumables;
            while (_slots.Count < consumables.Count) _slots.Add(Instantiate(consumableSlotPrefab, consumablesContainer));
            for (int i = 0; i < _slots.Count; i++)
            {
                bool used = i < consumables.Count && consumables[i] != null;
                _slots[i].gameObject.SetActive(used);
                if (used) _slots[i].Setup(consumables[i], Buy);
            }
        }

        private void RefreshSlots()
        {
            foreach (var s in _slots) if (s.gameObject.activeSelf) s.Refresh();
        }

        private void Buy(ConsumableData data)
        {
            var result = ShopService.BuyConsumable(data);
            ShopController.ReportResult(result, data.price.currency);
            if (result == PurchaseResult.Success) GameSession.EquippedConsumables.Add(data.type); // auto-equipar
            RefreshSlots();
        }

        private void Play()
        {
            AudioManager.Play(SfxId.ButtonClick);
            if (_endless) GameSession.StartEndless();
            else if (_level != null) GameSession.StartLevel(_level);
        }

        public static string DescribeMission(LevelData level)
        {
            int minutes = Mathf.FloorToInt(level.timeLimit / 60f);
            int seconds = Mathf.FloorToInt(level.timeLimit % 60f);
            string time = $"{minutes}:{seconds:00}";
            switch (level.mission)
            {
                case MissionType.KillEnemies: return $"Elimina a {level.missionTarget} enemigos\nTiempo: {time}";
                case MissionType.Survive: return $"Sobrevive {level.missionTarget} segundos";
                default: return $"¡Derrota al Jefe!\nTiempo: {time}";
            }
        }
    }
}
