// PlayerController.cs
using System;
using UnityEngine;
using TMPro;

namespace Game.Player
{
    /// <summary>
    /// Продакшн-контроллер игрока. Обязателен CharacterController.
    /// Не знает про UI/аудио/камеру — общается через события и интерфейсы.
    /// Ввод изолирован в ReadInput (заменяется на Input System без изменения логики).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
        // ───────────── КОНФИГУРАЦИЯ ─────────────
        [Header("Configuration")]
        [SerializeField] private MovementConfig movement = new MovementConfig();
        [SerializeField] private GravityConfig gravity = new GravityConfig();
        [SerializeField] private JumpConfig jump = new JumpConfig();
        [SerializeField] private CrouchConfig crouch = new CrouchConfig();
        [SerializeField] private SlideConfig slide = new SlideConfig();
        [SerializeField] private DashConfig dash = new DashConfig();
        [SerializeField] private CameraShakeConfig cameraShake = new CameraShakeConfig();
        [SerializeField] private InteractionConfig interaction = new InteractionConfig();

        [Header("Input")]
        [SerializeField] private InputConfig input = new InputConfig();

        [Header("Presentation")]
        [SerializeField] private CursorConfig cursor = new CursorConfig();
        [SerializeField] private AnimatorConfig animatorParams = new AnimatorConfig();
        [SerializeField] private DebugConfig debug = new DebugConfig();

        // ───────────── ССЫЛКИ ─────────────
        [Header("References (optional)")]
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private Animator modelAnimator;
        [SerializeField] private TMP_Text interactText;

        // ───────────── СОБЫТИЯ ─────────────
        public event Action OnJump;
        public event Action OnFallStart;
        public event Action<float> OnLand;
        public event Action<float> OnLandWithFallSpeed;
        public event Action OnDashStart;
        public event Action OnDashEnd;
        public event Action OnSlideStart;
        public event Action OnSlideEnd;
        public event Action OnCrouchStart;
        public event Action OnCrouchEnd;

        // ───────────── ПУБЛИЧНЫЕ СВОЙСТВА (readonly) ─────────────
        public MovementConfig Movement => movement;
        public GravityConfig Gravity => gravity;
        public JumpConfig Jump => jump;
        public CrouchConfig Crouch => crouch;
        public SlideConfig Slide => slide;
        public DashConfig Dash => dash;

