using UnityEngine;

public class UIScrollingObject : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f; // Movement speed

    [Header("Position Reset Settings")]
    public Vector2 resetPosition = new(-20f, 0f); // Reset position when the loop happens
    public Vector2 startPosition = new(20f, 0f); // Starting position after reset

    private RectTransform rectTransform;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            Debug.LogError("This script requires a RectTransform. Please attach it to a UI element.");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        // Calculate direction from startPosition to resetPosition
        Vector2 direction = (resetPosition - startPosition).normalized;

        // Move the RectTransform
        rectTransform.anchoredPosition += moveSpeed * Time.deltaTime * direction;

        // Reset position when crossing the reset threshold
        if (Vector2.Dot(rectTransform.anchoredPosition - resetPosition, direction) > 0)
        {
            rectTransform.anchoredPosition = startPosition;
        }
    }

    private void OnDrawGizmos()
    {
        if (transform.parent == null) return;

        // Visualize resetPosition in the Scene view (relative to RectTransform)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.parent.TransformPoint(resetPosition), 10f);

        // Visualize startPosition
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.parent.TransformPoint(startPosition), 10f);

        // Visualize the movement path
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.parent.TransformPoint(startPosition), transform.parent.TransformPoint(resetPosition));
    }
}
