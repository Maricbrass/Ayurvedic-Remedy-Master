using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Remedy
{
    public string name;
    public List<string> ingredients;
    public string instructions;
    public string benefits;
}

[System.Serializable]
public class RemedyList
{
    public List<Remedy> remedies;
}

public class RemedyManager : MonoBehaviour
{
    public static RemedyManager instance;
    public RemedyList remedyData;
    private bool isInitialized = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // DontDestroyOnLoad(gameObject);
            LoadRemedies(); // Load immediately in Awake
            isInitialized = true;
            
            // Add scene load listener to handle scene transitions
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Clean up when object is destroyed
    private void OnDestroy()
    {
        if (instance == this)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
    
    // Handle scene transitions
// Update the OnSceneLoaded method:
private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
{
    // Check if we're loading the illness list scene
    if (scene.name == "IllnessList" || scene.name == "MainMenu")
    {
        Debug.Log("Loading illness list or main menu. Clearing PlayerPrefs data.");
        PlayerPrefs.DeleteKey("SelectedIllness");
        
        // Allow a brief moment for the scene to initialize before logging
        StartCoroutine(LogSceneObjects());
    }
}

private System.Collections.IEnumerator LogSceneObjects()
{
    yield return new WaitForSeconds(0.3f);
    
    // Log hierarchy for debugging
    Debug.Log("Current scene hierarchy:");
    GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
    foreach (GameObject obj in rootObjects)
    {
        LogObjectAndChildren(obj, 0);
    }
}

private void LogObjectAndChildren(GameObject obj, int depth)
{
    string indent = new string(' ', depth * 2);
    Debug.Log($"{indent}- {obj.name} [{obj.tag}]");
    
    foreach (Transform child in obj.transform)
    {
        LogObjectAndChildren(child.gameObject, depth + 1);
    }
}

    public void LoadRemedies()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("remedies");
        if (jsonFile != null)
        {
            remedyData = JsonUtility.FromJson<RemedyList>(jsonFile.text);
            Debug.Log("Remedies Loaded Successfully!");
        }
        else
        {
            Debug.LogError("Remedies JSON file not found!");
        }
    }

    public Remedy GetRemedy(string illness)
    {
        if (remedyData == null || remedyData.remedies == null)
        {
            Debug.LogError("Remedy data not loaded yet!");
            LoadRemedies(); // Try to reload
            return null;
        }
        
        // Try exact match first
        Remedy result = remedyData.remedies.Find(r => r.name == illness);
        
        // If not found, try case-insensitive
        if (result == null)
        {
            result = remedyData.remedies.Find(r => 
                string.Equals(r.name, illness, System.StringComparison.OrdinalIgnoreCase));
        }
        
        return result;
    }
    
    // Method to completely reset the manager (for debugging)
    public void ResetManager()
    {
        PlayerPrefs.DeleteKey("SelectedIllness");
        PlayerPrefs.Save();
        
        // Force reload remedies
        LoadRemedies();
    }
}