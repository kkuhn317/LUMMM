using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class MobileControlButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public string buttonID;
    public bool buttonPressed;
    public UnityEvent onPress;
    public UnityEvent onRelease;
    public bool toggleButton; // If true, button stays pressed until pressed again
    public bool useActivateSprites; // If true, uses activateSprite & deactivateSprite instead of upSprite & downSprite

    public Sprite upSprite; // Default sprite when unpressed
    public Sprite downSprite; // Default sprite when pressed
    public Sprite activateSprite;  // Toggle ON sprite
    public Sprite deactivateSprite; // Toggle OFF sprite
    private Image image;
    public bool isRunButton = false; // If true, this button will be used to run

    private float buttonPressedOpacity;
    private float buttonUnpressedOpacity;

    void Start()
    {
        image = GetComponent<Image>();

        // Get Opacity from PlayerPrefs
        buttonPressedOpacity = PlayerPrefs.GetFloat(SettingsKeys.ButtonPressedOpacityKey, 0.38f);
        buttonUnpressedOpacity = PlayerPrefs.GetFloat(SettingsKeys.ButtonUnpressedOpacityKey, 0.38f);

        RectTransform rectTransform = GetComponent<RectTransform>();

        // Load the position and scale from PlayerPrefs
        if (PlayerPrefs.HasKey(buttonID + SettingsKeys.ButtonPosXKey))
        {
            float x = PlayerPrefs.GetFloat(buttonID + SettingsKeys.ButtonPosXKey);
            float y = PlayerPrefs.GetFloat(buttonID + SettingsKeys.ButtonPosYKey);
            rectTransform.anchoredPosition = new Vector2(x, y);
        }
        if (PlayerPrefs.HasKey(buttonID + SettingsKeys.ButtonScaleKey))
        {
            float scale = PlayerPrefs.GetFloat(buttonID + SettingsKeys.ButtonScaleKey);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        // Set initial state
        UpdateButtonAppearance();

        if (isRunButton && GlobalVariables.OnScreenControls && GlobalVariables.mobileRunButtonPressed)
        {
            StartCoroutine(TurnOnAtBeginCoroutine());
        }
    }

    IEnumerator TurnOnAtBeginCoroutine()
    {
        yield return new WaitForEndOfFrame();
        TurnOn();
    }

    private void UpdateButtonAppearance()
    {
        // Decide which set of sprites to use
        if (useActivateSprites)
        {
            image.sprite = buttonPressed ? activateSprite : deactivateSprite;
        }
        else
        {
            image.sprite = buttonPressed ? downSprite : upSprite;
        }

        float alpha = buttonPressed ? buttonPressedOpacity : buttonUnpressedOpacity;
        image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
    }

    void TurnOn()
    {
        if (GameManager.isPaused) return;

        buttonPressed = true;
        if (isRunButton) GlobalVariables.mobileRunButtonPressed = true;

        onPress.Invoke();
        UpdateButtonAppearance();
    }

    void TurnOff()
    {
        buttonPressed = false;
        if (isRunButton) GlobalVariables.mobileRunButtonPressed = false;

        onRelease.Invoke();
        UpdateButtonAppearance();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (toggleButton)
        {
            buttonPressed = !buttonPressed; // Toggle the state
            if (buttonPressed) onPress.Invoke();
            else onRelease.Invoke();
        }
        else
        {
            TurnOn();
        }
        UpdateButtonAppearance();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!toggleButton)
        {
            TurnOff();
        }
    }
}
