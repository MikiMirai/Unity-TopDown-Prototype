using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EffectHandler effectHandler;

    [SerializeField] private bool isEnemyObject = false;
    [SerializeField] private int maxHealth = 5;
    public int CurrentHealth;

    // Events
    public event Action OnHit;

    void Awake()
    {
        CurrentHealth = maxHealth;

        effectHandler = GetComponent<EffectHandler>();
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
            OnHit?.Invoke();
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

    public void SubscribeToHitEvent(EffectHandler handler)
    {
        OnHit += handler.StartHitFlash; // Subscribe to the OnHit event
    }

    public void UnsubscribeFromHitEvent(EffectHandler handler)
    {
        OnHit -= handler.StartHitFlash; // Unsubscribe from the OnHit event
    }
}
