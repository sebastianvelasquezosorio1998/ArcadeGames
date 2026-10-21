using System;
using System.Collections;
using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Proveedor simulado para el editor y pruebas: no requiere SDK.
    /// Simula la duración del anuncio (en silencio, sin textos en pantalla) y opcionalmente fallos aleatorios.
    /// </summary>
    public class MockAdsProvider : AdsProviderBase
    {
        private bool _ready;
        private bool _interstitialReady;

        public override string Name => "Mock";
        public override bool IsRewardedReady => _ready;

        public override void Initialize(AdsConfig config, MonoBehaviour host, Action<bool> onInitialized)
        {
            base.Initialize(config, host, onInitialized);
            onInitialized?.Invoke(true);
        }

        public override void LoadRewarded() => _ready = true;

        public override bool IsInterstitialReady => _interstitialReady;
        public override void LoadInterstitial() => _interstitialReady = true;

        public override void ShowInterstitial(Action onClosed)
        {
            if (!_interstitialReady) { onClosed?.Invoke(); return; }
            _interstitialReady = false;
            Host.StartCoroutine(SimulateInterstitial(onClosed));
        }

        private IEnumerator SimulateInterstitial(Action onClosed)
        {
            float duration = Config != null ? Config.mockAdDurationSeconds + 1f : 2.5f;
            yield return new WaitForSecondsRealtime(duration);
            onClosed?.Invoke();
            LoadInterstitial();
        }

        public override void ShowRewarded(Action<AdShowResult> onFinished)
        {
            if (!_ready) { onFinished?.Invoke(AdShowResult.NotReady); return; }
            _ready = false;
            Host.StartCoroutine(Simulate(onFinished));
        }

        private IEnumerator Simulate(Action<AdShowResult> onFinished)
        {
            yield return new WaitForSecondsRealtime(Config != null ? Config.mockAdDurationSeconds : 1f);
            bool fail = Config != null && UnityEngine.Random.value < Config.mockFailChance;
            onFinished?.Invoke(fail ? AdShowResult.Failed : AdShowResult.Rewarded);
            LoadRewarded();
        }
    }
}
