using System.Collections;
using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Director de la partida (escena Game). Orquesta:
    /// jugador + skin, escenario, misión, oleadas o modo infinito, tiempo, potenciadores,
    /// muerte → oferta de revivir (anuncio) → derrota, victoria → estrellas → recompensas → resultado.
    /// </summary>
    [RequireComponent(typeof(PowerUpController))]
    public class MatchController : MonoBehaviour
    {
        public static MatchController Instance { get; private set; }

        [Header("Escena")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private EndlessDirector endlessDirector;
        [SerializeField] private CameraFollow2D cameraFollow;
        [SerializeField] private EnvironmentBuilder environment;

        [Header("UI")]
        [SerializeField] private HUDController hud;
        [SerializeField] private RevivePopup revivePopup;
        [SerializeField] private ResultPopup resultPopup;
        [SerializeField] private PausePopup pausePopup;

        [Header("Ritmo")]
        [SerializeField] private float startDelay = 1.5f;
        [SerializeField] private float resultDelay = 1.2f;
        [Tooltip("Radio alrededor del jugador que se limpia de enemigos al revivir.")]
        [SerializeField] private float reviveClearRadius = 5f;

        public MatchState State { get; private set; } = MatchState.Starting;
        public PlayerHealth Player { get; private set; }
        public PlayerCombat PlayerCombat { get; private set; }
        public PowerUpController PowerUps { get; private set; }
        public MissionTracker Mission { get; private set; }
        public MatchStats Stats { get; } = new MatchStats();
        public LevelData Level { get; private set; }
        public bool IsEndless => GameSession.Mode == GameMode.Endless;
        public float Elapsed { get; private set; }
        public float TimeRemaining => Level != null ? Mathf.Max(0f, Level.timeLimit - Elapsed) : 0f;

        private int _revivesUsed;
        private bool _rewardsCommitted;
        private GameConfig _config;

        private void Awake()
        {
            Instance = this;
            EnemyRegistry.Clear();
            PowerUps = GetComponent<PowerUpController>();
        }

        private void OnEnable()
        {
            GameEvents.EnemyKilled += OnEnemyKilled;
            GameEvents.BossDefeated += OnBossDefeated;
            GameEvents.PlayerDied += OnPlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.EnemyKilled -= OnEnemyKilled;
            GameEvents.BossDefeated -= OnBossDefeated;
            GameEvents.PlayerDied -= OnPlayerDied;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            EnemyRegistry.Clear();
        }

        private void Start()
        {
            _config = GameConfig.Instance;
            GameSession.EnsureLevelForTesting(_config);
            Level = IsEndless ? null : GameSession.CurrentLevel;
            if (!ValidateSetup()) { enabled = false; return; }

            ConfigurePhysicsLayers();
            SpawnPlayer();

            string envKey = IsEndless ? (_config.endless != null ? _config.endless.environmentKey : null) : Level.environmentKey;
            if (environment != null) environment.Build(envKey);

            Mission = IsEndless ? null : new MissionTracker(Level.mission, Level.mission == MissionType.DefeatBoss ? 1 : Level.missionTarget);
            PowerUps.Setup(Player, Stats);
            spawner.Setup(Player);

            if (hud != null) hud.Setup(this);
            AudioManager.PlayMusic(MusicId.Gameplay);
            PlayerProgress.RegisterMatchPlayed();

            StartCoroutine(BeginAfterDelay());
        }

        private bool ValidateSetup()
        {
            string problem = null;
            if (_config == null) problem = "falta Resources/GameConfig.asset";
            else if (_config.playerPrefab == null) problem = "GameConfig no tiene 'playerPrefab'";
            else if (PlayerProgress.SelectedCharacter == null) problem = "GameConfig no tiene personajes";
            else if (PlayerProgress.SelectedWeapon == null) problem = "GameConfig no tiene armas";
            else if (!IsEndless && Level == null) problem = "GameConfig no tiene niveles";
            else if (IsEndless && _config.endless == null) problem = "GameConfig no tiene EndlessConfig";
            else if (spawner == null) problem = "MatchController no tiene 'spawner' asignado";
            if (problem == null) return true;
            Debug.LogError($"[Partida] No se puede iniciar: {problem}. Ejecuta Tools > HyperArcade > 1. Crear contenido por defecto.");
            return false;
        }

        private void SpawnPlayer()
        {
            var character = PlayerProgress.SelectedCharacter;
            var weapon = PlayerProgress.SelectedWeapon;
            Vector3 pos = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;

            var controller = Instantiate(_config.playerPrefab, pos, Quaternion.identity);
            controller.ApplyCharacter(character);

            Player = controller.GetComponent<PlayerHealth>();
            Player.Setup(character.maxHealth);

            PlayerCombat = controller.GetComponent<PlayerCombat>();
            PlayerCombat.Setup(weapon, character.damageMultiplier);

            if (cameraFollow != null) cameraFollow.SetTarget(controller.transform);
        }

        private IEnumerator BeginAfterDelay()
        {
            State = MatchState.Starting;
            if (hud != null) hud.ShowBanner(IsEndless ? SkinManager.GetText(BrandTextSlot.Slogan) : Mission.Describe());
            yield return new WaitForSeconds(startDelay);

            State = MatchState.Playing;
            if (IsEndless)
            {
                endlessDirector.Begin(_config.endless, spawner);
            }
            else
            {
                spawner.MaxAlive = Level.maxEnemiesAlive;
                spawner.HealthMultiplier = Level.enemyHealthMultiplier;
                spawner.StartWaves(Level.waves, Level.loopWaves);
            }
        }

        private void Update()
        {
            if (State != MatchState.Playing) return;
            Elapsed += Time.deltaTime;

            if (IsEndless)
            {
                Stats.Score = Stats.Kills * 10 + Mathf.FloorToInt(Elapsed) * _config.endless.pointsPerSecond
                              + Stats.BossesKilled * _config.endless.pointsPerBoss;
                return;
            }

            switch (Level.mission)
            {
                case MissionType.Survive:
                    Mission.SetProgress(Mathf.FloorToInt(Elapsed));
                    if (Mission.IsComplete) { Win(); return; }
                    break;
                case MissionType.DefeatBoss:
                    if (!Mission.BossSpawned && Elapsed >= Level.bossSpawnTime && Level.boss != null)
                    {
                        Mission.BossSpawned = true;
                        spawner.SpawnBoss(Level.boss);
                        Mission.NotifyChanged();
                    }
                    break;
            }

            if (TimeRemaining <= 0f)
            {
                if (Level.mission == MissionType.Survive) Win();
                else Lose(Texts.TimeUp);
            }
        }

        // ------------------------------------------------------------ Economía de partida

        /// <summary>Monedas recogidas (aplica el multiplicador x2 si está activo).</summary>
        public void AddMatchCoins(int baseValue)
        {
            if (_rewardsCommitted) return;
            int value = Mathf.Max(1, Mathf.RoundToInt(baseValue * Stats.CoinMultiplier));
            Stats.Coins += value;
            GameEvents.RaiseMatchCoinsChanged(Stats.Coins, value);
        }

        // ------------------------------------------------------------ Eventos

        private void OnEnemyKilled(EnemyController enemy)
        {
            if (State != MatchState.Playing) return;
            Stats.Kills++;
            Stats.Score += enemy.Data.scoreValue;
            if (Mission != null && Mission.Type == MissionType.KillEnemies)
            {
                Mission.AddProgress();
                if (Mission.IsComplete) Win();
            }
        }

        private void OnBossDefeated(EnemyController boss)
        {
            Stats.BossesKilled++;
            if (Mission != null && Mission.Type == MissionType.DefeatBoss)
            {
                Mission.Complete();
                Win();
            }
        }

        private void OnPlayerDied()
        {
            if (State != MatchState.Playing) return;
            StartCoroutine(HandleDeath());
        }

        private IEnumerator HandleDeath()
        {
            State = MatchState.ReviveOffer;
            spawner.Paused = true;
            yield return new WaitForSeconds(0.8f); // deja ver la animación de muerte

            int maxRevives = _config.economy != null ? _config.economy.maxRevivesPerMatch : 1;
            bool canOffer = _revivesUsed < maxRevives && revivePopup != null &&
                            AdsManager.Instance != null && AdsManager.Instance.IsReady;
            if (canOffer)
            {
                Time.timeScale = 0f;
                float countdown = _config.economy != null ? _config.economy.reviveCountdownSeconds : 6f;
                revivePopup.Show(countdown, OnReviveAccepted, () => Lose(null));
            }
            else
            {
                Lose(null);
            }
        }

        /// <summary>Llamado por RevivePopup cuando el anuncio se completó.</summary>
        private void OnReviveAccepted()
        {
            _revivesUsed++;
            Time.timeScale = 1f;

            // Limpia enemigos cercanos para que el jugador no muera al instante.
            foreach (var e in EnemyRegistry.Snapshot())
                if (e != null && !e.Data.isBoss && Vector2.Distance(e.transform.position, Player.transform.position) < reviveClearRadius)
                    e.Despawn();

            // Si murió por caída, reaparece en el punto de inicio.
            if (playerSpawnPoint != null && Player.transform.position.y < playerSpawnPoint.position.y - 3f)
            {
                Player.transform.position = playerSpawnPoint.position;
                var rb = Player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.SetVelocity(Vector2.zero);
            }

            float pct = _config.economy != null ? _config.economy.reviveHealthPercent : 1f;
            Player.Revive(pct);
            spawner.Paused = false;
            State = MatchState.Playing;
        }

        // ------------------------------------------------------------ Fin de partida

        public void Win()
        {
            if (State == MatchState.Victory || State == MatchState.Defeat) return;
            State = MatchState.Victory;
            StartCoroutine(EndMatch(true, null));
        }

        public void Lose(string reason)
        {
            if (State == MatchState.Victory || State == MatchState.Defeat) return;
            State = MatchState.Defeat;
            Time.timeScale = 1f;
            StartCoroutine(EndMatch(false, reason));
        }

        private IEnumerator EndMatch(bool victory, string reason)
        {
            spawner.StopWaves();
            spawner.Paused = true;
            if (endlessDirector != null) endlessDirector.Stop();

            var controller = Player.GetComponent<PlayerController>();
            controller.ControlsEnabled = false;
            controller.Stop();

            // Recoge al instante las monedas que aún vuelan y retira enemigos.
            Coin.CollectAll();
            if (victory) foreach (var e in EnemyRegistry.Snapshot()) if (e != null) e.ConvertToCoins();
            yield return null;
            Coin.CollectAll();

            var result = new MatchResult { victory = victory, endless = IsEndless, level = Level, reason = reason };

            if (IsEndless)
            {
                result.score = Stats.Score;
                result.newRecord = PlayerProgress.RecordEndlessResult(Stats.Score, Elapsed);
                result.bestScore = PlayerProgress.EndlessBestScore;
                result.coinsEarned = Stats.Coins;
            }
            else
            {
                result.coinsEarned = Stats.Coins + (victory ? Level.completionCoins : 0);
                if (victory)
                {
                    result.stars = Level.CalculateStars(Player.Normalized, Elapsed);
                    result.newRecord = PlayerProgress.RecordLevelResult(Level.id, result.stars, result.coinsEarned);
                    if (AdsManager.Instance != null) AdsManager.Instance.NotifyLevelCompleted();
                }
            }

            // Las monedas se guardan SIEMPRE (también al perder): mejora la retención.
            _rewardsCommitted = true;
            Wallet.Add(CurrencyType.Coins, result.coinsEarned, victory ? "match_victory" : "match_defeat");

            AudioManager.Play(victory ? SfxId.Victory : SfxId.Defeat);
            AudioManager.PlayMusic(MusicId.None);
            spawner.DespawnAll();

            yield return new WaitForSecondsRealtime(resultDelay);
            if (hud != null) hud.gameObject.SetActive(false);
            if (resultPopup != null) resultPopup.Show(result);
        }

        // ------------------------------------------------------------ Pausa

        public void Pause()
        {
            if (State != MatchState.Playing) return;
            State = MatchState.Paused;
            Time.timeScale = 0f;
            if (pausePopup != null) pausePopup.Show();
        }

        public void Resume()
        {
            if (State != MatchState.Paused) return;
            Time.timeScale = 1f;
            State = MatchState.Playing;
            if (pausePopup != null) pausePopup.Hide();
        }

        private void OnApplicationPause(bool paused)
        {
            // Si el jugador sale de la app, al volver encuentra la partida en pausa.
            if (paused && AdsManager.Instance != null && !AdsManager.Instance.IsShowing) Pause();
        }

        // ------------------------------------------------------------ Física

        private static void ConfigurePhysicsLayers()
        {
            int player = LayerMask.NameToLayer("Player");
            int enemy = LayerMask.NameToLayer("Enemy");
            int pickup = LayerMask.NameToLayer("Pickup");
            int projectile = LayerMask.NameToLayer("Projectile");
            if (projectile >= 0)
            {
                Physics2D.IgnoreLayerCollision(projectile, projectile, true);
                if (pickup >= 0) Physics2D.IgnoreLayerCollision(projectile, pickup, true);
            }
            if (player >= 0 && enemy >= 0) Physics2D.IgnoreLayerCollision(player, enemy, true);
            if (enemy >= 0) Physics2D.IgnoreLayerCollision(enemy, enemy, true);
            if (pickup >= 0)
            {
                if (player >= 0) Physics2D.IgnoreLayerCollision(pickup, player, true);
                if (enemy >= 0) Physics2D.IgnoreLayerCollision(pickup, enemy, true);
                Physics2D.IgnoreLayerCollision(pickup, pickup, true);
            }
        }
    }
}
