using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RebindSettings : MonoBehaviour
{
    [SerializeField] Slider buttonPressedOpacitySlider;
    [SerializeField] Slider buttonUnpressedOpacitySlider;
    [SerializeField] TMP_Text buttonPressedOpacityText;
    [SerializeField] TMP_Text buttonUnpressedOpacityText;
    [SerializeField] CanvasGroup optionsCanvasGroup;
    [SerializeField] Button firstSelectedButton;
    [SerializeField] Button ControlsButton; // Will be selected when the rebind menu is closed

    // Start is called before the first frame update
    void Start()
    {
        // Update the text labels
        UpdateOpacityText(buttonPressedOpacityText, buttonPressedOpacitySlider.value);
        UpdateOpacityText(buttonUnpressedOpacityText, buttonUnpressedOpacitySlider.value);
    }

    void OnEnable()
    {
        // Disable interactability of the options canvas group
        optionsCanvasGroup.interactable = false;

        // Set the selected object to the first button
        firstSelectedButton.Select();   // TODO: This was working, but now it's not. I don't know why.
    }

    void OnDisable()
    {
        // Enable interactability of the options canvas group
        optionsCanvasGroup.interactable = true;

        // Set the selected object to the controls button
        ControlsButton.Select();
    }

    // Update is called once per frame
    void Update()
    {
        ChangeButtonPressedOpacityText();
        ChangeButtonUnpressedOpacityText();
    }

    public void ChangeButtonPressedOpacityText()
    {
        float opacity = buttonPressedOpacitySlider.value;
        UpdateOpacityText(buttonPressedOpacityText, opacity);
    }

    public void ChangeButtonUnpressedOpacityText()
    {
        float opacity = buttonUnpressedOpacitySlider.value;
        UpdateOpacityText(buttonUnpressedOpacityText, opacity);
    }

    private void UpdateOpacityText(TMP_Text text, float value)
    {
        text.text = Mathf.RoundToInt(value * 100) + "%";
    }
}
