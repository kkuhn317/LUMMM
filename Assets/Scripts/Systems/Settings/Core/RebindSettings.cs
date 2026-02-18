using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RebindSettings : MonoBehaviour, IPauseToggleGate
{
    [SerializeField] Slider buttonPressedOpacitySlider;
    [SerializeField] Slider buttonUnpressedOpacitySlider;
    [SerializeField] TMP_Text buttonPressedOpacityText;
    [SerializeField] TMP_Text buttonUnpressedOpacityText;
    [SerializeField] CanvasGroup optionsCanvasGroup;
    [SerializeField] Button firstSelectedButton;
    [SerializeField] Button ControlsButton; // Will be selected when the rebind menu is closed
    [SerializeField] UnityEvent onMenuEnabled;
    [SerializeField] bool updateOpacityImmediately = false; // Used in Options scene so the controls for the test level update immediately

    public bool CanTogglePause { get; private set; } = true;
    bool IPauseToggleGate.CanTogglePause => CanTogglePause;
    private MobileControls mobileControls;

    public void EnableTogglePause()
    {
        CanTogglePause = true;
    }

    public void DisableTogglePause()
    {
        CanTogglePause = false;
    }

    void OnEnable()
    {
        DisableTogglePause(); 

        // Disable interactability of the options canvas group
        optionsCanvasGroup.interactable = false;
        optionsCanvasGroup.blocksRaycasts = true;
        
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
        EnableTogglePause(); 

        // Enable interactability of the options canvas group
        optionsCanvasGroup.interactable = true;
        optionsCanvasGroup.blocksRaycasts = true;

        // Set the selected object to the controls button
        ControlsButton.Select();
    }
    
    // Start is called before the first frame update
    void Start()
    {
        mobileControls = FindObjectOfType<MobileControls>(true);

        // Update the text labels
        buttonPressedOpacitySlider.onValueChanged.AddListener((value) => UpdateButtonPressedOpacity(value));
        buttonUnpressedOpacitySlider.onValueChanged.AddListener((value) => UpdateButtonUnpressedOpacity(value));

        // Manually trigger the events to update the text labels
        UpdateButtonPressedOpacity(buttonPressedOpacitySlider.value);
        UpdateButtonUnpressedOpacity(buttonUnpressedOpacitySlider.value);
    }

    private void UpdateButtonPressedOpacity(float value)
    {
        buttonPressedOpacityText.text = Mathf.RoundToInt(value * 100) + "%";
        if (updateOpacityImmediately)
        {
            // GameManager.Instance.UpdateMobileOpacity(value, buttonUnpressedOpacitySlider.value);
            if (mobileControls == null) mobileControls = FindObjectOfType<MobileControls>(true);
            mobileControls?.UpdateButtonOpacity(value, buttonUnpressedOpacitySlider.value);
        }
    }

    private void UpdateButtonUnpressedOpacity(float value)
    {
        buttonUnpressedOpacityText.text = Mathf.RoundToInt(value * 100) + "%";
        if (updateOpacityImmediately)
        {
            // GameManager.Instance.UpdateMobileOpacity(buttonPressedOpacitySlider.value, value);
            if (mobileControls == null) mobileControls = FindObjectOfType<MobileControls>(true);
            mobileControls?.UpdateButtonOpacity(buttonPressedOpacitySlider.value, value);
        }
    }
}