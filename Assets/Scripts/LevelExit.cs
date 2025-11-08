using UnityEngine;

public class LevelExit : MonoBehaviour
{
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject HUD;

    private void OnTriggerEnter(Collider other)
    {
        winPanel.SetActive(true);
        HUD.SetActive(false);
        Time.timeScale = 0f;
    }
}
