using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ControllerInput : MonoBehaviour
{
    [Header("Directional Pad")]
    public Image dpadImage;
    public Sprite idleSprite;
    public Sprite leftSprite;
    public Sprite rightSprite;
    public Sprite upSprite;
    public Sprite downSprite;

    [Header("Button Triggers")]
    public List<ButtonAction> buttonActions;

    [System.Serializable]
    public class ButtonAction
    {
        public Image buttonImage;
        public List<string> triggerButtons;
        public Sprite pressedSprite;
        public Sprite defaultSprite;
    }

    private void Update()
    {
        // Handle input for the D-pad
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        dpadImage.sprite = GetDpadSprite(horizontalInput, verticalInput);

        // Handle input for buttons
        foreach (var action in buttonActions)
        {
            bool anyButtonPressed = action.triggerButtons.Exists(triggerButton => Input.GetButton(triggerButton));
            action.buttonImage.sprite = anyButtonPressed ? action.pressedSprite : action.defaultSprite;
        }
    }

    private Sprite GetDpadSprite(float horizontalInput, float verticalInput)
    {
        if (horizontalInput > 0) return rightSprite;
        if (horizontalInput < 0) return leftSprite;
        if (verticalInput > 0) return upSprite;
        if (verticalInput < 0) return downSprite;
        return idleSprite;
    }
}
