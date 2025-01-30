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

        // Load saved position and scale
        MobileRebindingData mobileData = GlobalVariables.currentLayout.mobileData;
        if (mobileData.buttonData.ContainsKey(buttonID))
        {
            MobileRebindingData.MobileButtonData myData = mobileData.buttonData[buttonID];
            RectTransform rectTransform = GetComponent<RectTransform>();
            // Position
            rectTransform.anchoredPosition = myData.position;

            // Scale
            float scale = myData.scale;
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        // Opacity
        buttonPressedOpacity = mobileData.buttonPressedOpacity;
        buttonUnpressedOpacity = mobileData.buttonUnpressedOpacity;

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
