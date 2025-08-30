using UnityEngine;

public class EventManager : MonoBehaviour
{
    // Declare a delegate (event type) for hit events
    public delegate void PlayerDeathEvent();

    // Create the event, which uses the delegate above
    public static event PlayerDeathEvent OnPlayerDeath;

    // Method to trigger the OnEnemyHit event
    public static void TriggerPlayerDeathEvent()
    {
        // Invoke the event if there are subscribers
        OnPlayerDeath?.Invoke();
    }
}
