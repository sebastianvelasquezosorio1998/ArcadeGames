using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HyperArcade
{
    /// <summary>
    /// Unifica los controles táctiles con teclado (para probar en el editor/PC).
    /// El jugador solo lee de aquí: Move, JumpHeld, ConsumeJump(), AttackHeld, ConsumeSpecial().
    /// Teclado: A/D o flechas = mover, W/↑ = apuntar arriba, Espacio = saltar, J = atacar, K = especial.
    /// </summary>
    public class PlayerInputHub : MonoBehaviour
    {
        public static PlayerInputHub Instance { get; private set; }

        [SerializeField] private VirtualJoystick joystick;
        [SerializeField] private TouchActionButton jumpButton;
        [SerializeField] private TouchActionButton attackButton;
        [SerializeField] private TouchActionButton specialButton;
        [Tooltip("Disparo automático mientras haya enemigos cerca (muy recomendado en hipercasual).")]
        [SerializeField] private bool autoFire = false;

        [Header("Transparencia de los controles táctiles")]
        [Tooltip("Opacidad en reposo del joystick y los botones (0 = invisibles, 1 = opacos).")]
        [Range(0.05f, 1f)] [SerializeField] private float controlsOpacity = 0.4f;
        [Tooltip("Opacidad mientras se tocan (feedback visual).")]
        [Range(0.05f, 1f)] [SerializeField] private float pressedOpacity = 0.85f;

        public bool Enabled { get; set; } = true;
        public bool AutoFire { get => autoFire; set => autoFire = value; }
        public TouchActionButton SpecialButton => specialButton;

        private bool _keyJumpPressed, _keySpecialPressed;
        private CanvasGroup _joystickGroup, _jumpGroup, _attackGroup, _specialGroup;
        private CanvasGroup[] _powerUpGroups = new CanvasGroup[0];

        private void Awake()
        {
            Instance = this;
            _joystickGroup = Translucent(joystick);
            _jumpGroup = Translucent(jumpButton);
            _attackGroup = Translucent(attackButton);
            _specialGroup = Translucent(specialButton);

            var powerUps = GetComponentsInChildren<PowerUpButtonUI>(true);
            _powerUpGroups = new CanvasGroup[powerUps.Length];
            for (int i = 0; i < powerUps.Length; i++) _powerUpGroups[i] = Translucent(powerUps[i]);
        }

        /// <summary>Un CanvasGroup baja la opacidad de todo el control (no afecta a los toques).</summary>
        private CanvasGroup Translucent(Component control)
        {
            if (control == null) return null;
            var group = control.GetComponent<CanvasGroup>();
            if (group == null) group = control.gameObject.AddComponent<CanvasGroup>();
            group.alpha = controlsOpacity;
            return group;
        }

        private void SetOpacity(CanvasGroup group, bool pressed)
        {
            if (group != null) group.alpha = pressed ? pressedOpacity : controlsOpacity;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public Vector2 Move
        {
            get
            {
                if (!Enabled) return Vector2.zero;
                Vector2 v = joystick != null ? joystick.Value : Vector2.zero;
                Vector2 k = KeyboardMove();
                return k.sqrMagnitude > v.sqrMagnitude ? k : v;
            }
        }

        public bool JumpHeld => Enabled && ((jumpButton != null && jumpButton.IsHeld) || KeyHeld(KeyId.Jump));
        public bool AttackHeld => Enabled && ((attackButton != null && attackButton.IsHeld) || KeyHeld(KeyId.Attack));

        public bool ConsumeJump()
        {
            bool touch = jumpButton != null && jumpButton.ConsumePress();
            bool key = _keyJumpPressed;
            _keyJumpPressed = false;
            return Enabled && (touch || key);
        }

        public bool ConsumeSpecial()
        {
            bool touch = specialButton != null && specialButton.ConsumePress();
            bool key = _keySpecialPressed;
            _keySpecialPressed = false;
            return Enabled && (touch || key);
        }

        private void Update()
        {
            SetOpacity(_joystickGroup, joystick != null && joystick.IsPressed);
            SetOpacity(_jumpGroup, jumpButton != null && jumpButton.IsHeld);
            SetOpacity(_attackGroup, attackButton != null && attackButton.IsHeld);
            SetOpacity(_specialGroup, specialButton != null && specialButton.IsHeld);
            foreach (var g in _powerUpGroups) SetOpacity(g, false);

            if (KeyDown(KeyId.Jump)) _keyJumpPressed = true;
            if (KeyDown(KeyId.Special)) _keySpecialPressed = true;
        }

        // ------------------------------------------------------- Teclado

        private enum KeyId { Jump, Attack, Special }

        private static Vector2 KeyboardMove()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return Vector2.zero;
            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1 : 0) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1 : 0);
            float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1 : 0) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1 : 0);
            return new Vector2(x, y);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
            return Vector2.zero;
#endif
        }

        private static bool KeyHeld(KeyId id)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            switch (id)
            {
                case KeyId.Jump: return kb.spaceKey.isPressed;
                case KeyId.Attack: return kb.jKey.isPressed;
                default: return kb.kKey.isPressed;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(ToKeyCode(id));
#else
            return false;
#endif
        }

        private static bool KeyDown(KeyId id)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return false;
            switch (id)
            {
                case KeyId.Jump: return kb.spaceKey.wasPressedThisFrame;
                case KeyId.Attack: return kb.jKey.wasPressedThisFrame;
                default: return kb.kKey.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(ToKeyCode(id));
#else
            return false;
#endif
        }

#if !ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
        private static KeyCode ToKeyCode(KeyId id) =>
            id == KeyId.Jump ? KeyCode.Space : id == KeyId.Attack ? KeyCode.J : KeyCode.K;
#endif
    }
}
