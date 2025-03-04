using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

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

    void Start()
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
        image.sprite = buttonPressed ? downSprite : upSprite;
        float alpha = buttonPressed ? buttonPressedOpacity : buttonUnpressedOpacity;
        image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
    }

    void TurnOn()
    {
        if (GameManager.isPaused && !canPressIfPaused) return;

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

    // public void OnPointerUp(PointerEventData eventData)
    // {
    //     // Make sure button releases when no touches/clicks remain
    //     if (!toggleButton)
    //     {
    //         TurnOff();
    //     }
    // }

    void Update()
    {
        bool wasTouchingLastFrame = isTouchingNow;
        isTouchingNow = false;

        // Check for touch input
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, touch.position, null))
                {
                    isTouchingNow = true;
                    break; // At least one valid touch is inside, no need to check further
                }
            }
        }

        // Check for mouse input
        if (Input.GetMouseButton(0)) // Left mouse button held down
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, null))
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
                    if (buttonPressed)
                    {
                        TurnOff();
                    }
                    else
                    {
                        TurnOn();
                    }
                }
                else
                {
                    TurnOn();
                }
            }
        }
        else
        {
            // If no input is detected, release the button (if not a toggle button)
            if (!toggleButton && wasTouchingLastFrame)
            {
                TurnOff();
            }
        }
    }
}
