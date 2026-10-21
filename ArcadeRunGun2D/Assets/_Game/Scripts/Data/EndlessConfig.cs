using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyperArcade
{
    [Serializable]
    public class EndlessEnemyEntry
    {
        public EnemyData enemy;
        [Tooltip("Segundo de partida a partir del cual puede aparecer.")]
        public float availableFromSecond = 0f;
        [Min(0.01f)] public float weight = 1f;
    }

    /// <summary>Curva de dificultad del Modo Infinito.</summary>
    [CreateAssetMenu(menuName = "HyperArcade/Endless Config", fileName = "EndlessConfig")]
    public class EndlessConfig : ScriptableObject
    {
        public string environmentKey = "env_city";
        public List<EndlessEnemyEntry> enemies = new List<EndlessEnemyEntry>();
        public List<EnemyData> bosses = new List<EnemyData>();

        [Header("Ritmo")]
        public float startSpawnInterval = 1.6f;
        public float minSpawnInterval = 0.35f;
        [Tooltip("Segundos hasta llegar al intervalo mínimo.")]
        public float rampDurationSeconds = 240f;
        public int startMaxAlive = 6;
        public int maxAliveCap = 18;

        [Header("Escalado")]
        [Tooltip("Vida extra de los enemigos por minuto (0.25 = +25%/min).")]
        public float healthIncreasePerMinute = 0.25f;
        public float bossEverySeconds = 90f;

        [Header("Puntuación")]
        public int pointsPerSecond = 1;
        public int pointsPerBoss = 500;
    }
}
