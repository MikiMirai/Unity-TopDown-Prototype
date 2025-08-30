using System.Collections;
using UnityEngine;

public class EffectHandler : MonoBehaviour
{
    public Renderer enemyRenderer; // The Renderer for color change (set in Inspector)
    public Color hitColor = Color.red; // Color when hit
    public float flashDuration = 0.2f; // Flash duration (seconds)

    private Color originalColor;

    private void Awake()
    {
        if (enemyRenderer != null)
            originalColor = enemyRenderer.material.color;
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
