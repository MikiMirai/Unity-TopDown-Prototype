using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int health = 5;

    public void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0) Invoke(nameof(GameOver), 0.5f);
    }
    private void GameOver()
    {
        // TODO: Implement game over screen
    }
}
