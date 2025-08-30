using UnityEngine;

public class EventManager : MonoBehaviour
{
    // Declaring global event for Player Death
    public delegate void PlayerDeathEvent();
    public static event PlayerDeathEvent OnPlayerDeath;

    // Declaring global event for the Toggle of Debug Colliders
    public delegate void ToggleColliderDebugEvent();
    public static event ToggleColliderDebugEvent OnToggleColliderDebug;

    // Method to trigger the OnEnemyHit event
    public static void TriggerPlayerDeathEvent()
    {
        // Invoke the event if there are subscribers
        OnPlayerDeath?.Invoke();
    }

    public static void TriggerToggleColliderDebugEvent()
    {
        OnToggleColliderDebug?.Invoke();
    }
}
