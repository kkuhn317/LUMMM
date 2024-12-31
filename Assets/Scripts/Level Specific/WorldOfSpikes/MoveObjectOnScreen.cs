using UnityEngine;
using System.Collections;

public class MoveObjectOnScreen : MonoBehaviour
{
    [Header("Target Position")]
    public Vector3 targetPosition; // The position to move the object to

    [Header("Movement Settings")]
    public float moveSpeed = 5f; // Speed at which the object moves
    public float delayBeforeMove = 1f; // Delay before starting the movement (in seconds)

    private Camera mainCamera;
    private bool isMoving = false; // Tracks if the object should start moving

    void Start()
    {
        // Get a reference to the main camera
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found. Make sure your scene has a camera tagged as MainCamera.");
        }
    }

    void Update()
    {
        // Check if the object should start moving after the delay
        if (!isMoving && IsObjectVisible())
        {
            StartCoroutine(StartMovingAfterDelay()); // Start the movement after a delay
        }

        if (isMoving)
        {
            // Move the object towards the target position
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }
    }

    private bool IsObjectVisible()
    {
        if (mainCamera == null) return false;

        // Get the viewport position of the object
        Vector3 viewportPosition = mainCamera.WorldToViewportPoint(transform.position);

        // Check if the object is even partially within the screen boundaries
        return viewportPosition.x > -0.025f && viewportPosition.x < 1.025f &&
               viewportPosition.y > -0.025f && viewportPosition.y < 1.025f &&
               viewportPosition.z > 0; // Ensure it's in front of the camera
    }

    // Coroutine to start moving after a delay
    private IEnumerator StartMovingAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeMove); // Wait for the specified delay
        isMoving = true; // Start moving
    }

    // Visualize in Scene view
    void OnDrawGizmos()
    {
        // Draw a line to show the movement path in the scene view
        if (isMoving)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetPosition);
        }

        // Visualize the target position
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(targetPosition, 0.2f);
    }
}