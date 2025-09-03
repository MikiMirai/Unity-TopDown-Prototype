using UnityEngine;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject gameOverPanel;

    [SerializeField] private int targetFPS = 240;

    private void Awake()
    {
        Application.targetFrameRate = targetFPS;

        EventManager.OnPlayerDeath += OnGameOver;

        GameData.Instance.ResetLevelData();
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

    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(0);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
