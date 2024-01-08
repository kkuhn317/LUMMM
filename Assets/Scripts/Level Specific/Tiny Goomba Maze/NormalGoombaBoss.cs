using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalGoombaBoss : EnemyAI
{

    [Header("Normal Goomba Boss")]
    bool fightStarted = false;
    bool fightEnded = false;
    public int health = 3;
    public float jumpHeight = 5f;
    private GameObject player;
    private AudioSource audioSource;
    public AudioClip jumpSound;
    private Animator animator;

    public void StartFight() {
        if (fightStarted) {
            return;
        }
        fightStarted = true;
        movement = ObjectPhysics.ObjectMovement.sliding;
        player = GameObject.FindGameObjectWithTag("Player");

        // jump after random time between 1 and 2 seconds
        Invoke (nameof(Jump), Random.Range(1f, 2f));
    }

    protected override void Start() {
        base.Start();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
    }

    protected override void FixedUpdate() {
        base.FixedUpdate();

        if (fightEnded) {
            velocity.x = 0;
            return;
        }
        if (health <= 0) {
            fightEnded = true;
            movement = ObjectPhysics.ObjectMovement.still;
            return;
        }
        if (velocity.x == 0) {
            // When you hit the axe
            fightEnded = true;
            return;
        }
        

        if (fightStarted && player != null) {
            // check if left or right of player
            if (player.transform.position.x < transform.position.x && objectState == ObjectState.grounded) {
                // move left
                movingLeft = true;
                velocity.x = 2f; // move faster to the left
            } else {
                // move right
                movingLeft = false;
                velocity.x = 0.5f; 
            }
        }

    }

    void Jump() {
        if (fightEnded) {
            return;
        }
        if (!movingLeft) {
            //print("JUMP");
            Fall();
            velocity.y = jumpHeight;
            audioSource.PlayOneShot(jumpSound);
        }
        Invoke (nameof(Jump), Random.Range(1f, 2f));
    }

    protected override void hitByStomp(GameObject player)
    {
        MarioMovement playerscript = player.GetComponent<MarioMovement>();
        audioSource.Play();
        playerscript.Jump();
        health--;

        switch (health) {
            case 2:
                transform.localScale = new Vector3(0.025f, 0.019f, 0.025f);
                transform.position = new Vector3(transform.position.x, transform.position.y - 0.003395f, transform.position.z); // 0.15f
                height = 0.7f;
                stompHeight = 0.15f;
                break;
            case 1:
                transform.localScale = new Vector3(0.025f, 0.014f, 0.025f);
                transform.position = new Vector3(transform.position.x, transform.position.y - 0.005905f, transform.position.z);
                height = 0.5f;
                stompHeight = 0.1f;
                break;
            case 0:
                transform.localScale = new Vector3(0.025f, 0.025f, 0.025f);
                transform.position = new Vector3(transform.position.x, transform.position.y + 0.25f, transform.position.z);
                height = 1f;

                GetComponent<Collider2D>().enabled = false;
                animator.SetBool("isCrushed", true);
                fightEnded = true;
                break;
        }
    }
}
