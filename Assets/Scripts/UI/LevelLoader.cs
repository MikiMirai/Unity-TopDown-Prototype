using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Animator _transitionAnimator;
    [SerializeField] private float _transitionTime = 1f;

    [Header("Debug")]
    [SerializeField] private string currentSceneName;
    [SerializeField] private bool _isMainMenu = false;

    private void Start()
    {
        if (_isMainMenu)
        {
            _transitionAnimator.SetBool("MainMenu", true);
        }

        currentSceneName = SceneManager.GetActiveScene().name;
    }

    public void LoadNextLevel()
    {
        // Reset animator bool (should always be false unless specified)
        if (_isMainMenu)
        {
            _transitionAnimator.SetBool("MainMenu", false);
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

    IEnumerator LoadLevel(int levelIndex)
    {
        // Make sure the time is flowing
        Time.timeScale = 1f;

        // Play animation
        _transitionAnimator.SetTrigger("Start");

        // Wait for (default 1 sec)
        yield return new WaitForSeconds(_transitionTime);

        // Load scene at index
        SceneManager.LoadSceneAsync(levelIndex);
    }

    public void RestartCurrentScene()
    {
        StartCoroutine(LoadLevelWithTransition(currentSceneName));
    }

    /// <summary>
    /// Plays the transition animation (fade out/in) before reloading or loading a new scene.
    /// </summary>
    private IEnumerator PlayTransition()
    {
        _transitionAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;

        // Option A — Animator-based transition
        if (_transitionAnimator != null)
        {
            // Play animation
            _transitionAnimator.SetTrigger("Start");
            yield return new WaitForSecondsRealtime(_transitionTime);
        }
        else
        {
            // Option B — fallback: simple timed wait if you don’t use Animator
            yield return new WaitForSecondsRealtime(_transitionTime);
        }

        // Fade out complete — control returns to SceneTransitionLoader
    }

    public IEnumerator LoadLevelWithTransition(string sceneName)
    {
        // Validate scene before starting
        if (string.IsNullOrEmpty(currentSceneName))
        {
            Debug.LogError("Cannot reload: Scene name is empty or null.");
            yield break;
        }

        // Start the transition animation
        if (_transitionAnimator != null)
        {
            // Play animation
            _transitionAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            _transitionAnimator.SetTrigger("Start");
            yield return new WaitForSecondsRealtime(_transitionTime);
        }
        else
        {
            Debug.LogWarning("No transition animator assigned. Skipping transition.");
        }

        // Load the scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(currentSceneName);

        if (asyncLoad == null)
        {
            Debug.LogError($"Failed to start async loading for {currentSceneName}.");
            yield break;
        }

        // Prevent the scene from activating immediately
        asyncLoad.allowSceneActivation = false;

        // Wait until the scene is fully loaded (progress reaches 0.9, as Unity doesn't hit 1.0 until activation)
        while (asyncLoad.progress < 0.9f)
        {
            Debug.Log($"Loading progress: {asyncLoad.progress * 100}%");
            yield return null;
        }

        // Scene is fully loaded; wait a bit for stability (optional)
        yield return new WaitForSecondsRealtime(0.1f);

        // Reset Time.timeScale to 1 before activating the new scene
        Time.timeScale = 1f;
        Debug.Log("Time.timeScale reset to 1.");

        // Allow the scene to activate
        asyncLoad.allowSceneActivation = true;

        // Optional: Wait until the scene is fully activated
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Debug.Log("Scene successfully loaded and activated.");
    }

}
