using UnityEngine;

public class LevelExit : MonoBehaviour
{
    [Header("Level Loading")]
    [SerializeField] private bool loadNextLevel = false;
    [SerializeField] private LevelLoader levelLoader;

    [Header("UI")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject HUD;


    private void OnTriggerEnter(Collider other)
    {
        if (loadNextLevel && levelLoader != null)
        {
            levelLoader.LoadNextLevel();
        }
        else
        {
            winPanel.SetActive(true);
            HUD.SetActive(false);
            Time.timeScale = 0f;
        }
    }
}
