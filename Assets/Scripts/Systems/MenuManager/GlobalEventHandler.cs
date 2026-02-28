using UnityEngine;
using System;

public static class GlobalEventHandler
{
    public static event Action<string> OnMenuOpened;
    public static event Action OnMenuClosed;
    public static event Action OnGUIPop;
    public static event Action<string> OnSubMenuOpened;
    public static event Action OnExitRequested;

    /// <summary>Fired when a cancel input was successfully handled by any layer of the cancel chain.</summary>
    public static event Action OnCancelConsumed;

    /// <summary>Fired when a cancel input reached the end of the chain without being handled (e.g. already at root with nothing to dismiss).</summary>
    public static event Action OnCancelRejected;

    /// <summary>
    /// Fired when menu ownership changes. Payload is the new owner (null means menus closed).
    /// Use this to drive input map switches, UI overlays, or anything that needs to know who owns the menu.
    /// </summary>
    public static event Action<UnityEngine.InputSystem.PlayerInput> OnMenuOwnerChanged;

    public static void TriggerMenuOpened(string menuName)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Event: Menu Opened - {menuName}");
#endif
        OnMenuOpened?.Invoke(menuName);
    }

    public static void TriggerMenuClosed()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("Event: Menu Closed");
#endif
        OnMenuClosed?.Invoke();
    }

    public static void TriggerGUIPop()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("Event: GUI Pop");
#endif
        OnGUIPop?.Invoke();
    }

    public static void TriggerSubMenuOpened(string subMenuName)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Event: SubMenu Opened - {subMenuName}");
#endif
        OnSubMenuOpened?.Invoke(subMenuName);
    }

    public static void TriggerExitRequested()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("Event: Exit Requested");
#endif
        OnExitRequested?.Invoke();
    }

    public static void TriggerCancelConsumed()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("Event: Cancel Consumed");
#endif
        OnCancelConsumed?.Invoke();
    }

    public static void TriggerCancelRejected()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log("Event: Cancel Rejected (no handler)");
#endif
        OnCancelRejected?.Invoke();
    }

    public static void TriggerMenuOwnerChanged(UnityEngine.InputSystem.PlayerInput newOwner)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"Event: Menu Owner Changed -> {(newOwner != null ? $"Player {newOwner.playerIndex}" : "none")}");
#endif
        OnMenuOwnerChanged?.Invoke(newOwner);
    }

    public static bool HasCancelConsumedSubscribers() => OnCancelConsumed != null;
    public static bool HasCancelRejectedSubscribers() => OnCancelRejected != null;

    // DEBUG METHODS - Safe ways to check event subscribers
    
    public static bool HasMenuOpenedSubscribers()
    {
        return OnMenuOpened != null;
    }

    public static bool HasMenuClosedSubscribers()
    {
        return OnMenuClosed != null;
    }

    public static bool HasGUIPopSubscribers()
    {
        return OnGUIPop != null;
    }

    public static bool HasSubMenuOpenedSubscribers()
    {
        return OnSubMenuOpened != null;
    }

    public static bool HasExitRequestedSubscribers()
    {
        return OnExitRequested != null;
    }

    // For more detailed debugging (optional - uses reflection which is slower)
    public static void DebugPrintAllSubscribers()
    {
        Debug.Log("=== GLOBAL EVENT HANDLER SUBSCRIBERS ===");
        
        Debug.Log($"OnMenuOpened has subscribers: {HasMenuOpenedSubscribers()}");
        Debug.Log($"OnMenuClosed has subscribers: {HasMenuClosedSubscribers()}");
        Debug.Log($"OnGUIPop has subscribers: {HasGUIPopSubscribers()}");
        Debug.Log($"OnSubMenuOpened has subscribers: {HasSubMenuOpenedSubscribers()}");
        Debug.Log($"OnExitRequested has subscribers: {HasExitRequestedSubscribers()}");
        
        Debug.Log("========================================");
    }
}