using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HyperArcade
{
    /// <summary>
    /// HUD de juego: vida, monedas de la partida, misión con progreso, tiempo, puntuación (infinito),
    /// barra del jefe, banner de inicio, pausa e indicador de x2.
    /// Los controles táctiles (joystick y botones) viven en el mismo Canvas, en PlayerInputHub.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Jugador")]
        [SerializeField] private Image healthFill;
        [SerializeField] private TMP_Text healthText;

        [Header("Partida")]
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private RectTransform coinsIcon;
        [SerializeField] private TMP_Text missionText;
        [SerializeField] private Image missionProgressFill;
        [Tooltip("Contenedor de la barra de misión (se oculta en Modo Infinito).")]
        [SerializeField] private GameObject missionProgressRoot;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private GameObject multiplierBadge;

        [Header("Jefe")]
        [SerializeField] private GameObject bossBarRoot;
        [SerializeField] private Image bossFill;

        [Header("Banner de inicio")]
        [SerializeField] private CanvasGroup banner;
        [SerializeField] private TMP_Text bannerText;

        [Header("Botones")]
        [SerializeField] private Button pauseButton;

        private MatchController _match;
        private float _shownHealth = 1f;
        private float _targetHealth = 1f;
        private Coroutine _coinPunch;

        public void Setup(MatchController match)
        {
            _match = match;
            if (pauseButton != null) pauseButton.onClick.AddListener(match.Pause);
            if (bossBarRoot != null) bossBarRoot.SetActive(false);
            if (scoreText != null) scoreText.gameObject.SetActive(match.IsEndless);
            if (missionProgressRoot != null) missionProgressRoot.SetActive(!match.IsEndless);
            if (multiplierBadge != null) multiplierBadge.SetActive(match.PowerUps.MultiplierActive);
            if (coinsText != null) coinsText.text = "0";

            if (match.Mission != null)
            {
                match.Mission.Changed += RefreshMission;
                RefreshMission();
            }
            else if (missionText != null) missionText.text = SkinManager.GetText(BrandTextSlot.Slogan);
        }

        private void OnEnable()
        {
            GameEvents.PlayerHealthChanged += OnHealth;
            GameEvents.MatchCoinsChanged += OnCoins;
            GameEvents.BossSpawned += OnBossSpawned;
            GameEvents.BossHealthChanged += OnBossHealth;
            GameEvents.BossDefeated += OnBossDefeated;
            GameEvents.PowerUpActivated += OnPowerUp;
        }

        private void OnDisable()
        {
            GameEvents.PlayerHealthChanged -= OnHealth;
            GameEvents.MatchCoinsChanged -= OnCoins;
            GameEvents.BossSpawned -= OnBossSpawned;
            GameEvents.BossHealthChanged -= OnBossHealth;
            GameEvents.BossDefeated -= OnBossDefeated;
            GameEvents.PowerUpActivated -= OnPowerUp;
            if (_match != null && _match.Mission != null) _match.Mission.Changed -= RefreshMission;
        }

        private void Update()
        {
            if (_match == null) return;

            // Vida suavizada
            _shownHealth = Mathf.MoveTowards(_shownHealth, _targetHealth, Time.unscaledDeltaTime * 1.5f);
            if (healthFill != null) healthFill.fillAmount = _shownHealth;

            // Tiempo: cuenta atrás en niveles, cronómetro en infinito
            if (timerText != null)
            {
                float t = _match.IsEndless ? _match.Elapsed : _match.TimeRemaining;
                int m = Mathf.FloorToInt(t / 60f), s = Mathf.FloorToInt(t % 60f);
                timerText.text = $"{m}:{s:00}";
                timerText.color = !_match.IsEndless && t <= 15f ? new Color(1f, 0.35f, 0.35f) : Color.white;
            }

            if (scoreText != null && _match.IsEndless) scoreText.text = string.Format(Texts.EndlessFormat, _match.Stats.Score);
            if (_match.Mission != null && _match.Mission.Type == MissionType.Survive && missionText != null)
                missionText.text = _match.Mission.Describe();
        }

        public void ShowBanner(string text)
        {
            if (banner == null) return;
            if (bannerText != null) bannerText.text = text;
            StartCoroutine(BannerRoutine());
        }

        private IEnumerator BannerRoutine()
        {
            banner.gameObject.SetActive(true);
            for (float t = 0; t < 0.25f; t += Time.deltaTime) { banner.alpha = t / 0.25f; yield return null; }
            banner.alpha = 1f;
            yield return new WaitForSeconds(1.6f);
            for (float t = 0; t < 0.4f; t += Time.deltaTime) { banner.alpha = 1f - t / 0.4f; yield return null; }
            banner.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------ Eventos

        private void OnHealth(float current, float max)
        {
            _targetHealth = max > 0 ? current / max : 0f;
            if (_targetHealth > _shownHealth) _shownHealth = _targetHealth; // curación/revivir: instantáneo
            if (healthText != null) healthText.text = Mathf.CeilToInt(current) + "/" + Mathf.CeilToInt(max);
        }

        private void OnCoins(int total, int delta)
        {
            if (coinsText != null) coinsText.text = CurrencyDisplay.Format(total);
            if (coinsIcon != null)
            {
                if (_coinPunch != null) StopCoroutine(_coinPunch);
                _coinPunch = StartCoroutine(Punch(coinsIcon));
            }
        }

        private void RefreshMission()
        {
            var mission = _match.Mission;
            if (missionText != null) missionText.text = mission.Describe();
            if (missionProgressFill != null) missionProgressFill.fillAmount = mission.Normalized;
        }

        private void OnBossSpawned(EnemyController boss)
        {
            if (bossBarRoot != null) bossBarRoot.SetActive(true);
            if (bossFill != null) bossFill.fillAmount = 1f;
        }

        private void OnBossHealth(float current, float max)
        {
            if (bossFill != null) bossFill.fillAmount = max > 0 ? current / max : 0f;
        }

        private void OnBossDefeated(EnemyController boss)
        {
            if (bossBarRoot != null) bossBarRoot.SetActive(false);
        }

        private void OnPowerUp(ConsumableType type, float duration)
        {
            if (type == ConsumableType.CoinMultiplier && multiplierBadge != null) multiplierBadge.SetActive(true);
        }

        private static IEnumerator Punch(RectTransform t)
        {
            for (float k = 0; k < 0.15f; k += Time.unscaledDeltaTime)
            {
                t.localScale = Vector3.one * (1f + 0.3f * Mathf.Sin(k / 0.15f * Mathf.PI));
                yield return null;
            }
            t.localScale = Vector3.one;
        }
    }
}
