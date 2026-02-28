using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuButton : MonoBehaviour
{
    public enum ButtonType
    {
        OpenMenu,
        OpenSubMenu,
        OpenMenuAsOverlay,
        Back,
        BackKeepOverlay,
        CloseOverlay,
        Exit
    }

    [Header("Button Configuration")]
    public ButtonType buttonType;
    public string targetMenuName;

    private Button button;
    private Selectable selectable;

    /// <summary>
    /// The PlayerInput that owns this button's actions.
    /// Set automatically by PlayerInput.GetComponent or assigned by your player setup code.
    /// Used to validate ownership before acting on a click.
    /// </summary>
    private PlayerInput playerInput;

    /// <summary>
    /// Called by PauseMenuController when pausing to inject the owning player.
    /// MenuButtons cannot find their owner via hierarchy because the Canvas
    /// lives under GameManager, not under the player prefab.
    /// Call with null on resume to clear ownership.
    /// </summary>
    public void SetPlayerInput(PlayerInput input)
    {
        playerInput = input;
    }

    private void Start()
    {
        button = GetComponent<Button>();
        selectable = GetComponent<Selectable>();
        // Note: playerInput is NOT resolved here — it must be injected via SetPlayerInput()

        if (button != null)
            button.onClick.AddListener(OnClick);
        else
            Debug.LogError($"MenuButton on {gameObject.name} has no Button component!");
    }

    private void OnClick()
    {
        if (GUIManager.Instance == null)
        {
            Debug.LogError("GUIManager not found! Make sure there's a GUIManager in the scene.");
            return;
        }

        // If menus have an owner and this button's player isn't it, ignore the click.
        // playerInput may be null in single-player or main menu scenes — that's fine,
        // ownership is only enforced when MenuOwnership.HasOwner is true.
        if (MenuOwnership.HasOwner && playerInput != null && !MenuOwnership.IsOwner(playerInput))
            return;

        switch (buttonType)
        {
            case ButtonType.OpenMenu:
                if (!string.IsNullOrEmpty(targetMenuName))
                    // Pass this button as the return target so Back() refocuses it
                    GUIManager.Instance.OpenMenu(targetMenuName, returnTo: selectable);
                else
                    Debug.LogError($"OpenMenu button '{gameObject.name}' has no target menu name!");
                break;

            case ButtonType.OpenSubMenu:
                if (!string.IsNullOrEmpty(targetMenuName))
                    GUIManager.Instance.OpenSubMenu(targetMenuName, returnTo: selectable);
                else
                    Debug.LogError($"OpenSubMenu button '{gameObject.name}' has no target menu name!");
                break;

            case ButtonType.OpenMenuAsOverlay:
                if (!string.IsNullOrEmpty(targetMenuName))
                    GUIManager.Instance.OpenMenuAsOverlay(targetMenuName, returnTo: selectable);
                else
                    Debug.LogError($"OpenMenuAsOverlay button '{gameObject.name}' has no target menu name!");
                break;

            case ButtonType.Back:
                GUIManager.Instance.Back();
                break;

            case ButtonType.BackKeepOverlay:
                GUIManager.Instance.BackKeepOverlay();
                break;

            case ButtonType.CloseOverlay:
                if (!string.IsNullOrEmpty(targetMenuName))
                    GUIManager.Instance.CloseOverlay(targetMenuName);
                else
                    Debug.LogError($"CloseOverlay button '{gameObject.name}' has no target menu name!");
                break;

            case ButtonType.Exit:
                GUIManager.Instance.Exit();
                break;

            default:
                Debug.LogError($"Unknown button type '{buttonType}' on '{gameObject.name}'");
                break;
        }
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }

    public void SetupButton(ButtonType type, string target = "")
    {
        buttonType     = type;
        targetMenuName = target;
    }
}