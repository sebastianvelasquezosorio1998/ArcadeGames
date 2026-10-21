using System;
using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Bus de eventos estático. Desacopla el gameplay (enemigos, jugador) de los
    /// sistemas que reaccionan (HUD, misiones, audio, economía).
    /// Regla: quien se suscribe en OnEnable se desuscribe en OnDisable.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<EnemyController> EnemyKilled;
        public static event Action<EnemyController> BossSpawned;
        public static event Action<EnemyController> BossDefeated;
        public static event Action<float, float> BossHealthChanged;     // (actual, máximo)
        public static event Action<float, float> PlayerHealthChanged;   // (actual, máximo)
        public static event Action PlayerDied;
        public static event Action PlayerRevived;
        public static event Action<int, int> MatchCoinsChanged;         // (total de la partida, delta)
        public static event Action<ConsumableType, float> PowerUpActivated; // (tipo, duración)

        public static void RaiseEnemyKilled(EnemyController e) => EnemyKilled?.Invoke(e);
        public static void RaiseBossSpawned(EnemyController e) => BossSpawned?.Invoke(e);
        public static void RaiseBossDefeated(EnemyController e) => BossDefeated?.Invoke(e);
        public static void RaiseBossHealthChanged(float cur, float max) => BossHealthChanged?.Invoke(cur, max);
        public static void RaisePlayerHealthChanged(float cur, float max) => PlayerHealthChanged?.Invoke(cur, max);
        public static void RaisePlayerDied() => PlayerDied?.Invoke();
        public static void RaisePlayerRevived() => PlayerRevived?.Invoke();
        public static void RaiseMatchCoinsChanged(int total, int delta) => MatchCoinsChanged?.Invoke(total, delta);
        public static void RaisePowerUpActivated(ConsumableType t, float duration) => PowerUpActivated?.Invoke(t, duration);

        // Limpia suscripciones al entrar en Play Mode (necesario si "Domain Reload" está desactivado).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            EnemyKilled = null; BossSpawned = null; BossDefeated = null; BossHealthChanged = null;
            PlayerHealthChanged = null; PlayerDied = null; PlayerRevived = null;
            MatchCoinsChanged = null; PowerUpActivated = null;
        }
    }
}
