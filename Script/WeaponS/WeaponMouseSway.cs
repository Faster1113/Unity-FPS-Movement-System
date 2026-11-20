using UnityEngine;

public class WeaponMouseSway : MonoBehaviour
{
    [Header("Настройки покачивания от мыши")]
    [Tooltip("Насколько сильно оружие смещается в сторону при повороте камеры")]
    public float positionSwayAmount = 0.02f;
    [Tooltip("Насколько сильно оружие наклоняется при повороте камеры")]
    public float rotationSwayAmount = 2f;
    [Tooltip("Как быстро оружие возвращается в центр")]
    public float smoothness = 8f;
    [Tooltip("Максимальное смещение (чтобы оружие не улетало)")]
    public float maxSway = 0.05f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        Vector3 targetPosition = initialPosition +
                                 new Vector3(-mouseX * positionSwayAmount,
                                             -mouseY * positionSwayAmount,
                                             0f);

        targetPosition.x = Mathf.Clamp(targetPosition.x,
            initialPosition.x - maxSway, initialPosition.x + maxSway);
        targetPosition.y = Mathf.Clamp(targetPosition.y,
            initialPosition.y - maxSway, initialPosition.y + maxSway);

        Quaternion targetRotation = initialRotation * Quaternion.Euler(
            mouseY * rotationSwayAmount,
            -mouseX * rotationSwayAmount,
            mouseX * rotationSwayAmount * 0.5f);

        float t = Time.deltaTime * smoothness;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, t);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, t);
    }
}
