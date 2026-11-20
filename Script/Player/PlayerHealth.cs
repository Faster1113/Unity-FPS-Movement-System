using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    public System.Action<float> OnDamaged;   // параметр: урон
    public System.Action OnDied;

    private void Start()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        CurrentHealth -= damage;
        OnDamaged?.Invoke(damage);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[PlayerHealth] HP: {CurrentHealth:F0}/{maxHealth:F0}", this);
#endif

        if (CurrentHealth <= 0f) Die();
    }

    private void Die()
    {
        IsDead = true;
        CurrentHealth = 0f;
        OnDied?.Invoke();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("[PlayerHealth] Игрок умер", this);
#endif
    }
}
