using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections.Generic;

public class InputActionUIHandler : MonoBehaviour
{
    [System.Serializable]
    public class ActionUIBinding
    {
        public InputActionReference inputActionReference; // Reference to the Input Action
        public UIElement uiElement; // The UI element to update
    }

    [System.Serializable]
    public class UIElement
    {
        public Image image; // For sprite updates
        public Sprite idleSprite;
        public Sprite activeSprite;
        public UnityEvent<Vector2> onVector2Input; // For Vector2 input handling (e.g., Move, Joystick)
        public UnityEvent<bool> onBooleanInput; // For Boolean input handling (e.g., Button Press)
    }

    public List<ActionUIBinding> actionBindings = new List<ActionUIBinding>();

    private void OnEnable()
    {
        foreach (var binding in actionBindings)
        {
            if (binding.inputActionReference != null)
            {
                var action = binding.inputActionReference.action;
                if (action != null)
                {
                    if (action.type == InputActionType.Value)
                    {
                        // Subscribe to Value (e.g., Vector2 or Float) actions
                        action.performed += ctx => HandleValueInput(binding, ctx);
                        action.canceled += ctx => HandleValueInput(binding, ctx);
                    }
                    else if (action.type == InputActionType.Button)
                    {
                        // Subscribe to Button actions
                        action.performed += ctx => HandleButtonInput(binding, true);
                        action.canceled += ctx => HandleButtonInput(binding, false);
                    }

                    action.Enable();
                }
            }
        }
    }

    private void OnDisable()
    {
        foreach (var binding in actionBindings)
        {
            if (binding.inputActionReference != null)
            {
                var action = binding.inputActionReference.action;
                if (action != null)
                {
                    if (action.type == InputActionType.Value)
                    {
                        action.performed -= ctx => HandleValueInput(binding, ctx);
                        action.canceled -= ctx => HandleValueInput(binding, ctx);
                    }
                    else if (action.type == InputActionType.Button)
                    {
                        action.performed -= ctx => HandleButtonInput(binding, true);
                        action.canceled -= ctx => HandleButtonInput(binding, false);
                    }
                }
            }
        }
    }

    private void HandleValueInput(ActionUIBinding binding, InputAction.CallbackContext context)
    {
        if (binding.uiElement == null) return;

        var value = context.ReadValue<Vector2>();
        binding.uiElement.onVector2Input?.Invoke(value);

        // Update sprites based on value
        if (binding.uiElement.image != null)
        {
            binding.uiElement.image.sprite = value != Vector2.zero ? binding.uiElement.activeSprite : binding.uiElement.idleSprite;
        }
    }

    private void HandleButtonInput(ActionUIBinding binding, bool isPressed)
    {
        if (binding.uiElement == null) return;

        binding.uiElement.onBooleanInput?.Invoke(isPressed);

        // Update sprites based on button state
        if (binding.uiElement.image != null)
        {
            binding.uiElement.image.sprite = isPressed ? binding.uiElement.activeSprite : binding.uiElement.idleSprite;
        }
    }
}