        public bool IsGrounded { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsCrouching { get; private set; }
        public bool IsSliding { get; private set; }
        public bool IsDashing { get; private set; }
        public bool IsJumpSquatting { get; private set; }
        public bool IsAiming { get; private set; }

        public float CurrentSpeed => _horizontalVelocity.magnitude;
        public float RealSpeed { get; private set; }
        public float CrouchProgress { get; private set; }
        public float SlideProgress { get; private set; }
        public float VerticalVelocity => _verticalVelocity;
        public float LastFallSpeed { get; private set; }

        public Vector3 MoveDirection => _horizontalVelocity.sqrMagnitude > 0.001f
            ? _horizontalVelocity.normalized : Vector3.zero;
        public Vector3 Velocity => _horizontalVelocity + Vector3.up * _verticalVelocity;
        public CollisionFlags LastCollisionFlags { get; private set; }

        // ───────────── ПРИВАТНЫЕ СОСТОЯНИЯ ─────────────
        private CharacterController _controller;
        private ICameraShaker _cameraShaker;
        private Camera _camera;
        private Transform _transform;

        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private Vector3 _lastPosition;
        private Vector3 _lastMoveWorld;
        private float _feetOffset;
        private Vector3 _initialCenter;

        private bool _wasGrounded;
        private bool _wasInAir;
        private float _lastGroundedTime = -999f;
        private float _ignoreGroundUntil = -999f;
        private float _jumpBufferTimer;
        private float _jumpSquatTimer;
        private float _pendingJumpVelocity;
        private bool _jumpCutApplied;

        private bool _isSlideActive;
        private float _slideTimer;
        private float _slideSpeed;
        private Vector3 _slideDirection;

        private float _dashTimer;
        private float _nextDashTime;
        private Vector3 _dashDirection;

        private PlayerInputState _inputState;

        private IInteractable _interactable;
        private Component _interactableComponent;

        // Хеши аниматора (инициализируются в Awake из настроек)
        private int _hashSpeed, _hashGrounded, _hashRunning, _hashCrouch, _hashSlide, _hashDash;

        // ───────────── ЖИЗНЕННЫЙ ЦИКЛ ─────────────
        private void Awake()
        {
            _transform = transform;
            InitializeComponents();
            ValidateSettings();
            InitializeParameters();
            InitializeGeometry();
            InitializeAnimatorHashes();
        }

        private void OnEnable()
        {
            if (cursor.LockOnEnable) LockCursor(true);
            _lastPosition = _transform.position;
            _wasGrounded = IsGrounded;
        }

        private void OnDisable()
        {
            if (cursor.UnlockOnDisable) LockCursor(false);
            SetInteractText(false);
        }

        private void OnValidate()
        {
            ValidateSettings();
        }

        private void Update()
        {
            if (!_controller.enabled) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            ReadInput();
            UpdateGrounding(dt);
            UpdateCrouchAndSlide(dt);
            UpdateDash(dt);
            UpdateJump(dt);
            ApplyMovement(dt);
            UpdateRealSpeed(dt);
            HandleFallDetection();
            HandleLanding();
            UpdateInteraction();
            TryInteract();
            UpdateAnimator();
        }

        private void OnDrawGizmosSelected()
        {
            if (!debug.DrawGizmos || !Application.isPlaying || _controller == null) return;

            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Vector3 origin = _transform.TransformPoint(_controller.center);
            origin.y -= _controller.height * 0.5f - _controller.radius;
            Gizmos.DrawWireSphere(origin + Vector3.down * (gravity.GroundCheckDistance + _controller.skinWidth),
                                  _controller.radius * 0.9f);
        }

        // ───────────── ИНИЦИАЛИЗАЦИЯ ─────────────
        private void InitializeComponents()
        {
            _controller = GetComponent<CharacterController>();

            if (cameraHolder != null)
            {
                _camera = cameraHolder.GetComponent<Camera>() ?? cameraHolder.GetComponentInChildren<Camera>();
                _cameraShaker = cameraHolder.GetComponent<ICameraShaker>();
            }

            if (_camera == null) _camera = Camera.main;
            if (_cameraShaker == null && _camera != null)
                _cameraShaker = _camera.GetComponent<ICameraShaker>();

            if (modelAnimator == null) modelAnimator = GetComponentInChildren<Animator>();

#if UNITY_EDITOR
            if (cameraHolder == null)
                Debug.LogWarning($"[{GetType().Name}] cameraHolder не назначен — движение относительно камеры не будет работать.", this);
#endif
        }

        private void InitializeParameters()
        {
            if (crouch.StandingHeight <= 0f) crouch.StandingHeight = _controller.height;

            float minHeight = _controller.radius * 2f + 0.01f;
            if (crouch.CrouchHeight < minHeight) crouch.CrouchHeight = minHeight;

            if (gravity.Gravity <= 0f)
            {
                Debug.LogWarning($"[{GetType().Name}] Gravity <= 0, устанавливаю 25f.", this);
                gravity.Gravity = 25f;
            }
        }

        private void InitializeGeometry()
        {
            _controller.height = crouch.StandingHeight;
            _controller.center = new Vector3(0f, crouch.StandingHeight * 0.5f, 0f);
            _initialCenter = _controller.center;
            _feetOffset = _controller.center.y - crouch.StandingHeight * 0.5f;
        }

        private void InitializeAnimatorHashes()
        {
            _hashSpeed = Animator.StringToHash(animatorParams.SpeedParam);
            _hashGrounded = Animator.StringToHash(animatorParams.IsGroundedParam);
            _hashRunning = Animator.StringToHash(animatorParams.IsRunningParam);
            _hashCrouch = Animator.StringToHash(animatorParams.IsCrouchingParam);
            _hashSlide = Animator.StringToHash(animatorParams.IsSlidingParam);
            _hashDash = Animator.StringToHash(animatorParams.IsDashingParam);
        }

        private void ValidateSettings()
        {
            var cc = _controller != null ? _controller : GetComponent<CharacterController>();
            if (cc != null)
            {
                float minHeight = cc.radius * 2f + 0.01f;
                if (crouch.CrouchHeight < minHeight) crouch.CrouchHeight = minHeight;
                if (crouch.StandingHeight <= 0f) crouch.StandingHeight = cc.height;
            }
            jump.Height = Mathf.Max(jump.Height, 0.01f);
            slide.Duration = Mathf.Max(slide.Duration, 0.05f);
            dash.Duration = Mathf.Max(dash.Duration, 0.01f);
        }

        // ───────────── ВВОД ─────────────
        /// <summary>Единственная точка привязки к системе ввода.</summary>
        private void ReadInput()
        {
            Vector2 raw = new Vector2(
                Input.GetAxisRaw(input.HorizontalAxis),
                Input.GetAxisRaw(input.VerticalAxis));

            Vector3 worldMove = cameraHolder != null
                ? cameraHolder.right * raw.x + cameraHolder.forward * raw.y
                : _transform.right * raw.x + _transform.forward * raw.y;

            worldMove.y = 0f;
            if (worldMove.sqrMagnitude > 1f) worldMove.Normalize();

            _inputState = new PlayerInputState
            {
                Move = raw,
                WorldMove = worldMove,
                Run = Input.GetKey(input.Run),
                Crouch = Input.GetKey(input.Crouch) ||
                                  (input.CrouchAlt != KeyCode.None && Input.GetKey(input.CrouchAlt)),
                JumpPressed = Input.GetKeyDown(input.Jump) || Input.GetButtonDown(input.JumpButton),
                JumpHeld = Input.GetKey(input.Jump) || Input.GetButton(input.JumpButton),
                SlidePressed = Input.GetKeyDown(input.Slide),
                DashPressed = Input.GetKeyDown(input.Dash),
                InteractPressed = Input.GetKeyDown(input.Interact),
                AimHeld = Input.GetMouseButton(1),
            };

            IsAiming = _inputState.AimHeld && !IsSliding && !IsDashing && IsGrounded;
            _lastMoveWorld = worldMove;
        }

        // ───────────── ФИЗИКА / GROUNDING ─────────────
        private void UpdateGrounding(float dt)
        {
            _wasGrounded = IsGrounded;

            if (Time.time < _ignoreGroundUntil)
            {
                IsGrounded = false;
            }
            else
            {
                Vector3 origin = _transform.TransformPoint(_controller.center);
                origin.y -= _controller.height * 0.5f - _controller.radius;

                IsGrounded =
                    Physics.SphereCast(origin, _controller.radius * 0.9f, Vector3.down, out _,
                        gravity.GroundCheckDistance + _controller.skinWidth,
                        gravity.GroundMask, QueryTriggerInteraction.Ignore)
                    || (_controller.isGrounded && _verticalVelocity <= 0.01f);
            }

            if (IsGrounded) _lastGroundedTime = Time.time;

            if (IsGrounded && !IsJumpSquatting)
                _verticalVelocity = -gravity.GroundStickForce;
            else
            {
                _verticalVelocity -= gravity.Gravity * dt;
                if (gravity.MaxFallSpeed > 0f)
                    _verticalVelocity = Mathf.Max(_verticalVelocity, -gravity.MaxFallSpeed);
            }

            if (!IsGrounded)
                LastFallSpeed = Mathf.Max(LastFallSpeed, -_verticalVelocity);
        }

        // ───────────── ДВИЖЕНИЕ ─────────────
        private void ApplyMovement(float dt)
        {
            if (IsDashing)
            {
                _horizontalVelocity = _dashDirection * dash.Speed;
                if (dash.ZeroVerticalVelocity) _verticalVelocity = 0f;
            }
            else if (IsSliding)
            {
                Vector3 target = _slideDirection * _slideSpeed;
                float rate = movement.Acceleration * slide.AccelMultiplier;
                _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, target, rate * dt);
            }
            else
            {
                IsRunning = _inputState.Run && !IsCrouching &&
                            _lastMoveWorld.sqrMagnitude > 0.01f &&
                            (IsGrounded || !movement.RunOnlyOnGround);

                float speed = IsCrouching ? movement.CrouchSpeed
                            : IsRunning ? movement.RunSpeed
                                          : movement.WalkSpeed;

                Vector3 target = IsJumpSquatting ? Vector3.zero : _lastMoveWorld * speed;
                float rate = _lastMoveWorld.sqrMagnitude > 0.01f ? movement.Acceleration : movement.Deceleration;
                if (!IsGrounded) rate *= movement.AirControlMultiplier;

                _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, target, rate * dt);
            }

