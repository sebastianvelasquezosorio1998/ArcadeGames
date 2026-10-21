// Requiere el plugin "Google Mobile Ads Unity" (v9 o superior) y el símbolo HA_ADMOB.
// El App ID se configura en Assets > Google Mobile Ads > Settings (no en código).
#if HA_ADMOB
using System;
using System.Collections;
using GoogleMobileAds.Api;
using UnityEngine;

namespace HyperArcade
{
    public class AdMobProvider : AdsProviderBase
    {
        private RewardedAd _ad;
        private bool _loading;
        private bool _earned;
        private Action<AdShowResult> _onShowFinished;

        public override string Name => "AdMob";
        public override bool IsRewardedReady => _ad != null && _ad.CanShowAd();

        public override void Initialize(AdsConfig config, MonoBehaviour host, Action<bool> onInitialized)
        {
            base.Initialize(config, host, onInitialized);

            // Los callbacks del SDK llegan en el hilo principal de Unity.
            MobileAds.RaiseAdEventsOnUnityMainThread = true;

            var requestConfig = new RequestConfiguration
            {
                TagForChildDirectedTreatment = config.childDirected
                    ? TagForChildDirectedTreatment.True
                    : TagForChildDirectedTreatment.Unspecified
            };
            MobileAds.SetRequestConfiguration(requestConfig);

            // IMPORTANTE (EEE/UK): solicitar consentimiento con Google UMP antes de inicializar.
            MobileAds.Initialize(_ => onInitialized?.Invoke(true));
        }

        // ------------------------------------------------ Intersticial

        private InterstitialAd _interstitial;
        private bool _interstitialLoading;
        private Action _onInterstitialClosed;

        public override bool IsInterstitialReady => _interstitial != null && _interstitial.CanShowAd();

        public override void LoadInterstitial()
        {
            if (_interstitialLoading || IsInterstitialReady) return;
            _interstitialLoading = true;
            if (_interstitial != null) { _interstitial.Destroy(); _interstitial = null; }

            InterstitialAd.Load(Config.AdMobInterstitialId, new AdRequest(), (InterstitialAd ad, LoadAdError error) =>
            {
                _interstitialLoading = false;
                if (error != null || ad == null)
                {
                    Debug.LogWarning("[AdMob] Carga de intersticial falló: " + error);
                    ScheduleInterstitialRetry();
                    return;
                }
                ResetInterstitialRetry();
                _interstitial = ad;
                _interstitial.OnAdFullScreenContentClosed += FinishInterstitial;
                _interstitial.OnAdFullScreenContentFailed += _ => FinishInterstitial();
            });
        }

        public override void ShowInterstitial(Action onClosed)
        {
            if (!IsInterstitialReady) { onClosed?.Invoke(); return; }
            _onInterstitialClosed = onClosed;
            _interstitial.Show();
        }

        private void FinishInterstitial()
        {
            var callback = _onInterstitialClosed;
            _onInterstitialClosed = null;
            callback?.Invoke();
            LoadInterstitial();
        }

        // ------------------------------------------------ Recompensado

        public override void LoadRewarded()
        {
            if (_loading || IsRewardedReady) return;
            _loading = true;

            if (_ad != null) { _ad.Destroy(); _ad = null; }

            RewardedAd.Load(Config.AdMobRewardedId, new AdRequest(), (RewardedAd ad, LoadAdError error) =>
            {
                _loading = false;
                if (error != null || ad == null)
                {
                    Debug.LogWarning("[AdMob] Carga falló: " + error);
                    ScheduleRetry();
                    return;
                }
                ResetRetry();
                _ad = ad;
                _ad.OnAdFullScreenContentClosed += OnClosed;
                _ad.OnAdFullScreenContentFailed += OnFailed;
            });
        }

        public override void ShowRewarded(Action<AdShowResult> onFinished)
        {
            if (!IsRewardedReady) { onFinished?.Invoke(AdShowResult.NotReady); return; }
            _earned = false;
            _onShowFinished = onFinished;
            _ad.Show(_ => _earned = true);
        }

        private void OnClosed()
        {
            // En algunos dispositivos la recompensa llega justo después del cierre: esperamos un instante.
            Host.StartCoroutine(FinishAfterDelay());
        }

        private void OnFailed(AdError error)
        {
            Debug.LogWarning("[AdMob] Show falló: " + error);
            Finish(AdShowResult.Failed);
        }

        private IEnumerator FinishAfterDelay()
        {
            yield return new WaitForSecondsRealtime(0.25f);
            Finish(_earned ? AdShowResult.Rewarded : AdShowResult.Skipped);
        }

        private void Finish(AdShowResult result)
        {
            var callback = _onShowFinished;
            _onShowFinished = null;
            callback?.Invoke(result);
            LoadRewarded();
        }
    }
}
#endif
