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
    public AudioClip stompSound;
    public AudioClip jumpSound;
    public AudioClip landSound;
    private Animator animator;

    public GameObject hitEffectPrefab;
    private bool isJumping = false;

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
            isJumping = true;
            Fall();
            velocity.y = jumpHeight;
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(jumpSound);
        }
        Invoke (nameof(Jump), Random.Range(1f, 2f));
    }

    public override void Land(GameObject other = null)
    {
        base.Land(other);

        objectState = ObjectState.grounded;

        // Trigger camera shake only if landing after a jump
        if (isJumping)
        {
            if (health >= 2){
                audioSource.pitch = 1f;
                audioSource.PlayOneShot(landSound);
                TriggerCameraShake();
            } 
            isJumping = false; // Reset jump state
        }
    }

    private void TriggerCameraShake() {
        CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (cameraFollow != null) {
            Debug.Log("Triggering Camera Shake");
            cameraFollow.ShakeCameraRepeatedly(0.1f, 0.25f, 1.0f, new Vector3(0, 1, 0), 2, 0.1f);
        } else {
            Debug.LogWarning("No CameraFollow component found on the main camera.");
        }
    }

    protected override void hitByStomp(GameObject player)
    {
        MarioMovement playerscript = player.GetComponent<MarioMovement>();
        audioSource.pitch = 1.25f;
        audioSource.PlayOneShot(stompSound);
        playerscript.Jump();
        health--;

        animator.SetTrigger("hurt");

        // Calculate the hit position based on the player's position
        Vector3 hitPosition = new Vector3(player.transform.position.x, transform.position.y + stompHeight / 2, transform.position.z);

        // Spawn hit effect at the player's hit position
        if (hitEffectPrefab != null) {
            Instantiate(hitEffectPrefab, hitPosition, Quaternion.identity);
        } else {
            Debug.LogWarning("Hit effect prefab is not assigned!");
        }

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
