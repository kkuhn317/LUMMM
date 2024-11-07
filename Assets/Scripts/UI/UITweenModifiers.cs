using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UITweenModifiers : MonoBehaviour
{
    [System.Serializable]
    public struct UITweenConfig
    {
        public Transform target;       // The UI element to move
        public Vector3 targetPosition; // The destination position to move to
        public float duration;         // Duration of the movement
        public bool useLocalPosition;  // If true, targetPosition is treated as local to the parent
    }

    // List of UI tween configurations to define movements
    public List<UITweenConfig> tweenConfigs;

    // Dictionary to store initial positions for returning elements to their original locations
    private readonly Dictionary<Transform, Vector3> initialPositions = new();

    // Track whether the UI elements are currently moved to their target positions
    private bool isMoved = false;

    public UnityEvent onMoveToTarget;
    public UnityEvent onReturnToInitial;

    void Start()
    {
        // Store the starting position (world or local as specified) for each element
        foreach (var config in tweenConfigs)
        {
            if (config.target != null)
            {
                if (config.useLocalPosition)
                    initialPositions[config.target] = config.target.localPosition;
                else
                    initialPositions[config.target] = config.target.position;
            }
        }
    }

    // Move UI elements to target positions based on the configurations
    public void MoveUIToTarget()
    {
        foreach (var config in tweenConfigs)
        {
            if (config.target != null)
            {
                if (config.useLocalPosition)
                {
                    // Move to a target position using local coordinates relative to the parent
                    LeanTween.moveLocal(config.target.gameObject, config.targetPosition, config.duration);
                }
                else
                {
                    // Move to a target position using world coordinates
                    LeanTween.move(config.target.gameObject, config.targetPosition, config.duration);
                }
            }
        }

        onMoveToTarget?.Invoke();
    }

    // Return UI elements to their initial positions
    public void ReturnUIToInitialPosition()
    {
        foreach (var config in tweenConfigs)
        {
            if (config.target != null)
            {
                if (config.useLocalPosition)
                {
                    LeanTween.moveLocal(config.target.gameObject, initialPositions[config.target], config.duration);
                }
                else
                {
                    LeanTween.move(config.target.gameObject, initialPositions[config.target], config.duration);
                }
            }
        }

        onReturnToInitial?.Invoke();
    }

    public void ToggleMovement()
    {
        if (isMoved)
        {
            ReturnUIToInitialPosition();
        }
        else
        {
            MoveUIToTarget();
        }

        // Toggle the state
        isMoved = !isMoved;
    }
}