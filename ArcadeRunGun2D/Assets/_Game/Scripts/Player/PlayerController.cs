using UnityEngine;

namespace HyperArcade
{
    /// <summary>
    /// Movimiento de plataformas 2D pensado para táctil:
    /// aceleración suave, coyote time, buffer de salto, salto variable y doble salto.
    /// Prefab: Rigidbody2D (Dynamic, Freeze Rotation Z) + CapsuleCollider2D + hijo "GroundCheck"
    /// + hijo "Visual" (SkinnedVisual) + PlayerHealth + PlayerCombat + WeaponController.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private SkinnedVisual visual;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.18f;
        [SerializeField] private LayerMask groundMask;

        [Header("Movimiento")]
        [SerializeField] private float acceleration = 70f;
        [SerializeField] private float deceleration = 80f;
        [Range(0f, 1f)] [SerializeField] private float airControl = 0.7f;

        [Header("Salto")]
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.12f;
        [Range(0f, 1f)] [SerializeField] private float jumpCutMultiplier = 0.5f;
        [SerializeField] private float fallGravityMultiplier = 1.7f;
        [SerializeField] private float maxFallSpeed = 20f;

        public bool FacingRight { get; private set; } = true;
        public bool IsGrounded { get; private set; }
        public bool ControlsEnabled { get; set; } = true;
        public SkinnedVisual Visual => visual;
        public CharacterData Character { get; private set; }

        private Rigidbody2D _rb;
        private float _baseGravity;
        private float _moveSpeed = 7f;
        private float _jumpForce = 14f;
        private int _extraJumps = 1;
        private int _jumpsLeft;
        private float _lastGroundedTime = -10f;
        private float _lastJumpPressedTime = -10f;
        private float _moveInput;
        private float _attackAnimUntil;
        private bool _isJumping;

        private PlayerInputHub Input => PlayerInputHub.Instance;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _baseGravity = _rb.gravityScale > 0 ? _rb.gravityScale : 3f;
            _rb.gravityScale = _baseGravity;
            if (groundMask == 0) groundMask = LayerMask.GetMask("Ground");
            if (visual == null) visual = GetComponentInChildren<SkinnedVisual>();
        }

        /// <summary>Aplica estadísticas y aspecto del personaje seleccionado.</summary>
        public void ApplyCharacter(CharacterData character)
        {
            Character = character;
            _moveSpeed = character.moveSpeed;
            _jumpForce = character.jumpForce;
            _extraJumps = character.extraJumps;
            if (visual != null) visual.SetKey(character.visualKey);
        }

        /// <summary>Dirección de apuntado: horizontal según orientación; arriba/diagonal con el joystick.</summary>
        public Vector2 AimDirection
        {
            get
            {
                Vector2 move = Input != null ? Input.Move : Vector2.zero;
                float fx = FacingRight ? 1f : -1f;
                if (move.y > 0.6f) return Mathf.Abs(move.x) < 0.3f ? Vector2.up : new Vector2(fx, 1f).normalized;
                return new Vector2(fx, 0f);
            }
        }

        public void PlayAttackAnim(float duration = 0.15f) => _attackAnimUntil = Time.time + duration;

        public void Stop()
        {
            _moveInput = 0f;
            _rb.SetVelocity(new Vector2(0f, _rb.GetVelocity().y));
        }

        private void Update()
        {
            CheckGround();

            if (!ControlsEnabled || Input == null)
            {
                _moveInput = 0f;
                UpdateAnimation();
                return;
            }

            _moveInput = Mathf.Abs(Input.Move.x) > 0.1f ? Mathf.Sign(Input.Move.x) * Mathf.Min(1f, Mathf.Abs(Input.Move.x) * 1.4f) : 0f;
            if (_moveInput > 0.01f) FacingRight = true;
            else if (_moveInput < -0.01f) FacingRight = false;

            if (Input.ConsumeJump()) _lastJumpPressedTime = Time.time;
            TryJump();

            // Salto variable: soltar el botón durante la subida la corta una vez.
            Vector2 v = _rb.GetVelocity();
            if (_isJumping && v.y <= 0f) _isJumping = false;
            if (_isJumping && !Input.JumpHeld)
            {
                _rb.SetVelocity(new Vector2(v.x, v.y * jumpCutMultiplier));
                _isJumping = false;
            }

            UpdateAnimation();
        }

        private void FixedUpdate()
        {
            Vector2 v = _rb.GetVelocity();
            float target = _moveInput * _moveSpeed;
            float rate = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;
            if (!IsGrounded) rate *= airControl;
            v.x = Mathf.MoveTowards(v.x, target, rate * Time.fixedDeltaTime);

            _rb.gravityScale = v.y < 0f ? _baseGravity * fallGravityMultiplier : _baseGravity;
            v.y = Mathf.Max(v.y, -maxFallSpeed);
            _rb.SetVelocity(v);
        }

        private void CheckGround()
        {
            Vector2 p = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
            bool grounded = Physics2D.OverlapCircle(p, groundCheckRadius, groundMask) != null;
            if (grounded && _rb.GetVelocity().y <= 0.01f)
            {
                _lastGroundedTime = Time.time;
                _jumpsLeft = _extraJumps;
            }
            IsGrounded = grounded;
        }

        private void TryJump()
        {
            if (Time.time - _lastJumpPressedTime > jumpBufferTime) return;

            bool canGroundJump = Time.time - _lastGroundedTime <= coyoteTime;
            if (!canGroundJump && _jumpsLeft <= 0) return;
            if (!canGroundJump) _jumpsLeft--;

            _lastJumpPressedTime = -10f;
            _lastGroundedTime = -10f;
            _rb.SetVelocity(new Vector2(_rb.GetVelocity().x, _jumpForce));
            _isJumping = true;
            AudioManager.Play(SfxId.Jump);
        }

        private void UpdateAnimation()
        {
            if (visual == null) return;
            visual.SetFacing(FacingRight);
            if (visual.State == AnimState.Death) return;

            Vector2 v = _rb.GetVelocity();
            AnimState state;
            if (Time.time < _attackAnimUntil) state = AnimState.Attack;
            else if (!IsGrounded) state = v.y > 0.1f ? AnimState.Jump : AnimState.Fall;
            else state = Mathf.Abs(v.x) > 0.2f ? AnimState.Run : AnimState.Idle;
            visual.SetState(state);
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
