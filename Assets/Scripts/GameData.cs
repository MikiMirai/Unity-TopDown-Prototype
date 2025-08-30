using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameData : MonoBehaviour
{
    // The one instance that will exist in the scene.
    public static GameData Instance { get; private set; }

    // Global variables
    public bool showDebugColliders = false;
    public string currentLevel;

    // Enforce a single instance and persist it across scenes
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);     // Keep it when loading new scenes
        }
        else
        {
            Destroy(gameObject);               // Another instance – remove it
        }
    }

    // Optional helper: reset values on start of a level, etc.
    public void ResetLevelData()
    {
        showDebugColliders = false;
        currentLevel = SceneManager.GetActiveScene().name;
    }
}
