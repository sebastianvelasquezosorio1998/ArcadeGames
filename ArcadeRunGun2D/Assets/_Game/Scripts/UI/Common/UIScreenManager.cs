using System.Collections.Generic;
using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Navegación del menú (escena MainMenu): Menú Principal ⇄ Mapa de Niveles ⇄ Tienda, más popups.
    /// Soporta el botón "Atrás" de Android (Escape).
    /// </summary>
    public class UIScreenManager : MonoBehaviour
    {
        public static UIScreenManager Instance { get; private set; }

        [SerializeField] private UIScreen[] screens;
        [SerializeField] private UIScreen defaultScreen;

        private readonly Stack<UIScreen> _history = new Stack<UIScreen>();
        private readonly Stack<UIScreen> _popups = new Stack<UIScreen>();
        private UIScreen _current;

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            foreach (var s in screens) if (s != null) s.HideImmediate();

            string requested = GameSession.ReturnScreenId;
            GameSession.ReturnScreenId = null;
            var first = Find(requested);
            if (first == null || first.IsPopup) first = defaultScreen;
            if (first != null && first != defaultScreen && defaultScreen != null) _history.Push(defaultScreen);
            Open(first, false);

            AudioManager.PlayMusic(MusicId.Menu);
        }

        private void Update()
        {
            if (InputUtil.BackPressed()) Back();
        }

        public void Show(string id)
        {
            var screen = Find(id);
            if (screen == null) { Debug.LogWarning("[UI] Pantalla no encontrada: " + id); return; }
            Open(screen, true);
        }

        public T Show<T>() where T : UIScreen
        {
            foreach (var s in screens)
                if (s is T typed) { Open(typed, true); return typed; }
            return null;
        }

        public void Back()
        {
            AudioManager.Play(SfxId.ButtonClick);
            if (_popups.Count > 0) { _popups.Pop().Hide(); return; }
            if (_history.Count == 0) return;
            var previous = _history.Pop();
            if (_current != null) _current.Hide();
            _current = previous;
            _current.Show();
        }

        public void CloseTopPopup()
        {
            if (_popups.Count > 0) _popups.Pop().Hide();
        }

        private void Open(UIScreen screen, bool addToHistory)
        {
            if (screen == null) return;
            if (screen.IsPopup)
            {
                if (!_popups.Contains(screen)) _popups.Push(screen);
                screen.Show();
                return;
            }

            while (_popups.Count > 0) _popups.Pop().Hide();
            if (_current == screen) { screen.Show(); return; }
            if (_current != null)
            {
                if (addToHistory) _history.Push(_current);
                _current.Hide();
            }
            // Volver al menú principal reinicia la pila (evita historiales infinitos).
            if (screen == defaultScreen) _history.Clear();
            _current = screen;
            _current.Show();
        }

        private UIScreen Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var s in screens) if (s != null && s.Id == id) return s;
            return null;
        }
    }
}
