using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// New Input System
using UnityEngine.InputSystem;

[RequireComponent(typeof(ScrollRect))]
public class UIDropdownScroller : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float scrollSpeed = 10f;
    private bool mouseOver = false;

    private List<Selectable> m_Selectables = new List<Selectable>();
    private ScrollRect m_ScrollRect;

    private Vector2 m_NextScrollPosition = Vector2.up;

    void OnEnable()
    {
        if (m_ScrollRect)
        {
            m_ScrollRect.content.GetComponentsInChildren(m_Selectables);
        }
    }
    void Awake()
    {
        m_ScrollRect = GetComponent<ScrollRect>();
    }
    void Start()
    {
        m_ScrollRect = GetComponent<ScrollRect>();
        if (m_ScrollRect)
        {
            m_ScrollRect.content.GetComponentsInChildren(m_Selectables);
        }
        ScrollToSelected(true);
    }
    void Update()
    {
        // Scroll via input.
        InputScroll();

        if (!mouseOver)
        {
            // Lerp scrolling code.
            m_ScrollRect.normalizedPosition = Vector2.Lerp(
                m_ScrollRect.normalizedPosition,
                m_NextScrollPosition,
                scrollSpeed * Time.unscaledDeltaTime); // unscaled so it works while paused
        }
        else
        {
            m_NextScrollPosition = m_ScrollRect.normalizedPosition;
        }
    }

    void InputScroll()
    {
        if (m_Selectables.Count == 0) return;

        if (IsNavPressed())
        {
            ScrollToSelected(false);
        }
    }

    // TODO: Replace this with a proper InputAction reference
    private bool IsNavPressed()
    {
        var kb = Keyboard.current;
        var gp = Gamepad.current;

        bool keyboardPressed =
            kb != null && (
                kb.upArrowKey.isPressed    || kb.downArrowKey.isPressed ||
                kb.leftArrowKey.isPressed  || kb.rightArrowKey.isPressed ||
                kb.wKey.isPressed          || kb.sKey.isPressed ||
                kb.aKey.isPressed          || kb.dKey.isPressed
            );

        bool gamepadDpad =
            gp != null && (
                gp.dpad.up.isPressed || gp.dpad.down.isPressed ||
                gp.dpad.left.isPressed || gp.dpad.right.isPressed
            );

        // Treat an actuated left stick as navigation too
        const float stickThreshold = 0.5f;
        bool gamepadStick =
            gp != null && gp.leftStick.ReadValue().magnitude > stickThreshold;

        return keyboardPressed || gamepadDpad || gamepadStick;
    }

    void ScrollToSelected(bool quickScroll)
    {
        int selectedIndex = -1;
        Selectable selectedElement = EventSystem.current.currentSelectedGameObject
            ? EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>()
            : null;

        if (selectedElement)
        {
            selectedIndex = m_Selectables.IndexOf(selectedElement);
        }
        if (selectedIndex > -1 && m_Selectables.Count > 1)
        {
            var target = new Vector2(0, 1 - (selectedIndex / ((float)m_Selectables.Count - 1)));

            if (quickScroll)
            {
                m_ScrollRect.normalizedPosition = target;
                m_NextScrollPosition = target;
            }
            else
            {
                m_NextScrollPosition = target;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => mouseOver = true;

    public void OnPointerExit(PointerEventData eventData)
    {
        mouseOver = false;
        ScrollToSelected(false);
    }
}
