using UnityEngine;
using UnityEngine.SceneManagement;

public class BackButtonScript : MonoBehaviour
{
    public void BackButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }
        
    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
    public void GoToCooking()
    {
        SceneManager.LoadScene("CookingScene");
    }
}
