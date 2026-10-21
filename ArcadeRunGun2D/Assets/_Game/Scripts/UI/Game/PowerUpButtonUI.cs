using UnityEngine;
using UnityEngine.UI;

namespace HyperArcade
{
    /// <summary>Botón del HUD para activar Escudo o Convertidor (solo visible si se equipó en la pre-partida).</summary>
    [RequireComponent(typeof(Button))]
    public class PowerUpButtonUI : MonoBehaviour
    {
        [SerializeField] private ConsumableType type = ConsumableType.Shield;
        [SerializeField] private Image icon;
        [Tooltip("Relleno radial que muestra la duración del efecto (opcional).")]
        [SerializeField] private Image durationFill;

        private Button _button;
        private PowerUpController _powerUps;
        private float _activeUntil;
        private float _duration;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);
        }

        private void Start()
        {
            var match = MatchController.Instance;
            _powerUps = match != null ? match.PowerUps : null;
            var data = GameConfig.Instance.GetConsumable(type);
            if (icon != null && data != null)
            {
                var sprite = SkinManager.GetUiSprite(data.icon);
                if (sprite != null) icon.sprite = sprite;
            }
            if (_powerUps != null) _powerUps.Changed += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_powerUps != null) _powerUps.Changed -= Refresh;
        }

        private void Refresh()
        {
            bool available = _powerUps != null && _powerUps.IsAvailable(type);
            gameObject.SetActive(available || Time.time < _activeUntil);
            _button.interactable = available;
        }

        private void OnClick()
        {
            if (_powerUps == null) return;
            var data = GameConfig.Instance.GetConsumable(type);
            if (!_powerUps.TryActivate(type)) return;
            _duration = data != null ? data.duration : 0f;
            _activeUntil = Time.time + _duration;
            if (durationFill == null || _duration <= 0f) Refresh();
        }

        private void Update()
        {
            if (durationFill == null || _duration <= 0f) return;
            float left = _activeUntil - Time.time;
            durationFill.fillAmount = Mathf.Clamp01(left / _duration);
            if (left <= 0f) { _duration = 0f; Refresh(); }
        }
    }
}