            LastCollisionFlags = _controller.Move(
                (_horizontalVelocity + Vector3.up * _verticalVelocity) * dt);

            if ((LastCollisionFlags & CollisionFlags.Sides) != 0)
                _horizontalVelocity *= 0.9f;
        }

        private void UpdateRealSpeed(float dt)
        {
            Vector3 delta = _transform.position - _lastPosition;
            delta.y = 0f;
            RealSpeed = delta.magnitude / Mathf.Max(dt, 0.0001f);
            _lastPosition = _transform.position;
        }

        // ───────────── ПРЫЖОК ─────────────
        private void UpdateJump(float dt)
        {
            if (IsJumpSquatting)
            {
                _jumpSquatTimer += dt;
                if (_jumpSquatTimer >= jump.SquatDuration) ExecuteJump();
                return;
            }

            if (_inputState.JumpPressed) _jumpBufferTimer = jump.BufferTime;
            else _jumpBufferTimer -= dt;

            if (!IsGrounded && _verticalVelocity > 0f && !_jumpCutApplied && !_inputState.JumpHeld)
            {
                _verticalVelocity *= jump.VariableJumpMultiplier;
                _jumpCutApplied = true;
            }

            bool canCoyote = Time.time - _lastGroundedTime <= jump.CoyoteTime;
            bool allowed = !IsCrouching && !IsSliding &&
                           (jump.AllowDuringDash || !IsDashing) &&
                           (IsGrounded || canCoyote);

            if (_jumpBufferTimer > 0f && allowed)
            {
                _jumpBufferTimer = 0f;
                _pendingJumpVelocity = Mathf.Sqrt(jump.Height * 2f * gravity.Gravity);

                if (jump.SquatDuration > 0.001f)
                {
                    IsJumpSquatting = true;
                    _jumpSquatTimer = 0f;
                }
                else
                {
                    ExecuteJump();
                }
            }
        }

        private void ExecuteJump()
        {
            IsJumpSquatting = false;
            IsDashing = false;
            _verticalVelocity = _pendingJumpVelocity;
            _jumpCutApplied = false;
            IsGrounded = false;
            _wasGrounded = false;
            _lastGroundedTime = -999f;
            _ignoreGroundUntil = Time.time + jump.GroundIgnoreAfterJump;
            LastFallSpeed = 0f;
            _wasInAir = true;

            OnJump?.Invoke();
            LogEvent("Jump");
        }

        // ───────────── ПРИСЕД / СЛАЙД ─────────────
        private void UpdateCrouchAndSlide(float dt)
        {
            if (IsSliding)
            {
                _slideTimer -= dt;
                _slideSpeed = Mathf.MoveTowards(_slideSpeed, 0f, slide.Deceleration * dt);

                bool released = slide.StopOnInputRelease && _lastMoveWorld.sqrMagnitude < 0.01f;
                bool inAir = slide.StopInAir && !IsGrounded;
                bool tooSlow = _slideSpeed <= movement.CrouchSpeed + 0.05f;

                if (_slideTimer <= 0f || released || inAir || tooSlow) StopSlide();
            }

            bool wantCrouch = (_inputState.Crouch || IsSliding) && !IsDashing;
            if (wantCrouch != IsCrouching)
            {
                IsCrouching = wantCrouch;
                if (wantCrouch) OnCrouchStart?.Invoke();
                else OnCrouchEnd?.Invoke();
                LogEvent(wantCrouch ? "CrouchStart" : "CrouchEnd");
            }

            CrouchProgress = Mathf.MoveTowards(CrouchProgress, IsCrouching ? 1f : 0f,
                                               crouch.TransitionSpeed * dt);
            if (!IsCrouching && CrouchProgress <= 0.01f) CrouchProgress = 0f;

            SlideProgress = Mathf.MoveTowards(SlideProgress, IsSliding ? 1f : 0f,
                                              slide.ProgressSmoothSpeed * dt);

            float newHeight = Mathf.Lerp(crouch.StandingHeight, crouch.CrouchHeight, CrouchProgress);
            UpdateControllerGeometry(newHeight);

            if (_inputState.SlidePressed && !IsSliding && IsGrounded && _lastMoveWorld.sqrMagnitude > 0.01f)
                StartSlide();
        }

        private void StartSlide()
        {
            if (IsDashing || IsJumpSquatting) return;

            IsSliding = true;
            _slideTimer = slide.Duration;
            _slideSpeed = Mathf.Max(slide.Speed, CurrentSpeed);
            _slideDirection = _lastMoveWorld.normalized;

            OnSlideStart?.Invoke();
            LogEvent("SlideStart");
        }

        private void StopSlide()
        {
            if (!IsSliding) return;
            IsSliding = false;
            OnSlideEnd?.Invoke();
            LogEvent("SlideEnd");
        }

        private void UpdateControllerGeometry(float height)
        {
            _controller.height = height;
            _controller.center = new Vector3(_initialCenter.x, _feetOffset + height * 0.5f, _initialCenter.z);
        }

        // ───────────── РЫВОК ─────────────
        private void TryStartDash()
        {
            if (IsDashing || Time.time < _nextDashTime) return;
            if (dash.OnlyOnGround && !IsGrounded) return;
            if (!_inputState.DashPressed) return;

            Vector3 dir;
            if (_lastMoveWorld.sqrMagnitude > 0.01f) dir = _lastMoveWorld.normalized;
            else if (dash.ForwardWhenNoInput)
                dir = cameraHolder != null ? FlatForward(cameraHolder.forward) : FlatForward(_transform.forward);
            else
                return;

            if (Physics.Raycast(_transform.position, dir, dash.ObstacleCheckDistance,
                                dash.ObstacleMask, QueryTriggerInteraction.Ignore))
            {
                LogEvent("DashBlocked");
                return;
            }

            IsDashing = true;
            IsSliding = false;
            IsJumpSquatting = false;
            _dashTimer = dash.Duration;
            _nextDashTime = Time.time + dash.Cooldown;
            _dashDirection = dir;
            if (dash.ZeroVerticalVelocity) _verticalVelocity = 0f;

            OnDashStart?.Invoke();
            LogEvent("DashStart");
        }

        private void UpdateDash(float dt)
        {
            TryStartDash();
            if (!IsDashing) return;

            _dashTimer -= dt;
            if (_dashTimer <= 0f)
            {
                IsDashing = false;
                _horizontalVelocity = _dashDirection * movement.WalkSpeed;
                OnDashEnd?.Invoke();
                LogEvent("DashEnd");
            }
        }

        // ───────────── ПАДЕНИЕ / ПРИЗЕМЛЕНИЕ ─────────────
        private void HandleFallDetection()
        {
            if (!_wasInAir && !IsGrounded && !_wasGrounded && _verticalVelocity < -1f)
            {
                _wasInAir = true;
                OnFallStart?.Invoke();
                LogEvent("FallStart");
            }

            if (IsGrounded) _wasInAir = false;
        }

        private void HandleLanding()
        {
            if (!IsGrounded || _wasGrounded) return;

            OnLand?.Invoke(LastFallSpeed);
            if (LastFallSpeed >= cameraShake.MinFallSpeedForShake)
            {
                if (_cameraShaker != null)
                {
                    float t = Mathf.InverseLerp(
                        cameraShake.MinFallSpeedForShake,
                        cameraShake.MinFallSpeedForShake * cameraShake.MaxShakeFallSpeedFactor,
                        LastFallSpeed);

                    float intensity = cameraShake.LandingIntensity *
                                      Mathf.Max(t, cameraShake.MinIntensityFactor);

                    _cameraShaker.Shake(intensity, 0.15f);
                }

                OnLandWithFallSpeed?.Invoke(LastFallSpeed);
            }

            LastFallSpeed = 0f;
            LogEvent("Land");
        }

        // ───────────── ВЗАИМОДЕЙСТВИЕ ─────────────
        private void UpdateInteraction()
        {
            if (_camera == null) return;

            Ray ray = interaction.RayFromScreenCenter
                ? _camera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f))
                : new Ray(_camera.transform.position, _camera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, interaction.Distance,
                                interaction.Mask, QueryTriggerInteraction.Ignore))
            {
                var candidate = hit.collider.GetComponentInParent<IInteractable>();
                if (candidate != null)
                {
                    _interactable = candidate;
                    _interactableComponent = candidate as Component;

                    SetInteractText(true, candidate.GetInteractPrompt());
                    return;
                }
            }

            _interactable = null;
            _interactableComponent = null;
            SetInteractText(false);
        }

        private void TryInteract()
        {
            if (!_inputState.InteractPressed || _interactable == null) return;

            _interactable.Interact(gameObject);
            LogEvent($"Interacted with {_interactableComponent?.name}");
        }

        private void SetInteractText(bool visible, string prompt = null)
        {
            if (interactText == null) return;

            interactText.gameObject.SetActive(visible);
            if (visible && prompt != null) interactText.text = prompt;
        }

        // ───────────── АНИМАТОР ─────────────
        private void UpdateAnimator()
        {
            if (modelAnimator == null) return;

            modelAnimator.SetFloat(_hashSpeed, RealSpeed);
            modelAnimator.SetBool(_hashGrounded, IsGrounded);
            modelAnimator.SetBool(_hashRunning, IsRunning);
            modelAnimator.SetBool(_hashCrouch, IsCrouching);
            modelAnimator.SetBool(_hashSlide, IsSliding);
            modelAnimator.SetBool(_hashDash, IsDashing);
        }

        // ───────────── ВСПОМОГАТЕЛЬНЫЕ ─────────────
        private void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogEvent(string message)
        {
            if (!debug.LogEvents) return;
            Debug.Log($"[{GetType().Name}] {message} at {Time.time:F3}s", this);
        }

        private static Vector3 FlatForward(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
        }
    }
}
