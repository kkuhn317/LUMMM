using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch; // new input system touch
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class HoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public GameObject hoverPanel;
    private bool isHovering = false;

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    // Called when the pointer enters the UI element or object
    public void OnPointerEnter(PointerEventData eventData)
    {
        StartHoverEffect();
    }

    // Called when the pointer exits the UI element or object
    public void OnPointerExit(PointerEventData eventData)
    {
        StopHoverEffect();
    }

     // Called when the element is selected via keyboard navigation (Tab key)
    public void OnSelect(BaseEventData eventData)
    {
        StartHoverEffect();
    }

    // Called when the element is deselected
    public void OnDeselect(BaseEventData eventData)
    {
        StopHoverEffect();
    }

    private void Update()
    {
        // Handle touch input for mobile
        // Only run if there are active touches
        if (Touch.activeTouches.Count > 0)
        {
            var touch = Touch.activeTouches[0];

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                Vector2 touchPosition = touch.screenPosition;
                if (RectTransformUtility.RectangleContainsScreenPoint(
                    GetComponent<RectTransform>(), touchPosition, Camera.main))
                {
                    StartHoverEffect();
                }
            }
            else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                     touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                StopHoverEffect();
            }
        }
    }

    private void StartHoverEffect()
    {
        if (isHovering) return;
        isHovering = true;

        // Implement what happens when the hover starts
        Debug.Log("Hover started");
        hoverPanel.SetActive(true);
    }

    private void StopHoverEffect()
    {
        if (!isHovering) return;
        isHovering = false;

        // Implement what happens when the hover ends
        Debug.Log("Hover ended");
        hoverPanel.SetActive(false);
    }
}