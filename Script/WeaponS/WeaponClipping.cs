using UnityEngine;

public class WeaponClipping : MonoBehaviour
{
    [Header("Настройки проверки стены")]
    public float checkDistance = 1.0f;

    [Header("Смещение оружия при стене (Позиция)")]
    public float offsetX = 1.2f;
    public float offsetY = -0.6f;
    public float offsetZ = 0f;

    [Header("Поворот оружия при стене (Градусы)")]
    public float rotateX = -30f;
    public float rotateY = -70f;
    public float rotateZ = 0f;

    [Header("Скорость сглаживания")]
    public float smoothSpeed = 15f;
    public float rotationSmoothSpeed = 10f;

    [Header("Слои для проверки")]
    public LayerMask collisionMask = ~0;

    [Header("Защита от пола и лестниц")]
    public float floorAngleThreshold = 45f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Camera cameraCache;

    // Нет аллокаций в LateUpdate — переиспользуемый буфер.
    private readonly RaycastHit[] hitsBuffer = new RaycastHit[16];

    private void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        targetPosition = initialPosition;
        targetRotation = initialRotation;
        cameraCache = Camera.main;
    }

    private void LateUpdate()
    {
        if (cameraCache == null) return;

        Ray ray = new Ray(cameraCache.transform.position, cameraCache.transform.forward);
        int hitCount = Physics.RaycastNonAlloc(ray, hitsBuffer, checkDistance, collisionMask);

        bool hitWall = false;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = hitsBuffer[i];
            if (hit.collider.isTrigger) continue;                             // StairZone и т.п.
            if (Vector3.Angle(hit.normal, Vector3.up) <= floorAngleThreshold) continue; // пол / пандус
            hitWall = true;
            break;
        }

        if (hitWall)
        {
            targetPosition = initialPosition + new Vector3(offsetX, offsetY, offsetZ);
            targetRotation = initialRotation * Quaternion.Euler(rotateX, rotateY, rotateZ);
        }
        else
        {
            targetPosition = initialPosition;
            targetRotation = initialRotation;
        }

        float dt = Time.deltaTime;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, dt * smoothSpeed);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, dt * rotationSmoothSpeed);
    }
}
