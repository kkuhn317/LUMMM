using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class KeyPressButtonActivator_NewInput : MonoBehaviour
{
    [Header("UI target")]
    [SerializeField] private Button targetButton;

    [Header("Actions that should trigger the click")]
    [Tooltip("Add one or more Button-type InputActionReferences (e.g., Select, Submit, Confirm).")]
    [SerializeField] private InputActionReference[] inputActions;

    private void OnEnable()
    {
        if (inputActions == null) return;

        foreach (var ar in inputActions)
        {
            if (ar == null || ar.action == null) continue;

            // For Button actions set to Press behavior, 'started' fires once on press.
            ar.action.started += OnActionStarted;
            if (!ar.action.enabled) ar.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (inputActions == null) return;

        foreach (var ar in inputActions)
        {
            if (ar == null || ar.action == null) continue;
            ar.action.started -= OnActionStarted;
            // Don't Disable here if this action is shared elsewhere.
        }
    }

    private void OnActionStarted(InputAction.CallbackContext ctx)
    {
        if (ConfirmPopup.IsAnyPopupOpen) return;
        
        if (targetButton != null && targetButton.interactable)
            targetButton.onClick.Invoke();
    }
}
