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

    // Start is called before the first frame update
    void Start()
    {
        buttonPressedOpacitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.ButtonPressedOpacityKey, 0.38f);
        buttonUnpressedOpacitySlider.value = PlayerPrefs.GetFloat(SettingsKeys.ButtonUnpressedOpacityKey, 0.38f);

        // Update the text labels
        UpdateOpacityText(buttonPressedOpacityText, buttonPressedOpacitySlider.value);
        UpdateOpacityText(buttonUnpressedOpacityText, buttonUnpressedOpacitySlider.value);
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
