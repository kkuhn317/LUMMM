using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class MobileControlRebinding : MonoBehaviour
{
    public static MobileControlRebinding instance;
    private DraggableMobileButton selectedButton = null;
    public GameObject instructionsObject;
    public GameObject scaleSizeObject;
    public TMP_Text scalePercentageText;
    public Slider scaleSlider;
    [HideInInspector] public UnityEvent onResetPressed; // Buttons will subscribe to this event to reset their position and scale
    private List<DraggableMobileButton> buttons = new();    // Will be populated automatically by the buttons

    void Awake() {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        scaleSlider.onValueChanged.AddListener(OnScaleSliderChanged);
        // Start with scalePercentageText hidden since no button is selected
        instructionsObject.SetActive(true);
        scaleSizeObject.SetActive(false);
        scalePercentageText.gameObject.SetActive(false);
    }

    void OnDisable() {
        // Save all the button positions and scales
        Debug.Log("Saving mobile button layout");
        Dictionary<string, MobileRebindingData.MobileButtonData> newData = new();
        foreach (DraggableMobileButton button in buttons) {
            newData.Add(button.buttonID, button.GetData());
        }
        RebindSaveLoad.SaveMobileBindings(newData);
    }

    public void AddButton(DraggableMobileButton button) {
        buttons.Add(button);
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

        instructionsObject.SetActive(false);
        scaleSizeObject.SetActive(true);
        scalePercentageText.gameObject.SetActive(true);
        UpdateScalePercentageText(scaleSlider.value);
    }

    public void OnScaleSliderChanged(float value)
    {
        if (selectedButton != null)
        {
            selectedButton.SetScale(value);
            UpdateScalePercentageText(value);
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
        instructionsObject.SetActive(true);
        scaleSizeObject.SetActive(false);
        // Hide the scale percentage text when no button is selected
        scalePercentageText.gameObject.SetActive(false);
    }

    private void UpdateScalePercentageText(float scaleValue)
    {
        if (scalePercentageText == null) return; // Prevent errors if the UI is missing

        int percentage = Mathf.RoundToInt(scaleValue * 100);
        scalePercentageText.text = $"{percentage}%";
    }
}
