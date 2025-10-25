using UnityEngine;

public class GameOverScript : MonoBehaviour
{
    private void Start()
    {
        StageManager.Instance.gameOverPanel = gameObject;
        gameObject.SetActive(false);
    }
}
