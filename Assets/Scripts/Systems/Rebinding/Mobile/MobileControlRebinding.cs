using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

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
    public CanvasGroup rebindCanvasGroup;

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
        // NOTE: do NOT set rebindCanvasGroup.interactable here. OptionsGameManager
        // owns this flag (OnPause = true/usable, OnResume = false), and it is
        // already true when this menu opens. Setting it false here deactivated the
        // whole rebind menu the moment the mobile submenu opened.
    }

    void OnEnable() {
        // OptionsGameManager.OnPause deactivates these buttons via SetActive(false)
        // (they double as the on-screen gameplay controls, hidden while paused).
        // This editor is where you position/scale them, so they must be active here.
        // Re-activate every draggable button — including ones left inactive by
        // OnPause — whenever the editor opens. GetComponentsInChildren(true) also
        // finds inactive children. Activating them lets their Start run and register
        // via AddButton, which the save in OnDisable depends on.
        foreach (var b in GetComponentsInChildren<DraggableMobileButton>(true)) {
            if (b != null && !b.gameObject.activeSelf)
                b.gameObject.SetActive(true);
        }
    }

    void OnDisable() {
        // Save all the button positions and scales (while they are still active,
        // so their transforms read correctly).
        Debug.Log("Saving mobile button layout");
        Dictionary<string, MobileRebindingData.MobileButtonData> newData = new();
        foreach (DraggableMobileButton button in buttons) {
            if (button == null) continue; // a button may already be destroyed during scene unload
            newData.Add(button.buttonID, button.GetData());
        }
        RebindSaveLoad.SaveMobileBindings(newData);

        // Restore the hidden state OnPause expects, so the buttons don't linger
        // over the options menu after the editor closes. OnResume re-shows them for
        // gameplay. Guarded because this also runs during scene unload/quit.
        foreach (var b in GetComponentsInChildren<DraggableMobileButton>(true)) {
            if (b != null && b.gameObject.activeSelf)
                b.gameObject.SetActive(false);
        }

        // rebindCanvasGroup lives on a different GameObject and may already be
        // destroyed when OnDisable runs during scene unload or application quit —
        // object destruction order isn't guaranteed. Unity's overloaded null
        // check returns true for a destroyed object, so this skips the access
        // during teardown while still restoring interactability on a normal close.
        if (rebindCanvasGroup != null)
            rebindCanvasGroup.interactable = true;
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

    public void SetButtonAsSelected(GameObject buttonToSelect)
    {
        EventSystem.current.SetSelectedGameObject(buttonToSelect);
    }
}