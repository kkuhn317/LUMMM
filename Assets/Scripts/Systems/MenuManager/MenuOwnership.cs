using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Single source of truth for which PlayerInput currently owns the menu system.
/// 
/// USAGE:
///   - When a player opens a menu: MenuOwnership.Claim(playerInput)
///   - When menus close:           MenuOwnership.Release()
///   - To check before acting:     MenuOwnership.IsOwner(playerInput)
///   - To guard a method:          if (!MenuOwnership.IsOwner(source)) return;
/// 
/// Designed for "one player freezes everyone" multiplayer. Expanding to per-player
/// menus later would replace this class without touching the callers.
/// </summary>
public static class MenuOwnership
{
    private static PlayerInput currentOwner;

    /// <summary>The PlayerInput that currently owns the menu, or null if no menu is open.</summary>
    public static PlayerInput Owner => currentOwner;

    /// <summary>True when any player has the menu open.</summary>
    public static bool HasOwner => currentOwner != null;

    /// <summary>
    /// Claim menu ownership. Call when a player opens the first menu.
    /// If another player already owns it, the claim is ignored.
    /// </summary>
    public static bool Claim(PlayerInput claimant)
    {
        if (claimant == null) return false;

        // Already owned by someone else — reject
        if (currentOwner != null && currentOwner != claimant)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"[MenuOwnership] Player {claimant.playerIndex} tried to claim ownership " +
                             $"but Player {currentOwner.playerIndex} already owns the menu.");
#endif
            return false;
        }

        currentOwner = claimant;
        GlobalEventHandler.TriggerMenuOwnerChanged(currentOwner);
        return true;
    }

    /// <summary>
    /// Release ownership. Call when all menus are closed.
    /// </summary>
    public static void Release()
    {
        if (currentOwner == null) return;
        currentOwner = null;
        GlobalEventHandler.TriggerMenuOwnerChanged(null);
    }

    /// <summary>
    /// Returns true if the given PlayerInput is the current menu owner,
    /// OR if there is no owner (menus are open to anyone).
    /// </summary>
    public static bool IsOwner(PlayerInput candidate)
    {
        if (candidate == null) return false;
        if (currentOwner == null) return true;
        return currentOwner == candidate;
    }

    /// <summary>
    /// Force-transfer ownership to a different player.
    /// Use sparingly — prefer Claim/Release for normal flow.
    /// </summary>
    public static void ForceTransfer(PlayerInput newOwner)
    {
        currentOwner = newOwner;
        GlobalEventHandler.TriggerMenuOwnerChanged(currentOwner);
    }
}