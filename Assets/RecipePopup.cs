using UnityEngine;
using TMPro;

public class RecipePopup : MonoBehaviour
{
    public TextMeshProUGUI recipeText;
    public GameObject popupPanel;

    private void Start()
    {
        // Ensure popup is closed initially
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
        
        // Debug the reference
        if (recipeText == null)
        {
            Debug.LogError("Recipe Text component is not assigned in the inspector!");
        }
    }

    public void ShowRecipe()
    {
        Debug.Log("ShowRecipe method called");
        
        // Make sure text component is assigned
        if (recipeText == null)
        {
            Debug.LogError("Recipe Text component is not assigned!");
            return;
        }
        
        string selectedIllness = PlayerPrefs.GetString("SelectedIllness", "");
        Debug.Log($"Selected illness: '{selectedIllness}'");
        
        // Check if the selected illness is empty
        if (string.IsNullOrEmpty(selectedIllness))
        {
            recipeText.text = "No illness selected.";
            Debug.Log("Setting empty illness text");
            if (popupPanel != null) popupPanel.SetActive(true);
            return;
        }
        
        // Try to get the remedy from RemedyManager
        if (RemedyManager.instance != null && RemedyManager.instance.remedyData != null)
        {
            Remedy remedy = RemedyManager.instance.GetRemedy(selectedIllness);
            Debug.Log($"Looking for remedy: '{selectedIllness}', Found: {remedy != null}");
            
            if (remedy != null)
            {
                // Format the recipe nicely
                string recipeContent = $"<b>{remedy.name}</b>\n\n<u>Ingredients:</u>\n";
                
                // Add ingredients list
                if (remedy.ingredients != null && remedy.ingredients.Count > 0)
                {
                    foreach (string ingredient in remedy.ingredients)
                    {
                        recipeContent += $"• {ingredient}\n";
                    }
                }
                else
                {
                    recipeContent += "No ingredients listed.\n";
                }
                
                // Add instructions
                if (!string.IsNullOrEmpty(remedy.instructions))
                {
                    recipeContent += $"\n<u>Instructions:</u>\n{FormatInstructions(remedy.instructions)}";
                }
                else
                {
                    recipeContent += "\n<u>Instructions:</u>\nNo instructions provided.";
                }
                
                // Set the text explicitly
                recipeText.text = recipeContent;
                Debug.Log($"Set recipe text (length: {recipeContent.Length}):\n{recipeContent}");
                
                // Force text update
                recipeText.ForceMeshUpdate();
            }
            else
            {
                // Recipe not found in RemedyManager
                recipeText.text = $"Recipe for '{selectedIllness}' not found. Please check the remedy data.";
                Debug.LogWarning($"Recipe for '{selectedIllness}' not found in RemedyManager");
            }
        }
        else
        {
            // RemedyManager not available
            recipeText.text = "Recipe data is not available. Please check if RemedyManager is initialized properly.";
            Debug.LogError("RemedyManager is not available");
        }
        
        // Always show the popup
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
            Debug.Log("Popup panel activated");
        }
        else
        {
            Debug.LogError("Popup panel reference is null!");
        }
    }

    public void CloseRecipe()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
            Debug.Log("Popup panel closed");
        }
    }
    
    private string FormatInstructions(string rawInstructions)
    {
        if (string.IsNullOrEmpty(rawInstructions))
            return "No instructions available.";
            
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