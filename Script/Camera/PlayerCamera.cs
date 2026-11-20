using UnityEngine;
using Game.Player;

/// <summary>
/// Камера от первого лица. Реализует ICameraShaker — новый контроллер найдёт её автоматически
/// через cameraHolder.GetComponent<ICameraShaker>().
/// </summary>
public class PlayerCamera : MonoBehaviour, ICameraShaker
{
    [Header("Настройки обзора")]
    public Transform playerBody;
    public float mouseSensitivity = 2f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Позиция камеры")]
    public float standEyeHeight = 4.0f;
    public float crouchCameraDrop = 1.5f;

    [Header("Наклон при стрейфе (Roll)")]
    public float rollAmount = 2f;
    public float rollSmoothness = 8f;

    [Header("Визуал СЛАЙДА (Камера)")]
    public Vector3 slideCameraOffset = new Vector3(0, -0.2f, 0);
    public Vector3 slideCameraRotation = new Vector3(15f, 0, 5f);

    [Header("Главные переключатели")]
    public bool enableHeadBob = true;
    public bool enableVerticalRecoil = true;
    public bool enableHorizontalRecoil = true;
    public bool enableShake = true;

    [Header("Множители при прицеливании (ПКМ)")]
    [Range(0f, 1f)] public float recoilMultiplierADS = 0.2f;
    [Range(0f, 1f)] public float shakeMultiplierADS = 0.1f;
    [Range(0f, 1f)] public float headBobMultiplierADS = 0.2f;

    [Header("Покачивание (Head Bob) - Ходьба")]
    public float walkBobFrequency = 8f;
    public float walkBobAmplitudeY = 0.03f;
    public float walkBobAmplitudeX = 0.015f;

    [Header("Покачивание (Head Bob) - Бег")]
    public float runBobFrequency = 14f;
    public float runBobAmplitudeY = 0.06f;
    public float runBobAmplitudeX = 0.03f;

    [Header("Сглаживание покачивания")]
    public float bobSmoothing = 10f;

    [Header("Вертикальная отдача")]
    public float verticalKick = 1.2f;
    public float maxVerticalRecoil = 10f;
    public float verticalRecovery = 8f;

    [Header("Горизонтальная отдача")]
    public float horizontalKick = 0.4f;
    public float maxHorizontalRecoil = 3f;
    public float horizontalRecovery = 6f;

    [Header("Мелкая вибрация")]
    public float shakeIntensity = 0.05f;
    public float shakeDamping = 15f;
    public float shakeSpeed = 30f;

    [Header("Тряска при приземлении")]
    public float landingShakeDamping = 10f;

    private float currentLandingShake;
    private float landingShakeTimer;
    private float pitchRotation;
    private float bobTimer;
    private Vector3 bobTargetPos;
    private PlayerController player;
    private float currentVerticalRecoil;
    private float currentHorizontalRecoil;
    private float targetVerticalRecoil;
    private float targetHorizontalRecoil;
    private float currentShake;
    private float shakeTimer;
    private float currentRoll;
    private Vector3 currentSlideOffset = Vector3.zero;
    private Vector3 currentSlideRotation = Vector3.zero;

    private void Start()
    {
        if (playerBody != null) player = playerBody.GetComponent<PlayerController>();
        if (player == null) player = GetComponentInParent<PlayerController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (playerBody == null || player == null) return;

        // ───────── ОБЗОР ─────────
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        pitchRotation -= mouseY;
        pitchRotation = Mathf.Clamp(pitchRotation, minPitch, maxPitch);
        playerBody.Rotate(Vector3.up * mouseX);

        bool isAiming = player.IsAiming;

        // ───────── ОТДАЧА ─────────
        float currentRecoilMult = isAiming ? recoilMultiplierADS : 1f;
        if (enableVerticalRecoil)
        {
            if (!Input.GetMouseButton(0))
                targetVerticalRecoil = Mathf.Lerp(targetVerticalRecoil, 0f, Time.deltaTime * verticalRecovery);
            currentVerticalRecoil = Mathf.Lerp(currentVerticalRecoil, targetVerticalRecoil, Time.deltaTime * 15f);
        }
        if (enableHorizontalRecoil)
        {
            if (!Input.GetMouseButton(0))
                targetHorizontalRecoil = Mathf.Lerp(targetHorizontalRecoil, 0f, Time.deltaTime * horizontalRecovery);
            currentHorizontalRecoil = Mathf.Lerp(currentHorizontalRecoil, targetHorizontalRecoil, Time.deltaTime * 15f);
        }

        // ───────── ВИБРАЦИЯ ─────────
        float currentShakeMult = isAiming ? shakeMultiplierADS : 1f;
        float shakeX = 0f, shakeY = 0f;
        if (enableShake && currentShake > 0f)
        {
            shakeTimer += Time.deltaTime * shakeSpeed;
            currentShake = Mathf.Lerp(currentShake, 0f, Time.deltaTime * shakeDamping);
            shakeX = (Mathf.PerlinNoise(shakeTimer, 0f) - 0.5f) * currentShake * currentShakeMult;
            shakeY = (Mathf.PerlinNoise(0f, shakeTimer) - 0.5f) * currentShake * currentShakeMult;
        }

        // ───────── ТРЯСКА ПРИ ПРИЗЕМЛЕНИИ ─────────
        float landingShakeOffsetY = 0f;
        float landingShakeOffsetPitch = 0f;
        if (currentLandingShake > 0f)
        {
            landingShakeTimer += Time.deltaTime * 20f;
            currentLandingShake = Mathf.Lerp(currentLandingShake, 0f, Time.deltaTime * landingShakeDamping);
            float wave = Mathf.Sin(landingShakeTimer) * Mathf.Exp(-landingShakeTimer * 0.5f);
            landingShakeOffsetY = wave * currentLandingShake * 0.1f * currentShakeMult;
            landingShakeOffsetPitch = wave * currentLandingShake * 2f * currentShakeMult;
        }

        float targetRoll = player.IsGrounded ? -player.MoveDirection.x * rollAmount : 0f;
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * rollSmoothness);

