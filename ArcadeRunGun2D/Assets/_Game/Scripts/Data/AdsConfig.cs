using UnityEngine;

namespace HyperArcade
{
    public enum AdsProviderType { Mock, UnityAds, AdMob }

    /// <summary>Formato del bloque "recompensado" creado en AdMob (debe coincidir con el de la consola).</summary>
    public enum AdMobRewardedFormat { Rewarded, RewardedInterstitial }

    /// <summary>
    /// IDs de anuncios por build/cliente. Los valores por defecto son IDs de PRUEBA oficiales:
    /// reemplázalos por los reales de cada cliente antes de publicar.
    /// </summary>
    [CreateAssetMenu(menuName = "HyperArcade/Ads Config", fileName = "AdsConfig")]
    public class AdsConfig : ScriptableObject
    {
        [Tooltip("Proveedor en dispositivo. En el editor siempre se usa Mock.")]
        public AdsProviderType provider = AdsProviderType.AdMob;
        public bool testMode = true;
        [Tooltip("Si el SDK no está instalado o falla, usar Mock (solo recomendable en desarrollo).")]
        public bool mockWhenUnavailable = true;

        [Header("Unity Ads (requiere símbolo HA_UNITY_ADS)")]
        public string unityGameIdAndroid = "0000000";
        public string unityGameIdIOS = "0000000";
        public string unityRewardedAndroid = "Rewarded_Android";
        public string unityRewardedIOS = "Rewarded_iOS";
        public string unityInterstitialAndroid = "Interstitial_Android";
        public string unityInterstitialIOS = "Interstitial_iOS";

        [Header("AdMob (requiere símbolo HA_ADMOB). El App ID va en Assets > Google Mobile Ads > Settings")]
        [Tooltip("Rewarded = bloque 'Bonificado'. RewardedInterstitial = bloque 'Intersticial bonificado'.")]
        public AdMobRewardedFormat admobRewardedFormat = AdMobRewardedFormat.Rewarded;
        public string admobRewardedAndroid = "ca-app-pub-3940256099942544/5224354917"; // ID de prueba de Google
        public string admobRewardedIOS = "ca-app-pub-3940256099942544/1712485313";     // ID de prueba de Google
        public string admobInterstitialAndroid = "ca-app-pub-3940256099942544/1033173712"; // ID de prueba de Google
        public string admobInterstitialIOS = "ca-app-pub-3940256099942544/4411468910";     // ID de prueba de Google
        [Tooltip("Marcar si la app está dirigida a menores (COPPA / Families).")]
        public bool childDirected;

        [Header("Mock (editor)")]
        public float mockAdDurationSeconds = 1.5f;
        [Range(0, 1)] public float mockFailChance = 0f;

        public string UnityGameId =>
            Application.platform == RuntimePlatform.IPhonePlayer ? unityGameIdIOS : unityGameIdAndroid;

        public string UnityRewardedId =>
            Application.platform == RuntimePlatform.IPhonePlayer ? unityRewardedIOS : unityRewardedAndroid;

        public string UnityInterstitialId =>
            Application.platform == RuntimePlatform.IPhonePlayer ? unityInterstitialIOS : unityInterstitialAndroid;

        public string AdMobInterstitialId =>
            Application.platform == RuntimePlatform.IPhonePlayer ? admobInterstitialIOS : admobInterstitialAndroid;

        public string AdMobRewardedId =>
            Application.platform == RuntimePlatform.IPhonePlayer ? admobRewardedIOS : admobRewardedAndroid;
    }
}
