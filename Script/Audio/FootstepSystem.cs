using UnityEngine;
using Game.Player; // ВАЖНО: Namespace нового контроллера

[RequireComponent(typeof(AudioSource))]
public class FootstepSystem : MonoBehaviour
{
    [Header("Звуки")]
    public AudioClip[] walkSounds;
    public AudioClip[] runSounds;
    public AudioClip[] descendWalkSounds;
    public AudioClip[] descendRunSounds;

    [Header("Ритм (Интервалы)")]
    public float walkInterval = 0.6f;
    public float runInterval = 0.35f;
    public float descendWalkInterval = 0.45f;
    public float descendRunInterval = 0.28f;

    [Header("Присед (Ctrl)")]
    public float crouchWalkInterval = 0.8f;
    [Range(0f, 1f)] public float crouchVolume = 0.15f;

    [Header("Громкость")]
    [Range(0f, 1f)] public float walkVolume = 0.3f;
    [Range(0f, 1f)] public float runVolume = 0.6f;
    [Range(0f, 1f)] public float descendWalkVolume = 0.4f;
    [Range(0f, 1f)] public float descendRunVolume = 0.7f;

    [Header("Сглаживание перехода Бег ↔ Ходьба")]
    [Tooltip("Скорость плавного перехода между звуками бега и ходьбы (чем выше, тем резче)")]
    [Range(1f, 20f)] public float transitionSpeed = 8f;

    [Tooltip("Порог скорости: ниже этого значения звук полностью переходит на ходьбу")]
    [Min(0f)] public float runSoundSpeedThreshold = 5.5f;

    [Header("Сглаживание остановки")]
    public float fadeOutSpeed = 50f;
    public float minInputTimeForStep = 0.1f;
    public float stopDelay = 0.15f;
    [Range(0.01f, 0.5f)] public float inputThreshold = 0.2f;
    [Range(0.01f, 2f)] public float minSpeedForStep = 0.2f;

    private bool isDescending = false;
    private PlayerController player;
    private AudioSource audioSource;
    private float nextStepTime;
    private bool isStepping;
    private bool isFading = false;
    private float inputHoldTimer = 0f;
    private float stopDelayTimer = 0f;

    // НОВЫЕ ПЕРЕМЕННЫЕ ДЛЯ ПЛАВНОГО ПЕРЕХОДА
    private float currentRunBlend = 0f; // 0 = ходьба, 1 = бег
    private float currentInterval;
    private float currentVolume;
    private AudioClip[] currentClips;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            Debug.LogError("FootstepSystem requires an AudioSource component.", this);
            return;
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = 0f;

