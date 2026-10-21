using System;
using UnityEngine;

namespace HyperArcade
{
    /// <summary>Misión clara en pantalla: "Elimina a 30 enemigos", "Sobrevive 90s", "Derrota al Jefe".</summary>
    public class MissionTracker
    {
        public MissionType Type { get; }
        public int Target { get; }
        public int Progress { get; private set; }
        public bool BossSpawned { get; set; }
        public bool IsComplete { get; private set; }

        public event Action Changed;

        public MissionTracker(MissionType type, int target)
        {
            Type = type;
            Target = Mathf.Max(1, target);
        }

        public void AddProgress(int amount = 1) => SetProgress(Progress + amount);

        public void SetProgress(int value)
        {
            if (IsComplete) return;
            int clamped = Mathf.Clamp(value, 0, Target);
            if (clamped == Progress) return;
            Progress = clamped;
            if (Progress >= Target) IsComplete = true;
            Changed?.Invoke();
        }

        public void Complete()
        {
            if (IsComplete) return;
            Progress = Target;
            IsComplete = true;
            Changed?.Invoke();
        }

        public void NotifyChanged() => Changed?.Invoke();

        public float Normalized => (float)Progress / Target;

        public string Describe()
        {
            switch (Type)
            {
                case MissionType.KillEnemies: return string.Format(Texts.MissionKillFormat, Target, Progress);
                case MissionType.Survive: return string.Format(Texts.MissionSurviveFormat, Mathf.Max(0, Target - Progress));
                default: return BossSpawned ? Texts.MissionBoss : Texts.MissionBossWaiting;
            }
        }
    }

    /// <summary>Estadísticas de la partida en curso.</summary>
    public class MatchStats
    {
        public int Coins;
        public int Kills;
        public int BossesKilled;
        public int Score;
        public float CoinMultiplier = 1f;
    }

    /// <summary>Resumen que recibe la pantalla de Victoria/Derrota.</summary>
    public struct MatchResult
    {
        public bool victory;
        public bool endless;
        public int stars;
        public int coinsEarned;
        public int score;
        public int bestScore;
        public bool newRecord;
        public string reason;
        public LevelData level;
    }
}
