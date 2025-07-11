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
    public bool snapModifiersToMultiples = false; // This toggles snapping behavior for modifier steps

    private bool isHolding = false;
    private float holdTimer = 0f;
    private float repeatTimer = 0f;
    private int holdDirection = 0;
    private bool awaitingRelease = false;
    private Mario inputActions; // Replace "PlayerInputActions" with your InputActions name

    protected override void Awake()
    {
        base.Awake();
        inputActions = new Mario();
        inputActions.Enable();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        inputActions?.Disable();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return;
#endif
        inputActions?.Dispose();
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
    }

    public override void OnDeselect(BaseEventData eventData)
    {
        base.OnDeselect(eventData);
        awaitingRelease = false;
    }

    private void StartHold(int direction, bool useModifier)
    {
        isHolding = true;
        holdDirection = direction;
        holdTimer = 0f;
        repeatTimer = 0f;

        float step = useModifier ? GetModifierStep(direction) : stepSmall * direction;
        MoveSlider(step);
    }

    private new void Update()
    {
        if (inputActions == null) return;

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
            }
        }

        if (inputActions != null && inputActions.UI.Navigate.ReadValue<Vector2>() == Vector2.zero)
        {
            awaitingRelease = false;
        }
    }

    private bool IsPressingDirection()
    {
        if (EventSystem.current?.currentSelectedGameObject != gameObject)
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
        if (inputActions == null) return false;
        
        return
            inputActions.UI.StepModifier10.ReadValue<float>() > 0.5f ||
            inputActions.UI.StepModifier25.ReadValue<float>() > 0.5f ||
            inputActions.UI.StepModifier50.ReadValue<float>() > 0.5f;
    }

    private float GetModifierStep(int direction)
    {
        float range = maxValue - minValue;

        if (inputActions.UI.StepModifier50.ReadValue<float>() > 0.5f)
            return snapModifiersToMultiples ? SnapToMultiple(range * 0.5f, direction) : range * 0.5f * direction;

        if (inputActions.UI.StepModifier25.ReadValue<float>() > 0.5f)
            return snapModifiersToMultiples ? SnapToMultiple(range * 0.25f, direction) : range * 0.25f * direction;

        if (inputActions.UI.StepModifier10.ReadValue<float>() > 0.5f)
            return snapModifiersToMultiples ? SnapToMultiple(range * 0.1f, direction) : range * 0.1f * direction;

        return stepSmall * direction;
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