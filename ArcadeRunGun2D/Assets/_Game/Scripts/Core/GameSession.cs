using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HyperArcade
{
    /// <summary>
    /// Datos que viajan del menú a la escena de juego (modo, nivel, consumibles equipados)
    /// y utilidades de carga de escenas.
    /// </summary>
    public static class GameSession
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameScene = "Game";

        public static GameMode Mode { get; private set; } = GameMode.Levels;
        public static LevelData CurrentLevel { get; private set; }

        /// <summary>Consumibles que el jugador decidió llevar a la próxima partida (pre-partida).</summary>
        public static readonly HashSet<ConsumableType> EquippedConsumables = new HashSet<ConsumableType>();

        /// <summary>Pantalla del menú a abrir al volver de una partida (ej. el mapa de niveles).</summary>
        public static string ReturnScreenId { get; set; }

        public static void StartLevel(LevelData level)
        {
            Mode = GameMode.Levels;
            CurrentLevel = level;
            LoadScene(GameScene);
        }

        public static void StartEndless()
        {
            Mode = GameMode.Endless;
            CurrentLevel = null;
            LoadScene(GameScene);
        }

        public static void Restart() => LoadScene(GameScene);

        public static void GoToMenu(string screenId = null)
        {
            ReturnScreenId = screenId;
            LoadScene(MainMenuScene);
        }

        /// <summary>Usado al abrir la escena Game directamente en el editor.</summary>
        public static void EnsureLevelForTesting(GameConfig config)
        {
            if (Mode == GameMode.Levels && CurrentLevel == null && config != null && config.levels.Count > 0)
                CurrentLevel = config.levels[0];
        }

        private static void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(sceneName);
        }
    }
}
