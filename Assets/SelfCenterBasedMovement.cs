using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfCenterBasedMovement : MonoBehaviour
{
    public float maxDistance = 2.0f; // Maximum distance from the center
    public float movementSpeed = 2.0f;

    private Vector3 initialPosition;
    private bool movingRight = true;

    private void Start()
    {
        initialPosition = transform.position;
    }

    private void Update()
    {
        // Calculate the distance between the object and the initial position
        float distanceToCenter = Vector3.Distance(transform.position, initialPosition);

        // Update the direction of movement based on the distance to center
        if (distanceToCenter >= maxDistance)
        {
            movingRight = !movingRight;
        }

        // Calculate the new position based on the direction of movement
        Vector3 newPosition = movingRight ? initialPosition + Vector3.right * maxDistance : initialPosition;

        // Move the object towards the new position
        transform.position = Vector3.MoveTowards(transform.position, newPosition, movementSpeed * Time.deltaTime);
    }
}
