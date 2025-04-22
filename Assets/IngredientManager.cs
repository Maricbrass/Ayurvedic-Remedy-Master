using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IngredientDisplayManager : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public Transform contentPanel;
    public GameObject ingredientPrefab;
    public TextMeshProUGUI instructionsText;
    
    [Header("Ingredient Images")]
    [Tooltip("Path to ingredient images within Resources folder")]
    public string ingredientImagePath = "Ingredients/";
    [Tooltip("Fallback sprite to use when an ingredient image isn't found")]
    public Sprite defaultIngredientSprite;
    
    [Header("Settings")]
    [Tooltip("How long to wait before trying to load remedies if they're not ready")]
    public float retryDelay = 0.5f;
    [Tooltip("Maximum number of retries to load the remedy data")]
    public int maxRetries = 10;

    private string selectedIllness;
    private Remedy currentRemedy;

    void Start()
    {
        // First ensure RemedyManager exists and has loaded data
        if (RemedyManager.instance == null)
        {
            Debug.LogError("RemedyManager instance not found! Creating one...");
            GameObject remedyManagerObj = new GameObject("RemedyManager");
            RemedyManager manager = remedyManagerObj.AddComponent<RemedyManager>();
            manager.LoadRemedies();
        }
        else if (RemedyManager.instance.remedyData == null || 
                 RemedyManager.instance.remedyData.remedies == null || 
                 RemedyManager.instance.remedyData.remedies.Count == 0)
        {
            Debug.LogWarning("RemedyManager exists but has no data. Reloading...");
            RemedyManager.instance.LoadRemedies();
        }

        StartCoroutine(InitializeWithRetry());
    }

    System.Collections.IEnumerator InitializeWithRetry()
    {
        int retryCount = 0;
        bool dataLoaded = false;
        
        // Get the selected illness from PlayerPrefs
        selectedIllness = PlayerPrefs.GetString("SelectedIllness", "");
        
        if (string.IsNullOrEmpty(selectedIllness))
        {
            Debug.LogError("No illness was selected in PlayerPrefs!");
            yield break;
        }
        
        titleText.text = "Ingredients for " + selectedIllness;
        Debug.Log($"Looking for remedy data for '{selectedIllness}'");
        
        while (!dataLoaded && retryCount < maxRetries)
        {
            // Check if RemedyManager is available
            if (RemedyManager.instance == null)
            {
                Debug.LogWarning($"RemedyManager instance not found! Retry {retryCount+1}/{maxRetries}");
                yield return new WaitForSeconds(retryDelay);
                retryCount++;
                continue;
            }
            
            // Check if data is loaded
            if (RemedyManager.instance.remedyData == null || 
                RemedyManager.instance.remedyData.remedies == null || 
                RemedyManager.instance.remedyData.remedies.Count == 0)
            {
                Debug.LogWarning($"RemedyManager has no data! Retry {retryCount+1}/{maxRetries}");
                RemedyManager.instance.LoadRemedies();
                yield return new WaitForSeconds(retryDelay);
                retryCount++;
                continue;
            }
            
            // Debug all available remedies to help diagnose the issue
            if (retryCount == 0 || retryCount == 3) {
                Debug.Log("Available remedies:");
                foreach (Remedy r in RemedyManager.instance.remedyData.remedies) {
                    Debug.Log($"- '{r.name}'");
                }
            }
            
            // Get the remedy data from RemedyManager
            currentRemedy = RemedyManager.instance.GetRemedy(selectedIllness);
            
            if (currentRemedy == null)
            {
                // Try case-insensitive manual search as a fallback
                foreach (Remedy remedy in RemedyManager.instance.remedyData.remedies)
                {
                    if (string.Equals(remedy.name, selectedIllness, System.StringComparison.OrdinalIgnoreCase))
                    {
                        currentRemedy = remedy;
                        Debug.Log($"Found remedy using manual search: {remedy.name}");
                        break;
                    }
                }
                
                if (currentRemedy == null)
                {
                    Debug.LogWarning($"Selected illness '{selectedIllness}' not found in RemedyManager! Retry {retryCount+1}/{maxRetries}");
                    yield return new WaitForSeconds(retryDelay);
                    retryCount++;
                    continue;
                }
            }
            
            dataLoaded = true;
            Debug.Log($"Successfully loaded remedy data for '{selectedIllness}'");
            DisplayIngredients();
        }
        
        if (!dataLoaded)
        {
            Debug.LogError($"Failed to load remedy data for '{selectedIllness}' after maximum retries!");
        }
    }

    void DisplayIngredients()
    {
        // First clear any existing ingredients
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }
        
        // Make sure we have ingredients to display
        if (currentRemedy.ingredients == null || currentRemedy.ingredients.Count == 0)
        {
            Debug.LogWarning($"No ingredients found for '{selectedIllness}'");
            
            // Create a "No Ingredients" text
            GameObject noIngredientsObj = new GameObject("NoIngredientsText");
            noIngredientsObj.transform.SetParent(contentPanel, false);
            
            TextMeshProUGUI noIngredientsText = noIngredientsObj.AddComponent<TextMeshProUGUI>();
            noIngredientsText.text = "No ingredients found for this remedy!";
            noIngredientsText.fontSize = 24;
            noIngredientsText.alignment = TextAlignmentOptions.Center;
            noIngredientsText.color = Color.red;
            
            RectTransform rt = noIngredientsText.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            return;
        }
        
        Debug.Log($"Displaying {currentRemedy.ingredients.Count} ingredients for '{selectedIllness}'");
        
        // Create ingredient UI elements
        foreach (string ingredientName in currentRemedy.ingredients)
        {
            GameObject newIngredient = Instantiate(ingredientPrefab, contentPanel);
            
            if (newIngredient == null)
            {
                Debug.LogError("Failed to instantiate ingredient prefab!");
                continue;
            }
            
            // Set the ingredient name - try both "name" and "IngredientName" as possible child names
            Transform nameTransform = newIngredient.transform.Find("name");
            if (nameTransform == null)
                nameTransform = newIngredient.transform.Find("IngredientName");
                
            if (nameTransform != null)
            {
                TextMeshProUGUI nameText = nameTransform.GetComponent<TextMeshProUGUI>();
                if (nameText != null)
                {
                    nameText.text = ingredientName;
                }
            }
            else
            {
                Debug.LogWarning("Could not find name/IngredientName child in prefab");
            }
            
            // Try to find an image component in the prefab
            Image ingredientImage = newIngredient.GetComponentInChildren<Image>();
            
            // If prefab has a specific "IngredientImage" child, use that instead
            Transform imageTransform = newIngredient.transform.Find("IngredientImage");
            if (imageTransform != null)
            {
                Image specificImage = imageTransform.GetComponent<Image>();
                if (specificImage != null)
                {
                    ingredientImage = specificImage;
                }
            }
            
            // If we found an image component, try to load the ingredient image
            if (ingredientImage != null)
            {
                // Prepare the image name - remove spaces and special characters
                string sanitizedName = ingredientName.Replace(" ", "")
                    .Replace("-", "")
                    .Replace("(", "")
                    .Replace(")", "")
                    .Replace("'", "")
                    .ToLower();
                
                // Try different possible filenames
                Sprite ingredientSprite = TryLoadIngredientSprite(ingredientName);
                
                if (ingredientSprite != null)
                {
                    ingredientImage.sprite = ingredientSprite;
                    Debug.Log($"Loaded image for {ingredientName}");
                    
                    // Make sure sprite aspect ratio is preserved
                    ingredientImage.preserveAspect = true;
                }
                else
                {
                    // Use default sprite if available
                    if (defaultIngredientSprite != null)
                    {
                        ingredientImage.sprite = defaultIngredientSprite;
                        Debug.LogWarning($"Image not found for {ingredientName}, using default sprite");
                    }
                    else
                    {
                        Debug.LogWarning($"No image found for {ingredientName} and no default sprite set");
                    }
                }
            }
            
            // Handle ingredient description if needed
            Transform descTransform = newIngredient.transform.Find("IngredientDescription");
            if (descTransform != null)
            {
                TextMeshProUGUI descText = descTransform.GetComponent<TextMeshProUGUI>();
                if (descText != null)
                {
                    descText.text = ""; // No description available in current data structure
                }
            }
        }
        
        // Display the recipe instructions if available
        if (!string.IsNullOrEmpty(currentRemedy.instructions) && instructionsText != null)
        {
            instructionsText.text = FormatInstructions(currentRemedy.instructions);
        }
    }
    
    // Try to load an ingredient sprite with different name variations
    private Sprite TryLoadIngredientSprite(string ingredientName)
    {
        // Create variations of the name to try
        List<string> nameVariations = new List<string>
        {
            ingredientName,
            ingredientName.ToLower(),
            ingredientName.Replace(" ", ""),
            ingredientName.Replace(" ", "_"),
            ingredientName.Replace(" ", "-"),
            ingredientName.ToLower().Replace(" ", ""),
            ingredientName.ToLower().Replace(" ", "_"),
            ingredientName.ToLower().Replace(" ", "-")
        };
        
        // Try all variations
        foreach (string variation in nameVariations)
        {
            // Try with .jpg extension
            string path = ingredientImagePath + variation;
            Sprite sprite = Resources.Load<Sprite>(path);
            
            if (sprite != null)
            {
                Debug.Log($"Found sprite at {path}");
                return sprite;
            }
            
            // If the direct path doesn't work, Unity sometimes needs the path without extension
            // (it figures out the extension on its own)
            string pathWithoutExtension = path.Replace(".jpg", "");
            sprite = Resources.Load<Sprite>(pathWithoutExtension);
            
            if (sprite != null)
            {
                Debug.Log($"Found sprite at {pathWithoutExtension}");
                return sprite;
            }
        }
        
        Debug.LogWarning($"Could not find image for {ingredientName} after trying multiple variations");
        return null;
    }
    
    private string FormatInstructions(string rawInstructions)
    {
        // Format the instructions to be more readable
        string[] steps = rawInstructions.Split('.');
        string formattedInstructions = "";
        
        for (int i = 0; i < steps.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(steps[i]))
            {
                formattedInstructions += (i + 1) + ". " + steps[i].Trim() + ".\n\n";
            }
        }
        
        return formattedInstructions.TrimEnd('\n');
    }
}