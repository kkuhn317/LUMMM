using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class MobileControlButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
    public bool buttonPressed;

    public UnityEvent onPress;
    public UnityEvent onRelease;
    
    public void OnPointerDown(PointerEventData eventData){
        if (GameManager.isPaused) {
            return;
        }
        buttonPressed = true;
        onPress.Invoke();
    }
    
    public void OnPointerUp(PointerEventData eventData){
        buttonPressed = false;
        onRelease.Invoke();
    }
}
