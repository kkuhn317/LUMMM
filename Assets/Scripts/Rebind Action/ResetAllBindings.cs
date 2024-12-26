using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class ResetAllBindings : MonoBehaviour
{
    [SerializeField]
    private InputActionAsset actionAsset;

    /// <summary>
    /// Resets all bindings in the provided InputActionAsset to their defaults.
    /// </summary>
    public void ResetBindings()
    {
        if (actionAsset == null)
        {
            Debug.LogError("No InputActionAsset assigned to reset bindings.");
            return;
        }

        // Remove all overrides for all action maps
        foreach (var actionMap in actionAsset.actionMaps)
        {
            actionMap.RemoveAllBindingOverrides();
        }
    }
}
