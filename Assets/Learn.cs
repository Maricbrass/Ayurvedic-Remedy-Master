using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Learn : MonoBehaviour
{
    // [Header("Scene Names")]
    // [Tooltip("Name of the Ayurvedic principles scene")]
    // public string principlesSceneName = "AyurvedicPrinciples";
    
    // [Tooltip("Name of the common herbs scene")]
    // public string herbsSceneName = "CommonHerbs";
    
    // [Tooltip("Name of the preparation techniques scene")]
    // public string techniquesSceneName = "PreparationTechniques";
    
    // [Tooltip("Name of the doshas information scene")]
    // public string doshasSceneName = "Doshas";
    
    // [Tooltip("Name of the history scene")]
    // public string historySceneName = "AyurvedicHistory";
    
    // [Tooltip("Name of the previous/main menu scene")]
    // public string previousSceneName = "MainMenu";

    public void LoadLearnPage1()
    {
        SceneManager.LoadScene("LearnPage1");
    }

    public void LoadLearnPage2()
    {
        SceneManager.LoadScene("LearnPage2");
    }

    public void LoadLearnPage3()
    {
        SceneManager.LoadScene("LearnPage3");
    }

    public void LoadQuizPage()
    {
        SceneManager.LoadScene("QuizPage");
    }

    public void BackButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }

    public void mainMenuButton()
    {
        SceneManager.LoadScene("MainMenu");
    }
}