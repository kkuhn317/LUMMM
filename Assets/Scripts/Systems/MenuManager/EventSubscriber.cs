using UnityEngine;

/// <summary>
/// Example subscriber — attach to any GameObject that needs to react to menu events.
/// Most commonly used to drive audio, haptics, or analytics.
/// Duplicate and customize per use case rather than cramming everything here.
/// </summary>
public class EventSubscriber : MonoBehaviour
{
    private void OnEnable()
    {
        GlobalEventHandler.OnMenuOpened    += HandleMenuOpened;
        GlobalEventHandler.OnMenuClosed    += HandleMenuClosed;
        GlobalEventHandler.OnGUIPop        += HandleGUIPop;
        GlobalEventHandler.OnSubMenuOpened += HandleSubMenuOpened;
        GlobalEventHandler.OnExitRequested += HandleExitRequested;
        GlobalEventHandler.OnCancelConsumed += HandleCancelConsumed;
        GlobalEventHandler.OnCancelRejected += HandleCancelRejected;
        GlobalEventHandler.OnMenuOwnerChanged += HandleMenuOwnerChanged;
    }

    private void OnDisable()
    {
        GlobalEventHandler.OnMenuOpened -= HandleMenuOpened;
        GlobalEventHandler.OnMenuClosed -= HandleMenuClosed;
        GlobalEventHandler.OnGUIPop -= HandleGUIPop;
        GlobalEventHandler.OnSubMenuOpened -= HandleSubMenuOpened;
        GlobalEventHandler.OnExitRequested -= HandleExitRequested;
        GlobalEventHandler.OnCancelConsumed -= HandleCancelConsumed;
        GlobalEventHandler.OnCancelRejected -= HandleCancelRejected;
        GlobalEventHandler.OnMenuOwnerChanged -= HandleMenuOwnerChanged;
    }

    private void HandleMenuOpened(string menuName)
    {
        // e.g. AudioManager.Play("menu_open");
    }

    private void HandleMenuClosed()
    {
        // e.g. AudioManager.Play("menu_close");
    }

    private void HandleGUIPop()
    {
        // e.g. AudioManager.Play("menu_back"); HapticManager.Light();
    }

    private void HandleSubMenuOpened(string subMenuName)
    {
        // e.g. AudioManager.Play("menu_navigate");
    }

    private void HandleExitRequested()
    {
        // e.g. SaveManager.SaveBeforeQuit();
    }

    /// <summary>
    /// Cancel was handled — play a back/dismiss sound or trigger a haptic pulse.
    /// </summary>
    private void HandleCancelConsumed()
    {
        // e.g. AudioManager.Play("menu_back"); HapticManager.Light();
    }

    /// <summary>
    /// Cancel was ignored (nothing to dismiss, already at root) — play a rejection
    /// sound or do nothing. Do NOT trigger the back sound here.
    /// </summary>
    private void HandleCancelRejected()
    {
        // e.g. AudioManager.Play("menu_invalid");
    }

    /// <summary>
    /// Menu ownership changed. Use this to drive input indicator swaps,
    /// "Player 1 is in the menu" overlays, or controller icon changes.
    /// newOwner is null when menus are fully closed.
    /// </summary>
    private void HandleMenuOwnerChanged(UnityEngine.InputSystem.PlayerInput newOwner)
    {
        // e.g. HUDController.ShowMenuOwnerIndicator(newOwner);
    }
}