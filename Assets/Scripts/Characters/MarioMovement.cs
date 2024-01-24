using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.U2D.Animation;
using Unity.Mathematics;
using Unity.VisualScripting;
using System.Collections.Generic;

public class MarioMovement : MonoBehaviour
{
    public int playerNumber = 0;    // 0 = player 1, 1 = player 2, etc.
    private Vector3 originalPosition;
    
    /* Input System */
    // Other scripts can access these variables to get the player's input
    // Do not use the old input system or raw keyboard input anywhere in the game
    [HideInInspector] public Vector2 moveInput; // The raw directional input from the player's controller
    [HideInInspector] public bool crouchPressed = false;
    private bool jumpPressed = false;
    bool runPressed = false;


    [Header("Horizontal Movement")]
    public float moveSpeed = 10f;
    public float runSpeed = 20f;
    public float slowDownForce = 5f;
    
    public Vector2 direction;
    public bool facingRight = true;
    private bool inCrouchState = false;
    private bool isCrawling = false;    // Currently small mario only


    [Header("Vertical Movement")]
    public float jumpSpeed = 15f;
    public float jumpDelay = 0.25f;
    public float terminalvelocity = 10f;
    public float startfallingspeed = 1f;
    private float jumpTimer;

    [Header("Components")]
    public Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Animator animator;
    public LayerMask groundLayer;
    private MarioAbility marioAbility;

    [Header("Camera")]
    public CameraFollow cameraFollow;

    [Header("Child Objects")]
    public GameObject heldObjectPosition;
    private GameObject relPosObj;

    [Header("Physics")]
    public float maxSpeed = 7f;
    public float maxRunSpeed = 10f;
    public float linearDrag = 4f;
    public float runlinearDrag = 2f;
    public float gravity = 0;
    public float fallgravity = 5f;
    public float airtime = 1f;
    private float airtimer = 0;
    private bool changingDirections;

    [Header("Swimming")]
    public float bubbleSpawnDelay = 2.5f;
    public GameObject bubblePrefab;
    public GameObject splashWaterPrefab;
    public bool swimming = false;
    public float swimForce = 5f;
    public float swimGravity = 1f;
    public float swimDrag = 3f;
    public float swimTerminalVelocity = 2f;

    [Header("Collision")]
    public bool onGround = false;
    public float groundLength = 0.6f;
    public bool onMovingPlatform = false;
    public float ceilingLength = 0.5f;

    public bool doCornerCorrection = true; // false: disable corner correction, true: enable corner correction
    public float cornerCorrection = 0.1f; // Portion of the player's width that can overlap with the ceiling and still correct the position
    // Example: 0.1f means that if the player's width is 1, the player can overlap with the ceiling by up to 0.1 (10%) and still correct the position

    float colliderY;
    float collideroffsetY;

    public Vector3 colliderOffset;

    public float damageinvinctime = 3f;
    public float invincetimeremain = 0f;

    public Vector2 groundPos;
    public enum PowerupState {

        small,
        big,
        power
    }

    [Header("Animation Events")]
    private bool isYeahAnimationPlaying = false;
    private bool hasEnteredAnimationYeahTrigger = false;

    private SpriteLibraryAsset normalSpriteLibrary;

    public bool canSkid = true;
    public bool canCrouch = true;
    public float walkAnimatorSpeed = 0.125f;

    [Header("Powerups")]

    public PowerupState powerupState = PowerupState.small;

    //public GameObject BigMario;
    public GameObject powerDownMario;

    // so that you can only hurt the player once per frame
    private bool damaged = false;

    [HideInInspector]
    public bool starPower = false;
    private readonly Color[] StarColors = { Color.green, Color.yellow, Color.blue, Color.red };
    private int selectedStarColor = 0;

    [Header("Death")]

    public GameObject deadMario;

    private bool dead = false;

    [Header("Sound Effects")]

    public AudioClip damageSound;
    public AudioClip bonkSound;
    public AudioClip swimSound;
    public AudioClip yeahAudioClip;
    private AudioSource audioSource;
    private bool frozen = false;

    [Header("Carrying")]

    public bool carrying = false;
    public bool pressRunToGrab = false;
    public bool crouchToGrab = false;
    public enum CarryMethod {
        inFront,
        onHand
    }
    public CarryMethod carryMethod = CarryMethod.onHand;
    public AudioClip pickupSound;
    public AudioClip dropSound;
    public AudioClip throwSound;

    private float grabRaycastHeight => powerupState == PowerupState.small ? -0.1f : -0.4f;

    private bool isLookingUp = false;

    [Header("Additional Abilities")]
    public bool canWallJump = false;    // Only small mario has an animation for wall jumping right now, so it will not be transferred after powerup
    public bool canSpinJump = false;    // not implemented yet
    private bool wallSliding = false;

    /* Levers */
    private List<LeverController> levers = new();

