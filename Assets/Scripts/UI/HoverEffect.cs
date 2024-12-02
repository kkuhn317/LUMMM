using UnityEngine;
using UnityEngine.EventSystems;

public class HoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public GameObject hoverPanel;
    private bool isHovering = false;

    // Called when the pointer enters the UI element or object
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        StartHoverEffect();
    }

    // Called when the pointer exits the UI element or object
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        StopHoverEffect();
    }

     // Called when the element is selected via keyboard navigation (Tab key)
    public void OnSelect(BaseEventData eventData)
    {
        isHovering = true;
        StartHoverEffect();
    }

    // Called when the element is deselected
    public void OnDeselect(BaseEventData eventData)
    {
        isHovering = false;
        StopHoverEffect();
    }

    private void Update()
    {
        // Handle touch input for mobile
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Vector2 touchPosition = touch.position;
                if (RectTransformUtility.RectangleContainsScreenPoint(
                    GetComponent<RectTransform>(), touchPosition, Camera.main))
                {
                    isHovering = true;
                    StartHoverEffect();
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isHovering = false;
                StopHoverEffect();
            }
        }
    }

    private void StartHoverEffect()
    {
        // Implement what happens when the hover starts
        Debug.Log("Hover started");
        hoverPanel.SetActive(true);
    }

    private void StopHoverEffect()
    {
        // Implement what happens when the hover ends
        Debug.Log("Hover ended");
        hoverPanel.SetActive(false);
    }
}