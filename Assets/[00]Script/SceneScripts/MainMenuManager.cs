using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void StartGame()
    {
        if (FadeBlackScreen.Instance != null)
        {
            FadeBlackScreen.Instance.FadeInThenLoadScene("StartScene");
        }
        else
        {
            Debug.LogWarning("MainMenuManager: FadeBlackScreen.Instance not found, loading scene without fade.");
            SceneManager.LoadScene("StartScene");
        }
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