    // use this in other scripts to check if mario is moving (walking or jumping)
    public bool isMoving {
        get {
            return rb.velocity.x > 0.01 || !onGround;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        sprite = GetComponent<SpriteRenderer>();
        colliderY = GetComponent<BoxCollider2D>().size.y;
        collideroffsetY = GetComponent<BoxCollider2D>().offset.y;
        relPosObj = transform.GetChild(0).gameObject;
        animator = GetComponent<Animator>();
        animator.SetInteger("grabMethod", (int)carryMethod);
        GameManager.Instance.SetPlayer(this, playerNumber);

        if (powerupState == PowerupState.power) {
            marioAbility = GetComponent<MarioAbility>();
        }
        normalSpriteLibrary = GetComponent<SpriteLibrary>().spriteLibraryAsset;

        // Store player's position at the beginning of the level (respawn)
        originalPosition = transform.position;

        StartCoroutine(SpawnBubbles());
    }

    // Update is called once per frame
    void Update()
    {
        direction = moveInput;

        SpriteRenderer sprite = GetComponent<SpriteRenderer>();

        // die
        if (GameManager.Instance.currentTime <= 0)
        {
            toDead();
        }

        if (invincetimeremain > 0f) {
            sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 0.5f);
            invincetimeremain -= Time.deltaTime;
        } else {
            sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 1.0f);
            invincetimeremain = 0f;
        }

        // Picking up item
        if (!pressRunToGrab && runPressed && (!crouchToGrab || crouchPressed) && !carrying) {
            checkForCarry();
        }

        // Throwing item
        if (carrying && !runPressed) {
            if (crouchPressed) {
                dropCarry();
            } else {
                throwCarry();
            }
        }

        // Look Up
        if (!isMoving && direction.y > 0.8f) {
            // Set the flag to true to indicate that the player is looking up
            isLookingUp = true;
        } else {
            // Reset the flag to false if the player is not looking up
            isLookingUp = false;
        }


        // TODO: temporary fix until big mario gets looking up animation
        if (powerupState == PowerupState.small) {
            animator.SetBool("isLookingUp", isLookingUp);
        }
        

