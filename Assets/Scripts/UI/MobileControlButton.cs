using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class MobileControlButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
    public bool buttonPressed;

    public UnityEvent onPress;
    public UnityEvent onRelease;

    public bool toggleButton; // if true, button stays pressed until pressed again

    public Sprite upSprite;
    public Sprite downSprite;
    private Image image;
    public bool isRunButton = false;  // if true, this button will be used to run

    private float buttonPressedOpacity;
    private float buttonUnpressedOpacity;

    // TODO: Make this code better lol

    void Start() {
        image = GetComponent<Image>();

        buttonPressedOpacity = PlayerPrefs.GetFloat(SettingsKeys.ButtonPressedOpacityKey, 0.38f);
        buttonUnpressedOpacity = PlayerPrefs.GetFloat(SettingsKeys.ButtonUnpressedOpacityKey, 0.38f);

        // Set opacity
        if (buttonPressed) {
            image.color = new Color(image.color.r, image.color.g, image.color.b, buttonPressedOpacity);
        } else {
            image.color = new Color(image.color.r, image.color.g, image.color.b, buttonUnpressedOpacity);
        }

        if (isRunButton && GlobalVariables.OnScreenControls && GlobalVariables.mobileRunButtonPressed) {

            StartCoroutine(TurnOnAtBeginCoroutine());
        }
            
    }


    IEnumerator TurnOnAtBeginCoroutine() {
        yield return new WaitForEndOfFrame();
        TurnOn();
    }
    
    void TurnOn(){
        if (GameManager.isPaused) {
            return;
        }
        buttonPressed = true;
        if (isRunButton) {
            GlobalVariables.mobileRunButtonPressed = true;
        }
        onPress.Invoke();
        image.sprite = downSprite;
        image.color = new Color(image.color.r, image.color.g, image.color.b, buttonPressedOpacity);
    }

    void TurnOff() {
        buttonPressed = false;
        if (isRunButton) {
            GlobalVariables.mobileRunButtonPressed = false;
        }
        onRelease.Invoke();
        image.sprite = upSprite;
        image.color = new Color(image.color.r, image.color.g, image.color.b, buttonUnpressedOpacity);
    }

    public void OnPointerDown(PointerEventData eventData){
        if (toggleButton) {
            if (buttonPressed) {
                TurnOff();
            } else {
                TurnOn();
            }
        } else {
            TurnOn();
        }
    }
    
    public void OnPointerUp(PointerEventData eventData){
        if (!toggleButton) {
            TurnOff();
        }
    }

    


}
