using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Thwomp : EnemyAI
{
    [Header("Thwomp")]
    public float initialDelay = 1f; // The time it will wait before it falls for the first time
    public float waitAtTopTime = 1f; // The time it will wait at the top before falling
    public float startFallSpeed = 0f; // The speed it will move down when it starts falling
    public float waitAtBottomTime = 1f; // The time it will wait at the bottom before rising
    public float riseSpeed = 1f;    // The speed it will move up when it is not falling (constant, not affected by gravity)

    // TODO: Implement this variable
    public float activateDistance = 0f; // The horizontal distance away the player must be for the thwomp to start falling
    private float topY; // The y position of the top of the thwomp
    private float internalGravity;
    private bool isRising = false;
    private bool isFalling = false;
    private AudioSource audioSource;
    
    // NEW: Track time-based movement instead of frame-based
    private float riseStartTime;
    private float targetRiseDuration;
    private Vector3 riseStartPosition;

    protected override void Start()
    {
        base.Start();
        topY = transform.position.y;
        internalGravity = gravity;
        gravity = 0f;

        if (TryGetComponent(out AudioSource foundAudioSource))
        {
            audioSource = foundAudioSource;
        }

        // Start the thwomp's behavior
        Invoke(nameof(ThwompFall), initialDelay);
    }

    private void ThwompFall()
    {
        isFalling = true;
        velocity.y = startFallSpeed;
        gravity = internalGravity;
    }

    public override void Land(GameObject other = null)
    {
        base.Land();
        if (!isFalling) return;
        isFalling = false;
        gravity = 0f;
        velocity.y = 0f;
        if (audioSource != null)
        {
            audioSource.Play();
        }

        // Wait at the bottom for a bit
        Invoke(nameof(ThwompRise), waitAtBottomTime);
    }

    private void ThwompRise()
    {
        objectState = ObjectState.falling;
        isRising = true;
        velocity.y = riseSpeed;
        gravity = 0f;
        
        // Calculate rise parameters for time-based movement
        float distanceToTop = topY - transform.position.y;
        targetRiseDuration = distanceToTop / riseSpeed;
        riseStartTime = Time.time;
        riseStartPosition = transform.position;
    }

    protected override void Update()
    {
        base.Update();

        // Now it's time-based rising instead of position-based
        if (isRising)
        {
            float elapsedTime = Time.time - riseStartTime;
            float progress = Mathf.Clamp01(elapsedTime / targetRiseDuration);
            
            // Move using Lerp for consistent time-based movement
            Vector3 newPosition = riseStartPosition;
            newPosition.y = Mathf.Lerp(riseStartPosition.y, topY, progress);
            transform.position = newPosition;

            // Check if we've reached the top
            if (progress >= 1f)
            {
                isRising = false;
                velocity.y = 0f;
                gravity = 0f;
                transform.position = new Vector3(transform.position.x, topY, transform.position.z); // Snap to exact position

                // Wait at the top for a bit
                Invoke(nameof(ThwompFall), waitAtTopTime);
            }
        }
    }
}