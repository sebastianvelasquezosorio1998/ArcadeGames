using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HyperArcade
{
    /// <summary>Pausa: continuar, reiniciar, salir al menú, sonido.</summary>
    public class PausePopup : UIScreen
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private TMP_Text soundLabel;

        private void Awake()
        {
            resumeButton.onClick.AddListener(() => MatchController.Instance.Resume());
            restartButton.onClick.AddListener(GameSession.Restart);
            menuButton.onClick.AddListener(() =>
                GameSession.GoToMenu(GameSession.Mode == GameMode.Endless ? ScreenIds.MainMenu : ScreenIds.LevelMap));
            if (soundButton != null) soundButton.onClick.AddListener(() =>
            {
                AudioManager.SetMuted(!AudioManager.IsMuted);
                RefreshSound();
            });
        }

        protected override void OnShow() => RefreshSound();

        private void Update()
        {
            if (IsVisible && InputUtil.BackPressed()) MatchController.Instance.Resume();
        }

        private void RefreshSound()
        {
            if (soundLabel != null) soundLabel.text = AudioManager.IsMuted ? "Sonido: NO" : "Sonido: SÍ";
        }
    }
}
