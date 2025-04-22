using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class CookingManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform ingredientContainer; 
    public GameObject ingredientPrefab; 
    public GameObject[] ingredientSlots;
    public GameObject pot;
    public GameObject pot_filled;
    public GameObject mixButton;
    public GameObject nextButton;
    public TextMeshProUGUI instructionsText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI noitemText;
    
    [Header("Settings")]
    public float retryDelay = 0.5f;
    public int maxRetries = 10;
    public string fallbackScene = "IllnessList";
    
    private string selectedIllness;
    private Remedy currentRemedy;
    private List<string> requiredIngredients = new List<string>();
    private List<string> addedIngredients = new List<string>();
    private bool allIngredientsAdded = false;
    
    // Reference to our own instance of RemedyManager data (to avoid DontDestroyOnLoad issues)
    private RemedyList localRemedyData;

    void Start()
    {
        // Hide buttons initially
        if (mixButton) mixButton.SetActive(false);
        if (nextButton) nextButton.SetActive(false);
        
        // Get the selected illness from PlayerPrefs
        selectedIllness = PlayerPrefs.GetString("SelectedIllness", "");
        
        if (string.IsNullOrEmpty(selectedIllness))
        {
            Debug.LogError("No illness was selected in PlayerPrefs!");
            ShowErrorAndOfferReturn("No illness selected");
            return;
        }
        
        // Create a local copy of remedy data to avoid DontDestroyOnLoad issues
        if (RemedyManager.instance != null && RemedyManager.instance.remedyData != null)
        {
            localRemedyData = RemedyManager.instance.remedyData;
        }
        else
        {
            // If no RemedyManager, load the data directly
            TextAsset jsonFile = Resources.Load<TextAsset>("remedies");
            if (jsonFile != null)
            {
                localRemedyData = JsonUtility.FromJson<RemedyList>(jsonFile.text);
                Debug.Log("Loaded remedies data directly in CookingManager");
            }
            else
            {
                Debug.LogError("Remedies JSON file not found!");
                ShowErrorAndOfferReturn("Remedy data file not found");
                return;
            }
        }
        
        StartCoroutine(InitializeWithRetry());
    }
    
    IEnumerator InitializeWithRetry()
    {
        int retryCount = 0;
        bool dataLoaded = false;
        
        Debug.Log($"Looking for remedy data for '{selectedIllness}'");
        
        // First wait a moment to ensure everything is initialized
        yield return new WaitForSeconds(0.1f);
        
        while (!dataLoaded && retryCount < maxRetries)
        {
            Debug.Log($"Attempt {retryCount+1}/{maxRetries} to load remedy '{selectedIllness}'");
            
            // First try to find the remedy in our local data
            if (localRemedyData != null && localRemedyData.remedies != null && localRemedyData.remedies.Count > 0)
            {
                // Try to find the remedy
                currentRemedy = FindRemedyInList(selectedIllness, localRemedyData.remedies);
                
                if (currentRemedy != null)
                {
                    dataLoaded = true;
                    Debug.Log($"Found remedy in local data: {currentRemedy.name}");
                    break;
                }
            }
            
            // If we still don't have a remedy, try RemedyManager again
            if (RemedyManager.instance != null)
            {
                currentRemedy = RemedyManager.instance.GetRemedy(selectedIllness);
                if (currentRemedy != null)
                {
                    // Make a copy to avoid DontDestroyOnLoad issues
                    currentRemedy = CloneRemedy(currentRemedy);
                    dataLoaded = true;
                    Debug.Log($"Found remedy in RemedyManager: {currentRemedy.name}");
                    break;
                }
            }
            
            // If still not found, try alternate names
            string[] alternateNames = {
                selectedIllness.ToLower(),
                selectedIllness.ToUpper(),
                System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(selectedIllness.ToLower()),
                selectedIllness.Replace(" ", ""),
                selectedIllness.Replace("-", " ")
            };
            
            foreach (string alternateName in alternateNames)
            {
                if (localRemedyData != null && localRemedyData.remedies != null)
                {
                    currentRemedy = FindRemedyInList(alternateName, localRemedyData.remedies);
                    if (currentRemedy != null)
                    {
                        dataLoaded = true;
                        Debug.Log($"Found remedy using alternate name: '{alternateName}'");
                        break;
                    }
                }
            }
            
            // If we still don't have it, retry after a delay
            if (!dataLoaded)
            {
                Debug.LogWarning($"Selected illness '{selectedIllness}' not found! Retry {retryCount+1}/{maxRetries}");
                yield return new WaitForSeconds(retryDelay);
                retryCount++;
            }
        }
        
        if (dataLoaded)
        {
            Debug.Log($"Successfully loaded remedy data for '{selectedIllness}'");
            SetupCookingScene();
        }
        else
        {
            Debug.LogError($"Failed to load remedy data for '{selectedIllness}' after maximum retries!");
            ShowErrorAndOfferReturn($"Could not find remedy for '{selectedIllness}'");
        }
    }
    
    // Helper method to find a remedy in a list using case-insensitive matching
    private Remedy FindRemedyInList(string illness, List<Remedy> remedyList)
    {
        // Try exact match first
        Remedy result = remedyList.Find(r => r.name == illness);
        
        // If not found, try case-insensitive
        if (result == null)
        {
            result = remedyList.Find(r => 
                string.Equals(r.name, illness, System.StringComparison.OrdinalIgnoreCase));
        }
        
        // If still not found, try trimming whitespace
        if (result == null)
        {
            result = remedyList.Find(r => 
                string.Equals(r.name.Trim(), illness.Trim(), System.StringComparison.OrdinalIgnoreCase));
        }
        
        return result;
    }
    
    // Helper method to clone a remedy to avoid DontDestroyOnLoad issues
    private Remedy CloneRemedy(Remedy original)
    {
        Remedy clone = new Remedy();
        clone.name = original.name;
        clone.instructions = original.instructions;
        
        // Clone the ingredients list
        clone.ingredients = new List<string>();
        if (original.ingredients != null)
        {
            foreach (string ingredient in original.ingredients)
            {
                clone.ingredients.Add(ingredient);
            }
        }
        
        return clone;
    }
    
    private void ShowErrorAndOfferReturn(string errorMessage)
    {
        if (statusText) statusText.text = $"Error: {errorMessage}\nThe recipe data could not be loaded.";
        
        // Create a return button if we don't have the next button
        if (nextButton)
        {
            nextButton.SetActive(true);
            Button button = nextButton.GetComponent<Button>();
            if (button)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SceneManager.LoadScene(fallbackScene));
            }
            
            // Update button text if possible
            TextMeshProUGUI buttonText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText) buttonText.text = "Return to Illness List";
        }
    }
    
    void SetupCookingScene()
    {
        // Store required ingredients
        requiredIngredients = new List<string>(currentRemedy.ingredients);
        
        // Show instructions
        if (instructionsText != null && !string.IsNullOrEmpty(currentRemedy.instructions))
        {
            instructionsText.text = $"Recipe for {currentRemedy.name}:\n{FormatInstructions(currentRemedy.instructions)}";
        }
        
        // Update status text
        if (statusText)
        {
            statusText.text = $"Preparing remedy for: {currentRemedy.name}\nDrag ingredients to the pot.";
        }
        
        // Create ingredient objects
        SpawnIngredients();
    }
    
    // Modification to the SpawnIngredients method

