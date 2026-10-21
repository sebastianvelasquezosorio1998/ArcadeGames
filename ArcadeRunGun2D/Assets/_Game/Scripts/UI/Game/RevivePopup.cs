using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HyperArcade
{
    /// <summary>
    /// "¿Continuar?" al morir: "Ver anuncio para revivir" con cuenta atrás.
    /// Si el tiempo se agota o el jugador rechaza, se va a la derrota.
    /// </summary>
    public class RevivePopup : UIScreen
    {
        [SerializeField] private Button reviveButton;
        [SerializeField] private TMP_Text reviveLabel;
        [SerializeField] private Button declineButton;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private Image countdownFill;
        [Tooltip("Retraso antes de mostrar 'No, gracias' (aumenta la tasa de visualización sin ser agresivo).")]
        [SerializeField] private float declineDelay = 1.2f;

        private Action _onRevive;
        private Action _onDecline;
        private float _duration;
        private float _remaining;
        private float _shownAt;
        private bool _resolved;

        private void Awake()
        {
            reviveButton.onClick.AddListener(WatchAd);
            declineButton.onClick.AddListener(Decline);
        }

        public void Show(float seconds, Action onRevive, Action onDecline)
        {
            _onRevive = onRevive;
            _onDecline = onDecline;
            _duration = Mathf.Max(1f, seconds);
            _remaining = _duration;
            _shownAt = Time.unscaledTime;
            _resolved = false;
            if (reviveLabel != null) reviveLabel.text = Texts.AdRevive;
            declineButton.gameObject.SetActive(false);
            reviveButton.interactable = true;
            Show();
        }

        private void Update()
        {
            if (_resolved || !IsVisible) return;
            if (!declineButton.gameObject.activeSelf && Time.unscaledTime - _shownAt >= declineDelay)
                declineButton.gameObject.SetActive(true);

            // La cuenta atrás se congela mientras el anuncio está en pantalla.
            if (AdsManager.Instance != null && AdsManager.Instance.IsShowing) return;

            _remaining -= Time.unscaledDeltaTime;
            if (countdownText != null) countdownText.text = Mathf.CeilToInt(Mathf.Max(0f, _remaining)).ToString();
            if (countdownFill != null) countdownFill.fillAmount = Mathf.Clamp01(_remaining / _duration);
            if (_remaining <= 0f) Decline();
        }

        private void WatchAd()
        {
            if (_resolved || AdsManager.Instance == null) return;
            reviveButton.interactable = false;
            AdsManager.Instance.ShowRewarded(AdPlacement.Revive, () =>
            {
                if (_resolved) return;
                _resolved = true;
                Hide();
                _onRevive?.Invoke();
            }, () =>
            {
                // Falló o se saltó: el jugador puede reintentar mientras quede tiempo.
                reviveButton.interactable = true;
            });
        }

        private void Decline()
        {
            if (_resolved) return;
            _resolved = true;
            Hide();
            _onDecline?.Invoke();
        }
    }
}
