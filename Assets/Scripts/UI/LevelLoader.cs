using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Animator _transition;
    [SerializeField] private float _transitionTime = 1f;

    [Header("Debug")]
    [SerializeField] private bool _isMainMenu = false;

    private void Start()
    {
        if (_isMainMenu)
        {
            _transition.SetBool("MainMenu", true);
        }
    }

    public void LoadNextLevel()
    {
        // Reset animator bool (should always be false unless specified)
        if (_isMainMenu)
        {
            _transition.SetBool("MainMenu", false);
        }

        StartCoroutine(LoadLevel(SceneManager.GetActiveScene().buildIndex + 1));
    }

    /// <summary>
    /// Loads the level before the current one, specified in Build settings (mostly for debug).
    /// </summary>
    public void LoadPreviousLevel()
    {
        StartCoroutine(LoadLevel(SceneManager.GetActiveScene().buildIndex - 1));
    }

    public void LoadMenuLevel()
    {
        StartCoroutine(LoadLevel(0));
    }

    public void RestartCurrentLevel()
    {
        StartCoroutine(LoadLevel(SceneManager.GetActiveScene().buildIndex));
    }

    IEnumerator LoadLevel(int levelIndex)
    {
        // Make sure the time is flowing
        Time.timeScale = 1f;

        // Play animation
        _transition.SetTrigger("Start");

        // Wait for (default 1 sec)
        yield return new WaitForSeconds(_transitionTime);

        // Load scene at index
        SceneManager.LoadSceneAsync(levelIndex);
    }
}