// Update the SpawnIngredients method to include image loading

void SpawnIngredients()
{
    if (ingredientContainer == null || ingredientPrefab == null)
    {
        Debug.LogError("Missing reference to ingredientContainer or ingredientPrefab!");
        return;
    }
    
    // Clear any existing ingredients
    foreach (Transform child in ingredientContainer)
    {
        Destroy(child.gameObject);
    }
    
    // Check if we have any ingredients
    if (currentRemedy.ingredients == null || currentRemedy.ingredients.Count == 0)
    {
        Debug.LogWarning("No ingredients found for this remedy!");
        
        // Create a "No Ingredients" text
        GameObject noIngredientsObj = new GameObject("NoIngredientsText");
        noIngredientsObj.transform.SetParent(ingredientContainer, false);
        
        // Add TextMeshProUGUI component
        TMPro.TextMeshProUGUI noIngredientsText = noIngredientsObj.AddComponent<TMPro.TextMeshProUGUI>();
        noIngredientsText.text = "No ingredients found for this remedy!";
        noIngredientsText.fontSize = 24;
        noIngredientsText.alignment = TMPro.TextAlignmentOptions.Center;
        noIngredientsText.color = Color.red;
        
        // Set up RectTransform to fill container
        RectTransform rectTransform = noIngredientsText.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        // Update status text
        if (statusText) statusText.text = "No ingredients found for this remedy!";
        
        // Show the noitemText if available
        if (noitemText != null && noitemText.gameObject != null)
        {
            noitemText.gameObject.SetActive(true);
        }
        
        return;
    }
    
    // Hide the noitemText if there are ingredients
    if (noitemText != null && noitemText.gameObject != null)
    {
        noitemText.gameObject.SetActive(false);
    }
    
    // Spawn all ingredients
    foreach (string ingredientName in currentRemedy.ingredients)
    {
        // Create the ingredient - NOT using the parent parameter to avoid DontDestroyOnLoad issues
        GameObject newIngredient = Instantiate(ingredientPrefab);   
        // Set parent after instantiation
        newIngredient.transform.SetParent(ingredientContainer, false);
        
        newIngredient.name = "Ingredient_" + ingredientName; // Set a meaningful name
        
        // Set the ingredient text
        TMPro.TextMeshProUGUI nameText = newIngredient.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (nameText != null)
        {
            nameText.text = ingredientName;
        }
        else
        {
            Debug.LogWarning($"No TextMeshProUGUI component found on ingredient prefab for {ingredientName}!");
        }
        
        // Add identifier component to track this ingredient
        IngredientIdentifier identifier = newIngredient.GetComponent<IngredientIdentifier>();
        if (identifier == null)
        {
            identifier = newIngredient.AddComponent<IngredientIdentifier>();
        }
        identifier.ingredientName = ingredientName;
        
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
            // Try to load the image from Resources
            Sprite ingredientSprite = TryLoadIngredientSprite(ingredientName);
            
            if (ingredientSprite != null)
            {
                ingredientImage.sprite = ingredientSprite;
                ingredientImage.preserveAspect = true;
                Debug.Log($"Loaded image for {ingredientName}");
            }
            else
            {
                Debug.LogWarning($"No image found for {ingredientName}");
            }
        }
        
        // Add dragging component if not already present
        DragIngredient dragComponent = newIngredient.GetComponent<DragIngredient>();
        if (dragComponent == null)
        {
            dragComponent = newIngredient.AddComponent<DragIngredient>();
        }
        
        // Make sure it has a CanvasGroup for dragging
        if (newIngredient.GetComponent<CanvasGroup>() == null)
        {
            newIngredient.AddComponent<CanvasGroup>();
        }
        
        Debug.Log($"Spawned ingredient: {ingredientName} with identifier: {(identifier != null ? "yes" : "no")}");
    }
    
    // Update UI with ingredient count
    if (statusText) 
    {
        if (requiredIngredients.Count > 0)
        {
            statusText.text = $"Add {requiredIngredients.Count} ingredients to the pot";
        }
        else
        {
            statusText.text = "No ingredients required for this remedy";
        }
    }
}

