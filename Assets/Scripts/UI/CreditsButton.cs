using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CreditsButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public GameObject creditsScroller; // Reference to the CreditsScroller component
    public enum ButtonType { Up, Down }; // Enum to determine the type of button
    public ButtonType buttonType; // The type of button

    // Method to handle the button click event
    public void OnPointerDown(PointerEventData eventData) {
        // Check the type of button
        switch (buttonType)
        {
            case ButtonType.Up:
                // Call the OnUpButtonPress method of the CreditsScroller component
                creditsScroller.GetComponent<CreditsScroller>().OnUpButtonPress();
                break;
            case ButtonType.Down:
                // Call the OnDownButtonPress method of the CreditsScroller component
                creditsScroller.GetComponent<CreditsScroller>().OnDownButtonPress();
                break;
        }
    }

    // Method to handle the button release event
    public void OnPointerUp(PointerEventData eventData) {
        // Call the OnButtonRelease method of the CreditsScroller component
        creditsScroller.GetComponent<CreditsScroller>().OnButtonRelease();
    }
}
