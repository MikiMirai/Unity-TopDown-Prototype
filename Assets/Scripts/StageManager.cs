using UnityEngine;

public class StageManager : MonoBehaviour
{
    [SerializeField] private int targetFPS = 240;

    [Header("References")]
    public GameObject gameOverPanel;

    public static StageManager Instance { get; private set; }

    private void Awake()
    {
        Application.targetFrameRate = targetFPS;

        EventManager.OnPlayerDeath += OnGameOver;

        GameData.Instance.ResetLevelData();

        // Singleton pattern — only one GameManager should exist
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        EventManager.OnPlayerDeath -= OnGameOver;
    }

    void OnGameOver()
    {
        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