        if (cameraFollow != null)
        {
            if (isLookingUp) {
                cameraFollow.StartCameraMoveUp();
            } else {
                cameraFollow.StopCameraMoveUp();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D other) {
        if (groundLayer == (groundLayer | (1 << other.gameObject.layer))) {
            Vector2 impulse = Vector2.zero;

            int contactCount = other.contactCount;
            for (int i = 0; i < contactCount; i++) {
                var contact = other.GetContact(i);
                impulse += contact.normal * contact.normalImpulse;
                impulse.x += contact.tangentImpulse * contact.normal.y;
                impulse.y -= contact.tangentImpulse * contact.normal.x;
            }

            if (impulse.y < 0) {
                audioSource.PlayOneShot(bonkSound, 0.5f);
            }
        }
    }

    private void changeRainbowColor() {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        sprite.color = StarColors[selectedStarColor];
        selectedStarColor += 1;
        if (selectedStarColor == StarColors.Length)
            selectedStarColor = 0;
    }

    public void startStarPower(float time) {
        // stop any current star power
        CancelInvoke(nameof(stopStarPower));
        CancelInvoke(nameof(changeRainbowColor));

        // start new star power
        InvokeRepeating(nameof(changeRainbowColor), 0, 0.1f);
        starPower = true;
        if (time != -1) {
            Invoke(nameof(stopStarPower), time);
        }
    }

    public void stopStarPower() {
        CancelInvoke(nameof(changeRainbowColor));
        starPower = false;
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        sprite.color = new Color(1, 1, 1, sprite.color.a);
    }

    private void FixedUpdate() {

        if (frozen) {
            return;
        }

        // Movement
        moveCharacter(direction.x);
        if (jumpTimer > Time.time && (onGround || swimming || wallSliding)) {

            if (swimming) {
                audioSource.PlayOneShot(swimSound);
                Swim();
            } else if (wallSliding) {
                audioSource.Play();
                WallJump();
            } else {
                audioSource.Play();
                Jump();
            }
        }

        // Floor detection
        onMovingPlatform = false;
        transform.parent = null;

        RaycastHit2D groundHit1 = Physics2D.Raycast(transform.position + colliderOffset, Vector2.down, groundLength, groundLayer);
        RaycastHit2D groundHit2 = Physics2D.Raycast(transform.position - colliderOffset, Vector2.down, groundLength, groundLayer);

        onGround = (groundHit1 || groundHit2) && rb.velocity.y <= 0.01f;

        if (onGround) {

            RaycastHit2D hitRay = groundHit1;

            // print("ground1: " + groundHit1.transform.gameObject.tag);
            // print("ground2: " + groundHit2.transform.gameObject.tag);

            if (groundHit1) {
                hitRay = groundHit1;
                if (groundHit1.transform.gameObject.tag == "MovingPlatform") {
                    onMovingPlatform = true;
                }
            } else {
                hitRay = groundHit2;
                if (groundHit2.transform.gameObject.tag == "MovingPlatform") {
                    onMovingPlatform = true;
                }
            }

            groundPos = hitRay.point;

            if (onMovingPlatform) {
                transform.parent = hitRay.transform;
                // change y position to be on top of platform
                //transform.position = new Vector3(transform.position.x, groundPos.y + groundLength - 0.01f, transform.position.z);
                // apparently without this line, the moving platform works better lol
            } else {
                transform.parent = null;
            }

            if (hitRay.transform.gameObject.tag == "Damaging") {
                damageMario();
            }
        }

        // Corner correction
        if (rb.velocity.y > 0 && doCornerCorrection) {

            // Get the height to start at which will be 1 physics frame ahead of the boxcollider top
            float startHeight = GetComponent<BoxCollider2D>().bounds.size.y / 2 + (rb.velocity.y * Time.fixedDeltaTime) + 0.01f;
            float playerWidth = GetComponent<BoxCollider2D>().bounds.size.x;

            // Calculate ray length based on player's width
            float rayLength = playerWidth / 2 * 1.1f;

            // Perform raycasts to check for gaps on both sides of the player
            RaycastHit2D hitLeft = Physics2D.Raycast(transform.position + new Vector3(0, startHeight, 0), Vector2.left, rayLength, groundLayer);
            RaycastHit2D hitRight = Physics2D.Raycast(transform.position + new Vector3(0, startHeight, 0), Vector2.right, rayLength, groundLayer);

            // Check if there's a gap and adjust player's position accordingly
            if (hitLeft.collider == null && hitRight.collider == null)
            {
                // No gaps detected, do nothing
            } else {
                float distleft = hitLeft.collider == null ? 999 : hitLeft.distance;
                float distright = hitRight.collider == null ? 999 : hitRight.distance;
                float totalDistance = distleft + distright;
                float gapWidth = totalDistance - playerWidth;

                // Debug.Log("Left Hit: " + hitLeft.point.x);
                // Debug.Log("Right Hit: " + hitRight.point.x);
                // Debug.Log("Total Distance: " + totalDistance);
                // Debug.Log("Gap Width: " + gapWidth);

                float playerleft = transform.position.x - playerWidth / 2;
                float playerright = transform.position.x + playerWidth / 2;

                if (gapWidth >= 0)
                {
                    //print("GAP WIDE ENOUGH");
                    // check each side to see if:
                    // 1. there is a wall
                    // 2. the hit point is close enough to the player's edge
                    // 3. the hit point is still within the player's bounds (or else no correction is needed)
                    if (hitLeft.collider != null && hitLeft.point.x < playerleft + cornerCorrection && hitLeft.point.x > playerleft)
                    {
                        Debug.Log("CORNER CORRECTION ACTIVE LEFT");
                        float newPositionX = hitLeft.point.x + (playerWidth / 2 * 1.1f);
                        transform.position = new Vector3(newPositionX, transform.position.y, transform.position.z);
                    }
                    else if (hitRight.collider != null && hitRight.point.x > playerright - cornerCorrection && hitRight.point.x < playerright)
                    {
                        Debug.Log("CORNER CORRECTION ACTIVE RIGHT");
                        float newPositionX = hitRight.point.x - (playerWidth / 2 * 1.1f);
                        transform.position = new Vector3(newPositionX, transform.position.y, transform.position.z);
                    }
                }
            }

        }

        // Ceiling detection
        // TODO: Use this for hittable blocks (will fix not being able to hit the block you want)
        RaycastHit2D ceilLeft = Physics2D.Raycast(transform.position - colliderOffset, Vector2.up, ceilingLength, groundLayer);
        RaycastHit2D ceilMid = Physics2D.Raycast(transform.position, Vector2.up, ceilingLength, groundLayer);
        RaycastHit2D ceilRight = Physics2D.Raycast(transform.position + colliderOffset, Vector2.up, ceilingLength, groundLayer);

        if (ceilLeft.collider != null || ceilMid.collider != null || ceilRight.collider != null) {

            RaycastHit2D hitRay = ceilMid;

            if (ceilMid) {
                hitRay = ceilMid;
            } else if (ceilLeft) {
                hitRay = ceilLeft;
            } else if (ceilRight) {
                hitRay = ceilRight;
            }
        }

        // Wall detection (for wall jumping)
        if (canWallJump && !onGround && !swimming) {

            float raycastlength = GetComponent<BoxCollider2D>().bounds.size.y / 2 + 0.03f;

            // raycast in the direction the stick is pointing
            RaycastHit2D wallHitLeft = Physics2D.Raycast(transform.position, Vector2.left, raycastlength, groundLayer);
            RaycastHit2D wallHitRight = Physics2D.Raycast(transform.position, Vector2.right, raycastlength, groundLayer);

            if ((wallHitLeft.collider != null && direction.x < 0) || (wallHitRight.collider != null && direction.x > 0)) {
                if (!wallSliding) {
                    // flip mario to face the wall
                    FlipTo(direction.x > 0);
                    wallSliding = true;
                }
            } else {
                wallSliding = false;
            }
        } else {
            wallSliding = false;
        }

        animator.SetBool("isWallSliding", wallSliding);

        // Physics
        modifyPhysics();
    }

    void moveCharacter(float horizontal) {
        bool crouch = crouchPressed && canCrouch;

        // Crouching
        if (crouch && onGround && !carrying && !swimming) {

            // Start Crouch
            animator.SetBool("isCrouching", true);
            inCrouchState = true;

            if (powerupState != PowerupState.small) {
                GetComponent<BoxCollider2D>().size = new Vector2(GetComponent<BoxCollider2D>().size.x, 1.0f);
                GetComponent<BoxCollider2D>().offset = new Vector2(GetComponent<BoxCollider2D>().offset.x, -0.5f);
                ceilingLength = 0.1f;
            }

        } else if ((!crouch && onGround) || carrying) {

            // Stop Crouch
            animator.SetBool("isCrouching", false);
            inCrouchState = false;
            if (powerupState != PowerupState.small) {
                GetComponent<BoxCollider2D>().size = new Vector2(GetComponent<BoxCollider2D>().size.x, colliderY);
                GetComponent<BoxCollider2D>().offset = new Vector2(GetComponent<BoxCollider2D>().offset.x, collideroffsetY);
                ceilingLength = 1.03f; // NOTE: THIS IS BAD PROGRAMMING BUT WHATEVER
            }

        }

        // Running or Walking or Crawling

        // You can only crawl if you are small mario, on the ground, crouching, and not carrying anything, and not swimming
        // AND you are pressing left or right pretty hard
        // AND either you are already crawling or you are stopped
        isCrawling = inCrouchState && onGround && !carrying && !swimming && powerupState == PowerupState.small && math.abs(horizontal) > 0.5 && (math.abs(rb.velocity.x) < 0.05f || isCrawling);
        bool regularMoving = !inCrouchState || !onGround;

        if (regularMoving || isCrawling) {
            if (runPressed && !swimming && !isCrawling) {
                rb.AddForce(horizontal * runSpeed * Vector2.right);
            } else {
                if (Mathf.Abs(rb.velocity.x) <= maxSpeed) {
                    rb.AddForce(horizontal * moveSpeed * Vector2.right);
                } else {
                    rb.AddForce(Mathf.Sign(rb.velocity.x) * slowDownForce * Vector2.left);
                }
            }
        }

        // Changing Direction
        if (onGround || swimming) {
            if ((horizontal > 0 && !facingRight) || (horizontal < 0 && facingRight)) {
                Flip();
            }
            if (horizontal == 0) {
                if ((rb.velocity.x > 0 && !facingRight) || (rb.velocity.x < 0 && facingRight)) {
                    Flip();
                }
            }
        }

        // Max Speed (Horizontal)
        if (runPressed) {
            if (Mathf.Abs(rb.velocity.x) > maxRunSpeed) {
                rb.velocity = new Vector2(Mathf.Sign(rb.velocity.x) * maxRunSpeed, rb.velocity.y);
            }
        }

        // Max Speed (Vertical)
        float tvel = swimming ? swimTerminalVelocity : terminalvelocity;

        if (wallSliding) {  // slide down wall slower
            tvel /= 3;
        }

        if (-rb.velocity.y > tvel) {
            rb.velocity = new Vector2(rb.velocity.x, -tvel);
        }
        if (rb.velocity.y > (tvel*2) && swimming) { // swimming up speed limit
            rb.velocity = new Vector2(rb.velocity.x, tvel*2);
        }

        // Animation
        animator.SetFloat("Horizontal", Mathf.Abs(rb.velocity.x) * walkAnimatorSpeed);
        if (Mathf.Abs(rb.velocity.x) <= 0.5f) {
            animator.SetBool("isRunning", false);
        } else {
            animator.SetBool("isRunning", true);
        }

        if (onGround) {
            if (!inCrouchState && canSkid) {
                animator.SetBool("isSkidding", changingDirections);
            }
            animator.SetBool("onGround", true);
        } else {
            animator.SetBool("isSkidding", false);
            animator.SetBool("onGround", false);
        }

        animator.SetBool("isCrawling", isCrawling);
    }

    // for jumping and also stomping enemies
    public void Jump(float jumpMultiplier = 1f) {
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(Vector2.up * jumpSpeed * (swimming ? 0.5f : 1f) * jumpMultiplier, ForceMode2D.Impulse);
        jumpTimer = 0;
        airtimer = Time.time + (airtime * jumpMultiplier);
    }

    // for swimming
    public void Swim() {
        rb.AddForce(Vector2.up * swimForce, ForceMode2D.Impulse);
        animator.SetTrigger("swim");
        jumpTimer = 0;
    }

    // jump out of water
    public void JumpOutOfWater() {
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(.75f * jumpSpeed * Vector2.up, ForceMode2D.Impulse);
        jumpTimer = 0;
        airtimer = Time.time + airtime;

        // Instantiate the splash water prefab at the current position
        if (splashWaterPrefab != null)
        {
            Instantiate(splashWaterPrefab, transform.position, Quaternion.identity);
        }
    }

    // jumping off a wall
    public void WallJump() {
        Jump(0.75f);
        // add horizontal force in the opposite direction of the wall (where you are facing)
        int dirToSign = facingRight ? -1 : 1;
        rb.AddForce(dirToSign * Vector2.right * jumpSpeed, ForceMode2D.Impulse);
        // flip mario to face away from the wall
        FlipTo(!facingRight);
    }

    void modifyPhysics() {
        changingDirections = (direction.x > 0 && rb.velocity.x < 0) || (direction.x < 0 && rb.velocity.x > 0);

        // special swimming physics
        if (swimming) {
            rb.gravityScale = swimGravity;
            rb.drag = swimDrag;
            return;
        }

        if (onGround) {
            // no crazy crouch sliding
            if (inCrouchState)
            {
                direction = new Vector2(0, direction.y);
                animator.SetBool("isCrouching", true);
            }
            else
            {
                animator.SetBool("isCrouching", false);
            }             

            // if not holding left or right all the way or changing directions
            if (Mathf.Abs(direction.x) < 0.4f || changingDirections) {

                // if running and holding left or right
                if (runPressed && (direction.x != 0)) {
                    rb.drag = runlinearDrag;

                // if not holding left or right
                } else if (direction.x == 0) {

                    if (Mathf.Abs(rb.velocity.x) < 5f) {
                        rb.drag = linearDrag;
                    } else {
                        rb.drag = 3 * runlinearDrag;
                    }

                // if walking left or right (not all the way or changing directions)
                } else {
                    rb.drag = linearDrag;
                }

                // if holding left or right all the way and not changing directions
            } else {
                rb.drag = 0;
            }

            // 0 gravity on the ground
            rb.gravityScale = 0;

            // if falling while on the ground, set y velocity to 0
            if (rb.velocity.y < 0) {
                rb.velocity = new Vector2(rb.velocity.x, 0f);
            }

            // Stick to ground
            if (!onMovingPlatform) {
                transform.position = new Vector3(transform.position.x, groundPos.y + groundLength - 0.01f, transform.position.z);
            }


        } else {
            // in the air
            rb.gravityScale = gravity;
            rb.drag = linearDrag * 0.15f;
            //if(rb.velocity.y < startfallingspeed){

            // Rising
            if (airtimer < Time.time || rb.velocity.y < startfallingspeed) {
                rb.gravityScale = fallgravity;

                // Falling
            } else if (rb.velocity.y > 0 && !jumpPressed) {
                rb.gravityScale = fallgravity;
                airtimer = Time.time - 1f;
                //rb.gravityScale = gravity * fallMultiplier;
            }
        }
    }
    
    void Flip() {
        facingRight = !facingRight;
        //transform.rotation = Quaternion.Euler(0, facingRight ? 0 : 180, 0);
        GetComponent<SpriteRenderer>().flipX = !facingRight;
        float relScaleX = facingRight ? 1 : -1;

        // flip might be called before start, this fixes that
        if (relPosObj == null) {
            relPosObj = transform.GetChild(0).gameObject;
        }

        relPosObj.transform.localScale = new Vector3(relScaleX, 1, 1);
    }

    void FlipTo(bool right) {
        if (facingRight != right) {
            Flip();
        }
    }

    public bool IsBelowBlock(float blockYPosition)
    {
        // Check if Mario's height is below a certain point relative to the block
        return transform.position.y < blockYPosition - 0.5f; // You can adjust the value (0.5f) as needed
    }

    public void damageMario(bool force=false) {
        if (invincetimeremain == 0f) {
            if (damaged && !force) {
                return;
            }
            // are we invincible mario?
            if (starPower && !force) {
                return;
            }
            damaged = true;

            // If you comment this, the tranformIntoPig will work without instantiating the deadMario with the pigMario
            // but the player will not be harmed by the enemies, only the wizard goomba's magic attack
            if (powerupState == PowerupState.small) {
                toDead();
            } else {
                powerDown();
            }
        }
    }

    private void powerDown() {
        GameObject newMario;
        if (powerupState == PowerupState.big) {
            // go down to Small Mario
            newMario = Instantiate(powerDownMario, new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z), transform.rotation);
        } else {
            // go down to Big Mario
            newMario = Instantiate(powerDownMario, new Vector3(transform.position.x, transform.position.y, transform.position.z), transform.rotation);
        }
        
        var newMarioMovement = transferProperties(newMario);
        newMarioMovement.invincetimeremain = damageinvinctime;
        newMarioMovement.playDamageSound();

        Destroy(gameObject);
    }

