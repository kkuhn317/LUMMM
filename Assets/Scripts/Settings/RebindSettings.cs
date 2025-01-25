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
        buttonPressedOpacitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.ButtonPressedOpacityKey, 0.38f);
        buttonUnpressedOpacitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.ButtonUnpressedOpacityKey, 0.38f);

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
        ChangeButtonPressedOpacity();
        ChangeButtonUnpressedOpacity();
    }

    public void ChangeButtonPressedOpacity()
    {
        float opacity = buttonPressedOpacitySlider.value;
        PlayerPrefs.SetFloat(SettingsKeys.ButtonPressedOpacityKey, opacity);
        UpdateOpacityText(buttonPressedOpacityText, opacity);
    }

    public void ChangeButtonUnpressedOpacity()
    {
        float opacity = buttonUnpressedOpacitySlider.value;
        PlayerPrefs.SetFloat(SettingsKeys.ButtonUnpressedOpacityKey, opacity);
        UpdateOpacityText(buttonUnpressedOpacityText, opacity);
    }

    private void UpdateOpacityText(TMP_Text text, float value)
    {
        text.text = Mathf.RoundToInt(value * 100) + "%";
    }
}
