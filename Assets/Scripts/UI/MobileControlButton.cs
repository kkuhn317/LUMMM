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
    // TODO: Make this code better lol

    void Start() {
        image = GetComponent<Image>();

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
    }

    void TurnOff() {
        buttonPressed = false;
        if (isRunButton) {
            GlobalVariables.mobileRunButtonPressed = false;
        }
        onRelease.Invoke();
        image.sprite = upSprite;
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
