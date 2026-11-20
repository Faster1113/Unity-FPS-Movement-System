using UnityEngine;
using Game.Player;

public class WeaponSway : MonoBehaviour
{
    [Header("Главные переключатели")]
    public bool enableSway = true;

    [Header("Прицеливание (ПКМ) - Абсолютная позиция")]
    public Vector3 adsPosition = new Vector3(0, -0.1f, -0.2f);
    public Vector3 adsRotation = new Vector3(0, 0, 0);
    public float adsSpeed = 10f;
    [Range(0f, 1f)] public float swayMultiplierADS = 0.1f;

    [Header("Присед")]
    [Range(0f, 1f)] public float crouchSwayMultiplier = 0.2f;

    [Header("Порог движения")]
    public float movementThreshold = 1.5f;

    [Header("Покачивание при ХОДЬБЕ")]
    public float walkSwaySpeed = 6f;
    public float walkAmountX = 0.02f;
    public float walkAmountY = 0.03f;

    [Header("Покачивание при БЕГЕ")]
    public float runSwaySpeed = 14f;
    public float runAmountX = 0.04f;
    public float runAmountY = 0.06f;

    [Header("Плавность")]
    public float amplitudeSmoothness = 5f;
    public float returnSpeed = 4f;

    [Header("Снижение при стрельбе")]
    [Range(0f, 1f)] public float shootingSwayReduction = 0.8f;
    public float shootingTransitionSpeed = 6f;

    [Header("Поворот при беге")]
    public float runTiltX = -5f;
    public float runTiltY = 0f;
    public float runTiltZ = 3f;
    public float tiltSmoothness = 3f;

    [Header("Визуал СЛАЙДА")]
    public Vector3 slideWeaponOffset = new Vector3(0.1f, -0.1f, -0.2f);
    public Vector3 slideWeaponRotation = new Vector3(10f, -20f, 15f);

    [Header("Синхронизация с шагами")]
    [Tooltip("Скорость перехода положения оружия при беге (совпадает с runInterval в FootstepSystem)")]
    public float runTransitionSpeed = 14f;

    private PlayerController player;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float timer;
    private float smoothedSpeed;
    private float currentSwayMultiplier = 1f;
    private float currentAmountX;
    private float currentAmountY;
    private Vector3 currentSlideOffset = Vector3.zero;
    private Vector3 currentSlideRot = Vector3.zero;
    private Vector3 currentRunTilt = Vector3.zero;
    private Vector3 currentAdsPos;
    private Quaternion currentAdsRot;

    private void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;

        player = GetComponentInParent<PlayerController>();

        currentAdsPos = initialPosition;
        currentAdsRot = initialRotation;
    }

    private void Update()
    {
        if (!enableSway || player == null) return;

        bool isAiming = player.IsAiming;
        bool isCrouching = player.IsCrouching;

        Vector3 targetBasePos = isAiming ? adsPosition : initialPosition;
        Quaternion targetBaseRot = isAiming ? Quaternion.Euler(adsRotation) : initialRotation;

        currentAdsPos = Vector3.Lerp(currentAdsPos, targetBasePos, Time.deltaTime * adsSpeed);
        currentAdsRot = Quaternion.Slerp(currentAdsRot, targetBaseRot, Time.deltaTime * adsSpeed);

        float targetSpeed = player.RealSpeed;
        if (player.IsSliding || player.IsDashing || !player.IsGrounded) targetSpeed = 0f;

        smoothedSpeed = Mathf.Lerp(smoothedSpeed, targetSpeed, Time.deltaTime * runTransitionSpeed);
        float runTiltBlend = 0f;

        if (smoothedSpeed > movementThreshold && player.IsGrounded)
        {
            float walk = player.Movement.WalkSpeed;
            float run = player.Movement.RunSpeed;
            float targetBlend = Mathf.Clamp01((smoothedSpeed - walk) / Mathf.Max(run - walk, 0.01f));
            if (Input.GetMouseButton(0)) targetBlend = 0f;
            runTiltBlend = targetBlend;

            float currentSwaySpeed = Mathf.Lerp(walkSwaySpeed, runSwaySpeed, runTiltBlend);
            float targetAmountX = Mathf.Lerp(walkAmountX, runAmountX, runTiltBlend);
            float targetAmountY = Mathf.Lerp(walkAmountY, runAmountY, runTiltBlend);

            float currentAdsMult = isAiming ? swayMultiplierADS : 1f;
            float currentCrouchMult = isCrouching ? crouchSwayMultiplier : 1f;
            float targetMultiplier = Input.GetMouseButton(0) ? (1f - shootingSwayReduction) : 1f;

            currentSwayMultiplier = Mathf.Lerp(currentSwayMultiplier, targetMultiplier, Time.deltaTime * shootingTransitionSpeed);

            targetAmountX *= currentSwayMultiplier * currentAdsMult * currentCrouchMult;
            targetAmountY *= currentSwayMultiplier * currentAdsMult * currentCrouchMult;

            currentAmountX = Mathf.Lerp(currentAmountX, targetAmountX, Time.deltaTime * amplitudeSmoothness);
            currentAmountY = Mathf.Lerp(currentAmountY, targetAmountY, Time.deltaTime * amplitudeSmoothness);

            if (currentAmountX < 0.0001f && currentAmountY < 0.0001f) timer = 0f;
            timer += Time.deltaTime * currentSwaySpeed;
        }
        else
        {
            currentAmountX = Mathf.Lerp(currentAmountX, 0f, Time.deltaTime * returnSpeed);
            currentAmountY = Mathf.Lerp(currentAmountY, 0f, Time.deltaTime * returnSpeed);
            currentSwayMultiplier = Mathf.Lerp(currentSwayMultiplier, 1f, Time.deltaTime * shootingTransitionSpeed);

            if (currentAmountX < 0.0001f && currentAmountY < 0.0001f)
            {
                timer = 0f;
                currentAmountX = 0f;
                currentAmountY = 0f;
            }
        }

        float xOffset = Mathf.Cos(timer) * currentAmountX;
        float yOffset = Mathf.Sin(timer * 2f) * currentAmountY;

        Vector3 targetTilt = new Vector3(runTiltX * runTiltBlend, runTiltY * runTiltBlend, runTiltZ * runTiltBlend);
        currentRunTilt = Vector3.Lerp(currentRunTilt, targetTilt, Time.deltaTime * tiltSmoothness);

        float slideProgress = player.SlideProgress;
        currentSlideOffset = Vector3.Lerp(Vector3.zero, slideWeaponOffset, slideProgress);
        currentSlideRot = Vector3.Lerp(Vector3.zero, slideWeaponRotation, slideProgress);

        Vector3 finalPos = currentAdsPos + new Vector3(xOffset, yOffset, 0f) + currentSlideOffset;
        Quaternion finalRot = currentAdsRot * Quaternion.Euler(currentRunTilt + currentSlideRot);

        transform.localPosition = finalPos;
        transform.localRotation = finalRot;
    }
}
