using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HyperArcade
{
    /// <summary>Lecturas de entrada independientes del sistema de Input activo.</summary>
    public static class InputUtil
    {
        /// <summary>Botón "Atrás" de Android / Escape en PC.</summary>
        public static bool BackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }
    }
}
