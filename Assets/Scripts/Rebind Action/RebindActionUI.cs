using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RebindActionUI : MonoBehaviour
{
    [SerializeField] private InputAction inputAction;
    private InputActionRebindingExtensions.RebindingOperation rebindOperation;

    void StartInteractiveRebind()
    {
        rebindOperation = inputAction.PerformInteractiveRebinding().OnComplete(operation => RebindCompleted());
        rebindOperation.Start();
    }

    void RebindCompleted()
    {
        rebindOperation.Dispose();

        // Apply UI Changes (IE: New Binding Icon)
    }
}
