using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDropdownScroller : MonoBehaviour, ISelectHandler, IPointerEnterHandler
{
    private ScrollRect scrollRect;
    private float scrollPosition = 1;
    private bool isPointerSelection = false; // Flag to track pointer-based selection

    void Start()
    {
        scrollRect = GetComponentInParent<ScrollRect>(true);

        if (scrollRect == null || scrollRect.content == null)
        {
            Debug.LogWarning("ScrollRect or its content not found.");
            return;
        }

        int childCount = scrollRect.content.transform.childCount - 1;
        int childIndex = transform.GetSiblingIndex();

        childIndex = Mathf.Clamp(childIndex, 0, childCount - 1);
        scrollPosition = 1 - ((float)childIndex / childCount);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Set the flag to indicate pointer interaction
        isPointerSelection = true;
    }

    public void OnSelect(BaseEventData eventData)
    {
        // If the selection was caused by pointer interaction, reset the flag and skip
        if (isPointerSelection)
        {
            isPointerSelection = false; // Reset the flag
            return;
        }

        // Perform scrolling only for navigation input (keyboard/controller)
        if (scrollRect)
        {
            Canvas.ForceUpdateCanvases(); // Ensure layout updates
            scrollRect.verticalScrollbar.value = scrollPosition;
        }
    }
}