        // Инициализируем начальные значения
        currentInterval = walkInterval;
        currentVolume = walkVolume;
        currentClips = walkSounds;
    }

    public void SetDescending(bool value)
    {
        if (isDescending != value)
        {
            isDescending = value;
            isStepping = false;
            nextStepTime = Time.time;
        }
    }

    void Update()
    {
        if (!player || !audioSource) return;

        if (isFading)
        {
            audioSource.volume = Mathf.MoveTowards(audioSource.volume, 0f, Time.deltaTime * fadeOutSpeed);
            if (audioSource.volume <= 0.01f)
            {
                audioSource.Stop();
                isFading = false;
            }
            return;
        }

        // --- ОПРЕДЕЛЕНИЕ ЦЕЛЕВОГО СОСТОЯНИЯ ---
        float horizontal = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > inputThreshold ? Input.GetAxisRaw("Horizontal") : 0f;
        float vertical = Mathf.Abs(Input.GetAxisRaw("Vertical")) > inputThreshold ? Input.GetAxisRaw("Vertical") : 0f;
        bool hasInput = Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f;

        if (hasInput && player.RealSpeed < minSpeedForStep) hasInput = false;

        if (hasInput && !isStepping && inputHoldTimer < Time.deltaTime * 2f)
            nextStepTime = Time.time;

        if (hasInput) { inputHoldTimer += Time.deltaTime; stopDelayTimer = 0f; }
        else { inputHoldTimer = 0f; stopDelayTimer += Time.deltaTime; }

        bool isShiftPressed = Input.GetKey(KeyCode.LeftShift);
        bool isCrouching = player.IsCrouching;

        // Целевое состояние: хотим ли мы бежать?
        bool targetIsRunning = isShiftPressed && !isCrouching && hasInput &&
                               (player.RealSpeed >= runSoundSpeedThreshold) &&
                               player.IsGrounded && !player.IsSliding && !player.IsDashing;

        bool canPlay = hasInput && (player.IsGrounded || isDescending) && !player.IsSliding && !player.IsDashing;

        // Обработка остановки
        if (!canPlay || player.RealSpeed < minSpeedForStep)
        {
            if (stopDelayTimer >= stopDelay)
            {
                StopFootsteps();
                return;
            }
        }
        else { stopDelayTimer = 0f; }

        if (stopDelayTimer > 0f)
        {
            isStepping = false;
            nextStepTime = 0f;
            return;
        }

        // --- ПЛАВНЫЙ ПЕРЕХОД (LERP) ---
        float targetBlend = targetIsRunning ? 1f : 0f;
        currentRunBlend = Mathf.Lerp(currentRunBlend, targetBlend, Time.deltaTime * transitionSpeed);

        // Интерполяция параметров на основе blend (0..1)
        float targetInterval, targetVol;
        AudioClip[] targetClips;

        if (isDescending)
        {
            targetInterval = Mathf.Lerp(descendWalkInterval, descendRunInterval, currentRunBlend);
            targetVol = Mathf.Lerp(descendWalkVolume, descendRunVolume, currentRunBlend);
            targetClips = currentRunBlend > 0.5f ? descendRunSounds : descendWalkSounds;
        }
        else if (isCrouching)
        {
            targetInterval = crouchWalkInterval;
            targetVol = crouchVolume;
            targetClips = walkSounds;
        }
        else
        {
            targetInterval = Mathf.Lerp(walkInterval, runInterval, currentRunBlend);
            targetVol = Mathf.Lerp(walkVolume, runVolume, currentRunBlend);
            targetClips = currentRunBlend > 0.5f ? runSounds : walkSounds;
        }

        // Применяем интерполированные значения
        currentInterval = targetInterval;
        currentVolume = targetVol;
        // Обновляем массив клипов только при пересечении порога 0.5, чтобы не менять звук посреди шага
        if ((currentRunBlend > 0.5f && currentClips != runSounds && !isCrouching && !isDescending) ||
            (currentRunBlend <= 0.5f && currentClips != walkSounds && !isCrouching && !isDescending))
        {
            currentClips = targetClips;
        }

        // --- ВОСПРОИЗВЕДЕНИЕ ШАГА ---
        if (!isStepping)
        {
            if (hasInput && inputHoldTimer >= minInputTimeForStep && player.RealSpeed >= minSpeedForStep)
            {
                PlayStep();
                isStepping = true;
                nextStepTime = Time.time + currentInterval;
            }
        }
        else if (Time.time >= nextStepTime)
        {
            if (hasInput && player.RealSpeed >= minSpeedForStep)
            {
                PlayStep();
                nextStepTime = Time.time + currentInterval;
            }
            else
            {
                isStepping = false;
                nextStepTime = 0f;
            }
        }
    }

    private void StopFootsteps()
    {
        isStepping = false;
        nextStepTime = 0f;
        stopDelayTimer = 0f;
        inputHoldTimer = 0f;
        currentRunBlend = 0f; // Сбрасываем blend при полной остановке
        if (audioSource.isPlaying && !isFading) isFading = true;
    }

    private void PlayStep()
    {
        isFading = false;
        if (currentClips == null || currentClips.Length == 0) return;

        AudioClip clip = currentClips[Random.Range(0, currentClips.Length)];
        audioSource.clip = clip;
        audioSource.pitch = isDescending ? Random.Range(0.85f, 1.05f) : Random.Range(0.9f, 1.1f);
        audioSource.volume = currentVolume;
        audioSource.Play();
    }
}