// Add this helper method to try loading the ingredient sprite
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
    
    // Path to ingredient images folder
    string ingredientImagePath = "Ingredients/";
    
    // Try all variations
    foreach (string variation in nameVariations)
    {
        // Try loading the sprite
        Sprite sprite = Resources.Load<Sprite>(ingredientImagePath + variation);
        
        if (sprite != null)
        {
            Debug.Log($"Found sprite at {ingredientImagePath + variation}");
            return sprite;
        }
        
        // Try without extension (Unity sometimes figures out the extension automatically)
        sprite = Resources.Load<Sprite>(ingredientImagePath + variation.Replace(".jpg", ""));
        
        if (sprite != null)
        {
            Debug.Log($"Found sprite at {ingredientImagePath + variation.Replace(".jpg", "")}");
            return sprite;
        }
    }
    
    Debug.LogWarning($"Could not find image for {ingredientName} after trying multiple variations");
    return null;
}
    
    public void IngredientAddedToPot(string ingredientName)
    {
        if (!addedIngredients.Contains(ingredientName))
        {
            addedIngredients.Add(ingredientName);
            Debug.Log($"Added {ingredientName} to pot. ({addedIngredients.Count}/{requiredIngredients.Count})");
            
            // Update UI
            if (statusText) statusText.text = $"Added {addedIngredients.Count} of {requiredIngredients.Count} ingredients";
            
            // Check if all ingredients are added
            CheckIngredients();
        }
    }
    
    private void CheckIngredients()
    {
        // Check if we have added all required ingredients
        if (addedIngredients.Count >= requiredIngredients.Count)
        {
            // Optional: Check if they are the correct ingredients
            bool allCorrect = true;
            foreach (string ingredient in addedIngredients)
            {
                if (!requiredIngredients.Contains(ingredient))
                {
                    allCorrect = false;
                    break;
                }
            }
            
            allIngredientsAdded = allCorrect;
            
            if (allIngredientsAdded)
            {
                mixButton.SetActive(true);
                if (statusText) statusText.text = "All ingredients added! Click Mix to prepare the remedy.";
            }
            else
            {
                if (statusText) statusText.text = "Some ingredients are incorrect. Please check the recipe.";
            }
        }
    }
    
    public void MixIngredients()
    {
        Debug.Log("Cooking Started...");
        if (statusText) statusText.text = "Mixing ingredients...";
        pot_filled.SetActive(true);
        mixButton.SetActive(false);
        
        // Play mixing animation or effect here
        
        // Show success message
        if (statusText) statusText.text = "Remedy prepared successfully!";
        
        // Enable next button
        nextButton.SetActive(true);
    }
    
    public void GoToNextScene()
    {
        SceneManager.LoadScene("ResultScene");
    }
    
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
}