using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("StartScene");
    }

    public void OpenSettings()
    {
        // เปิด Settings Panel
    }

    public void CloseSettings()
    {
        // ปิด Settings Panel
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
