using System;
using UnityEngine;

public abstract class SceneHasComponentConditionBase : CutsceneCondition
{
    [Header("Presence logic")]
    [Tooltip("If true, condition passes when at least one component exists. " +
             "If false, condition passes when there are none.")]
    [SerializeField] private bool shouldExist = true;

    // Each subclass returns the Component type it cares about
    protected abstract Type ComponentType { get; }

    public override bool IsMet(in CutsceneContext ctx)
    {
        var type = ComponentType;
        if (type == null || !typeof(Component).IsAssignableFrom(type))
        {
            Debug.LogWarning($"{GetType().Name}: ComponentType is null or not a UnityEngine.Component.");
            return false;
        }

        // Check if at least one instance exists in the scene(s)
        var exists = UnityEngine.Object.FindObjectOfType(type) != null;

        // If shouldExist = true → we want exists == true
        // If shouldExist = false → we want exists == false
        return shouldExist ? exists : !exists;
    }
}