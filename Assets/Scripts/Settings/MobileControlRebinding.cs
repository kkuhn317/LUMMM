using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MobileControlRebinding : MonoBehaviour
{
    public static MobileControlRebinding instance;
    private DraggableMobileButton selectedButton = null;
    public Slider scaleSlider;
    [HideInInspector] public UnityEvent onResetPressed; // Buttons will subscribe to this event to reset their position and scale

    void Awake() {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        scaleSlider.onValueChanged.AddListener(OnScaleSliderChanged);
    }

    public void SetSelectedButton(DraggableMobileButton button)
    {
        if (selectedButton == button) { return; }

        if (selectedButton != null)
        {
            selectedButton.UnselectButton();
        }

        selectedButton = button;
        scaleSlider.value = selectedButton.transform.localScale.x;
    }

    public void OnScaleSliderChanged(float value)
    {
        if (selectedButton != null)
        {
            selectedButton.SetScale(value);
        }
    }

    public void ResetButtons()
    {
        onResetPressed?.Invoke();
        
        if (selectedButton != null)
        {
            selectedButton.UnselectButton();
            selectedButton = null; 
        }
        
        scaleSlider.value = 1f;
    }
}
