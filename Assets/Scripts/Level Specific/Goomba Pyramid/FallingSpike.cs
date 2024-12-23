using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FallingSpike : EnemyAI
{
    public bool deadly = true;
    private Animator animator;
    private AudioSource audioSource;
    private bool wasFalling = false;
    private bool isDead = false;

    public UnityEvent onStartFalling;
    public UnityEvent onStopFalling;

    protected override void Start()
    {
        base.Start();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        // Ensure UnityEvents are initialized
        if (onStartFalling == null) onStartFalling = new UnityEvent();
        if (onStopFalling == null) onStopFalling = new UnityEvent();
    }

    protected override void Update()
    {
        base.Update();

        if (isDead) return; // Skip logic if the spike is dead

        bool isFalling = (movement != ObjectMovement.still && velocity.y != 0);

        // Trigger events based on state changes
        if (isFalling && !wasFalling)
        {
            onStartFalling.Invoke();
        }
        else if (!isFalling && wasFalling)
        {
            onStopFalling.Invoke();
        }

        if (movement != ObjectMovement.still && velocity.y == 0)
        {
            deadly = false;

            if (wasFalling)
            {
                animator.SetBool("hasFallen", true);
                audioSource.Play();
            }
        }
        else
        {
            deadly = true;
        }

        // Update the falling state for the next frame
        wasFalling = isFalling;
    }

    protected override void hitByPlayer(GameObject player)
    {
        MarioMovement playerscript = player.GetComponent<MarioMovement>();

        if (playerscript.starPower || !deadly)
        {
            KnockAway(player.transform.position.x > transform.position.x);
            GameManager.Instance.AddScorePoints(100);
            Die(); // Mark the spike as dead
        }
        else
        {
            playerscript.damageMario();
        }
    }

    public void fallDown()
    {
        if (!isDead) // Prevent falling behavior if the spike is dead
        {
            movement = ObjectMovement.sliding;
        }
    }

    private void Die()
    {
        if (isDead) return; // Prevent multiple calls to Die
        isDead = true;
        onStopFalling.Invoke(); // Trigger stop falling event if necessary
    }
}