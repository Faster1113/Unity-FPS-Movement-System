using UnityEngine;

public class WeaponFollow : MonoBehaviour
{
    [Tooltip("Камера, за которой следует оружие. Если пусто — возьмётся Camera.main.")]
    public Transform cameraTransform;

    [Header("Смещение относительно камеры")]
    public float forwardOffset = 0.5f;
    public float downOffset = 0.3f;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        transform.position = cameraTransform.position
                             + cameraTransform.forward * forwardOffset
                             - cameraTransform.up * downOffset;

        transform.rotation = cameraTransform.rotation;
    }
}
