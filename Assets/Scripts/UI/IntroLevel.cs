using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IntroLevelUI : MonoBehaviour
{
    public List<RectTransform> targets; // List of UI elements (RectTransforms)
    public Vector3 targetPosition; // Position to move all objects to (in local space)
    public float moveDuration = 1f; // Duration of the move
    public float delayBeforeMove = 0.5f; // Delay before starting the movement
    public Color lineColor = Color.green; // Color of the debug lines

    private Dictionary<RectTransform, Vector3> initialPositions = new Dictionary<RectTransform, Vector3>();

    private void Start()
    {
        if (targets == null || targets.Count == 0)
        {
            Debug.LogError("No targets assigned for IntroLevelUI.");
            return;
        }

        // Cache the initial positions of all targets
        foreach (RectTransform target in targets)
        {
            if (target != null)
            {
                initialPositions[target] = target.anchoredPosition;
            }
        }

        // Start the delayed movement coroutine
        StartCoroutine(DelayedMoveAll());
    }

    private IEnumerator DelayedMoveAll()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(delayBeforeMove);

        // Move all targets simultaneously
        foreach (RectTransform target in targets)
        {
            if (target == null) continue;

            LeanTween.move(target, targetPosition, moveDuration);
        }
    }

    private void OnDrawGizmos()
    {
        if (targets == null || targets.Count == 0)
            return;

        foreach (RectTransform target in targets)
        {
            if (target == null) continue;

            // Convert UI positions to world space for visualization
            Vector3 worldStart = target.position;
            Vector3 worldEnd = transform.TransformPoint(targetPosition);

            // Use Debug.DrawLine to visualize paths in world space
            Debug.DrawLine(worldStart, worldEnd, lineColor);
        }
    }
}