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

    public override void Land()
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
    }

    protected override void Update()
    {
        base.Update();

        // Check if the thwomp is rising and has reached the top
        if (isRising && transform.position.y >= topY)
        {
            isRising = false;
            velocity.y = 0f;
            gravity = 0f;

            // Wait at the top for a bit
            Invoke(nameof(ThwompFall), waitAtTopTime);
        }
    }

}
