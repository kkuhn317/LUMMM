using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class IntroLevel : MonoBehaviour
{
    public Image marioImage;
    public Image xImage;
    public TMP_Text lives;
    public TMP_Text conditionText;
    public Image conditionIconImage;

    [System.Serializable]
    public class TargetData
    {
        public RectTransform target; // The UI element to move
        public Vector3 targetPosition; // The specific target position for this UI element
        public LeanTweenType easingType = LeanTweenType.linear; // Easing type for the move
    }

    public List<TargetData> targetDataList; // List of targets and their respective transition settings
    public float moveDuration = 1f; // Global duration of the move
    public float delayBeforeMove = 1f; // Global delay before starting the movement
    public Color lineColor = Color.green; // Color of the debug lines
    public UnityEvent OnMoveStart; // Event triggered when a move starts
    public UnityEvent OnTargetReached; // Event triggered when all targets finish moving

    private int completedTweens = 0;

    private void Start()
    {
        // Reset LeanTween to avoid stale tweens
        LeanTween.reset();

        // Force layout update to ensure consistent UI positions
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);

        if (targetDataList == null || targetDataList.Count == 0)
        {
            Debug.LogError("No target data assigned for IntroLevel.");
            return;
        }

        // Reset the completed tween counter
        completedTweens = 0;

        // Start moving each target with its own easing type
        foreach (TargetData data in targetDataList)
        {
            if (data.target == null) continue;

            Debug.Log($"Preparing to move {data.target.name} to {data.targetPosition} with easing {data.easingType}");

            LeanTween.move(data.target, data.targetPosition, moveDuration)
                .setDelay(delayBeforeMove) // Use global delay
                .setEase(data.easingType) // Use easing type from TargetData
                .setOnStart(() =>
                {
                    Debug.Log($"Started moving {data.target.name}");
                    OnMoveStart?.Invoke(); // Trigger OnMoveStart event
                })
                .setOnComplete(() =>
                {
                    Debug.Log($"Completed moving {data.target.name} to {data.targetPosition}");
                    completedTweens++;

                    // Check if all tweens are complete
                    if (completedTweens == targetDataList.Count)
                    {
                        Debug.Log("All targets have reached their positions.");
                        OnTargetReached?.Invoke();
                    }
                });
        }
    }

    private void OnDrawGizmos()
    {
        if (targetDataList == null || targetDataList.Count == 0)
            return;

        foreach (TargetData data in targetDataList)
        {
            if (data.target == null) continue;

            // Convert UI positions to world space for visualization
            Vector3 worldStart = data.target.position;
            Vector3 worldEnd = data.target.parent.TransformPoint(data.targetPosition);

            // Use Debug.DrawLine to visualize paths in world space
            Debug.DrawLine(worldStart, worldEnd, lineColor);
        }
    }
}