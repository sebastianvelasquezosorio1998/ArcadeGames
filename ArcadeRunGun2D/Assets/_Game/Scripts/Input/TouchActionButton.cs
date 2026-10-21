using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HyperArcade
{
    /// <summary>
    /// Botón de acción táctil (Saltar / Atacar / Especial) con soporte de mantener pulsado
    /// y relleno radial de cooldown. Las pulsaciones se acumulan hasta que el jugador las consume,
    /// así no se pierden aunque el EventSystem se actualice después del jugador en el frame.
    /// </summary>
    public class TouchActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Tooltip("Image con Fill Method = Radial 360 para mostrar el cooldown (opcional).")]
        [SerializeField] private Image cooldownFill;
        [SerializeField] private float pressedScale = 0.9f;

        public bool IsHeld { get; private set; }
        private int _pendingPresses;
        private Vector3 _baseScale;

        private void Awake() => _baseScale = transform.localScale;

        public void OnPointerDown(PointerEventData eventData)
        {
            IsHeld = true;
            _pendingPresses++;
            transform.localScale = _baseScale * pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) { if (IsHeld) Release(); }

        /// <summary>Devuelve true una vez por cada pulsación.</summary>
        public bool ConsumePress()
        {
            if (_pendingPresses <= 0) return false;
            _pendingPresses = 0;
            return true;
        }

        /// <summary>0 = listo, 1 = recién usado.</summary>
        public void SetCooldown(float normalized)
        {
            if (cooldownFill != null) cooldownFill.fillAmount = Mathf.Clamp01(normalized);
        }

        private void Release()
        {
            IsHeld = false;
            transform.localScale = _baseScale;
        }

        private void OnDisable()
        {
            Release();
            _pendingPresses = 0;
        }
    }
}
