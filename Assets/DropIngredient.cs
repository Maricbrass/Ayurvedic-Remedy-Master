using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropIngredient : MonoBehaviour, IDropHandler
{
    [Tooltip("Container for tracking added ingredients (invisible)")]
    public Transform ingredientsContainer;
    
    [Tooltip("Optional animation effect for successful drops")]
    public GameObject dropEffectPrefab;
    
    private CookingManager cookingManager;
    
    private void Start()
    {
        // Find the cooking manager in the scene
        cookingManager = FindObjectOfType<CookingManager>();
        if (cookingManager == null)
        {
            Debug.LogError("CookingManager not found in the scene!");
        }
        
        // Create ingredients container if not assigned
        if (ingredientsContainer == null)
        {
            GameObject container = new GameObject("DroppedIngredientsContainer");
            container.transform.SetParent(transform);
            container.transform.localPosition = Vector3.zero;
            ingredientsContainer = container.transform;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("Something was dropped on the pot");
        
        GameObject droppedItem = eventData.pointerDrag;
        
        if (droppedItem != null)
        {
            Debug.Log($"Item dropped: {droppedItem.name}");
            
            // Get the ingredient identifier (checking for null)
            IngredientIdentifier identifier = droppedItem.GetComponent<IngredientIdentifier>();
            
            if (identifier == null)
            {
                Debug.LogWarning("IngredientIdentifier component missing on dropped item! Adding one...");
                
                // Try to get the name from TextMeshProUGUI component as fallback
                TMPro.TextMeshProUGUI nameText = droppedItem.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                string ingredientName = nameText != null ? nameText.text : "Unknown Ingredient";
                
                // Add identifier component with the name we found
                identifier = droppedItem.AddComponent<IngredientIdentifier>();
                identifier.ingredientName = ingredientName;
                
                Debug.Log($"Added identifier with name: {ingredientName}");
            }
            
            // Hide the original draggable item
            droppedItem.SetActive(false);
            
            // Notify the cooking manager about the added ingredient
            if (cookingManager != null && identifier != null)
            {
                Debug.Log($"Notifying cooking manager about: {identifier.ingredientName}");
                cookingManager.IngredientAddedToPot(identifier.ingredientName);
                
                // Play drop effect animation if available
                if (dropEffectPrefab != null)
                {
                    GameObject effect = Instantiate(dropEffectPrefab, transform);
                    effect.transform.localPosition = Vector3.zero;
                    Destroy(effect, 2f); // Clean up effect after 2 seconds
                }
            }
            else
            {
                Debug.LogWarning("Could not identify dropped ingredient or cooking manager is null!");
                // Re-activate the item if there was an error
                droppedItem.SetActive(true);
            }
            
            // Disable further dragging
            DragIngredient dragComponent = droppedItem.GetComponent<DragIngredient>();
            if (dragComponent != null)
            {
                dragComponent.enabled = false;
            }
        }
        else
        {
            Debug.LogWarning("Null item was dropped!");
        }
    }
}