using System.Collections;
using UnityEngine;

public class EffectHandler : MonoBehaviour
{
    public Renderer enemyRenderer; // The Renderer for color change (set in Inspector)
    public Color hitColor = Color.red; // Color when hit
    public float flashDuration = 0.2f; // Flash duration (seconds)

    private Color originalColor;
    private Health health;

    private void Awake()
    {
        if (enemyRenderer != null)
            originalColor = enemyRenderer.material.color;

        // Get the Health component attached to this GameObject
        health = GetComponent<Health>();

        if (health != null)
        {
            // Subscribe to the OnHit event of the Health component
            health.SubscribeToHitEvent(this);
        }
        else
        {
            Debug.LogError("No Health component found on this GameObject. EffectHandler will not function correctly.");
        }
    }

    private void OnDestroy()
    {
        // Ensure to unsubscribe when the EffectHandler is destroyed
        if (health != null)
        {
            health.UnsubscribeFromHitEvent(this);
        }
    }

    public void StartHitFlash()
    {
        StartCoroutine(FlashColor());
    }

    private IEnumerator FlashColor()
    {
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = hitColor; // Flash color
            yield return new WaitForSeconds(flashDuration); // Wait for the flash duration
            enemyRenderer.material.color = originalColor; // Reset to original color
        }
    }
}