    public void ChangePowerup(GameObject newMarioObject) {
        // NOTE: we will assume here that mario can always change powerups. The PowerUP.cs script will determine if mario can change powerups

        PowerupState newPowerupState = newMarioObject.GetComponent<MarioMovement>().powerupState;

        float verticalOffset = 0f;
        if (newPowerupState == PowerupState.small && powerupState != PowerupState.small) {
            verticalOffset = -0.5f;
        } else if (newPowerupState != PowerupState.small && powerupState == PowerupState.small) {
            verticalOffset = 0.5f;
        }

        GameObject newMario = Instantiate(newMarioObject, new Vector3(transform.position.x, transform.position.y + verticalOffset, transform.position.z), Quaternion.identity);
        
        transferProperties(newMario);

        Destroy(gameObject);
    }

    MarioMovement transferProperties(GameObject newMario) {
        newMario.GetComponent<Rigidbody2D>().velocity = gameObject.GetComponent<Rigidbody2D>().velocity;
        var newMarioMovement = newMario.GetComponent<MarioMovement>();

        if (!facingRight)
            newMarioMovement.Flip();

        newMarioMovement.pressRunToGrab = pressRunToGrab;
        newMarioMovement.crouchToGrab = crouchToGrab;
        newMarioMovement.carryMethod = carryMethod;
        newMarioMovement.playerNumber = playerNumber;

        
        try {
            var myDevices = GetComponent<PlayerInput>().devices;
            // set new mario's input device to the same as this mario's
            newMario.GetComponent<PlayerInput>().SwitchCurrentControlScheme(myDevices.ToArray());
        } catch {
            // this might error if only one controller is connected
            print("Could not transfer input device to new mario. This is probably fine.");
        }
        
        if (carrying) {
            // move carried object to new mario
            GameObject carriedObject = heldObjectPosition.transform.GetChild(0).gameObject;
            carriedObject.transform.parent = newMarioMovement.heldObjectPosition.transform;
            carriedObject.transform.localPosition = Vector3.zero;
            newMarioMovement.carrying = true;
        }

        // transfer pressed buttons (for mobile controls)
        newMarioMovement.crouchPressed = crouchPressed;
        newMarioMovement.jumpPressed = jumpPressed;
        newMarioMovement.runPressed = runPressed;
        newMarioMovement.moveInput = moveInput;

        return newMarioMovement;
    }

