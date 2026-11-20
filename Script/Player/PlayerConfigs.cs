// PlayerConfigs.cs
using UnityEngine;

namespace Game.Player
{
    [System.Serializable]
    public sealed class MovementConfig
    {
        [Header("Speeds")]
        [Min(0f)] public float WalkSpeed = 4f;
        [Min(0f)] public float RunSpeed = 7f;
        [Min(0f)] public float CrouchSpeed = 2f;

        [Header("Acceleration")]
        [Tooltip("Разгон при появлении ввода, м/с²")]
        [Min(0f)] public float Acceleration = 20f;
        [Tooltip("Торможение при снятии ввода, м/с²")]
        [Min(0f)] public float Deceleration = 45f;
        [Range(0f, 1f)] public float AirControlMultiplier = 0.4f;

        public bool RunOnlyOnGround = true;
    }

    [System.Serializable]
    public sealed class GravityConfig
    {
        [Tooltip("Положительная величина, м/с²")]
        [Min(0f)] public float Gravity = 25f;
        [Min(0f)] public float MaxFallSpeed = 50f;
        [Min(0f)] public float GroundStickForce = 2f;
        [Min(0f)] public float GroundCheckDistance = 0.15f;
        public LayerMask GroundMask = ~0;
    }

    [System.Serializable]
    public sealed class JumpConfig
    {
        [Min(0f)] public float Height = 1.5f;
        [Min(0f)] public float CoyoteTime = 0.15f;
        [Min(0f)] public float BufferTime = 0.2f;
        [Range(0f, 1f)] public float VariableJumpMultiplier = 0.5f;
        [Min(0f)] public float SquatDuration = 0.08f;
        [Tooltip("Время игнорирования земли после старта прыжка, с")]
        [Min(0f)] public float GroundIgnoreAfterJump = 0.1f;
        public bool AllowDuringDash = false;
    }

    [System.Serializable]
    public sealed class CrouchConfig
    {
        [Tooltip("0 = взять высоту из CharacterController в Awake")]
        [Min(0f)] public float StandingHeight = 0f;
        [Min(0.1f)] public float CrouchHeight = 1f;
        [Min(0.1f)] public float TransitionSpeed = 10f;
    }

    [System.Serializable]
    public sealed class SlideConfig
    {
        [Min(0f)] public float Speed = 12f;
        [Min(0.05f)] public float Duration = 0.8f;
        [Min(0f)] public float Deceleration = 10f;
        public bool StopOnInputRelease = true;
        public bool StopInAir = true;
        [Min(0.1f)] public float ProgressSmoothSpeed = 5f;
        [Min(0.1f)] public float AccelMultiplier = 2f;
    }

    [System.Serializable]
    public sealed class DashConfig
    {
        [Min(0f)] public float Speed = 18f;
        [Min(0.01f)] public float Duration = 0.15f;
        [Min(0f)] public float Cooldown = 1f;
        public bool OnlyOnGround = false;
        public bool ZeroVerticalVelocity = true;
        public bool ForwardWhenNoInput = true;
        public LayerMask ObstacleMask = ~0;
        [Min(0.1f)] public float ObstacleCheckDistance = 2f;
    }

    [System.Serializable]
    public sealed class CameraShakeConfig
    {
        [Min(0f)] public float LandingIntensity = 0.08f;
        [Min(0f)] public float MinFallSpeedForShake = 8f;
        [Tooltip("Верхняя граница шкалы тряски = MinFallSpeed × этот множитель")]
        [Min(1.1f)] public float MaxShakeFallSpeedFactor = 3f;
        [Range(0f, 1f)] public float MinIntensityFactor = 0.35f;
    }

    [System.Serializable]
    public sealed class InteractionConfig
    {
        [Min(0.1f)] public float Distance = 3f;
        public LayerMask Mask = ~0;
        [Tooltip("Рейкаст из центра экрана вместо forward камеры")]
        public bool RayFromScreenCenter = false;
    }

    [System.Serializable]
    public sealed class InputConfig
    {
        public KeyCode Run = KeyCode.LeftShift;
        public KeyCode Crouch = KeyCode.LeftControl;
        public KeyCode CrouchAlt = KeyCode.C;
        public KeyCode Jump = KeyCode.Space;
        public KeyCode Slide = KeyCode.Q;
        public KeyCode Dash = KeyCode.LeftAlt;
        public KeyCode Interact = KeyCode.E;

        public string HorizontalAxis = "Horizontal";
        public string VerticalAxis = "Vertical";
        public string JumpButton = "Jump";
    }

    [System.Serializable]
    public sealed class AnimatorConfig
    {
        public string SpeedParam = "Speed";
        public string IsGroundedParam = "IsGrounded";
        public string IsRunningParam = "IsRunning";
        public string IsCrouchingParam = "IsCrouching";
        public string IsSlidingParam = "IsSliding";
        public string IsDashingParam = "IsDashing";
    }

    [System.Serializable]
    public sealed class CursorConfig
    {
        public bool LockOnEnable = true;
        public bool UnlockOnDisable = true;
    }

    [System.Serializable]
    public sealed class DebugConfig
    {
        public bool DrawGizmos = true;
        public bool LogEvents = false;
    }
}
