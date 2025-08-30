using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EffectHandler effectHandler;

    [SerializeField] private bool isEnemyObject = false;
    [SerializeField] private int maxHealth = 5;
    public int CurrentHealth;

    void Awake()
    {
        CurrentHealth = maxHealth;

        if (effectHandler != null)
        {
            TryGetComponent<EffectHandler>(out EffectHandler handlerRef);
            effectHandler = handlerRef;
        }
    }

    public void TakeDamage(int amount)
    {
        CurrentHealth -= amount;
        if (CurrentHealth <= 0)
        {
            Die();
        }
        else
        {
            effectHandler?.StartHitFlash();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} died");

        if(isEnemyObject)
        {
            Destroy(gameObject, 1f);
        }
        // TODO: Add death logic here (animation, respawn, etc.)
    }
}
