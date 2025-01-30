using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
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
    [SerializeField] UnityEvent onMenuEnabled;
    

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
        
        // Get SwipeController and prevent auto-selection
        SwipeController swipeController = FindObjectOfType<SwipeController>();
        swipeController.PreventAutoSelection(true);

        // Trigger Unity Event
        onMenuEnabled?.Invoke();

        // Set the selected object to the first button
        StartCoroutine(DelayedSelectFirstButton(swipeController));
    }

    private IEnumerator DelayedSelectFirstButton(SwipeController swipeController)
    {
        yield return null; // Wait one frame to let GoToPage() process

        // Select the correct first button
        EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);

        // Re-enable auto-selection AFTER setting our own selection
        swipeController.PreventAutoSelection(false);
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
