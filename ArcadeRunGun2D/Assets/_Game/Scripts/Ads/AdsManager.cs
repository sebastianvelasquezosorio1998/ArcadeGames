using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Punto único para mostrar anuncios recompensados. El resto del juego solo llama:
    ///   AdsManager.Instance.ShowRewarded(AdPlacement.Revive, onRewarded, onFailed);
    /// Gestiona: selección de SDK, precarga, límites diarios, cooldown, pausa del juego
    /// durante el anuncio y eventos para analítica.
    /// </summary>
    public class AdsManager : MonoBehaviour
    {
        public static AdsManager Instance { get; private set; }

        /// <summary>Para analítica: (ubicación, resultado).</summary>
        public static event Action<AdPlacement, AdShowResult> AdFinished;

        private readonly ConcurrentQueue<Action> _mainThread = new ConcurrentQueue<Action>();
        private IAdsProvider _provider;
        private bool _showing;
        private float _timeScaleBefore = 1f;

        public bool IsShowing => _showing;
        public bool IsReady => _provider != null && _provider.IsRewardedReady;
        public string ProviderName => _provider != null ? _provider.Name : "None";

        private static EconomyConfig Economy => GameConfig.Instance != null ? GameConfig.Instance.economy : null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            var config = GameConfig.Instance != null ? GameConfig.Instance.ads : null;
            if (config == null)
            {
                Debug.LogWarning("[Ads] Sin AdsConfig: se usa el proveedor Mock.");
                config = ScriptableObject.CreateInstance<AdsConfig>();
            }

            _provider = CreateProvider(config);
            Debug.Log("[Ads] Proveedor: " + _provider.Name);
            _provider.Initialize(config, this, ok => RunOnMainThread(() =>
            {
                if (ok) _provider.LoadRewarded();
                else Debug.LogWarning("[Ads] La inicialización del SDK falló.");
            }));
        }

        private static IAdsProvider CreateProvider(AdsConfig config)
        {
#if UNITY_EDITOR
            return new MockAdsProvider();
#else
            switch (config.provider)
            {
                case AdsProviderType.UnityAds:
#if HA_UNITY_ADS
                    return new UnityAdsProvider();
#else
                    Debug.LogWarning("[Ads] Unity Ads seleccionado pero falta el símbolo HA_UNITY_ADS.");
                    break;
#endif
                case AdsProviderType.AdMob:
#if HA_ADMOB
                    return new AdMobProvider();
#else
                    Debug.LogWarning("[Ads] AdMob seleccionado pero falta el símbolo HA_ADMOB.");
                    break;
#endif
                case AdsProviderType.Mock:
                    return new MockAdsProvider();
            }
            return config.mockWhenUnavailable ? (IAdsProvider)new MockAdsProvider() : new NullAdsProvider();
#endif
        }

        private void Update()
        {
            while (_mainThread.TryDequeue(out var action)) action?.Invoke();
        }

        public void RunOnMainThread(Action action) => _mainThread.Enqueue(action);

        // ------------------------------------------------------------------ API

        /// <summary>¿Se puede ofrecer este anuncio ahora? "reason" explica por qué no (para la UI).</summary>
        public bool CanShow(AdPlacement placement, out string reason)
        {
            reason = null;
            if (_showing) { reason = Texts.AdLoading; return false; }
            if (RemainingToday(placement) <= 0) { reason = Texts.AdDailyLimit; return false; }
            float cooldown = CooldownRemaining(placement);
            if (cooldown > 0f) { reason = string.Format(Texts.AdCooldownFormat, Mathf.CeilToInt(cooldown)); return false; }
            if (!IsReady) { reason = Texts.AdLoading; return false; }
            return true;
        }

        public void ShowRewarded(AdPlacement placement, Action onRewarded, Action onFailed = null)
        {
            if (!CanShow(placement, out string reason))
            {
                Toast.Show(reason ?? Texts.AdNotAvailable);
                onFailed?.Invoke();
                return;
            }

            _showing = true;
            _timeScaleBefore = Time.timeScale;
            Time.timeScale = 0f;
            AudioListener.pause = true;

            _provider.ShowRewarded(result => RunOnMainThread(() =>
            {
                _showing = false;
                Time.timeScale = _timeScaleBefore;
                AudioListener.pause = false;

                AdFinished?.Invoke(placement, result);
                if (result == AdShowResult.Rewarded)
                {
                    RegisterWatched(placement);
                    onRewarded?.Invoke();
                }
                else
                {
                    if (result != AdShowResult.Skipped) Toast.Show(Texts.AdNotAvailable);
                    onFailed?.Invoke();
                }
            }));
        }

        // ------------------------------------------------- Límites y cooldowns

        /// <summary>Las recompensas de moneda comparten contador entre menú y tienda.</summary>
        private static string CapKey(AdPlacement p)
        {
            switch (p)
            {
                case AdPlacement.MenuDiamonds:
                case AdPlacement.ShopDiamonds: return "diamonds";
                case AdPlacement.MenuCoins:
                case AdPlacement.ShopCoins: return "coins";
                default: return p.ToString();
            }
        }

        private static bool IsCurrencyPlacement(AdPlacement p) =>
            p == AdPlacement.MenuDiamonds || p == AdPlacement.ShopDiamonds ||
            p == AdPlacement.MenuCoins || p == AdPlacement.ShopCoins;

        public int RemainingToday(AdPlacement placement)
        {
            var economy = Economy;
            if (economy == null || !IsCurrencyPlacement(placement)) return int.MaxValue;
            int max = CapKey(placement) == "diamonds" ? economy.maxDiamondAdsPerDay : economy.maxCoinAdsPerDay;
            var counters = GetTodayCounters();
            var entry = counters.watchedToday.Find(c => c.id == CapKey(placement));
            return Mathf.Max(0, max - (entry != null ? entry.count : 0));
        }

        public float CooldownRemaining(AdPlacement placement)
        {
            var economy = Economy;
            if (economy == null || !IsCurrencyPlacement(placement) || SaveSystem.Data == null) return 0f;
            var stamp = SaveSystem.Data.ads.lastWatched.Find(s => s.id == CapKey(placement));
            if (stamp == null) return 0f;
            double elapsed = (DateTime.UtcNow - new DateTime(stamp.utcTicks, DateTimeKind.Utc)).TotalSeconds;
            return Mathf.Max(0f, economy.menuAdCooldownSeconds - (float)elapsed);
        }

        private void RegisterWatched(AdPlacement placement)
        {
            if (SaveSystem.Data == null) return;
            var counters = GetTodayCounters();
            string key = CapKey(placement);

            var entry = counters.watchedToday.Find(c => c.id == key);
            if (entry == null) counters.watchedToday.Add(new ItemCount { id = key, count = 1 });
            else entry.count++;

            var stamp = counters.lastWatched.Find(s => s.id == key);
            if (stamp == null) counters.lastWatched.Add(new AdTimestamp { id = key, utcTicks = DateTime.UtcNow.Ticks });
            else stamp.utcTicks = DateTime.UtcNow.Ticks;

            counters.totalWatched++;
            SaveSystem.Save();
        }

        private static AdCounters GetTodayCounters()
        {
            var counters = SaveSystem.Data.ads;
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (counters.day != today)
            {
                counters.day = today;
                counters.watchedToday.Clear();
            }
            return counters;
        }
    }

    /// <summary>Proveedor vacío: nunca hay anuncios (builds sin SDK).</summary>
    public class NullAdsProvider : IAdsProvider
    {
        public string Name => "None";
        public bool IsRewardedReady => false;
        public void Initialize(AdsConfig config, MonoBehaviour host, Action<bool> onInitialized) => onInitialized?.Invoke(false);
        public void LoadRewarded() { }
        public void ShowRewarded(Action<AdShowResult> onFinished) => onFinished?.Invoke(AdShowResult.NotReady);
    }
}
