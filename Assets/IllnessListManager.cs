using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class IllnessListManager : MonoBehaviour
{
    public GameObject buttonPrefab;
    public Transform contentPanel;
    public TMP_Text ingredientsText;
    public TMP_Text recipeText;
        [Tooltip("The name of the scene to load when a remedy is selected")]
    public string nextSceneName = "IngredientSelection";
    
    [Tooltip("The PlayerPrefs key to store the selected illness name")]
    public string selectedIllnessKey = "SelectedIllness";

    [Tooltip("How long to wait before trying to load remedies if they're not ready")]
    public float retryDelay = 0.5f;
    [Tooltip("Maximum number of retries to load the remedy data")]
    public int maxRetries = 5;

    private List<Remedy> illnessList = new List<Remedy>();

void Start()
{
    // If we just came from another scene, clean up any persistent data
    if (RemedyManager.instance != null)
    {
        RemedyManager.instance.ResetManager();
    }
    
    // Make sure we have a valid content panel
    if (contentPanel == null)
    {
        OnEnable(); // Force UI initialization
    }
    
    // Clear out any existing children
    if (contentPanel != null)
    {
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }
    }
    
    StartCoroutine(InitializeWithRetry());
}
    
    IEnumerator InitializeWithRetry()
    {
        int retryCount = 0;
        bool dataLoaded = false;
        
        while (!dataLoaded && retryCount < maxRetries)
        {
            if (RemedyManager.instance == null)
            {
                Debug.LogWarning($"RemedyManager instance not found! Retry {retryCount+1}/{maxRetries}");
                yield return new WaitForSeconds(retryDelay);
                retryCount++;
                continue;
            }
            
            if (RemedyManager.instance.remedyData == null)
            {
                Debug.LogWarning($"RemedyManager.remedyData is null! Retry {retryCount+1}/{maxRetries}");
                yield return new WaitForSeconds(retryDelay);
                retryCount++;
                continue;
            }
            
            if (RemedyManager.instance.remedyData.remedies == null || 
                RemedyManager.instance.remedyData.remedies.Count == 0)
            {
                Debug.LogWarning($"RemedyManager.remedyData.remedies is empty! Retry {retryCount+1}/{maxRetries}");
                yield return new WaitForSeconds(retryDelay);
                retryCount++;
                continue;
            }
            
            illnessList = RemedyManager.instance.remedyData.remedies;
            
            if (illnessList != null && illnessList.Count > 0)
            {
                dataLoaded = true;
                Debug.Log($"Successfully loaded {illnessList.Count} remedies from RemedyManager");
                PopulateList();
            }
            else
            {
                Debug.LogWarning($"Failed to load remedies! Retry {retryCount+1}/{maxRetries}");
                yield return new WaitForSeconds(retryDelay);
                retryCount++;
            }
        }
        
        if (!dataLoaded)
        {
            Debug.LogError("Failed to load remedy data after maximum retries!");
        }
    }

void PopulateList()
{
    foreach (Transform child in contentPanel)
    {
        Destroy(child.gameObject);
    }
    
    if (illnessList == null || illnessList.Count == 0)
    {
        Debug.LogError("No remedies found in illnessList!");
        return;
    }
    
    Debug.Log($"Populating list with {illnessList.Count} remedies");
    
    foreach (Remedy remedy in illnessList)
    {
        if (remedy == null || string.IsNullOrEmpty(remedy.name))
        {
            Debug.LogWarning("Found null or empty remedy, skipping");
            continue;
        }
        
        Debug.Log($"Creating button for remedy: {remedy.name}");
        GameObject newButton = Instantiate(buttonPrefab, contentPanel);
        
        if (newButton == null)
        {
            Debug.LogError("Failed to instantiate button prefab!");
            continue;
        }
        
        TMP_Text buttonText = newButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = remedy.name;
        }
        else
        {
            Debug.LogError("Button prefab is missing TMP_Text component!");
        }
        
        Button button = newButton.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => SaveAndNavigate(remedy.name));
        }
        else
        {
            Debug.LogError("Button prefab is missing Button component!");
        }
    }
    
    Debug.Log("Finished populating remedy list");
}

    void SaveAndNavigate(string illnessName)
    {
        // Save the selected illness name to PlayerPrefs
        PlayerPrefs.SetString(selectedIllnessKey, illnessName);
        PlayerPrefs.Save();
        
        Debug.Log($"Saved illness '{illnessName}' to PlayerPrefs and navigating to {nextSceneName}");
        
        // Load the next scene
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogError("Next scene name is not set! Please assign it in the Inspector.");
        }
    }

    void DisplayInfo(string illness)
    {
        string ingredients = "";
        string recipe = "";

            Remedy remedy = FindRemedy(illness);
            
            if (remedy != null)
            {
                foreach (string ingredient in remedy.ingredients)
                {
                    ingredients += "🔸 " + ingredient + "\n";
                }
                
                string[] steps = remedy.instructions.Split('.');
                for (int i = 0; i < steps.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(steps[i]))
                    {
                        recipe += (i + 1) + "️⃣ " + steps[i].Trim() + ".\n";
                    }
                }
            }
        ingredients = ingredients.TrimEnd('\n');
        recipe = recipe.TrimEnd('\n');

        ingredientsText.text = ingredients;
        recipeText.text = recipe;
    }

    private Remedy FindRemedy(string illnessName)
    {
        if (RemedyManager.instance == null || RemedyManager.instance.remedyData == null)
            return null;
            
        return RemedyManager.instance.remedyData.remedies.Find(r => r.name.ToLower() == illnessName.ToLower());
    }
    void Awake()
{
    // Make sure we have a clean start
    if (RemedyManager.instance != null)
    {
        // Reset PlayerPrefs when starting the illness list
        PlayerPrefs.DeleteKey("SelectedIllness");
        PlayerPrefs.Save();
    }
}

void OnEnable()
{
    // Force a check for the content panel
    if (contentPanel == null)
    {
        // First try to find the specified content panel
        contentPanel = GameObject.Find("IllnessScrollViewContent")?.transform;
        
        // If that fails, try alternative names
        if (contentPanel == null)
            contentPanel = GameObject.Find("Content")?.transform;
            
        // If that fails too, try to find by tag or through hierarchy
        if (contentPanel == null)
        {
            // Find scrollview first
            GameObject scrollView = GameObject.Find("IllnessScrollView");
            if (scrollView != null)
            {
                // Look for content within scrollview
                ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
                if (scrollRect != null && scrollRect.content != null)
                {
                    contentPanel = scrollRect.content;
                }
            }
        }
        
        // If we still don't have it, log an error
        if (contentPanel == null)
        {
            Debug.LogError("Content panel not found! Check UI structure and names.");
            
            // Last resort - create an emergency content panel
            GameObject emergencyPanel = new GameObject("EmergencyContentPanel");
            RectTransform rt = emergencyPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            // Find canvas to parent to
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                emergencyPanel.transform.SetParent(canvas.transform, false);
                
                // Add a vertical layout group
                VerticalLayoutGroup vlg = emergencyPanel.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 10;
                vlg.padding = new RectOffset(10, 10, 10, 10);
                
                contentPanel = emergencyPanel.transform;
                Debug.LogWarning("Created emergency content panel as fallback!");
            }
        }
    }
}
}