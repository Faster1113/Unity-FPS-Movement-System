using UnityEngine;
using Game.Player; // Добавляем namespace для совместимости, если понадобится

public class WeaponSystem : MonoBehaviour
{
    [Header("Настройки стрельбы")]
    public float damage = 20f;
    public float range = 100f;
    public float fireRate = 0.15f;

    [Header("Прицеливание (ПКМ) - Настройки")]
    public Vector3 adsPositionOffset = new Vector3(0, -0.05f, -0.1f);
    public Vector3 adsRotationOffset = new Vector3(0, 0, 0);
    public float adsSpeed = 10f;
    public float adsFOV = 40f;
    public float normalFOV = 60f;

    [Header("Ссылки на компоненты")]
    public Camera playerCamera;
    public Animator armsAnimator;
    public GameObject hitEffectPrefab;

    [Header("Звуки стрельбы")]
    public AudioSource audioSource;
    public AudioClip[] shootSounds;
    public AudioClip boltSound;
    [Range(0f, 1f)] public float shootVolume = 0.8f;

    [Header("Эффекты и отдача")]
    public MuzzleFlashAnim muzzleFlashAnim;
    public WeaponRecoil weaponRecoil;
    public PlayerCamera playerCameraScript;
    public Transform weaponPivot;

    [Header("Пули и Рикошет")]
    public GameObject tracerPrefab;
    public Transform muzzlePoint;
    public GameObject sparkPrefab;
    public int maxRicochets = 2;
    public float ricochetAngleThreshold = 30f;

    private float nextTimeToFire;
    private Vector3 initialWeaponPos;
    private Quaternion initialWeaponRot;

    private void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;

        if (weaponPivot != null)
        {
            initialWeaponPos = weaponPivot.localPosition;
            initialWeaponRot = weaponPivot.localRotation;
        }
    }

    private void Update()
    {
        // 1. Прицеливание (ПКМ)
        bool isAiming = Input.GetMouseButton(1);
        float targetFov = isAiming ? adsFOV : normalFOV;
        if (playerCamera != null)
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, Time.deltaTime * adsSpeed);

        if (weaponPivot != null)
        {
            Vector3 targetPos = isAiming ? initialWeaponPos + adsPositionOffset : initialWeaponPos;
            Quaternion targetRot = isAiming ? initialWeaponRot * Quaternion.Euler(adsRotationOffset) : initialWeaponRot;
            weaponPivot.localPosition = Vector3.Lerp(weaponPivot.localPosition, targetPos, Time.deltaTime * adsSpeed);
            weaponPivot.localRotation = Quaternion.Slerp(weaponPivot.localRotation, targetRot, Time.deltaTime * adsSpeed);
        }

        // 2. Стрельба
        if (Input.GetMouseButton(0) && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + fireRate;
            Shoot();
        }

        if (armsAnimator != null)
            armsAnimator.SetBool("isShooting", Input.GetMouseButton(0));
    }

    private void Shoot()
    {
        if (muzzleFlashAnim != null) muzzleFlashAnim.PlayFlash();
        if (weaponRecoil != null) weaponRecoil.ApplyRecoil();
        if (playerCameraScript != null) playerCameraScript.AddRecoil();

        // Звук выстрела
        if (audioSource != null && shootSounds != null && shootSounds.Length > 0)
        {
            AudioClip clip = shootSounds[Random.Range(0, shootSounds.Length)];
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(clip, shootVolume);
        }

        // Лязг затвора только если задан
        if (audioSource != null && boltSound != null)
            StartCoroutine(PlayBoltSound());

        // Луч и рикошет
        Vector3 rayOrigin = playerCamera != null ? playerCamera.transform.position : transform.position;
        Vector3 rayDirection = playerCamera != null ? playerCamera.transform.forward : transform.forward;
        Vector3 tracerStart = muzzlePoint != null ? muzzlePoint.position : rayOrigin;

        int ricochetCount = 0;
        float currentRange = range;
        Vector3 currentOrigin = rayOrigin;
        Vector3 currentDirection = rayDirection;

        while (ricochetCount <= maxRicochets)
        {
            if (!Physics.Raycast(currentOrigin, currentDirection, out RaycastHit hit, currentRange))
            {
                SpawnTracer(tracerStart, currentOrigin + currentDirection * currentRange);
                break;
            }

            SpawnTracer(tracerStart, hit.point);

            if (ricochetCount < maxRicochets && CanRicochet(hit, currentDirection))
            {
                SpawnSparks(hit.point, hit.normal);
                currentDirection = Vector3.Reflect(currentDirection, hit.normal);
                currentOrigin = hit.point;
                currentRange -= hit.distance;
                tracerStart = hit.point;
                ricochetCount++;
                continue;
            }

            if (hitEffectPrefab != null)
            {
                GameObject impact = Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impact, 1f);
            }

            // GetComponentInParent — корректно для моделей с иерархией
            EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();
            if (enemy != null) enemy.TakeDamage(damage);
            break;
        }
    }

    private System.Collections.IEnumerator PlayBoltSound()
    {
        yield return new WaitForSeconds(0.1f);
        if (audioSource != null && boltSound != null)
            audioSource.PlayOneShot(boltSound, 0.5f);
    }

    private void SpawnTracer(Vector3 start, Vector3 end)
    {
        if (tracerPrefab == null) return;

        GameObject tracer = Instantiate(tracerPrefab, start, Quaternion.identity);
        BulletVisual bulletScript = tracer.GetComponent<BulletVisual>();
        if (bulletScript != null) bulletScript.Setup(start, end);
        else Destroy(tracer, 0.1f);
    }

    private void SpawnSparks(Vector3 position, Vector3 normal)
    {
        if (sparkPrefab == null) return;
        GameObject spark = Instantiate(sparkPrefab, position, Quaternion.LookRotation(normal));
        Destroy(spark, 1f);
    }

    private bool CanRicochet(RaycastHit hit, Vector3 incomingDirection)
    {
        float angle = Vector3.Angle(incomingDirection, hit.normal);
        if (angle < 90f - ricochetAngleThreshold) return false;

        PhysicsMaterial material = hit.collider.sharedMaterial;
        return material != null && material.bounciness > 0.3f;
    }
}