using UnityEngine;
using TMPro;

public class CreditsScroller : MonoBehaviour
{
    public GameObject creditsText; // Reference to the TMP Text component for credits
    public float scrollSpeed = 50f; // Speed at which the credits scroll
    public Transform creditPositionTracker;
    public Transform stopDetector; // The object that detects where credits should stop

    private bool isScrolling = true; // Flag to determine if credits are scrolling

    void Update()
    {
        if (isScrolling)
        {
            // Move the credits upwards based on the scroll speed
            creditsText.transform.position += Vector3.up * scrollSpeed * Time.deltaTime;

            // Check if the credits have reached the stop detector
            if (creditPositionTracker.transform.position.y >= stopDetector.position.y)
            {
                StopScrolling();
            }
        }
    }

    // Method to stop the scrolling
    private void StopScrolling()
    {
        isScrolling = false; // Stop the scrolling
        creditPositionTracker.transform.position = stopDetector.position; // Snap credits to stop position
    }
}
