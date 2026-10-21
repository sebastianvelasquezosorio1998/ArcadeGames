using System;
using UnityEngine;

namespace HyperArcade
{
    public enum AdShowResult { Rewarded, Skipped, Failed, NotReady }

    /// <summary>
    /// Contrato que cumple cada SDK de anuncios. Para añadir otro proveedor
    /// (AppLovin, LevelPlay, etc.) basta con implementar esta interfaz y registrarla en AdsManager.
    /// </summary>
    public interface IAdsProvider
    {
        string Name { get; }
        bool IsRewardedReady { get; }

        /// <param name="host">MonoBehaviour para corrutinas (reintentos, esperas).</param>
        void Initialize(AdsConfig config, MonoBehaviour host, Action<bool> onInitialized);
        void LoadRewarded();
        void ShowRewarded(Action<AdShowResult> onFinished);

        // Intersticial: video a pantalla completa SIN recompensa (entre niveles).
        bool IsInterstitialReady { get; }
        void LoadInterstitial();
        /// <param name="onClosed">Se llama siempre al terminar (visto, cerrado o fallido).</param>
        void ShowInterstitial(Action onClosed);
    }

    /// <summary>Utilidad común: reintento de carga con espera exponencial (5s, 10s, 20s... máx. 60s).</summary>
    public abstract class AdsProviderBase : IAdsProvider
    {
        protected AdsConfig Config;
        protected MonoBehaviour Host;
        private int _retryAttempt;
        private int _interstitialRetryAttempt;

        public abstract string Name { get; }
        public abstract bool IsRewardedReady { get; }
        public abstract void LoadRewarded();
        public abstract void ShowRewarded(Action<AdShowResult> onFinished);
        public abstract bool IsInterstitialReady { get; }
        public abstract void LoadInterstitial();
        public abstract void ShowInterstitial(Action onClosed);

        public virtual void Initialize(AdsConfig config, MonoBehaviour host, Action<bool> onInitialized)
        {
            Config = config;
            Host = host;
        }

        protected void ResetRetry() => _retryAttempt = 0;
        protected void ResetInterstitialRetry() => _interstitialRetryAttempt = 0;

        protected void ScheduleInterstitialRetry()
        {
            if (Host == null) return;
            float delay = Mathf.Min(60f, 5f * Mathf.Pow(2f, _interstitialRetryAttempt++));
            Host.StartCoroutine(RetryInterstitialAfter(delay));
        }

        private System.Collections.IEnumerator RetryInterstitialAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            LoadInterstitial();
        }

        protected void ScheduleRetry()
        {
            if (Host == null) return;
            float delay = Mathf.Min(60f, 5f * Mathf.Pow(2f, _retryAttempt++));
            Host.StartCoroutine(RetryAfter(delay));
        }

        private System.Collections.IEnumerator RetryAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            LoadRewarded();
        }
    }
}
