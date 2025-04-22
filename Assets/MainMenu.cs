using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void LearnAboutAyurveda()
    {
        SceneManager.LoadScene("LearnScene");
    }

    public void OpenSettings()
    {
        Debug.Log("Settings Menu Opened");
    }

    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Game Closed");
    }
}
