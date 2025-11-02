using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EffectHandler effectHandler;

    [SerializeField] private int maxHealth = 10;
    public int MaxHealth => maxHealth;

    [SerializeField] private int currentHealth;
    public int CurrentHealth => currentHealth;

    [SerializeField] private bool isEnemyObject = true;

    [Header("Debug")]
    [SerializeField] private bool godMode = false;

    // Events
    public event Action OnHit;

    void Awake()
    {
        currentHealth = maxHealth;

        effectHandler = GetComponent<EffectHandler>();
    }

    public void TakeDamage(int amount)
    {
        if (godMode)
        {
            //DEBUG: skip the damage calculation for the player, only for testing
            return;
        }

        currentHealth -= amount;
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
        else
        {
            EventManager.TriggerPlayerDeathEvent();
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
