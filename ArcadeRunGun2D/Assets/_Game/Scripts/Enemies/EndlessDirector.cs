using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Modo Infinito: genera enemigos de forma procedural con dificultad creciente
    /// (más rápido, más enemigos a la vez, más vida) y un jefe cada X segundos.
    /// </summary>
    public class EndlessDirector : MonoBehaviour
    {
        [SerializeField] private EnemySpawner spawner;

        public float Elapsed { get; private set; }
        public bool Running { get; private set; }

        private EndlessConfig _config;
        private float _nextSpawn;
        private float _nextBoss;

        public void Begin(EndlessConfig config, EnemySpawner enemySpawner)
        {
            _config = config;
            if (enemySpawner != null) spawner = enemySpawner;
            Elapsed = 0f;
            _nextSpawn = 1f;
            _nextBoss = config.bossEverySeconds;
            Running = true;
        }

        public void Stop() => Running = false;

        /// <summary>0 → 1 a lo largo de rampDurationSeconds.</summary>
        public float Difficulty01 => _config == null ? 0f : Mathf.Clamp01(Elapsed / Mathf.Max(1f, _config.rampDurationSeconds));

        private void Update()
        {
            if (!Running || _config == null || spawner == null || spawner.Paused) return;
            Elapsed += Time.deltaTime;

            spawner.MaxAlive = Mathf.RoundToInt(Mathf.Lerp(_config.startMaxAlive, _config.maxAliveCap, Difficulty01));
            spawner.HealthMultiplier = 1f + _config.healthIncreasePerMinute * (Elapsed / 60f);
            spawner.SpeedMultiplier = Mathf.Lerp(1f, 1.35f, Difficulty01);

            if (Elapsed >= _nextBoss && _config.bosses.Count > 0)
            {
                _nextBoss += _config.bossEverySeconds;
                spawner.SpawnBoss(_config.bosses[Random.Range(0, _config.bosses.Count)]);
            }

            if (Elapsed >= _nextSpawn)
            {
                float interval = Mathf.Lerp(_config.startSpawnInterval, _config.minSpawnInterval, Difficulty01);
                _nextSpawn = Elapsed + interval;
                if (EnemyRegistry.Count < spawner.MaxAlive)
                {
                    var enemy = PickEnemy();
                    if (enemy != null) spawner.Spawn(enemy, spawner.HealthMultiplier);
                }
            }
        }

        private EnemyData PickEnemy()
        {
            float total = 0f;
            foreach (var e in _config.enemies)
                if (e.enemy != null && Elapsed >= e.availableFromSecond) total += e.weight;
            if (total <= 0f) return null;

            float roll = Random.value * total;
            EnemyData last = null;
            foreach (var e in _config.enemies)
            {
                if (e.enemy == null || Elapsed < e.availableFromSecond) continue;
                last = e.enemy;
                roll -= e.weight;
                if (roll <= 0f) return e.enemy;
            }
            return last; // por redondeo de coma flotante
        }
    }
}