        float slideProgress = player.SlideProgress;
        currentSlideOffset = Vector3.Lerp(Vector3.zero, slideCameraOffset, slideProgress);
        currentSlideRotation = Vector3.Lerp(Vector3.zero, slideCameraRotation, slideProgress);

        float finalPitch = pitchRotation - (currentVerticalRecoil * currentRecoilMult);
        finalPitch = Mathf.Clamp(finalPitch, minPitch, maxPitch);

        transform.localRotation = Quaternion.Euler(
            finalPitch + shakeY + currentSlideRotation.x + landingShakeOffsetPitch,
            (currentHorizontalRecoil * currentRecoilMult) + shakeX + currentSlideRotation.y,
            currentRoll + currentSlideRotation.z
        );

        // ───────── ВЫСОТА КАМЕРЫ ─────────
        float targetHeight = standEyeHeight - (player.CrouchProgress * crouchCameraDrop);
        // Присед перед прыжком (сакват) уже отражён в CrouchProgress нового контроллера,
        // отдельного JumpSquatAmount там нет — если понадобится чётче, добавь:
        // if (player.IsJumpSquatting) targetHeight -= crouchCameraDrop * 0.15f;

        // ───────── ПОКАЧИВАНИЕ ─────────
        float currentBobMult = isAiming ? headBobMultiplierADS : 1f;
        if (enableHeadBob && player.IsGrounded && player.RealSpeed > 0.5f)
        {
            bool isRunning = player.RealSpeed > (player.Movement.WalkSpeed + 1f);
            float currentFreq = isRunning ? runBobFrequency : walkBobFrequency;
            float speedRatio = Mathf.Clamp01(player.RealSpeed / Mathf.Max(player.Movement.RunSpeed, 0.01f));

            float currentAmpY = Mathf.Lerp(walkBobAmplitudeY, runBobAmplitudeY, speedRatio) * currentBobMult;
            float currentAmpX = Mathf.Lerp(walkBobAmplitudeX, runBobAmplitudeX, speedRatio) * currentBobMult;

            bobTimer += Time.deltaTime * player.RealSpeed * currentFreq;
            bobTargetPos.y = Mathf.Sin(bobTimer) * currentAmpY;
            bobTargetPos.x = Mathf.Cos(bobTimer * 0.5f) * currentAmpX;
        }
        else
        {
            bobTargetPos = Vector3.Lerp(bobTargetPos, Vector3.zero, Time.deltaTime * bobSmoothing);
            bobTimer = 0f;
        }

        transform.localPosition = new Vector3(
            bobTargetPos.x + currentSlideOffset.x,
            targetHeight + bobTargetPos.y + currentSlideOffset.y + landingShakeOffsetY,
            currentSlideOffset.z
        );
    }

    public void AddRecoil()
    {
        if (enableVerticalRecoil)
            targetVerticalRecoil = Mathf.Clamp(targetVerticalRecoil + verticalKick, 0f, maxVerticalRecoil);

        if (enableHorizontalRecoil)
        {
            float randomDir = Random.Range(-1f, 1f);
            targetHorizontalRecoil = Mathf.Clamp(
                targetHorizontalRecoil + (horizontalKick * randomDir),
                -maxHorizontalRecoil, maxHorizontalRecoil);
        }

        if (enableShake) currentShake += shakeIntensity;
    }

    /// <summary>Реализация ICameraShaker — вызывается PlayerController при жёстком приземлении.</summary>
    public void Shake(float intensity, float duration)
    {
        // duration не используется намеренно: затухание задаётся landingShakeDamping в инспекторе
        float power = Mathf.Clamp(intensity / Mathf.Max(landingShakeDamping, 0.01f), 0.5f, 2f);
        currentLandingShake = intensity * power;
        landingShakeTimer = 0f;
    }

    /// <summary>Обратная совместимость, если где-то вызывается старый метод.</summary>
    public void TriggerLandingShake(float intensity, float fallSpeed)
    {
        currentLandingShake = intensity * Mathf.Clamp(fallSpeed / 20f, 0.5f, 2f);
        landingShakeTimer = 0f;
    }
}
