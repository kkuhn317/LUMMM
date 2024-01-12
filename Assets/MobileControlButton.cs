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

    void Start() {
        image = GetComponent<Image>();
    }
    
    void TurnOn(){
        if (GameManager.isPaused) {
            return;
        }
        buttonPressed = true;
        onPress.Invoke();
        image.sprite = downSprite;
    }

    void TurnOff() {
        buttonPressed = false;
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
