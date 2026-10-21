// Requiere el paquete "Advertisement Legacy" (com.unity.ads) y el símbolo de compilación HA_UNITY_ADS
// (Project Settings > Player > Other Settings > Scripting Define Symbols).
#if HA_UNITY_ADS
using System;
using UnityEngine;
using UnityEngine.Advertisements;

namespace HyperArcade
{
    public class UnityAdsProvider : AdsProviderBase,
        IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
    {
        private Action<bool> _onInitialized;
        private Action<AdShowResult> _onShowFinished;
        private bool _loaded;
        private bool _loading;
        private bool _interstitialLoaded;
        private bool _interstitialLoading;
        private Action _onInterstitialClosed;

        private bool IsInterstitialId(string placementId) => placementId == Config.UnityInterstitialId;

        public override string Name => "UnityAds";
        public override bool IsRewardedReady => _loaded;

        public override void Initialize(AdsConfig config, MonoBehaviour host, Action<bool> onInitialized)
        {
            base.Initialize(config, host, onInitialized);
            _onInitialized = onInitialized;
            if (Advertisement.isInitialized) { OnInitializationComplete(); return; }
            Advertisement.Initialize(config.UnityGameId, config.testMode, this);
        }

        public override void LoadRewarded()
        {
            if (_loading || _loaded) return;
            _loading = true;
            Advertisement.Load(Config.UnityRewardedId, this);
        }

        public override bool IsInterstitialReady => _interstitialLoaded;

        public override void LoadInterstitial()
        {
            if (_interstitialLoading || _interstitialLoaded) return;
            _interstitialLoading = true;
            Advertisement.Load(Config.UnityInterstitialId, this);
        }

        public override void ShowInterstitial(Action onClosed)
        {
            if (!_interstitialLoaded) { onClosed?.Invoke(); return; }
            _interstitialLoaded = false;
            _onInterstitialClosed = onClosed;
            Advertisement.Show(Config.UnityInterstitialId, this);
        }

        private void FinishInterstitial()
        {
            var callback = _onInterstitialClosed;
            _onInterstitialClosed = null;
            callback?.Invoke();
            LoadInterstitial();
        }

        public override void ShowRewarded(Action<AdShowResult> onFinished)
        {
            if (!_loaded) { onFinished?.Invoke(AdShowResult.NotReady); return; }
            _loaded = false;
            _onShowFinished = onFinished;
            Advertisement.Show(Config.UnityRewardedId, this);
        }

        // --- Inicialización ---
        public void OnInitializationComplete() => _onInitialized?.Invoke(true);

        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            Debug.LogWarning($"[UnityAds] Init falló: {error} {message}");
            _onInitialized?.Invoke(false);
        }

        // --- Carga ---
        public void OnUnityAdsAdLoaded(string placementId)
        {
            if (IsInterstitialId(placementId))
            {
                _interstitialLoading = false;
                _interstitialLoaded = true;
                ResetInterstitialRetry();
                return;
            }
            _loading = false;
            _loaded = true;
            ResetRetry();
        }

        public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
        {
            Debug.LogWarning($"[UnityAds] Carga falló ({placementId}): {error} {message}");
            if (IsInterstitialId(placementId))
            {
                _interstitialLoading = false;
                _interstitialLoaded = false;
                ScheduleInterstitialRetry();
                return;
            }
            _loading = false;
            _loaded = false;
            ScheduleRetry();
        }

        // --- Visualización ---
        public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
        {
            Debug.LogWarning($"[UnityAds] Show falló ({placementId}): {error} {message}");
            if (IsInterstitialId(placementId)) { FinishInterstitial(); return; }
            Finish(AdShowResult.Failed);
        }

        public void OnUnityAdsShowStart(string placementId) { }
        public void OnUnityAdsShowClick(string placementId) { }

        public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState state)
        {
            if (IsInterstitialId(placementId)) { FinishInterstitial(); return; }
            Finish(state == UnityAdsShowCompletionState.COMPLETED ? AdShowResult.Rewarded : AdShowResult.Skipped);
        }

        private void Finish(AdShowResult result)
        {
            var callback = _onShowFinished;
            _onShowFinished = null;
            callback?.Invoke(result);
            LoadRewarded(); // precargar el siguiente
        }
    }
}
#endif
