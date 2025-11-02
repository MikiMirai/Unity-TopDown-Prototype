using UnityEngine;

public class MainMenu : MonoBehaviour
{
    private void Awake()
    {
        // Ensure cursor is always visible on the main menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
