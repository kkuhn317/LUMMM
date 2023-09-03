using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleBehavior : MonoBehaviour
{
    public float riseSpeed = 0.5f;
    public float horizontalSpeed = 0.025f;

    public float movementLimit = 0.1f;
    public float fadeDuration = 2f;
    private float startTime;
    private float horizontalVelocity; // Define the horizontalVelocity variable

    private SpriteRenderer spriteRenderer;

    private bool movingRight = true; // Added flag for movement direction

    private void Start()
    {
        startTime = Time.time;
        spriteRenderer = GetComponent<SpriteRenderer>();
        horizontalVelocity = horizontalSpeed; // Initialize horizontalVelocity here
    }

    public void StartRising()
    {
        // Make the bubble rise and move initially to the right
        GetComponent<Rigidbody2D>().velocity = new Vector2(horizontalSpeed, riseSpeed);
    }

    public void DestroyBubble()
    {
        Destroy(gameObject);
    }

    private void Update()
    {
        // Fade out the bubble over time
        float elapsedTime = Time.time - startTime;
        if (elapsedTime >= fadeDuration)
        {
            Destroy(gameObject); // Fade out complete, destroy the bubble
        }
        else
        {
            // Adjust the opacity of the bubble based on time
            float alpha = 1f - (elapsedTime / fadeDuration);
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }

        // Update horizontal velocity based on movement direction
        float horizontalVelocity = movingRight ? horizontalSpeed : -horizontalSpeed;

        // Check if the bubble has moved too far left or right
        if (movingRight && transform.position.x >= movementLimit)
        {
            movingRight = false;
        }
        else if (!movingRight && transform.position.x <= -movementLimit)
        {
            movingRight = true;
        }

        // Apply horizontal velocity
        GetComponent<Rigidbody2D>().velocity = new Vector2(horizontalVelocity, GetComponent<Rigidbody2D>().velocity.y);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Check if the bubble has exited the water layer
        if (collision.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            Destroy(gameObject); // Destroy the bubble when it exits the water or touches the edge
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the player has entered the water layer
        if (collision.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            StartRising();
        }
    }

    private void OnDrawGizmos()
    {
        // Draw vertical movement limits
        Vector3 upperLimit = transform.position + new Vector3(0f, movementLimit, 0f);
        Vector3 lowerLimit = transform.position + new Vector3(0f, -movementLimit, 0f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(upperLimit, lowerLimit);

        // Draw horizontal movement limits
        Vector3 rightLimit = transform.position + new Vector3(movementLimit, 0f, 0f);
        Vector3 leftLimit = transform.position + new Vector3(-movementLimit, 0f, 0f);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(rightLimit, leftLimit);
    }
}
