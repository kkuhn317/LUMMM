using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CustomSlider : Slider
{
    public float stepSmall = 0.01f;
    public float stepBig = 0.02f;
    public float holdDelay = 0.5f;
    public float holdRepeatRate = 0.05f;
    public bool snapModifiersToMultiples = false;

    private bool isHolding = false;
    private float holdTimer = 0f;
    private float repeatTimer = 0f;
    private int holdDirection = 0;
    private bool awaitingRelease = false;
    private Mario inputActions;
    private bool inputActionsInitialized = false;
    
    // Debug variables
    [Header("Debug")]
    public bool showDebug = false;
    public string debugModifierState = "";
    public float debugStep = 0f;

    protected override void Awake()
    {
        base.Awake();
        
        // Check if InputActions already exist
        if (inputActions == null)
        {
            inputActions = new Mario();
            inputActions.Enable();
            inputActionsInitialized = true;
            
            if (showDebug)
                Debug.Log($"CustomSlider {gameObject.name}: InputActions created and enabled");
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        
        // Ensure input actions are enabled
        if (inputActions != null && !inputActionsInitialized)
        {
            inputActions.Enable();
            inputActionsInitialized = true;
            if (showDebug)
                Debug.Log($"CustomSlider {gameObject.name}: InputActions enabled in OnEnable");
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        
        // Disable input actions when slider is disabled
        if (inputActions != null && inputActionsInitialized)
        {
            inputActions.Disable();
            inputActionsInitialized = false;
            if (showDebug)
                Debug.Log($"CustomSlider {gameObject.name}: InputActions disabled in OnDisable");
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return;
#endif
        if (inputActions != null)
        {
            inputActions.Dispose();
            inputActions = null;
            inputActionsInitialized = false;
            if (showDebug)
                Debug.Log($"CustomSlider {gameObject.name}: InputActions disposed");
        }
    }

    public override void OnMove(AxisEventData eventData)
    {
        if (!IsActive() || !IsInteractable())
        {
            base.OnMove(eventData);
            return;
        }

        if (awaitingRelease) return;

        bool hasModifier = IsAnyModifierPressed();
        
        if (showDebug)
        {
            debugModifierState = GetModifierDebugString();
            Debug.Log($"OnMove: direction={eventData.moveDir}, hasModifier={hasModifier}, modState={debugModifierState}");
        }

        switch (eventData.moveDir)
        {
            case MoveDirection.Left:
                if (IsHorizontal() && FindSelectableOnLeft() == null)
                {
                    StartHold(-1, hasModifier);
                    awaitingRelease = true;
                }
                else
                {
                    base.OnMove(eventData);
                }
                break;

            case MoveDirection.Right:
                if (IsHorizontal() && FindSelectableOnRight() == null)
                {
                    StartHold(1, hasModifier);
                    awaitingRelease = true;
                }
                else
                {
                    base.OnMove(eventData);
                }
                break;

            case MoveDirection.Up:
                if (!IsHorizontal() && FindSelectableOnUp() == null)
                {
                    StartHold(-1, hasModifier);
                    awaitingRelease = true;
                }
                else
                {
                    base.OnMove(eventData);
                }
                break;

            case MoveDirection.Down:
                if (!IsHorizontal() && FindSelectableOnDown() == null)
                {
                    StartHold(1, hasModifier);
                    awaitingRelease = true;
                }
                else
                {
                    base.OnMove(eventData);
                }
                break;
        }
    }

    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);
        awaitingRelease = false;
        if (showDebug)
            Debug.Log($"CustomSlider {gameObject.name}: Selected");
    }

    public override void OnDeselect(BaseEventData eventData)
    {
        base.OnDeselect(eventData);
        awaitingRelease = false;
        if (showDebug)
            Debug.Log($"CustomSlider {gameObject.name}: Deselected");
    }

    private void StartHold(int direction, bool useModifier)
    {
        isHolding = true;
        holdDirection = direction;
        holdTimer = 0f;
        repeatTimer = 0f;

        float step = useModifier ? GetModifierStep(direction) : stepSmall * direction;
        debugStep = step;
        
        if (showDebug)
            Debug.Log($"StartHold: direction={direction}, useModifier={useModifier}, step={step}, value before={value}");
        
        MoveSlider(step);
    }

    private new void Update()
    {
        if (inputActions == null) 
        {
            // Try to reinitialize if null
            Awake();
            if (inputActions == null) return;
        }

        // Update debug info
        if (showDebug)
        {
            debugModifierState = GetModifierDebugString();
        }

        if (isHolding)
        {
            holdTimer += Time.unscaledDeltaTime;

            if (holdTimer >= holdDelay)
            {
                repeatTimer += Time.unscaledDeltaTime;

                if (repeatTimer >= holdRepeatRate)
                {
                    bool useModifier = IsAnyModifierPressed();
                    float step = useModifier
                        ? GetModifierStep(holdDirection)
                        : stepBig * holdDirection;
                    
                    debugStep = step;
                    
                    if (showDebug)
                        Debug.Log($"Hold repeat: useModifier={useModifier}, step={step}, value={value}");
                    
                    MoveSlider(step);
                    repeatTimer = 0f;
                }
            }

            if (!IsPressingDirection())
            {
                isHolding = false;
                holdDirection = 0;
                holdTimer = 0f;
                repeatTimer = 0f;
                if (showDebug)
                    Debug.Log("Hold released");
            }
        }

        // Reset awaitingRelease when no input is pressed
        if (inputActions != null && inputActions.UI.Navigate.ReadValue<Vector2>() == Vector2.zero)
        {
            awaitingRelease = false;
        }
    }

    private bool IsPressingDirection()
    {
        if (EventSystem.current?.currentSelectedGameObject != gameObject)
            return false;

        if (inputActions == null)
            return false;

        Vector2 moveInput = inputActions.UI.Navigate.ReadValue<Vector2>();

        if (holdDirection == -1)
            return IsHorizontal() ? moveInput.x < -0.5f : moveInput.y > 0.5f;
        else if (holdDirection == 1)
            return IsHorizontal() ? moveInput.x > 0.5f : moveInput.y < -0.5f;

        return false;
    }

    private bool IsHorizontal()
    {
        return direction == Direction.LeftToRight || direction == Direction.RightToLeft;
    }

    private void MoveSlider(float step)
    {
        value = Mathf.Clamp(value + step, minValue, maxValue);
    }

    private bool IsAnyModifierPressed()
    {
        if (inputActions == null || !inputActionsInitialized) 
        {
            if (showDebug)
                Debug.LogWarning("InputActions is null or not initialized in IsAnyModifierPressed");
            return false;
        }
        
        bool modifier10 = inputActions.UI.StepModifier10.ReadValue<float>() > 0.5f;
        bool modifier25 = inputActions.UI.StepModifier25.ReadValue<float>() > 0.5f;
        bool modifier50 = inputActions.UI.StepModifier50.ReadValue<float>() > 0.5f;
        
        if (showDebug && (modifier10 || modifier25 || modifier50))
        {
            Debug.Log($"Modifiers pressed: 10%={modifier10}, 25%={modifier25}, 50%={modifier50}");
        }
        
        return modifier10 || modifier25 || modifier50;
    }

    private float GetModifierStep(int direction)
    {
        if (inputActions == null || !inputActionsInitialized)
        {
            if (showDebug)
                Debug.LogWarning("InputActions not available in GetModifierStep");
            return stepSmall * direction;
        }

        float range = maxValue - minValue;
        float step = stepSmall * direction; // default
        
        if (inputActions.UI.StepModifier50.ReadValue<float>() > 0.5f)
        {
            step = snapModifiersToMultiples ? SnapToMultiple(range * 0.5f, direction) : range * 0.5f * direction;
            if (showDebug) Debug.Log($"50% modifier: step={step}");
        }
        else if (inputActions.UI.StepModifier25.ReadValue<float>() > 0.5f)
        {
            step = snapModifiersToMultiples ? SnapToMultiple(range * 0.25f, direction) : range * 0.25f * direction;
            if (showDebug) Debug.Log($"25% modifier: step={step}");
        }
        else if (inputActions.UI.StepModifier10.ReadValue<float>() > 0.5f)
        {
            step = snapModifiersToMultiples ? SnapToMultiple(range * 0.1f, direction) : range * 0.1f * direction;
            if (showDebug) Debug.Log($"10% modifier: step={step}");
        }
        
        return step;
    }

    private string GetModifierDebugString()
    {
        if (inputActions == null || !inputActionsInitialized)
            return "InputActions not available";
            
        float mod10 = inputActions.UI.StepModifier10.ReadValue<float>();
        float mod25 = inputActions.UI.StepModifier25.ReadValue<float>();
        float mod50 = inputActions.UI.StepModifier50.ReadValue<float>();
        
        return $"10%: {mod10:F2}, 25%: {mod25:F2}, 50%: {mod50:F2}";
    }

    private float SnapToMultiple(float stepSize, int direction)
    {
        float current = value;
        float remainder = (current - minValue) % stepSize;
        if (remainder < 0) remainder += stepSize;

        bool isAligned = remainder < 0.001f || Mathf.Abs(remainder - stepSize) < 0.001f;

        float target;
        if (isAligned)
        {
            target = current + stepSize * direction;
        }
        else
        {
            float baseValue = current - remainder;
            target = direction > 0 ? baseValue + stepSize : baseValue;
        }

        target = Mathf.Clamp(target, minValue, maxValue);
        return target - current;
    }
}