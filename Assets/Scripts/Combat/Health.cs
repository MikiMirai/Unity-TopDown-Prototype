using System;
using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EffectHandler effectHandler;

    [Header("UI")]
    [Tooltip("Only add this if you have a custom slider!")]
    [SerializeField] private Slider healthSlider;

    [Header("Settings")]
    [SerializeField] private int maxHealth = 10;
    public int MaxHealth => maxHealth;
    [SerializeField] private bool isEnemyObject = true;

    [Header("Debug")]
    [SerializeField] private int currentHealth;
    public int CurrentHealth => currentHealth;
    [SerializeField] private bool godMode = false;

    // Events
    public event Action OnHit;

    void Awake()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();

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
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthBar();

        if (CurrentHealth <= 0)
        {
            Die();
        }
        else
        {
            OnHit?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthBar();
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

    void UpdateHealthBar()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
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
