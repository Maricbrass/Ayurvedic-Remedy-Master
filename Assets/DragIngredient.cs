using UnityEngine;
using UnityEngine.EventSystems;

public class DragIngredient : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Transform originalParent;
    private Vector2 originalPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;

        // Ensure CanvasGroup exists
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log(gameObject.name + " Dragging started");
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        
        // Store reference to our original parent and position in case drop fails
        originalParent = transform.parent;
        originalPosition = rectTransform.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log(gameObject.name + " Dragging ended");
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        
        // If dropped on nothing, return to original position
        // This only happens if the drop target didn't handle the object
        if (transform.parent == originalParent)
        {
            rectTransform.anchoredPosition = originalPosition;
        }
    }
}