    public void playDamageSound() {
        GetComponent<AudioSource>().PlayOneShot(damageSound);
    }

    private void toDead() {
        // print("death attempt");
        if (!dead) {
            // Drop the carried object
            if (carrying)
            {
                dropCarry();
            }

            dead = true;
            GameObject newMario = Instantiate(deadMario, transform.position, transform.rotation);
            Destroy(gameObject);
            // print("death success");
        }
    }

    public void TransformIntoObject(GameObject newMario)
    {
        if (!dead)
        {
            dead = true;
            // Instantiate the pigPrefab at the current position and rotation
            GameObject m = Instantiate(newMario, transform.position, transform.rotation);
            Destroy(gameObject);
        }
    }

    public void PlayYeahAnimation()
    {
        if (!isYeahAnimationPlaying && !hasEnteredAnimationYeahTrigger)
        {
            StartCoroutine(PlayYeahAnimationWithAudio());
        }
    }

    private IEnumerator PlayYeahAnimationWithAudio()
    {
        isYeahAnimationPlaying = true;
        hasEnteredAnimationYeahTrigger = true;

        // Start playing the "yeah" animation
        animator.SetBool("yeah", true);

        // Play the audio clip
        audioSource.PlayOneShot(yeahAudioClip);

        // Wait for the audio clip to finish playing
        yield return new WaitForSeconds(yeahAudioClip.length + 0.2f);

        // Stop the "yeah" animation and set the parameter to false
        animator.SetBool("yeah", false);
        isYeahAnimationPlaying = false;
    }

