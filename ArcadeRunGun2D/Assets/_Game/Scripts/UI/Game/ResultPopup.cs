using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HyperArcade
{
    /// <summary>
    /// Pantalla de Victoria / Derrota con ventana emergente de calificación (1–3★),
    /// monedas obtenidas con animación y "Ver anuncio para duplicar las monedas".
    /// </summary>
    public class ResultPopup : UIScreen
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private StarsDisplay stars;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private GameObject newRecordBadge;

        [Header("Botones")]
        [SerializeField] private RewardedAdButton doubleCoinsButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;

        private MatchResult _result;
        private int _shownCoins;
        private bool _doubled;

        private void Awake()
        {
            if (doubleCoinsButton != null) doubleCoinsButton.onRewarded.AddListener(OnDoubleRewarded);
            nextButton.onClick.AddListener(() => Leave(GoNext));
            retryButton.onClick.AddListener(() => Leave(GameSession.Restart));
            menuButton.onClick.AddListener(() =>
                Leave(() => GameSession.GoToMenu(_result.endless ? ScreenIds.MainMenu : ScreenIds.LevelMap)));
        }

        public void Show(MatchResult result)
        {
            _result = result;
            _doubled = false;
            SetButtonsInteractable(true);
            _shownCoins = 0;
            Show();
            StartCoroutine(Present());
        }

        private IEnumerator Present()
        {
            titleText.text = _result.endless ? Texts.EndlessOver : (_result.victory ? Texts.Victory : Texts.Defeat);
            if (subtitleText != null)
            {
                subtitleText.text = _result.reason ?? (_result.level != null ? _result.level.displayName : "Modo Infinito");
            }
            if (newRecordBadge != null) newRecordBadge.SetActive(false);
            if (coinsText != null) coinsText.text = string.Format(Texts.CoinsEarnedFormat, 0);

            bool showStars = _result.victory && !_result.endless && stars != null;
            if (stars != null) stars.gameObject.SetActive(showStars);
            if (scoreText != null)
            {
                scoreText.gameObject.SetActive(_result.endless);
                scoreText.text = string.Format(Texts.EndlessResultFormat, _result.score, _result.bestScore);
            }

            // Doble de monedas: al ganar (y al terminar una partida infinita).
            bool canDouble = (_result.victory || _result.endless) && _result.coinsEarned > 0 && doubleCoinsButton != null;
            if (doubleCoinsButton != null)
            {
                doubleCoinsButton.gameObject.SetActive(canDouble);
                if (canDouble) doubleCoinsButton.Configure(Texts.AdDoubleCoinsFormat, _result.coinsEarned);
            }

            var next = _result.level != null ? GameConfig.Instance.GetNextLevel(_result.level) : null;
            nextButton.gameObject.SetActive(_result.victory && next != null);

            if (showStars) yield return stars.AnimateStars(_result.stars);
            yield return CountCoins(_result.coinsEarned);
            if (_result.newRecord && newRecordBadge != null) newRecordBadge.SetActive(true);
        }

        private IEnumerator CountCoins(int target)
        {
            int start = _shownCoins;
            const float duration = 0.6f;
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                _shownCoins = Mathf.RoundToInt(Mathf.Lerp(start, target, t / duration));
                coinsText.text = string.Format(Texts.CoinsEarnedFormat, _shownCoins);
                if (Time.frameCount % 3 == 0) AudioManager.Play(SfxId.Coin);
                yield return null;
            }
            _shownCoins = target;
            coinsText.text = string.Format(Texts.CoinsEarnedFormat, _shownCoins);
        }

        private void OnDoubleRewarded()
        {
            if (_doubled) return;
            _doubled = true;
            // Las monedas base ya se guardaron al terminar la partida: el anuncio suma otra vez la misma cantidad.
            Wallet.Add(CurrencyType.Coins, _result.coinsEarned, "ad_double_coins");
            doubleCoinsButton.gameObject.SetActive(false);
            StartCoroutine(CountCoins(_result.coinsEarned * 2));
        }

        /// <summary>
        /// Cada N niveles ganados se proyecta un video publicitario (intersticial) antes de continuar.
        /// Los videos recompensados (x2, revivir, monedas, diamantes) siguen siendo opcionales.
        /// </summary>
        private void Leave(System.Action action)
        {
            SetButtonsInteractable(false);
            if (AdsManager.Instance != null) AdsManager.Instance.ShowInterstitialIfDue(action);
            else action();
        }

        private void SetButtonsInteractable(bool value)
        {
            nextButton.interactable = value;
            retryButton.interactable = value;
            menuButton.interactable = value;
        }

        private void GoNext()
        {
            var next = GameConfig.Instance.GetNextLevel(_result.level);
            if (next != null) GameSession.StartLevel(next);
        }
    }
}
