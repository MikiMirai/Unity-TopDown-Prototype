using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    [SerializeField] private int targetFPS = 240;

    [Header("References")]
    public GameObject gameOverPanel;

    private static StageManager instance;
    public static StageManager Instance => instance;

    private void Awake()
    {
        Application.targetFrameRate = targetFPS;

        EventManager.OnPlayerDeath += OnGameOver;

        GameData.Instance.ResetLevelData();

        // Singleton pattern — only one GameManager should exist
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
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
