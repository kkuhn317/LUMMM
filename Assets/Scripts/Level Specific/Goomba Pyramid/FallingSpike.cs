using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingSpike : EnemyAI
{
    public bool deadly = true;
    private Animator animator;
    private AudioSource audioSource;
    private bool wasFalling = false;

    protected override void Start()
    {
        base.Start();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    protected override void Update()
    {
        base.Update();

        if (movement != ObjectMovement.still && velocity.y == 0) {
            deadly = false;

            if (wasFalling) {
                animator.SetBool("hasFallen", true);
                audioSource.Play();
            }
        } else {
            deadly = true;
        }

        // Update the falling state for the next frame
        wasFalling = (movement != ObjectMovement.still && velocity.y != 0);
    }

    protected override void hitByPlayer(GameObject player)
    {
        MarioMovement playerscript = player.GetComponent<MarioMovement>();

        if (playerscript.starPower || !deadly)
        {
            KnockAway(player.transform.position.x > transform.position.x);
            GameManager.Instance.AddScorePoints(100);
        }
        else
        {
            playerscript.damageMario();
        }
    }

    public void fallDown()
    {
        movement = ObjectMovement.sliding;
    }
}
