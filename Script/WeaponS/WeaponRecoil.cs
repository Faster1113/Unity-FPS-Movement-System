using UnityEngine;
using Game.Player;

public class WeaponRecoil : MonoBehaviour
{
    [Header("ГЛАВНЫЕ ПЕРЕКЛЮЧАТЕЛИ")]
    public bool enableVerticalKick = true;
    public bool enableHorizontalKick = true;

    [Header("Прицеливание (ПКМ)")]
    [Range(0f, 1f)] public float recoilMultiplierADS = 0.2f;

    [Header("Вертикальная отдача")]
    public float verticalAngle = 2.0f;
    public float maxVerticalAngle = 10f;
    public float verticalRecovery = 10f;

    [Header("Горизонтальная отдача")]
    public float horizontalAngle = 0.5f;
    public float maxHorizontalAngle = 3f;
    public float horizontalRecovery = 8f;

    [Header("Смещение назад (Kickback)")]
    public float kickbackDistance = 0.02f;
    public float kickbackRecovery = 12f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private float currentVertRecoil;
    private float currentHorizRecoil;
    private float currentKickback;

    private float targetVertRecoil;
    private float targetHorizRecoil;
    private float targetKickback;

    private PlayerController player;

    private void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        player = GetComponentInParent<PlayerController>();
    }

    private void LateUpdate()
    {
        bool isAiming = player != null ? player.IsAiming : Input.GetMouseButton(1);
        float currentRecoilMult = isAiming ? recoilMultiplierADS : 1f;

        if (!Input.GetMouseButton(0))
        {
            targetVertRecoil = Mathf.Lerp(targetVertRecoil, 0f, Time.deltaTime * verticalRecovery);
            targetHorizRecoil = Mathf.Lerp(targetHorizRecoil, 0f, Time.deltaTime * horizontalRecovery);
            targetKickback = Mathf.Lerp(targetKickback, 0f, Time.deltaTime * kickbackRecovery);
        }

        float t = Time.deltaTime * 15f;
        currentVertRecoil = Mathf.Lerp(currentVertRecoil, targetVertRecoil, t);
        currentHorizRecoil = Mathf.Lerp(currentHorizRecoil, targetHorizRecoil, t);
        currentKickback = Mathf.Lerp(currentKickback, targetKickback, t);

        Quaternion recoilRotation = Quaternion.Euler(
            -currentVertRecoil * currentRecoilMult,
             currentHorizRecoil * currentRecoilMult,
             0f);

        transform.localRotation = initialRotation * recoilRotation;
        transform.localPosition = initialPosition - new Vector3(0f, 0f, currentKickback * currentRecoilMult);
    }

    public void ApplyRecoil()
    {
        if (enableVerticalKick)
        {
            targetVertRecoil += verticalAngle;
            targetVertRecoil = Mathf.Clamp(targetVertRecoil, 0f, maxVerticalAngle);
        }

        if (enableHorizontalKick)
        {
            float randomDir = Random.Range(-1f, 1f);
            targetHorizRecoil += horizontalAngle * randomDir;
            targetHorizRecoil = Mathf.Clamp(targetHorizRecoil, -maxHorizontalAngle, maxHorizontalAngle);
        }

        targetKickback += kickbackDistance;
    }
}
