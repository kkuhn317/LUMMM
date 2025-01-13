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

    // Dash Attack
    private bool dashAttackMode = false;
    private enum DashAttackState {
        walkBack,
        jump,
        dash
    }
    private DashAttackState dashAttackState = DashAttackState.walkBack;
    public float dashWalkBackPos = -0.5f;  // walk back until reaching this position
    public float dashForwardPos = 0.5f;    // run forward until reaching this position
    public AudioClip dashJumpSound;
    public Transform dashParticlesPosition;
    public GameObject dashPrefab;
    public GameObject smirkGoomba;

    public void StartFight() {
        if (fightStarted) {
            return;
        }
        fightStarted = true;
        movement = ObjectMovement.sliding;
        player = GameObject.FindGameObjectWithTag("Player");

        // Choose which mode to start in
        float random = Random.Range(0f, 1f);
        //print("Random: " + random);
        if (random < 0.5) {
            // jump after random time between 1 and 2 seconds
            Invoke (nameof(Jump), Random.Range(1f, 2f));
        } else {
            // dash attack
            DashWalkBack();
        }
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

        if (fightStarted && player != null) {
            if (dashAttackMode) {
                DashAttackMovement();
            } else {
                FollowPlayer();
            }
        }
    }

    void DashAttackMovement() {
        switch (dashAttackState) {
            case DashAttackState.walkBack:
                if (transform.position.x < dashWalkBackPos) {
                    // Initiate jump
                    DashJump();
                }
                break;
            case DashAttackState.jump:
                if (objectState == ObjectState.grounded) {
                    // Start dashing
                    DashForward();
                }
                break;
            case DashAttackState.dash:
                if (transform.position.x > dashForwardPos) {
                    // stop dashing
                    animator.SetBool("dashing", false);
                    dashAttackMode = false;
                    animator.SetFloat("walkSpeed", 1);
                    movement = ObjectMovement.sliding;
                    Invoke (nameof(Jump), Random.Range(1f, 2f));
                }
                break;
        }
    }

    void DashWalkBack() {
        // walk back to charge up
        dashAttackMode = true;
        dashAttackState = DashAttackState.walkBack;
        movingLeft = true;
        velocity.x = 1f;
    }

    void DashJump() {
        if (fightEnded) {
            return;
        }
        animator.SetFloat("walkSpeed", 3);
        dashAttackState = DashAttackState.jump;
        isJumping = true;
        Fall();
        velocity = new Vector2(0, jumpHeight);
        audioSource.pitch = 1.1f;
        audioSource.PlayOneShot(dashJumpSound);
    }

    void DashForward() {
        if (fightEnded) {
            return;
        }
        animator.SetFloat("walkSpeed", 3);
        dashAttackState = DashAttackState.dash;
        animator.SetBool("dashing", true);
        movingLeft = false;
        velocity = new Vector2(3, 1f);
        Fall();
        /*if (dashPrefab != null && dashParticlesPosition != null){
            Instantiate(dashPrefab, dashParticlesPosition.transform.position, Quaternion.identity);
        }*/
        movement = ObjectMovement.bouncing; // tiny bounces
    }

    void FollowPlayer() {
        if (velocity.x == 0) {
            // When you hit the axe
            fightEnded = true;
            return;
        }   

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

    protected override void OnBounced()
    {
        base.OnBounced();
        if (dashPrefab != null && dashParticlesPosition != null){
            Instantiate(dashPrefab, dashParticlesPosition.transform.position, Quaternion.identity);
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

    protected override void hitOnSide(GameObject player){
        base.hitOnSide(player);

        // Get the player script
        MarioMovement playerScript = player.GetComponent<MarioMovement>();

        // Check if goomba's health is more or equals to 3 and the player is dead and the Goomba is moving left
        if (health >= 3 && playerScript != null && playerScript.Dead && movingLeft)
        {
            StartCoroutine(HandlePlayerDeath());
        }
       
    }

    private IEnumerator HandlePlayerDeath()
    {
        movement = ObjectPhysics.ObjectMovement.still;
         
        yield return new WaitForSeconds(0.5f);

        if (smirkGoomba != null)
        {
            smirkGoomba.SetActive(true);
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Draw the dash attack positions
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(new Vector3(dashWalkBackPos, transform.position.y, transform.position.z), 0.1f);
        Gizmos.DrawWireSphere(new Vector3(dashForwardPos, transform.position.y, transform.position.z), 0.1f);
    }
}