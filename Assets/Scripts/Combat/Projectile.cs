using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Settings")]
    public int damage = 1;
    public float lifetime = 5f;
    public string ownerTag; // "Player" or "Enemy" to avoid friendly fire

    private void Start()
    {
        Destroy(gameObject, lifetime); // Auto-destroy after X seconds
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore collisions with the shooter
        if (other.CompareTag(ownerTag)) return;

        // Apply damage if target has Health
        if (other.TryGetComponent<Health>(out Health targetHealth))
        {
            targetHealth.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
