using UnityEngine;
using Game.Player;

public class WeaponJumpReaction : MonoBehaviour
{
    [Header("--- РЕАКЦИЯ НА ПРЫЖОК / ПОДЪЕМ ---")]
    public Vector3 jumpOffsetPos = new Vector3(0f, -0.05f, 0f);
    public Vector3 jumpOffsetRot = new Vector3(-5f, 0f, 0f);
    public float jumpSpeed = 10f;

    [Header("--- РЕАКЦИЯ НА ПАДЕНИЕ ---")]
    public Vector3 fallOffsetPos = new Vector3(0f, 0.05f, 0f);
    public Vector3 fallOffsetRot = new Vector3(5f, 0f, 0f);
    public float fallSpeed = 10f;

    [Header("--- РЕАКЦИЯ НА ПРИЗЕМЛЕНИЕ ---")]
    public Vector3 landOffsetPos = new Vector3(0f, -0.2f, 0f);
    public Vector3 landOffsetRot = new Vector3(15f, 0f, 0f);
    public float landRecoverySpeed = 6f;

    [Header("--- НАСТРОЙКИ СИЛЫ ПРИЗЕМЛЕНИЯ ---")]
    public float minFallSpeed = 4f;
    public float maxFallSpeed = 15f;
    public AnimationCurve fallSpeedMultiplier = AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1f);

    private PlayerController player;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 currentOffsetPos = Vector3.zero;
    private Quaternion currentOffsetRot = Quaternion.identity;
    private bool wasGroundedLastFrame;
    private bool justLanded;
    private float impactPower;

    private void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        player = GetComponentInParent<PlayerController>();

        if (player == null)
            Debug.LogError($"[{GetType().Name}] PlayerController не найден в родителях {name}.", this);
    }

    private void LateUpdate()
    {
        if (player == null) return;

        bool isGrounded = player.IsGrounded;

        if (!wasGroundedLastFrame && isGrounded)
        {
            float impactSpeed = player.LastFallSpeed;
            if (impactSpeed >= minFallSpeed)
            {
                justLanded = true;
                float speedRatio = Mathf.Clamp01((impactSpeed - minFallSpeed) / Mathf.Max(maxFallSpeed - minFallSpeed, 0.01f));
                impactPower = fallSpeedMultiplier.Evaluate(speedRatio);
            }
        }
        wasGroundedLastFrame = isGrounded;

        Vector3 targetPos = Vector3.zero;
        Quaternion targetRot = Quaternion.identity;
        float currentSpeed = landRecoverySpeed;

        if (justLanded)
        {
            targetPos = landOffsetPos * impactPower;
            targetRot = Quaternion.Euler(landOffsetRot * impactPower);
            justLanded = false;
        }
        else if (!isGrounded)
        {
            float verticalVelocity = player.VerticalVelocity;

            if (verticalVelocity > 0.1f)
            {
                targetPos = jumpOffsetPos;
                targetRot = Quaternion.Euler(jumpOffsetRot);
                currentSpeed = jumpSpeed;
            }
            else if (verticalVelocity < -0.1f)
            {
                targetPos = fallOffsetPos;
                targetRot = Quaternion.Euler(fallOffsetRot);
                currentSpeed = fallSpeed;
            }
        }

        currentOffsetPos = Vector3.Lerp(currentOffsetPos, targetPos, Time.deltaTime * currentSpeed);
        currentOffsetRot = Quaternion.Slerp(currentOffsetRot, targetRot, Time.deltaTime * currentSpeed);

        transform.localPosition = initialPosition + currentOffsetPos;
        transform.localRotation = initialRotation * currentOffsetRot;
    }
}
