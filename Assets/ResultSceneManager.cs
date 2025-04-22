using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class ResultSceneManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Image dishImage; 
    public TextMeshProUGUI recipeTitle;
    public TextMeshProUGUI recipeBenefits;
    public TextMeshProUGUI recipeIngredients;
    public TextMeshProUGUI recipeInstructions;
    
    [Header("Default Images")]
    public Sprite defaultDishSprite;  // Fallback image if specific one isn't found
    
    [Header("Dynamic Image Loading")]
    [Tooltip("If true, will try to load images from Resources folder based on recipe name")]
    public bool loadImagesFromResources = true;
    [Tooltip("Folder path within Resources where recipe images are stored")]
    public string imageResourcePath = "RecipeImages/";
    
    [Header("Settings")]
    public float retryDelay = 0.5f;
    public int maxRetries = 5;

    private string selectedIllness;
    private Remedy currentRemedy;

    void Start()
    {
        // Get the recipe name stored in PlayerPrefs
        selectedIllness = PlayerPrefs.GetString("SelectedIllness", "");
        
        if (string.IsNullOrEmpty(selectedIllness))
        {
            DisplayError("No recipe was selected!");
            return;
        }
        
        // Start loading the recipe data
        StartCoroutine(LoadRecipeData());
    }
    
    IEnumerator LoadRecipeData()
    {
        int retryCount = 0;
        bool dataLoaded = false;
        
        Debug.Log($"Attempting to load result data for: {selectedIllness}");
        
        // Wait a moment to ensure everything is initialized
        yield return new WaitForSeconds(0.1f);
        
        while (!dataLoaded && retryCount < maxRetries)
        {
            // Check if RemedyManager exists and has data
            if (RemedyManager.instance != null && RemedyManager.instance.remedyData != null)
            {
                // Try to get the remedy data
                currentRemedy = RemedyManager.instance.GetRemedy(selectedIllness);
                
                if (currentRemedy != null)
                {
                    dataLoaded = true;
                    Debug.Log($"Successfully loaded recipe data for: {currentRemedy.name}");
                    DisplayRecipeInfo();
                    break;
                }
            }
            
            // Try to load the data directly if RemedyManager failed
            if (!dataLoaded)
            {
                TextAsset jsonFile = Resources.Load<TextAsset>("remedies");
                if (jsonFile != null)
                {
                    RemedyList remedyData = JsonUtility.FromJson<RemedyList>(jsonFile.text);
                    if (remedyData != null && remedyData.remedies != null)
                    {
                        foreach (Remedy remedy in remedyData.remedies)
                        {
                            if (string.Equals(remedy.name, selectedIllness, System.StringComparison.OrdinalIgnoreCase))
                            {
                                currentRemedy = remedy;
                                dataLoaded = true;
                                Debug.Log($"Loaded recipe data directly: {currentRemedy.name}");
                                DisplayRecipeInfo();
                                break;
                            }
                        }
                    }
                }
            }
            
            if (!dataLoaded)
            {
                Debug.LogWarning($"Attempt {retryCount+1}/{maxRetries} to load recipe data failed");
                yield return new WaitForSeconds(retryDelay);
                retryCount++;
            }
        }
        
        if (!dataLoaded)
        {
            DisplayError($"Could not load data for: {selectedIllness}");
        }
    }
    
    void DisplayRecipeInfo()
    {
        if (currentRemedy == null)
            return;
            
        // Set recipe title
        if (recipeTitle != null)
            recipeTitle.text = currentRemedy.name;
            
        // Set recipe benefits (extend your Remedy class to include benefits)
        if (recipeBenefits != null)
        {
            // If you extend Remedy class to have benefits, use that
            // Otherwise, use a default message
            // recipeBenefits.text = "This Ayurvedic remedy helps restore balance to your body.";
            recipeBenefits.text = currentRemedy.benefits;
        }
        
        // Set recipe ingredients
        if (recipeIngredients != null && currentRemedy.ingredients != null)
        {
            string ingredientList = "Ingredients:\n";
            foreach (string ingredient in currentRemedy.ingredients)
            {
                ingredientList += $"• {ingredient}\n";
            }
            recipeIngredients.text = ingredientList;
        }
        
        // Set recipe instructions
        if (recipeInstructions != null && !string.IsNullOrEmpty(currentRemedy.instructions))
        {
            recipeInstructions.text = $"Instructions:\n{FormatInstructions(currentRemedy.instructions)}";
        }
        
        // Try to load the image dynamically
        if (dishImage != null)
        {
            if (loadImagesFromResources)
            {
                // Try to load image based on recipe name (sanitize name for file path)
                string imageName = currentRemedy.name.Replace(" ", "").Replace("-", "");
                Sprite recipeSprite = Resources.Load<Sprite>(imageResourcePath + imageName);
                
                if (recipeSprite != null)
                {
                    dishImage.sprite = recipeSprite;
                    Debug.Log($"Loaded image: {imageResourcePath + imageName}");
                }
                else
                {
                    // Use default sprite if specific one not found
                    if (defaultDishSprite != null)
                        dishImage.sprite = defaultDishSprite;
                        
                    Debug.LogWarning($"Could not find image for {currentRemedy.name}. Using default.");
                }
            }
            else if (defaultDishSprite != null)
            {
                // Just use the default sprite
                dishImage.sprite = defaultDishSprite;
            }
        }
    }
    
    void DisplayError(string message)
    {
        Debug.LogError(message);
        
        if (recipeTitle != null)
            recipeTitle.text = "Error";
            
        if (recipeBenefits != null)
            recipeBenefits.text = message;
            
        if (recipeIngredients != null)
            recipeIngredients.text = "";
            
        if (recipeInstructions != null)
            recipeInstructions.text = "";
    }
    
    // Format instructions into numbered steps
    private string FormatInstructions(string rawInstructions)
    {
        string[] steps = rawInstructions.Split('.');
        string formattedInstructions = "";
        
        for (int i = 0; i < steps.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(steps[i]))
            {
                formattedInstructions += (i + 1) + ". " + steps[i].Trim() + ".\n";
            }
        }
        
        return formattedInstructions;
    }

public void GoToMainMenu()
{
    // Clear the selected illness before returning to main menu
    PlayerPrefs.DeleteKey("SelectedIllness");
    PlayerPrefs.Save();
    
    Debug.Log("Cleared PlayerPrefs and returning to main menu");
    SceneManager.LoadScene("MainMenu");
}

    public void RestartGame()
    {
        SceneManager.LoadScene("IllnessList");
    }
}