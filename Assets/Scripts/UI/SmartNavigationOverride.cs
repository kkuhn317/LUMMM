using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SmartNavigationOverride : MonoBehaviour
{
    public enum Direction { Left, Right, Up, Down }

    [System.Serializable]
    public class SmartOverrideRule
    {
        public GameObject originButton;
        public Direction triggerDirection;
        public bool ignorePrevious;
        public GameObject requiredPrevious;
        public GameObject fallbackTarget;
    }

    [Header("Smart Navigation Rules")]
    [SerializeField]
    public List<SmartOverrideRule> smartOverrides = new List<SmartOverrideRule>();

    [Header("Input Settings")]
    public InputActionReference navigateAction;

    protected Vector2 input;
    protected GameObject lastSelected;
    protected GameObject previousSelected;

    // Marking Awake as virtual to allow overriding
    protected virtual void Awake()
    {
        if (smartOverrides == null)
            smartOverrides = new List<SmartOverrideRule>();

        if (smartOverrides.Count == 0)
        {
            smartOverrides.Add(new SmartOverrideRule());
        }
    }

    // Marking Update as virtual to allow overriding
    protected virtual void Update()
    {
        input = navigateAction?.action.ReadValue<Vector2>() ?? Vector2.zero;
        var current = EventSystem.current.currentSelectedGameObject;

        if (current != null && current != lastSelected && IsTracked(current))
        {
            previousSelected = lastSelected;
            lastSelected = current;
        }

        // Continue checking all rules without breaking the loop
        foreach (var rule in smartOverrides)
        {
            if (rule.originButton == null || rule.fallbackTarget == null)
                continue;

            bool isAtOrigin = current == rule.originButton;
            bool isValidPrevious = rule.ignorePrevious || previousSelected == rule.requiredPrevious;

            if (isAtOrigin && isValidPrevious)
            {
                // If the condition is met, perform the fallback navigation
                if (IsDirectionPressed(rule.triggerDirection))
                {
                    if (rule.fallbackTarget.TryGetComponent(out Selectable selectable) && selectable.interactable)
                    {
                        EventSystem.current.SetSelectedGameObject(rule.fallbackTarget);
                    }
                }
            }
        }
    }

    protected bool IsTracked(GameObject obj)
    {
        foreach (var rule in smartOverrides)
        {
            if (rule.originButton == obj || rule.requiredPrevious == obj || rule.fallbackTarget == obj)
                return true;
        }
        return false;
    }

    protected bool IsDirectionPressed(Direction dir)
    {
        return dir switch
        {
            Direction.Left => input.x < -0.5f,
            Direction.Right => input.x > 0.5f,
            Direction.Up => input.y > 0.5f,
            Direction.Down => input.y < -0.5f,
            _ => false
        };
    }

    private void OnEnable() => navigateAction?.action.Enable();
    private void OnDisable() => navigateAction?.action.Disable();
}