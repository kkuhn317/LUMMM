using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MarioMoveUIBinding
{
    public MarioMoves moveFlag;
    public GameObject uiObject;
}

public class MarioMovesDisplay : MonoBehaviour
{
    [Header("Ability UI Bindings")]
    public List<MarioMoveUIBinding> moveBindings = new List<MarioMoveUIBinding>();

    private void OnEnable()
    {
        RefreshFromGlobalLevelInfo();
    }

    /// <summary>
    /// Call this if GlobalVariables.levelInfo changes and you want to refresh the UI.
    /// </summary>
    public void RefreshFromGlobalLevelInfo()
    {
        if (GlobalVariables.levelInfo == null)
        {
            // Don't touch the objects if we don't have data yet
            Debug.LogWarning($"{nameof(MarioMovesDisplay)}: GlobalLevelInfo.levelInfo is null, cannot update ability UI.");
            return;
        }

        ApplyMoves(GlobalVariables.levelInfo.marioMoves);
    }

    private void ApplyMoves(MarioMoves moves)
    {
        if (moveBindings == null) return;

        foreach (var binding in moveBindings)
        {
            if (binding == null || binding.uiObject == null)
                continue;

            bool enabled = moves.HasFlag(binding.moveFlag);
            binding.uiObject.SetActive(enabled);
        }
    }
}