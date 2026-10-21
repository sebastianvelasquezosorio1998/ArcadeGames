using UnityEngine;
using UnityEngine.SceneManagement;

namespace HyperArcade
{
    /// <summary>
    /// Crea automáticamente los sistemas persistentes (guardado, audio, anuncios, toasts)
    /// antes de cargar la primera escena. No hace falta una escena "Boot":
    /// cualquier escena se puede probar directamente con Play.
    /// </summary>
    public class Systems : MonoBehaviour
    {
        public static Systems Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;

            Application.targetFrameRate = 60;
            SaveSystem.Load();

            var go = new GameObject("[Systems]");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<Systems>();
            go.AddComponent<AudioManager>();
            go.AddComponent<AdsManager>();
            go.AddComponent<Toast>();

            SceneManager.sceneLoaded += (_, __) => PoolManager.Clear();
        }

        // Guardar al pasar a segundo plano: en móvil la app puede cerrarse sin OnApplicationQuit.
        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveSystem.Save();
        }

        private void OnApplicationQuit() => SaveSystem.Save();
    }
}
