using UnityEngine;

namespace HyperArcade
{
    /// <summary>Parámetros de economía y de recompensas por anuncios.</summary>
    [CreateAssetMenu(menuName = "HyperArcade/Economy Config", fileName = "EconomyConfig")]
    public class EconomyConfig : ScriptableObject
    {
        [Header("Saldo inicial")]
        public int startingCoins = 200;
        public int startingDiamonds = 5;

        [Header("Anuncios recompensados")]
        public int adRewardDiamonds = 10;
        public int adRewardCoins = 100;
        [Tooltip("Máximo de anuncios de diamantes por día (protege la economía premium).")]
        public int maxDiamondAdsPerDay = 5;
        [Tooltip("Máximo de anuncios de monedas por día.")]
        public int maxCoinAdsPerDay = 10;
        [Tooltip("Segundos de espera entre anuncios del mismo tipo en el menú.")]
        public float menuAdCooldownSeconds = 60f;

        [Header("Video entre niveles (intersticial, sin recompensa)")]
        [Tooltip("Mostrar un video a pantalla completa cada N niveles ganados (0 = desactivado).")]
        public int interstitialEveryLevels = 2;

        [Header("Revivir")]
        [Tooltip("Revivir con anuncio: una vez por partida.")]
        public int maxRevivesPerMatch = 1;
        [Range(0.1f, 1f)] public float reviveHealthPercent = 1f;
        public float reviveCountdownSeconds = 6f;
    }
}
