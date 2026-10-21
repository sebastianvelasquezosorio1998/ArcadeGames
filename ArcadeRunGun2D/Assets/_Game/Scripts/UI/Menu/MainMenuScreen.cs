using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HyperArcade
{
    /// <summary>
    /// Menú Principal: Jugar (mapa), Modo Infinito, Tienda, sonido y botones de anuncio
    /// (+10 Diamantes / +100 Monedas: componentes RewardedAdButton dentro de esta pantalla).
    /// </summary>
    public class MainMenuScreen : UIScreen
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button endlessButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private TMP_Text soundLabel;
        [SerializeField] private TMP_Text endlessBestText;
        [SerializeField] private TMP_Text totalStarsText;
        [SerializeField] private PreMatchPopup preMatchPopup;

        private void Awake()
        {
            playButton.onClick.AddListener(() => Navigate(ScreenIds.LevelMap));
            shopButton.onClick.AddListener(() => Navigate(ScreenIds.Shop));
            endlessButton.onClick.AddListener(OpenEndless);
            if (soundButton != null) soundButton.onClick.AddListener(ToggleSound);
        }

        protected override void OnShow()
        {
            if (endlessBestText != null) endlessBestText.text = "Récord: " + PlayerProgress.EndlessBestScore;
            if (totalStarsText != null) totalStarsText.text = PlayerProgress.TotalStars + " / " + GameConfig.Instance.levels.Count * 3;
            RefreshSound();
        }

        private void OpenEndless()
        {
            AudioManager.Play(SfxId.ButtonClick);
            if (preMatchPopup != null)
            {
                preMatchPopup.SetupEndless();
                UIScreenManager.Instance.Show(preMatchPopup.Id);
            }
            else GameSession.StartEndless();
        }

        private static void Navigate(string id)
        {
            AudioManager.Play(SfxId.ButtonClick);
            UIScreenManager.Instance.Show(id);
        }

        private void ToggleSound()
        {
            AudioManager.SetMuted(!AudioManager.IsMuted);
            AudioManager.Play(SfxId.ButtonClick);
            RefreshSound();
        }

        private void RefreshSound()
        {
            if (soundLabel != null) soundLabel.text = AudioManager.IsMuted ? "Sonido: NO" : "Sonido: SÍ";
        }
    }
}
