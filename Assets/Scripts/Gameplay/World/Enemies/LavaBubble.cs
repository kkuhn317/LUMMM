using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LavaBubble : EnemyAI
{
    [Header("Lava Bubble")]
    private float jumpVelocity; // Set this with the the velocity.y in the inspector
    public float jumpInitialDelay = 0f;
    public float jumpCooldown = 1f;
    private bool isJumping = false;
    private float initialPositionY;
    private float gravityInternal;
    private SpriteRenderer visualRenderer;

    private float startJumpTime = 0f;   // Used to help time the jumps with other lava bubbles

    protected override void Start()
    {
        initialPositionY = transform.position.y;
        jumpVelocity = velocity.y;
        velocity = new Vector2(0, 0);
        gravityInternal = gravity;
        gravity = 0;
        visualRenderer = GetComponent<SpriteRenderer>();
        base.Start();
        enabled = true; // Enable the enemy AI because we want it to jump from offscreen
        Invoke(nameof(Jump), jumpInitialDelay);
    }

    private void Jump()
    {
        if (isJumping || objectState == ObjectState.knockedAway)
        {
            return;
        }
        if (!isJumping)
        {
            //print("time between jumps: " + (Time.time - startJumpTime) + " seconds");
            startJumpTime = Time.time;
            isJumping = true;
            velocity = new Vector2(velocity.x, jumpVelocity);
            gravity = gravityInternal;
        }
    }

    protected override void Update()
    {
        base.Update();
        if (objectState == ObjectState.knockedAway)
        {
            return;
        }
        visualRenderer.flipY = velocity.y < 0;
        if (isJumping && velocity.y <= 0 && transform.position.y <= initialPositionY)
        {
            isJumping = false;
            transform.position = new Vector3(transform.position.x, initialPositionY, transform.position.z);
            velocity = new Vector2(0, 0);
            gravity = 0;
            //print("time it took to jump: " + (Time.time - startJumpTime) + " seconds");
            Invoke(nameof(Jump), jumpCooldown);
        }
    }
}
