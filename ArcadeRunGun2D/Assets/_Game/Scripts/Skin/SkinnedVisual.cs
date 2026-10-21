using System;
using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Renderiza una entidad a partir de su clave visual del skin y reproduce
    /// animaciones por fotogramas. Los controladores de lógica solo llaman a
    /// SetKey / SetState / SetFacing / Flash: nunca tocan sprites.
    /// Va en un hijo "Visual" del prefab, para poder escalarlo sin afectar físicas.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SkinnedVisual : MonoBehaviour
    {
        [SerializeField] private string visualKey;
        [SerializeField] private bool useUnscaledTime;

        public event Action<AnimState> ClipFinished;

        public SpriteRenderer Renderer { get; private set; }
        public VisualSet Visual { get; private set; }
        public AnimState State { get; private set; } = AnimState.Idle;

        private SpriteClip _clip;
        private float _timer;
        private int _frame;
        private bool _finished;
        private bool _facingRight = true;
        private float _flashUntil;
        private Color _flashColor = Color.white;
        private bool _blinking;

        private void Awake()
        {
            Renderer = GetComponent<SpriteRenderer>();
            if (!string.IsNullOrEmpty(visualKey)) SetKey(visualKey);
        }

        public void SetKey(string key)
        {
            if (Renderer == null) Renderer = GetComponent<SpriteRenderer>();
            visualKey = key;
            Visual = SkinManager.GetVisual(key);
            if (Visual != null)
            {
                transform.localPosition = Visual.offset;
                ApplyFacing();
            }
            ForceState(State);
        }

        public void SetState(AnimState state)
        {
            // Mismo estado en curso: no reiniciar. Un clip no-loop ya terminado sí se reinicia.
            if (state == State && !_finished) return;
            ForceState(state);
        }

        public void ForceState(AnimState state)
        {
            State = state;
            _clip = Visual != null ? Visual.Get(state) : null;
            _frame = 0;
            _timer = 0f;
            _finished = false;
            ShowFrame();
        }

        public void SetFacing(bool right)
        {
            if (_facingRight == right) return;
            _facingRight = right;
            ApplyFacing();
        }

        /// <summary>Destello de color breve (impacto).</summary>
        public void Flash(Color color, float duration = 0.08f)
        {
            _flashColor = color;
            _flashUntil = Time.time + duration;
        }

        /// <summary>Parpadeo continuo (invulnerabilidad).</summary>
        public void SetBlink(bool blink) => _blinking = blink;

        private void Update()
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (_clip != null && _clip.IsValid && !_finished && _clip.frames.Length > 1 && _clip.fps > 0f)
            {
                _timer += dt;
                float frameTime = 1f / _clip.fps;
                while (_timer >= frameTime)
                {
                    _timer -= frameTime;
                    _frame++;
                    if (_frame >= _clip.frames.Length)
                    {
                        if (_clip.loop) _frame = 0;
                        else
                        {
                            _frame = _clip.frames.Length - 1;
                            _finished = true;
                            ClipFinished?.Invoke(State);
                            break;
                        }
                    }
                }
                ShowFrame();
            }
            else if (_clip != null && !_clip.loop && !_finished)
            {
                // Clip de un solo fotograma: se considera terminado en el acto.
                _finished = true;
                ClipFinished?.Invoke(State);
            }

            UpdateColor();
        }

        private void ShowFrame()
        {
            if (Renderer == null) return;
            if (_clip != null && _clip.IsValid) Renderer.sprite = _clip.frames[Mathf.Clamp(_frame, 0, _clip.frames.Length - 1)];
            else if (Visual != null) Renderer.sprite = Visual.FirstSprite;
        }

        private void UpdateColor()
        {
            Color baseColor = Visual != null ? Visual.tint : Color.white;
            Color c = Time.time < _flashUntil ? _flashColor : baseColor;
            if (_blinking) c.a = Mathf.PingPong(Time.time * 12f, 1f) > 0.5f ? 1f : 0.3f;
            Renderer.color = c;
        }

        private void ApplyFacing()
        {
            Vector2 s = Visual != null ? Visual.scale : Vector2.one;
            bool flip = Visual != null ? (_facingRight != Visual.facesRight) : !_facingRight;
            transform.localScale = new Vector3(flip ? -Mathf.Abs(s.x) : Mathf.Abs(s.x), s.y, 1f);
        }

        private void OnDisable()
        {
            _blinking = false;
            _flashUntil = 0f;
        }
    }
}
