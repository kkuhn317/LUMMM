using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared conditional rule system for choosing which item to spawn based on the player's current power-up state.
/// Rules are evaluated top-to-bottom; first match wins.
/// </summary>
[Serializable]
public class ConditionalItemRules
{
    [Serializable]
    public class Rule
    {
        public string name;

        [Tooltip("Which player state must match for this rule to apply.")]
        public PlayerCondition condition = PlayerCondition.IsSmall;

        [Tooltip("The item (GameObject prefab) to spawn if this rule matches.")]
        public GameObject item;
    }

    public enum PlayerCondition
    {
        Any,
        IsSmall,
        IsBig,
        IsNotSmall
    }

    public enum NoMatchMode
    {
        /// <summary>Use fallbackItem (if assigned).</summary>
        UseFallbackItem,

        /// <summary>Return null (caller decides what to do).</summary>
        ReturnNull
    }

    [Tooltip("Enable conditional item resolution.")]
    public bool enabled = false;

    [Tooltip("Rules are evaluated top-to-bottom. First match wins.")]
    public List<Rule> rules = new();

    [Tooltip("What happens when no rule matches.")]
    public NoMatchMode noMatchMode = NoMatchMode.ReturnNull;

    [Tooltip("Used when noMatchMode is UseFallbackItem.")]
    public GameObject fallbackItem;

    /// <summary>True if there's at least one rule item or a fallback item configured.</summary>
    public bool HasAnyConfiguredItem()
    {
        if (rules != null)
        {
            foreach (var r in rules)
            {
                if (r != null && r.item != null)
                    return true;
            }
        }

        return fallbackItem != null;
    }

    /// <summary>
    /// Resolves an item prefab for the given player. Returns null if disabled, player is null,
    /// or no rules match (depending on noMatchMode/fallbackItem).
    /// </summary>
    public GameObject Resolve(MarioMovement player)
    {
        if (!enabled) return null;
        if (player == null) return null;

        if (rules != null)
        {
            foreach (var r in rules)
            {
                if (r == null || r.item == null) continue;
                if (Matches(r.condition, player))
                    return r.item;
            }
        }

        if (noMatchMode == NoMatchMode.UseFallbackItem)
            return fallbackItem;

        return null;
    }

    private bool Matches(PlayerCondition condition, MarioMovement player)
    {
        bool isSmall = PowerStates.IsSmall(player.powerupState);
        bool isBig = PowerStates.IsBig(player.powerupState);

        return condition switch
        {
            PlayerCondition.Any => true,
            PlayerCondition.IsSmall => isSmall,
            PlayerCondition.IsBig => isBig,
            PlayerCondition.IsNotSmall => !isSmall,
            _ => false
        };
    }
}