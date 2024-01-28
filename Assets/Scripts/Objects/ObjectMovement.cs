using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Used to move an object back and forth between two points
// Example, moving spikes in tiny goomba maze
public class ObjectMovement : MonoBehaviour
{
    public float moveDistance = 5f;
    public float forwardDuration = 2f;
    public float backwardDuration = 1.5f;
    public float forwardWaitDuration = 0.5f;   // New variable for forward wait duration
    public float backwardWaitDuration = 0.5f;  // New variable for backward wait duration
    public MovementDirection direction = MovementDirection.Right;

    private Vector3 originalPosition;
    private bool isMovingForward = true;
    [SerializeField]
    private bool startFromBackward = false;
    private float transitionTimer = 0f;

    public GameObject[] audioObjects;
    public AudioClip forwardAudioClip;
    public AudioClip backwardAudioClip;
    public AudioClip topforwardAudioClip;
    public AudioClip topbackwardAudioClip;

    // Enum to represent the movement direction
    public enum MovementDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    private void Start()
    {
        originalPosition = transform.position;

        // If startFromBackward is true, initialize the object's position at the backward end
        if (startFromBackward)
        {
            MoveToBackwardPosition();
        }

        StartCoroutine(MoveObject());
    }

    private void Update()
    {
        UpdateTransitionTimer();
        PlayMaxPositionAudio();
    }

    private void UpdateTransitionTimer()
    {
        if (!isMovingForward && transitionTimer < forwardDuration)
        {
            transitionTimer += Time.deltaTime;
        }
    }

    private void MoveToBackwardPosition()
    {
        float targetDistance = moveDistance;
        Vector3 targetPosition = originalPosition;
        switch (direction)
        {
            case MovementDirection.Up:
                targetPosition += Vector3.up * targetDistance;
                break;
            case MovementDirection.Down:
                targetPosition += Vector3.down * targetDistance;
                break;
            case MovementDirection.Left:
                targetPosition += Vector3.left * targetDistance;
                break;
            case MovementDirection.Right:
                targetPosition += Vector3.right * targetDistance;
                break;
        }

        transform.position = targetPosition;
    }

    private void PlayMaxPositionAudio()
    {
        // Play audio clip for reaching maximum position
        AudioClip maxPositionAudioClip = isMovingForward ? topforwardAudioClip : topbackwardAudioClip;

        if (maxPositionAudioClip != null)
        {
            AudioSource.PlayClipAtPoint(maxPositionAudioClip, transform.position);
        }
    }

    private System.Collections.IEnumerator MoveObject()
    {
        while (true)
        {
            // Calculate the target position based on the movement direction
            float targetDistance = isMovingForward ? moveDistance : 0f;
            Vector3 targetPosition = originalPosition;
            switch (direction)
            {
                case MovementDirection.Up:
                    targetPosition += Vector3.up * targetDistance;
                    break;
                case MovementDirection.Down:
                    targetPosition += Vector3.down * targetDistance;
                    break;
                case MovementDirection.Left:
                    targetPosition += Vector3.left * targetDistance;
                    break;
                case MovementDirection.Right:
                    targetPosition += Vector3.right * targetDistance;
                    break;
            }

            // Calculate the movement speed (distance / duration)
            float speed = moveDistance / (isMovingForward ? forwardDuration : backwardDuration);

            // Move the object towards the target position
            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
                yield return null;
            }

            // Toggle the movement direction and wait for the appropriate duration
            isMovingForward = !isMovingForward;

            // Determine the wait duration based on movement direction
            float waitDuration = isMovingForward ? forwardWaitDuration : backwardWaitDuration;
            yield return new WaitForSeconds(waitDuration);

            // Play audio clip based on movement direction
            int audioIndex = isMovingForward ? 0 : 1;

            if (audioIndex >= 0 && audioIndex < audioObjects.Length)
            {
                GameObject audioObject = audioObjects[audioIndex];
                if (audioObject != null)
                {
                    AudioSource audioSource = audioObject.GetComponent<AudioSource>();
                    if (audioSource != null)
                    {
                        AudioClip audioClip = isMovingForward ? forwardAudioClip : backwardAudioClip;
                        if (audioClip != null)
                        {
                            audioSource.PlayOneShot(audioClip);
                        }
                    }
                }
            }


            // Reset the transition timer
            transitionTimer = 0f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw a line to represent the movement range
        Vector3 targetPosition = originalPosition;

        switch (direction)
        {
            case MovementDirection.Up:
                targetPosition += Vector3.up * moveDistance;
                DrawLabel("Up", targetPosition);
                break;
            case MovementDirection.Down:
                targetPosition += Vector3.down * moveDistance;
                DrawLabel("Down", targetPosition);
                break;
            case MovementDirection.Left:
                targetPosition += Vector3.left * moveDistance;
                DrawLabel("Left", targetPosition);
                break;
            case MovementDirection.Right:
                targetPosition += Vector3.right * moveDistance;
                DrawLabel("Right", targetPosition);
                break;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawLine(originalPosition, targetPosition);

        // Draw spheres at the start and end points
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(originalPosition, 0.1f);
        Gizmos.DrawSphere(targetPosition, 0.1f);
    }

    private void DrawLabel(string text, Vector3 position)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.Label(position, text);
#endif
    }
}