    private IEnumerator SpawnBubbles()
    {
        while (true)
        {
            // Wait for the specified delay before spawning the bubble
            yield return new WaitForSeconds(bubbleSpawnDelay);

            // Instantiate the bubble prefab at the current position
            if (swimming && bubblePrefab != null)
            {
                Instantiate(bubblePrefab, transform.position, Quaternion.identity);
                // Debug.Log("Bubble created");
            }
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // firebar, flame, etc
        if (other.gameObject.tag == "Damaging")
        {
            damageMario();
        }
        // Lava
        if (other.gameObject.CompareTag("Deadly"))
        {
            toDead();
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the trigger collider is the one you want to trigger the "yeah" animation
        if (other.gameObject.CompareTag("AnimationYeah"))
        {
            PlayYeahAnimation();
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            swimming = true;
            animator.SetTrigger("enterWater");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            swimming = false;
            animator.SetTrigger("exitWater");

            // if you are moving up, you can jump out of water
            if (rb.velocity.y > 0)
            {
                JumpOutOfWater();
            }
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        // spikes
        if (collision.gameObject.CompareTag("Damaging"))
        {
            damageMario();
        }
    }

    private void OnDrawGizmos() {
        // Ground
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + colliderOffset, transform.position + colliderOffset + Vector3.down * groundLength);
        Gizmos.DrawLine(transform.position - colliderOffset, transform.position - colliderOffset + Vector3.down * groundLength);

        // Corner
        float startHeight = GetComponent<BoxCollider2D>().bounds.size.y / 2 + (rb.velocity.y * Time.fixedDeltaTime) + 0.01f;
        float playerWidth = GetComponent<BoxCollider2D>().bounds.size.x;
        float rayLength = playerWidth / 2 * 1.1f;
        Vector3 start = transform.position + new Vector3(0, startHeight, 0);

        Gizmos.color = Color.green;
        RaycastHit2D hitLeft = Physics2D.Raycast(start, Vector2.left, rayLength, groundLayer);

        if (hitLeft.collider != null)
        {
            Gizmos.color = Color.yellow;
        }

        // Draw left corner correction
        Gizmos.DrawLine(start, start + new Vector3(-rayLength, 0, 0));

        Gizmos.color = Color.cyan;
        RaycastHit2D hitRight = Physics2D.Raycast(start, Vector2.right, rayLength, groundLayer);

        if (hitRight.collider != null)
        {
            Gizmos.color = Color.yellow;
        }

        // Draw right corner correction
        Gizmos.DrawLine(start, start + new Vector3(rayLength, 0, 0));

        // Ceiling
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * ceilingLength);
        Gizmos.DrawLine(transform.position + colliderOffset, transform.position + colliderOffset + Vector3.up * ceilingLength);
        Gizmos.DrawLine(transform.position - colliderOffset, transform.position - colliderOffset + Vector3.up * ceilingLength);

        // Carry Raycast
        Gizmos.color = Color.blue;
        
        start = transform.position + new Vector3(0, grabRaycastHeight, 0);
        Gizmos.DrawLine(start, start + (facingRight ? Vector3.right : Vector3.left) * 0.6f);
    }


    // Input Actions

    // Move
    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    public void onMobileLeftPressed() {
        moveInput = new Vector2(-1, moveInput.y);
    }
    public void onMobileLeftReleased() {
        moveInput = new Vector2(0, moveInput.y);
    }
    public void onMobileRightPressed() {
        moveInput = new Vector2(1, moveInput.y);
    }
    public void onMobileRightReleased() {
        moveInput = new Vector2(0, moveInput.y);
    }
    public void onMobileUpPressed() {
        moveInput = new Vector2(moveInput.x, 1);
    }
    public void onMobileUpReleased() {
        moveInput = new Vector2(moveInput.x, 0);
    }
    public void onMobileDownPressed() {
        moveInput = new Vector2(moveInput.x, -1);
    }
    public void onMobileDownReleased() {    // might not be needed because of crouch button
        moveInput = new Vector2(moveInput.x, 0);
    }

    // Run
    public void Run(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            onRunPressed();
        }
        if (context.canceled)
        {
            onRunReleased();
        }
    }
    public void onRunPressed() {
        //print("run");
        runPressed = true;

        if (pressRunToGrab && (!crouchToGrab || crouchPressed) && !carrying) {
            checkForCarry();
        }
    }
    public void onRunReleased() {
        runPressed = false;
    }

