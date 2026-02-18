using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class MobileControlButton : MonoBehaviour
{
    public string buttonID;
    public bool buttonPressed;
    public UnityEvent onPress;
    public UnityEvent onRelease;
    private bool isTouchingNow; // Whether the button is currently being touched or clicked
    public bool toggleButton;   // If true, button stays pressed until touched again
    public Sprite upSprite;     // Default sprite when unpressed
    public Sprite downSprite;   // Default sprite when pressed 
    private Image image;
    public bool isRunButton = false;    // If true, this button will be used to run (state saved in GlobalVariables)
    public bool canPressIfPaused = false;   // Should only be used for the pause button in the rebind menu
    private float buttonPressedOpacity;
    private float buttonUnpressedOpacity;
    private RectTransform rectTransform;

    private void OnEnable()
    {
        // Enable EnhancedTouch so Touch.activeTouches works
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        UpdatePosScaleOpacity();

        if (isRunButton && GlobalVariables.OnScreenControls && GlobalVariables.mobileRunButtonPressed)
        {
            StartCoroutine(TurnOnAtBeginCoroutine());
        }
    }

    public void UpdatePosScaleOpacity()
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        // Load saved position and scale
        MobileRebindingData mobileData = GlobalVariables.currentLayout.mobileData;
        if (mobileData.buttonData.ContainsKey(buttonID))
        {
            MobileRebindingData.MobileButtonData myData = mobileData.buttonData[buttonID];
            rectTransform.anchoredPosition = myData.position;
            float scale = myData.scale;
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        // Opacity
        buttonPressedOpacity = mobileData.buttonPressedOpacity;
        buttonUnpressedOpacity = mobileData.buttonUnpressedOpacity;

        UpdateButtonAppearance();
    }

    public void UpdateButtonOpacity(float buttonPressedOpacity, float buttonUnpressedOpacity)
    {
        if (image == null) image = GetComponent<Image>();
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        this.buttonPressedOpacity = buttonPressedOpacity;
        this.buttonUnpressedOpacity = buttonUnpressedOpacity;
        UpdateButtonAppearance();
    }

    IEnumerator TurnOnAtBeginCoroutine()
    {
        yield return new WaitForEndOfFrame();
        TurnOn();
    }

    private void UpdateButtonAppearance()
    {
        image.sprite = buttonPressed ? downSprite : upSprite;
        float alpha = buttonPressed ? buttonPressedOpacity : buttonUnpressedOpacity;
        image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
    }

    void TurnOn()
    {
        if (GameManager.Instance.GetSystem<PauseMenuController>().IsPaused && !canPressIfPaused) return;

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

    void Update()
    {
        bool wasTouchingLastFrame = isTouchingNow;
        isTouchingNow = false;

        foreach (var t in Touch.activeTouches)
        {
            // Only treat Began/Moved/Stationary as "held"
            var p = t.phase;
            if (p == UnityEngine.InputSystem.TouchPhase.Began ||
                p == UnityEngine.InputSystem.TouchPhase.Moved ||
                p == UnityEngine.InputSystem.TouchPhase.Stationary)
            {
                Vector2 touchPosition = t.screenPosition;
                if (RectTransformUtility.RectangleContainsScreenPoint(
                        rectTransform, touchPosition))
                {
                    isTouchingNow = true;
                    break; // one finger inside is enough
                }
            }
        }

        // 2) Mouse (left button)
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed)
        {
            Vector2 mousePos = mouse.position.ReadValue();
            if (RectTransformUtility.RectangleContainsScreenPoint(
                    rectTransform, mousePos))
            {
                isTouchingNow = true;
            }
        }

        // If the button is being touched or clicked, activate it
        if (isTouchingNow)
        {
            if (!wasTouchingLastFrame)
            {
                if (toggleButton)
                {
                    if (buttonPressed) TurnOff();
                    else TurnOn();
                }
                else
                {
                    TurnOn();
                }
            }
        }
        else
        {
            // If no input is detected on this control, release (if not a toggle button)
            if (!toggleButton && wasTouchingLastFrame)
            {
                TurnOff();
            }
        }
    }
}
