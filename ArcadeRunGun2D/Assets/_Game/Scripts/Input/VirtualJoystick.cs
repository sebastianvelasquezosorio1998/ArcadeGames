using UnityEngine;
using UnityEngine.EventSystems;

namespace HyperArcade
{
    /// <summary>
    /// Joystick virtual (lado izquierdo de la pantalla).
    /// Modo dinámico: aparece donde el dedo toca dentro de su zona (recomendado en hipercasual).
    /// Estructura: TouchZone (este script, Image transparente a pantalla media) > Base > Knob.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform joystickBase;
        [SerializeField] private RectTransform knob;
        [SerializeField] private float radius = 110f;
        [Range(0f, 0.5f)] [SerializeField] private float deadZone = 0.15f;
        [SerializeField] private bool dynamicPosition = true;
        [Tooltip("Ocultar la base cuando no se toca (solo modo dinámico).")]
        [SerializeField] private bool hideWhenIdle = false;

        public Vector2 Value { get; private set; }
        public bool IsPressed { get; private set; }

        private RectTransform _zone;
        private Vector2 _defaultBasePosition;
        private int _pointerId = int.MinValue;
        private Canvas _canvas;

        private void Awake()
        {
            _zone = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
            if (joystickBase != null) _defaultBasePosition = joystickBase.anchoredPosition;
            SetVisible(!hideWhenIdle);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsPressed) return; // ignora un segundo dedo
            IsPressed = true;
            _pointerId = eventData.pointerId;

            if (dynamicPosition && joystickBase != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)joystickBase.parent, eventData.position, EventCamera, out var local))
            {
                joystickBase.anchoredPosition = local;
            }
            SetVisible(true);
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsPressed || eventData.pointerId != _pointerId || joystickBase == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickBase, eventData.position, EventCamera, out var local))
                return;

            Vector2 clamped = Vector2.ClampMagnitude(local, radius);
            if (knob != null) knob.anchoredPosition = clamped;

            Vector2 v = clamped / radius;
            Value = v.magnitude < deadZone ? Vector2.zero : v;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            Release();
        }

        private void OnDisable() => Release();

        private void Release()
        {
            IsPressed = false;
            _pointerId = int.MinValue;
            Value = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            if (dynamicPosition && joystickBase != null) joystickBase.anchoredPosition = _defaultBasePosition;
            SetVisible(!hideWhenIdle);
        }

        private Camera EventCamera =>
            _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;

        private void SetVisible(bool visible)
        {
            if (joystickBase != null) joystickBase.gameObject.SetActive(visible);
        }
    }
}
