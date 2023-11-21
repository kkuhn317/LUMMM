using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvincibilityStar : MonoBehaviour
{
    public float duration = 10f; // Duration of invincibility in seconds
    public float blinkInterval = 0.1f; // Interval at which the object will blink while invincible
    public float blinkDuration = 1f; // Duration of the blink effect
    public float speedMultiplier = 2f; // Multiplier for the player's speed while invincible
    public float bounceSpeed = 10f; // Speed at which the star bounces off surfaces

    private bool isInvincible = false; // Flag indicating if the object is currently invincible
    private SpriteRenderer spriteRenderer; // Reference to the object's sprite renderer
    private Rigidbody2D rb; // Reference to the object's rigidbody2d
    private Vector2 movementDirection = Vector2.up; // Direction of star movement

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        // Set the star's initial movement direction
        movementDirection = Vector2.right; // Change this to the desired direction

        // Set the star's initial velocity
        if (rb != null)
        {
            rb.velocity = movementDirection.normalized * bounceSpeed;
        }
    }

    public void Activate()
    {
        if (!isInvincible)
        {
            isInvincible = true;
            StartCoroutine(InvincibilityCoroutine());
        }
    }

    private IEnumerator InvincibilityCoroutine()
    {
        float endTime = Time.time + duration;
        bool isVisible = true;
        float blinkStartTime = Time.time;

        while (Time.time < endTime)
        {
            // Toggle the object's visibility
            if (Time.time - blinkStartTime >= blinkInterval)
            {
                isVisible = !isVisible;
                spriteRenderer.enabled = isVisible;
                blinkStartTime = Time.time;
            }

            yield return null;
        }

        // Reset the object's properties
        spriteRenderer.enabled = true;
        isInvincible = false;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (rb != null)
        {
            // Bounce off surfaces
            Vector2 normal = collision.contacts[0].normal;
            Vector2 direction = Vector2.Reflect(movementDirection, normal);
            movementDirection = direction.normalized;
            rb.velocity = movementDirection * bounceSpeed;
        }
    }
}
