using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyperArcade
{
    [Serializable]
    public class SpawnGroup
    {
        public EnemyData enemy;
        [Min(1)] public int count = 3;
    }

    [Serializable]
    public class WaveDefinition
    {
        [Tooltip("Segundos de espera antes de iniciar la oleada.")]
        public float delayBeforeWave = 2f;
        [Tooltip("Segundos entre cada aparición dentro de la oleada.")]
        public float spawnInterval = 1f;
        public List<SpawnGroup> groups = new List<SpawnGroup>();
    }

    /// <summary>
    /// Nivel del Modo Niveles: misión, oleadas, jefe, límite de tiempo y reglas de estrellas.
    /// Diseñado para partidas de 2–3 minutos.
    /// </summary>
    [CreateAssetMenu(menuName = "HyperArcade/Level", fileName = "Level_")]
    public class LevelData : ScriptableObject
    {
        public string id = "level_01";
        public string displayName = "Nivel 1";
        [Tooltip("Clave del EnvironmentSet (fondo/suelo) en el SkinProfile.")]
        public string environmentKey = "env_city";

        [Header("Misión")]
        public MissionType mission = MissionType.KillEnemies;
        [Tooltip("KillEnemies: nº de enemigos. Survive: segundos. DefeatBoss: ignorado.")]
        public int missionTarget = 30;
        [Tooltip("Duración máxima de la partida en segundos (120–180 recomendado).")]
        public float timeLimit = 150f;

        [Header("Oleadas")]
        public List<WaveDefinition> waves = new List<WaveDefinition>();
        [Tooltip("Repite las oleadas hasta cumplir la misión.")]
        public bool loopWaves = true;
        [Tooltip("Multiplicador de vida de enemigos (dificultad).")]
        public float enemyHealthMultiplier = 1f;
        public int maxEnemiesAlive = 10;

        [Header("Jefe (solo misión DefeatBoss)")]
        public EnemyData boss;
        [Tooltip("Segundos tras el inicio en los que aparece el jefe.")]
        public float bossSpawnTime = 45f;

        [Header("Estrellas (1★ = completar)")]
        [Range(0, 1)] public float twoStarsMinHealth = 0.4f;
        [Range(0, 1)] public float threeStarsMinHealth = 0.75f;
        [Tooltip("Para 3★ además hay que terminar antes de este tiempo (0 = sin requisito). En Survive se ignora.")]
        public float threeStarsMaxTime = 120f;

        [Header("Recompensa")]
        public int completionCoins = 50;

        public int CalculateStars(float healthPercent, float timeUsed)
        {
            int stars = 1;
            if (healthPercent >= twoStarsMinHealth) stars = 2;
            bool timeOk = mission == MissionType.Survive || threeStarsMaxTime <= 0f || timeUsed <= threeStarsMaxTime;
            if (healthPercent >= threeStarsMinHealth && timeOk) stars = 3;
            return stars;
        }
    }
}
