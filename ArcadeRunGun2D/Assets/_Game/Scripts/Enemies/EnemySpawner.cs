using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Generador de enemigos y oleadas. Usa puntos de aparición fuera de pantalla
    /// (suelo y aire), respeta un máximo de enemigos vivos y aplica multiplicadores de dificultad.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private Transform[] groundSpawnPoints;
        [SerializeField] private Transform[] airSpawnPoints;
        [Tooltip("Distancia mínima al jugador para elegir un punto de aparición.")]
        [SerializeField] private float minDistanceFromPlayer = 6f;

        public int MaxAlive { get; set; } = 10;
        public float HealthMultiplier { get; set; } = 1f;
        public float SpeedMultiplier { get; set; } = 1f;
        public bool Paused { get; set; }
        /// <summary>Oleada actual (1..n) para mostrar en el HUD si se desea.</summary>
        public int CurrentWave { get; private set; }

        private PlayerHealth _player;
        private Coroutine _wavesRoutine;

        public void Setup(PlayerHealth player) => _player = player;

        // ------------------------------------------------------------ Oleadas

        public void StartWaves(IReadOnlyList<WaveDefinition> waves, bool loop)
        {
            StopWaves();
            if (waves != null && waves.Count > 0) _wavesRoutine = StartCoroutine(RunWaves(waves, loop));
        }

        public void StopWaves()
        {
            if (_wavesRoutine != null) StopCoroutine(_wavesRoutine);
            _wavesRoutine = null;
        }

        private IEnumerator RunWaves(IReadOnlyList<WaveDefinition> waves, bool loop)
        {
            int cycle = 0;
            do
            {
                for (int w = 0; w < waves.Count; w++)
                {
                    CurrentWave = cycle * waves.Count + w + 1;
                    var wave = waves[w];
                    yield return Wait(wave.delayBeforeWave);

                    // Mezcla los grupos de la oleada para que no salgan en bloques.
                    var queue = BuildQueue(wave);
                    foreach (var enemy in queue)
                    {
                        while (Paused || EnemyRegistry.Count >= MaxAlive) yield return null;
                        // Cada vuelta completa del bucle los enemigos son un 15% más resistentes.
                        Spawn(enemy, HealthMultiplier * (1f + 0.15f * cycle));
                        yield return Wait(wave.spawnInterval);
                    }
                }
                cycle++;
            } while (loop);
            _wavesRoutine = null;
        }

        private static List<EnemyData> BuildQueue(WaveDefinition wave)
        {
            var list = new List<EnemyData>();
            foreach (var g in wave.groups)
                if (g.enemy != null)
                    for (int i = 0; i < g.count; i++) list.Add(g.enemy);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }

        private IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                if (!Paused) t += Time.deltaTime;
                yield return null;
            }
        }

        // ------------------------------------------------------------ Aparición

        public EnemyController Spawn(EnemyData data, float healthMultiplier)
        {
            var prefab = GameConfig.Instance != null ? GameConfig.Instance.enemyPrefab : null;
            if (prefab == null || data == null) return null;

            bool air = data.behavior == EnemyBehavior.Flyer;
            Vector3 pos = PickPoint(air && airSpawnPoints != null && airSpawnPoints.Length > 0 ? airSpawnPoints : groundSpawnPoints);
            var enemy = PoolManager.Spawn(prefab, pos, Quaternion.identity);
            enemy.Init(data, healthMultiplier, SpeedMultiplier, _player);
            return enemy;
        }

        public EnemyController SpawnBoss(EnemyData boss) => Spawn(boss, HealthMultiplier);

        private Vector3 PickPoint(Transform[] points)
        {
            if (points == null || points.Length == 0) return transform.position;
            Vector3 playerPos = _player != null ? _player.transform.position : Vector3.zero;

            // Intenta un punto aleatorio lejos del jugador; si no hay, el más lejano.
            Transform best = points[0];
            float bestDist = -1f;
            int start = Random.Range(0, points.Length);
            for (int i = 0; i < points.Length; i++)
            {
                var p = points[(start + i) % points.Length];
                if (p == null) continue;
                float d = Vector2.Distance(p.position, playerPos);
                if (d >= minDistanceFromPlayer) return p.position;
                if (d > bestDist) { bestDist = d; best = p; }
            }
            return best.position;
        }

        /// <summary>Retira todos los enemigos (victoria/derrota).</summary>
        public void DespawnAll()
        {
            foreach (var e in EnemyRegistry.Snapshot()) if (e != null) e.Despawn();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            if (groundSpawnPoints != null) foreach (var p in groundSpawnPoints) if (p != null) Gizmos.DrawWireCube(p.position, Vector3.one * 0.6f);
            Gizmos.color = Color.magenta;
            if (airSpawnPoints != null) foreach (var p in airSpawnPoints) if (p != null) Gizmos.DrawWireSphere(p.position, 0.4f);
        }
    }
}