    // Jump
    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            onJumpPressed();
        }
        if (context.canceled)
        {
            onJumpReleased();
        }     
    }
    public void onJumpPressed() {
        jumpTimer = Time.time + jumpDelay;
        jumpPressed = true;
    }
    public void onJumpReleased() {
        jumpPressed = false;
    }

    // Crouch
    public void Crouch(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            print("crouch");
            crouchPressed = true;
        }
        if (context.canceled)
        {
            print("stop crouch");
            crouchPressed = false;
        }
    }
    public void onMobileCrouchPressed() {
        crouchPressed = true;
    }
    public void onMobileCrouchReleased() {
        crouchPressed = false;
    }

    // Spin
    public void Spin(InputAction.CallbackContext context)
    {
        // TODO: Spin Jump
    }


    // Shoot
    public void Shoot(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            onShootPressed();
        }
    }
    public void onShootPressed() {
        // shoot fireball, etc
        if (!carrying && powerupState == PowerupState.power) {
            print("shoot!");
            marioAbility.shootProjectile();
        }
    }

    // Use
    public void Use(InputAction.CallbackContext context)
    {
        // use lever
        if (context.performed) {
            onUsePressed();
        }
    }
    public void onUsePressed() {
        // for right now, use the NEWEST lever we entered
        if (levers.Count > 0) {
            levers[^1].Use(this);
        }
    }

    public void Freeze() {
        // pause animations
        animator.enabled = false;
        // pause physics
        rb.simulated = false;

        frozen = true;
    }

    public void Unfreeze() {
        // unpause animations
        animator.enabled = true;
        // unpause physics
        rb.simulated = true;

        frozen = false;
    }

    void checkForCarry() {
        //print("Check carry");

        // raycast in front of feet of mario
        RaycastHit2D[] hit = Physics2D.RaycastAll(transform.position + new Vector3(0, grabRaycastHeight, 0), facingRight ? Vector2.right : Vector2.left, 0.6f);


        foreach(RaycastHit2D h in hit) {
            // if object has objectphysics script
            if (h.collider.gameObject.GetComponent<ObjectPhysics>() != null) {
                ObjectPhysics obj = h.collider.gameObject.GetComponent<ObjectPhysics>();
                // not carried and carryable
                if (!obj.carried && obj.carryable) {
                    carry(obj);
                    return;
                }
            }
        }
    }

    void carry(ObjectPhysics obj) {
        //print("carry!");
        carrying = true;

        animator.SetTrigger("grab");
        
        // set object to be child of mario's object holder
        obj.transform.parent = heldObjectPosition.transform;

        // sound
        if (pickupSound != null)
            audioSource.PlayOneShot(pickupSound);

        obj.getCarried();
    }

    public void dropCarry() {
        //print("drop!");
        carrying = false;

        animator.SetTrigger("grab");
        
        // get object from mario's object holder
        ObjectPhysics obj = heldObjectPosition.transform.GetChild(0).gameObject.GetComponent<ObjectPhysics>();
        obj.transform.parent = null;

        //obj.transform.position = new Vector3(transform.position.x + (facingRight ? 1 : -1), transform.position.y + (powerupState == PowerupState.small ? 0f : -.5f), transform.position.z);
        float halfwidth = obj.width / 2;
        float offset = powerupState == PowerupState.small ? 0f : -0.5f;
        Vector2? raycastPoint = ThrowRaycast(offset, 1f + halfwidth, obj.wallMask);
        if (raycastPoint != null) {
            obj.transform.position = (Vector2)raycastPoint + new Vector2(facingRight ? -halfwidth : halfwidth, 0);
            // move mario back (todo: mario might get stuck in a wall if he throws an object in a one block gap)
            transform.position = new Vector3(facingRight ? (raycastPoint.Value.x - obj.width - 0.5f) : (raycastPoint.Value.x + obj.width + 0.5f), transform.position.y, transform.position.z);
        } else {
            obj.transform.position = transform.position + new Vector3(facingRight ? 1 : -1, offset, 0);
        }

        // sound
        if (dropSound != null)
            audioSource.PlayOneShot(dropSound);

        obj.getDropped(facingRight);
    }

    void throwCarry() {
        //print("throw!");

        carrying = false;

        // check if heldObjectPosition has an object
        if (heldObjectPosition.transform.childCount == 0) {
            return;
        }

        // todo: throw animation

        // get object from mario's object holder
        ObjectPhysics obj = heldObjectPosition.transform.GetChild(0).gameObject.GetComponent<ObjectPhysics>();
        obj.transform.parent = null;

        float halfwidth = obj.width / 2;
        float offset = powerupState == PowerupState.small ? 0.1f : -0.1f;
        Vector2? raycastPoint = ThrowRaycast(offset, 1f + halfwidth, obj.wallMask);
        if (raycastPoint != null) {
            obj.transform.position = (Vector2)raycastPoint + new Vector2(facingRight ? -halfwidth : halfwidth, 0);
            // move mario back (todo: mario might get stuck in a wall if he throws an object in a one block gap)
            transform.position = new Vector3(facingRight ? (raycastPoint.Value.x - obj.width - 0.5f) : (raycastPoint.Value.x + obj.width + 0.5f), transform.position.y, transform.position.z);
        } else {
            obj.transform.position = transform.position + new Vector3(facingRight ? 1 : -1, offset, 0);
        }

        // sound
        if (throwSound != null)
            audioSource.PlayOneShot(throwSound);

        obj.GetThrown(facingRight);
    }

    // Raycasts from the specified vertical offset and returns the point of contact (if any)
    // TODO: maybe change it to 2 raycasts (one on top, one on bottom) to make sure that the object wont go inside a wall
    Vector2? ThrowRaycast(float offset, float distance, int layerMask) {
        layerMask &= ~(1 << gameObject.layer);  // remove mario's layer from the layermask
        Vector3 start = transform.position + new Vector3(0, offset, 0);
        RaycastHit2D hit = Physics2D.Raycast(start, facingRight ? Vector2.right : Vector2.left, distance, layerMask);

        if (hit.collider != null) {
            return hit.point;
        } else {
            return null;
        }
    }

    public void resetSpriteLibrary() {
        GetComponent<SpriteLibrary>().spriteLibraryAsset = normalSpriteLibrary;
    }

    /* Levers */
    // Levers use these to let Mario know that they are near him
    // When the Use button is pressed, Mario will activate one of these levers
    public void AddLever(LeverController lever) {
        if (!levers.Contains(lever)) {
            levers.Add(lever);
        }
    }

    public void RemoveLever(LeverController lever) {
        if (levers.Contains(lever)) {
            levers.Remove(lever);
        }
    }
}