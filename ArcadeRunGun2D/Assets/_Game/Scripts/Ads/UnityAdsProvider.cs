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
            _loading = false;
            _loaded = true;
            ResetRetry();
        }

        public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
        {
            Debug.LogWarning($"[UnityAds] Carga falló: {error} {message}");
            _loading = false;
            _loaded = false;
            ScheduleRetry();
        }

        // --- Visualización ---
        public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
        {
            Debug.LogWarning($"[UnityAds] Show falló: {error} {message}");
            Finish(AdShowResult.Failed);
        }

        public void OnUnityAdsShowStart(string placementId) { }
        public void OnUnityAdsShowClick(string placementId) { }

        public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState state)
        {
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
