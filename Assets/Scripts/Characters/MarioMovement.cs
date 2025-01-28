using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.U2D.Animation;
using Unity.Mathematics;
using Unity.VisualScripting;
using System.Collections.Generic;
using UnityEngine.AI;
using PowerupState = PowerStates.PowerupState;

public class MarioMovement : MonoBehaviour
{
    public int playerNumber = 0;    // 0 = player 1, 1 = player 2, etc.
    private Vector3 originalPosition;
    
    /* Input System */
    // Other scripts can access these variables to get the player's input
    // Do not use the old input system or raw keyboard input anywhere in the game
    [HideInInspector] public Vector2 moveInput; // The raw directional input from the player's controller
    [HideInInspector] public bool crouchPressed = false;
    private bool crouchPressedInAir = false;
    [HideInInspector] public bool groundPoundInWater = false;
    private float waterGroundPoundDuration = 1f; // Duración permitida en el agua
    [HideInInspector] public float waterGroundPoundStartTime;
    private float lastCancelTime = -1f; // Tracks the time of the last cancel
    private bool jumpPressed = false;
    private bool runPressed = false;
    private bool spinPressed = false;


    [Header("Horizontal Movement")]
    public float moveSpeed = 10f;
    public float runSpeed = 20f;
    public float slowDownForce = 5f;
    
    public Vector2 direction;
    public bool facingRight = true;
    private bool inCrouchState = false;
    private bool isCrawling = false;    // Currently small mario only
    private float floorAngle = 0f;  // -45 = \, 0 = _, 45 = /

    [Header("Vertical Movement")]
    public float jumpSpeed = 15f;
    public float jumpDelay = 0.25f; // How early you can press jump before landing
    public float terminalvelocity = 10f;
    public float startfallingspeed = 1f;
    private float jumpTimer;

    [Header("Components")]
    public Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Animator animator;
    public LayerMask groundLayer;
    private List<MarioAbility> abilities = new List<MarioAbility>();

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
    public bool wasGrounded = false;
    public float groundLength = 0.6f;
    public float groundSink = 0.1f; // how much the end of the ground raycast sinks into the ground
    public bool onMovingPlatform = false;
    public float ceilingLength = 0.5f;
    public bool doCornerCorrection = true; // false: disable corner correction, true: enable corner correction
    public float cornerCorrection = 0.1f; // Portion of the player's width that can overlap with the ceiling and still correct the position
    // Example: 0.1f means that if the player's width is 1, the player can overlap with the ceiling by up to 0.1 (10%) and still correct the position

    float colliderY;
    float collideroffsetY;

    // Height offset for raycasts
    Vector3 HOffset => new(0, inCrouchState ? -(groundLength / 2) : 0f, 0);

    // Total horizontal raycast offsets
    Vector3 raycastLeftOffset => new(raycastSeparation + raycastOffsetX, 0, 0);
    Vector3 raycastRightOffset => new(-raycastSeparation + raycastOffsetX, 0, 0);

    public float raycastSeparation = 0.35f; // How far apart the raycasts are from the center of the player
    public float raycastOffsetX = 0f;   // Offset for the raycasts in the x direction
    public float damageinvinctime = 3f;
    public float invincetimeremain = 0f;
    private bool flashing = false;

    public Vector2 groundPos;


    [Header("Animation Events")]
    public Vector3 animationScale = new(1, 1, 1);   // animate this instead of the scale directly
    public Vector3 originalScale;
    public float animationRotation = 0;  // If not 0, his z rotation will be set to this
    public bool wasScaledNormal = true;
    private bool isYeahAnimationPlaying = false;
    private bool hasEnteredAnimationYeahTrigger = false;

    private SpriteLibraryAsset normalSpriteLibrary;

    public bool canSkid = true;
    public bool canCrouch = true;
    public float crouchColOffset = -0.25f;
    public float crouchColHeight = 0.5f;

    public float walkAnimatorSpeed = 0.125f;

    [Header("Powerups")]
    public string currentPowerupType = "";
    public PowerupState powerupState = PowerupState.small;

    public GameObject transformMario;   // The prefab for the transformation animation
    public GameObject powerDownMario;   // The mario that he will transform into when he gets hit

    // so that you can only hurt the player once per frame
    private bool damaged = false;

    [HideInInspector]
    public bool starPower = false;
    private float starPowerRemainingTime = 0f;
    private readonly Color[] StarColors = { Color.green, Color.yellow, Color.blue, Color.red };
    private int selectedStarColor = 0;

    [Header("Death")]

    public GameObject deadMario;

    private bool dead = false;
    public bool Dead => dead;

    [Header("Sound Effects")]

    public AudioClip damageSound;
    public AudioClip bonkSound;
    public AudioClip swimSound;
    public AudioClip spinJumpSound;
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

    private float grabRaycastHeight => PowerStates.IsSmall(powerupState) ? -0.1f : -0.4f;

    private bool isLookingUp = false;

    [Header("Additional Abilities")]
    public bool canCrawl = false;       // Only small Mario has an animation for crawling right now, so it will not be transferred after powerup
    public bool canWallJump = false; 
    public bool canWallJumpWhenHoldingObject = false;
    public bool canSpinJump = false;
    [HideInInspector] public bool isCapeActive = false;
    [HideInInspector] public bool wallSliding = false;
    private bool pushing = false;
    private float pushingSpeed = 0f;
    [HideInInspector] public bool spinning = false;
    private bool spinJumpQueued = false;    // If the next jump should be a spin jump
    public bool canGroundPound = false;
    public GameObject groundPoundParticles;
    [HideInInspector] public bool groundPounding = false;
    [HideInInspector] public bool groundPoundRotating = false;
    private bool groundPoundLanded = false; // If the ground pound has landed but you are still in the animation and can't move

    // Made private to not clog inspector
    private float groundPoundSpinTime = 0.5f; // How long the player will be frozen in the air when ground pound starts

    public AudioClip spinJumpBounceSound;
    public AudioClip spinJumpPoofSound;
    public GameObject spinJumpBouncePrefab; // Spikey effect
    public GameObject spinJumpPoofPrefab;   // Puff of smoke
    public AudioClip groundPoundSound;
    public AudioClip groundPoundLandSound;
    public AudioClip capeSound;

    /* Levers */
    private List<UseableObject> useableObjects = new();

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
        originalScale = transform.lossyScale;
        GameManager.Instance.SetPlayer(this, playerNumber);
        abilities.AddRange(GetComponents<MarioAbility>());
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

        // set GLOBAL scale
        Transform myParent = transform.parent;
        transform.parent = null;
        if (animationScale != Vector3.one)
        {
            if (wasScaledNormal)
            {
                originalScale = transform.localScale;
                wasScaledNormal = false;
            }
            // set my scale to the animation scale (relative to the original scale)
            transform.localScale = Vector3.Scale(originalScale, animationScale);
        }
        transform.parent = myParent;

        // Set rotation based on animation
        // NOTE: We are not using rigidbody rotation because it can cause movement to be choppy if it is set frequently
        animationRotation = facingRight ? -Mathf.Abs(animationRotation) : Mathf.Abs(animationRotation); // Update rotation based on where the player is facing
        transform.rotation = Quaternion.Euler(0, 0, animationRotation);

        if (invincetimeremain > 0f) {
            invincetimeremain -= Time.deltaTime;
            if (!flashing) {
                StartCoroutine(FlashDuringInvincibility());
            }
        } else {
            invincetimeremain = 0f;
        }

        if (starPower && starPowerRemainingTime > 0)
        {
            starPowerRemainingTime -= Time.deltaTime;
            if (starPowerRemainingTime <= 0)
            {
                stopStarPower();
            }
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

        if (onGround){
            crouchPressedInAir = false;
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

    private void changeRainbowColor()
    {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.color = StarColors[selectedStarColor];
            selectedStarColor = (selectedStarColor + 1) % StarColors.Length;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} is missing a SpriteRenderer. Cannot change rainbow color.");
        }
    }

    public void startStarPower(float time) {
        // stop any current star power
        CancelInvoke(nameof(stopStarPower));
        CancelInvoke(nameof(changeRainbowColor));

        // start new star power
        InvokeRepeating(nameof(changeRainbowColor), 0, 0.1f);
        starPower = true;
        starPowerRemainingTime = time;
        if (time != -1) {
            Invoke(nameof(stopStarPower), time);
        }
    }

    public void stopStarPower() {
        CancelInvoke(nameof(changeRainbowColor));
        starPower = false;
        starPowerRemainingTime = 0f; // Reset remaining time

        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.color = new Color(1, 1, 1, sprite.color.a);
        }
        else
        {
            Debug.LogWarning("No SpriteRenderer found when stopping star power.");
        }
    }

    private void FixedUpdate() 
    {
        if (frozen) {
            return;
        }

        // Movement
        MoveCharacter(direction.x);

        // Jumping/Swimming
        bool jumpBlocked = false;
        foreach (MarioAbility ability in abilities) {
            if (ability.isBlockingJump) {
                jumpBlocked = true;
            }
        }
        if (Time.time < jumpTimer && (onGround || swimming || wallSliding) && !jumpBlocked && !groundPoundLanded) {

            if (swimming) {
                if (!groundPounding){
                    audioSource.PlayOneShot(swimSound);
                    Swim();
                }              
            } else if (wallSliding) {
                audioSource.Play();
                WallJump();
            } else if (spinJumpQueued) {
                SpinJump();
            } else {
                audioSource.Play();
                Jump();
            }
        }

        // Ground Pound Cancel
        if (groundPounding && !groundPoundRotating && direction.y > 0)
        {
            CancelGroundPound();
        }

        // Ground Pound
        if (canGroundPound && !onGround && crouchPressedInAir && !groundPounding && !wallSliding) {
            GroundPound();
        }

        bool wasInAir = !onGround;   // store if mario was in the air last frame

        RaycastHit2D? hitRayMaybe = CheckGround();

        bool wasOnMovingPlatform = onMovingPlatform;

        if (onGround && hitRayMaybe != null) {

            RaycastHit2D hitRay = (RaycastHit2D)hitRayMaybe;

            bool firstFrameMovingPlatform = false;
            if (hitRay.transform.gameObject.tag == "MovingPlatform") {
                if (!onMovingPlatform) {
                    firstFrameMovingPlatform = true;
                }
                onMovingPlatform = true;
            }

            groundPos = hitRay.point;

            if (onMovingPlatform) {
                //onMovingPlatform = true;
                //transform.parent = hitRay.transform;
                if (transform.parent != hitRay.transform) 
                {
                    transform.parent = hitRay.transform; // Only set parent if not already the moving platform
                }
            } else {
                /*if (wasOnMovingPlatform) {
                    // Transfer momentum to mario
                    TransferMovingPlatformMomentum();
                }
                onMovingPlatform = false;
                transform.parent = null;*/

                if (wasOnMovingPlatform && transform.parent != null && transform.parent.CompareTag("MovingPlatform")) 
                {
                    // Transfer momentum to mario only when detaching from a moving platform
                    TransferMovingPlatformMomentum();
                    transform.parent = null;
                }
                onMovingPlatform = false;
            }

            // Slope detection
            float newAngle;
            GameObject groundObject = hitRay.transform.gameObject;
            if (groundObject.CompareTag("Slope") && groundObject.TryGetComponent(out Slope slope)) {
                newAngle = slope.angle;
            } else {
                newAngle = 0f;
            }

            Vector2 slopeVector = new Vector2(Mathf.Cos(newAngle * Mathf.Deg2Rad), Mathf.Sin(newAngle * Mathf.Deg2Rad)).normalized;

            // if the angle has changed, change mario's velocity to match the slope
            if (newAngle != floorAngle && !wasInAir) {
                float moveMag = rb.velocity.magnitude;
                if (rb.velocity.x < 0) {
                    moveMag *= -1;
                }
                rb.velocity = slopeVector * moveMag;
            }

            // If landing on a slope, also change mario's velocity so he doesn't slide down the slope immediately
            // TODO: Make this not slow you down if you jump onto a slope while moving downhill
            if (newAngle != 0 && wasInAir) {
                float moveMag = rb.velocity.x;
                rb.velocity = slopeVector * moveMag;
            }

            floorAngle = newAngle;

            // Stick to ground (but not moving platforms, unless it's the first frame on the platform)
            if (!onMovingPlatform || firstFrameMovingPlatform) {
                transform.position = new Vector3(transform.position.x, groundPos.y + groundLength - groundSink, transform.position.z);    // Modified from 0.01f to 0.1f
            }

            // remove any off-the-ground velocity (accounting for slope)
            // https://stackoverflow.com/questions/72494915/how-do-you-get-the-component-of-a-vector-in-the-direction-of-a-ray
            rb.velocity = Vector2.Dot(rb.velocity, slopeVector) * slopeVector;

            if (hitRay.transform.gameObject.tag == "Damaging") {
                damageMario();
            }

            // Stop spinning, stop ground pounding
            spinning = false;
            if (groundPounding && !groundPoundLanded) {
                GroundPoundLand(hitRay.transform.gameObject);
            }

        } else {
            /*if (wasOnMovingPlatform) {
                // Transfer momentum to mario
                TransferMovingPlatformMomentum();
            }*/
            if (wasOnMovingPlatform && transform.parent != null && transform.parent.CompareTag("MovingPlatform")) 
            {
                // Transfer momentum to mario only when detaching from a moving platform
                TransferMovingPlatformMomentum();
                transform.parent = null;
            }
            onMovingPlatform = false;
            //transform.parent = null;
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
        float updCeilingLength = inCrouchState ? ceilingLength / 2 : ceilingLength;
        RaycastHit2D ceilLeft = Physics2D.Raycast(transform.position + raycastLeftOffset + HOffset, Vector2.up, updCeilingLength, groundLayer);
        RaycastHit2D ceilMid = Physics2D.Raycast(transform.position + HOffset, Vector2.up, ceilingLength, groundLayer);
        RaycastHit2D ceilRight = Physics2D.Raycast(transform.position + raycastRightOffset + HOffset, Vector2.up, updCeilingLength, groundLayer);

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
        if (canWallJump && !onGround && !swimming && (!carrying || canWallJumpWhenHoldingObject)) {

            float raycastlength = GetComponent<BoxCollider2D>().bounds.size.y / 2 + 0.03f;

            // raycast in the direction the stick is pointing
            RaycastHit2D wallHitLeft = Physics2D.Raycast(transform.position, Vector2.left, raycastlength, groundLayer);
            RaycastHit2D wallHitRight = Physics2D.Raycast(transform.position, Vector2.right, raycastlength, groundLayer);

            if ((wallHitLeft.collider != null && direction.x < 0) || (wallHitRight.collider != null && direction.x > 0) && !pushing) {
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
        animator.SetBool("isSpinning", spinning);
        animator.SetBool("isLookingUp", isLookingUp);

        // Look Up
        if (!isMoving && direction.y > 0.8f)
        {
            // Set the flag to true to indicate that the player is looking up
            isLookingUp = true;
        }
        else
        {
            // Reset the flag to false if the player is not looking up
            isLookingUp = false;
        }

        if (cameraFollow != null)
        {
            if (isLookingUp)
            {
                cameraFollow.StartCameraMoveUp();
            }
            else
            {
                cameraFollow.StopCameraMoveUp();
            }
        }

        // Physics
        ModifyPhysics();
    }

    private void TransferMovingPlatformMomentum() {
        if (onMovingPlatform) {
            // TODO!! Somehow group all kinds of moving platforms together so we don't need to check for each kind
            // Using an Interface for all moving platforms seems like a good solution
            // For now, just check for each kind of moving platform
            if (transform.parent == null) {
                print("HMM... onMovingPlatform is true but transform.parent is null");
                return;
            }
            if (transform.parent.GetComponent<MovingPlatform>() != null) {
                rb.velocity += transform.parent.GetComponent<MovingPlatform>().velocity;
            } else if (transform.parent.GetComponent<ConveyorBelt>() != null) {
                rb.velocity += transform.parent.GetComponent<ConveyorBelt>().velocity;
            }
        }
    }

    public RaycastHit2D? CheckGround() {
        // Vertical raycast offset (based on crouch state)
        float updGroundLength = inCrouchState ? groundLength / 2 : groundLength;

        // Floor detection
        RaycastHit2D groundHit1 = Physics2D.Raycast(transform.position + raycastLeftOffset + HOffset, Vector2.down, updGroundLength, groundLayer);
        RaycastHit2D groundHit2 = Physics2D.Raycast(transform.position + raycastRightOffset + HOffset, Vector2.down, updGroundLength, groundLayer);

        onGround = (groundHit1 || groundHit2) && (rb.velocity.y <= 0.01f || onGround);

        if (onGround) {
            RaycastHit2D hitRay = groundHit1;

            // print("ground1: " + groundHit1.transform.gameObject.tag);
            // print("ground2: " + groundHit2.transform.gameObject.tag);

            // instead, choose the higher of the two ground hits
            if (groundHit1 && groundHit2) {
                hitRay = groundHit1.point.y > groundHit2.point.y ? groundHit1 : groundHit2;
            } else if (groundHit1) {
                hitRay = groundHit1;
            } else if (groundHit2) {
                hitRay = groundHit2;
            }
            return hitRay;
        }
        return null;
    }

    void MoveCharacter(float horizontal) {
        bool crouch = crouchPressed && canCrouch;

        // Crouching
        if (crouch && onGround && !carrying && !swimming) {

            // Start Crouch
            animator.SetBool("isCrouching", true);
            inCrouchState = true;
            GetComponent<BoxCollider2D>().size = new Vector2(GetComponent<BoxCollider2D>().size.x, crouchColHeight);
            GetComponent<BoxCollider2D>().offset = new Vector2(GetComponent<BoxCollider2D>().offset.x, crouchColOffset);

        } else if ((!crouch && onGround) || carrying) {

            // Stop Crouch
            animator.SetBool("isCrouching", false);
            inCrouchState = false;
            GetComponent<BoxCollider2D>().size = new Vector2(GetComponent<BoxCollider2D>().size.x, colliderY);
            GetComponent<BoxCollider2D>().offset = new Vector2(GetComponent<BoxCollider2D>().offset.x, collideroffsetY);

        }

        // Running or Walking or Crawling

        // You can only crawl if you are small mario, on the ground, crouching, and not carrying anything, and not swimming, and if canCrawl is true;
        // AND you are pressing left or right pretty hard
        // AND either you are already crawling or you are stopped
        isCrawling = inCrouchState && onGround && !carrying && !swimming && powerupState == PowerupState.small && canCrawl
                     && math.abs(horizontal) > 0.5 && (math.abs(rb.velocity.x) < 0.05f || isCrawling);
        bool regularMoving = (!inCrouchState || !onGround) && !groundPounding;

        // use the angle of the slope instead of Vector2.right
        Vector2 moveDir = onGround ? new Vector2(Mathf.Cos(floorAngle * Mathf.Deg2Rad), Mathf.Sin(floorAngle * Mathf.Deg2Rad)) : Vector2.right;

        //print("moving in " + moveDir);
        if (regularMoving || isCrawling) {
            //print("regular moving");
            if (runPressed && !swimming && !isCrawling) {
                // Running
                rb.AddForce(horizontal * runSpeed * moveDir);
            } else {
                // Walking
                if (Mathf.Abs(rb.velocity.x) <= maxSpeed) {
                    rb.AddForce(horizontal * moveSpeed * moveDir);
                } else {
                    rb.AddForce(Mathf.Sign(rb.velocity.x) * slowDownForce * -moveDir);
                }
            }
        }

        // Prevent flipping during cape attack
        if (!isCapeActive)
        {
            if (onGround || swimming)
            {
                if ((horizontal > 0 && !facingRight) || (horizontal < 0 && facingRight))
                {
                    Flip();
                }
                if (horizontal == 0) 
                {
                    if ((rb.velocity.x > 0 && !facingRight) || (rb.velocity.x < 0 && facingRight))
                    {
                        Flip();
                    }
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
        if (!onGround) {
            // Terminal Velocity
            float tvel = swimming ? swimTerminalVelocity : terminalvelocity;

            if (wallSliding) {  // slide down wall slower
                tvel /= 3;
            }
            if (groundPounding) {  // fall faster during ground pound
                tvel *= 1.5f;
            }

            if (-rb.velocity.y > tvel) {
                rb.velocity = new Vector2(rb.velocity.x, -tvel);
            }
            if (rb.velocity.y > (tvel*2) && swimming) { // swimming up speed limit
                rb.velocity = new Vector2(rb.velocity.x, tvel*2);
            }
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
        onGround = false;
        jumpTimer = 0;
        airtimer = Time.time + (airtime * jumpMultiplier);
    }

    // for swimming
    public void Swim() {
        if (groundPounding) return; 

        onGround = false;
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

    // Spin Jump
    public void SpinJump() {
        audioSource.PlayOneShot(spinJumpSound);
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(Vector2.up * jumpSpeed * 1.1f, ForceMode2D.Impulse);
        onGround = false;
        jumpTimer = 0;
        airtimer = Time.time + (airtime * 0.3f);
        spinning = true;
    }

    // Called from enemy script when mario spin bounces on an enemy
    public void SpinJumpBounce(GameObject enemy) {
        audioSource.PlayOneShot(spinJumpBounceSound);
        // Instantiate the spin jump bounce effect where they are colliding
        Vector3 effectSpawnPos = enemy.GetComponentInChildren<Collider2D>().ClosestPoint(transform.position);
        Instantiate(spinJumpBouncePrefab, effectSpawnPos, Quaternion.identity);
        Jump(1f);
    }

    public void SpinJumpPoof(GameObject enemy) {
        audioSource.PlayOneShot(spinJumpPoofSound);
        // Instantiate the spin jump poof effect where they are colliding
        Vector3 effectSpawnPos = enemy.GetComponentInChildren<Collider2D>().ClosestPoint(transform.position);
        Instantiate(spinJumpPoofPrefab, effectSpawnPos, Quaternion.identity);
        // Destroy the enemy
        Destroy(enemy);
        // If we are not holding the jump or spin button, the bounce height is reduced
        float jumpMultiplier = jumpPressed || spinPressed ? 1f : 0.3f;
        Jump(jumpMultiplier);
    }

    private void GroundPound() {
        spinning = false;   // No longer spinning
        groundPounding = true;
        groundPoundRotating = true;

        // Freeze mario in the air for a bit
        rb.velocity = new Vector2(0, 0);
    
        // Start the ground pound animation
        animator.SetBool("isDropping", true);
        animator.SetBool("cancelDropping", false);

        // Play the ground pound sound
        audioSource.PlayOneShot(groundPoundSound);

        // Start the ground pound fall after a short delay
        Invoke(nameof(GroundPoundFall), groundPoundSpinTime);
    }

    private void GroundPoundFall() {
        if (!groundPounding) return; // Skip if ground pound is canceled
        
        // Start the ground pound fall
        groundPoundRotating = false;
        rb.velocity = new Vector2(rb.velocity.x, -jumpSpeed * 1.5f);
    }

    private void GroundPoundLand(GameObject hitObject) {
        //groundPounding = false;
        groundPoundLanded = true;
        groundPoundRotating = false;
        groundPoundInWater = false; 
        waterGroundPoundStartTime = 0f; // Reset timer
        audioSource.PlayOneShot(groundPoundLandSound);
        IGroundPoundable groundPoundable = hitObject.GetComponent<IGroundPoundable>();
        if (groundPoundable != null) {
            groundPoundable.OnGroundPound(this);
        }

        if (groundPoundParticles != null) {
            Vector3 particlePosition = new Vector3(transform.position.x, transform.position.y - (colliderY / 2), transform.position.z);
            Instantiate(groundPoundParticles, particlePosition, Quaternion.identity);
        }

        animator.SetBool("isDropping", false);
        animator.SetBool("cancelDropping", false);

        // Wait a bit before finishing the ground pound
        Invoke(nameof(FinishGroundPoundLand), 0.25f);
    }

    private void FinishGroundPoundLand() {
        groundPounding = false;
        groundPoundLanded = false;

        // start swim idle if you're swimming when the ground pound lands
        if (swimming)
        {
            animator.SetTrigger("enterWater");
        }
    }

    private void CancelGroundPound()
    {
        if (!groundPounding || groundPoundRotating) // Only cancel during the fall phase
        return;

        groundPounding = false;  // Exit ground pound state
        groundPoundRotating = false;  // Stop rotation effect
        crouchPressedInAir = false;
        groundPoundInWater = false;
        waterGroundPoundStartTime = 0f;

        // Allow normal air movement
        rb.gravityScale = gravity;

        // Play cancel animation or sound if needed
        animator.SetBool("cancelDropping", true);
        animator.SetBool("isDropping", false);

        if (swimming){
            animator.SetTrigger("enterWater");
        }
    }

    public void StopGroundPound()
    {
        // Cancel any pending ground pound actions
        CancelInvoke(nameof(GroundPoundFall));

        // Reset ground pound states
        groundPounding = false;
        groundPoundRotating = false;
        groundPoundInWater = false;
        waterGroundPoundStartTime = 0f;

        // Reset animations
        animator.SetBool("isDropping", false);

        // Reset physics
        rb.velocity = Vector2.zero; // Clear velocity
        rb.gravityScale = gravity; // Reset gravity to normal

        // Reset input flags (to prevent lingering input re-triggering the ground pound)
        crouchPressedInAir = false; 
        spinPressed = false;
    }

    void ModifyPhysics() {
        changingDirections = (direction.x > 0 && rb.velocity.x < 0) || (direction.x < 0 && rb.velocity.x > 0);

        // Special ground pound physics
        if (groundPounding) {
            rb.drag = 0;
            if (groundPoundRotating) {
                rb.gravityScale = 0; // Freeze during rotation phase
                rb.velocity = new Vector2(0, 0);
            } else {
                rb.gravityScale = gravity; // Normal gravity during fall phase
            }
            return;
        }

        // special swimming physics
        if (swimming) {
            rb.gravityScale = swimGravity;
            rb.drag = swimDrag;
            return;
        }

        // Special pushing physics
        animator.SetBool("isPushing", pushing);
        if (pushing && !changingDirections) {
            int pushDir = facingRight ? 1 : -1;
            rb.velocity = new Vector2(pushingSpeed * pushDir, rb.velocity.y);
            if (onGround) {
                print("I'm on the ground!!");
                // Fix for falling inside moving platforms while pushing
                rb.gravityScale = 0;
                rb.drag = 0;
            } else {
                rb.gravityScale = fallgravity;
                rb.drag = linearDrag * 0.15f;
            }
            return;
        }  

        Vector2 physicsInput = direction;   // So we can modify it without changing the direction variable

        if (onGround) { // Regular physics for air or ground
            // no crazy crouch sliding (but do allow crawling)
            if (inCrouchState)
            {
                physicsInput = new Vector2(0, direction.y);
                animator.SetBool("isCrouching", true);
            }
            else
            {
                animator.SetBool("isCrouching", false);
            }             

            // if not holding left or right all the way or changing directions
            if (Mathf.Abs(physicsInput.x) < 0.4f || changingDirections) {

                // if running and holding left or right
                if (runPressed && (physicsInput.x != 0)) {
                    rb.drag = runlinearDrag;

                // if not holding left or right
                } else if (physicsInput.x == 0) {

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

        } else {
            // in the air
            rb.gravityScale = gravity;
            rb.drag = linearDrag * 0.15f;
            //if(rb.velocity.y < startfallingspeed){

            // Rising
            if (airtimer < Time.time || rb.velocity.y < startfallingspeed) {
                rb.gravityScale = fallgravity;

            // Falling
            } else if (rb.velocity.y > 0 && !(jumpPressed || (spinPressed && spinning))) {
                rb.gravityScale = fallgravity;
                airtimer = Time.time - 1f;
                //rb.gravityScale = gravity * fallMultiplier;
            }
        }
    }
    
    void Flip() {
        if (groundPounding || groundPoundRotating)
        {
            return; // Prevent flipping during ground pound phases
        }

        facingRight = !facingRight;
        //transform.rotation = Quaternion.Euler(0, facingRight ? 0 : 180, 0);
        if (sprite) {
            sprite.flipX = !facingRight;
        } else {
            // If flip is called before start, it might not be assigned yet
            // So we assign it here
            sprite = GetComponent<SpriteRenderer>();
            if (sprite) {
                sprite.flipX = !facingRight;
            }
        }
        float relScaleX = facingRight ? 1 : -1;

        // flip might be called before start, this fixes that
        if (relPosObj == null) {
            relPosObj = transform.GetChild(0).gameObject;
        }

        relPosObj.transform.localScale = new Vector3(relScaleX, 1, 1);
    }

    public void FlipTo(bool right) {
        if (facingRight != right) {
            Flip();
        }
    }

    public bool IsBelowBlock(float blockYPosition)
    {
        // Check if Mario's height is below a certain point relative to the block
        return transform.position.y < blockYPosition - 0.5f; // You can adjust the value (0.5f) as needed
    }

    private IEnumerator FlashDuringInvincibility()
    {
        flashing = true;
        float flashSpeed = 1/20f;   // 20 fps flash speed
        while (invincetimeremain > 0f)
        {
            // Reduce alpha to half
            sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 0.5f);
            yield return new WaitForSeconds(flashSpeed);

            // Restore alpha to full
            sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 1f);
            yield return new WaitForSeconds(flashSpeed);
        }

        // Ensure visibility at the end
        sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 1f);
        sprite.enabled = true;
        flashing = false;
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
            if (PowerStates.IsSmall(powerupState)) {
                toDead();
            } else {
                powerDown();
            }
        }
    }

    private void powerDown() {
        invincetimeremain = damageinvinctime;

        GameObject newMario = Instantiate(transformMario, transform.position, Quaternion.identity);
        
        var newMarioMovement = transferProperties(newMario);
        PlayerTransformation playerTransformation = newMario.GetComponent<PlayerTransformation>();
        
        newMarioMovement.playDamageSound();
        playerTransformation.oldPlayer = gameObject;
        playerTransformation.newPlayer = powerDownMario;
        playerTransformation.startTransformation();

        Destroy(gameObject);
    }

    public void ChangePowerup(GameObject newMarioObject) {
        // NOTE: we will assume here that mario can always change powerups. The PowerUP.cs script will determine if mario can change powerups
        GameObject newMario = Instantiate(transformMario, transform.position, Quaternion.identity);
        transferProperties(newMario);

        var newMarioMovement = newMario.GetComponent<MarioMovement>();
        var playerTransformation = newMario.GetComponent<PlayerTransformation>();

        // Update the current power-up type
        newMarioMovement.currentPowerupType = newMarioObject.GetComponent<MarioMovement>().currentPowerupType;

        playerTransformation.oldPlayer = gameObject;
        playerTransformation.newPlayer = newMarioObject;
        playerTransformation.startTransformation();

        Destroy(gameObject);
    }

    public MarioMovement transferProperties(GameObject newMario) {
        newMario.GetComponent<Rigidbody2D>().velocity = gameObject.GetComponent<Rigidbody2D>().velocity;
        var newMarioMovement = newMario.GetComponent<MarioMovement>();

        // Transfer the parent relationship
        if (transform.parent != null)
        {
            newMario.transform.SetParent(transform.parent, true); // Maintain the parent's relationship
        }

        newMarioMovement.FlipTo(facingRight);

        newMarioMovement.pressRunToGrab = pressRunToGrab;
        newMarioMovement.crouchToGrab = crouchToGrab;
        newMarioMovement.carryMethod = carryMethod;
        newMarioMovement.playerNumber = playerNumber;
        newMarioMovement.invincetimeremain = invincetimeremain;

        if (starPower)
        {
            newMarioMovement.starPower = true;
            newMarioMovement.starPowerRemainingTime = starPowerRemainingTime;

            newMarioMovement.InvokeRepeating(nameof(changeRainbowColor), 0, 0.1f);

            if (starPowerRemainingTime > 0)
            {
                newMarioMovement.Invoke(nameof(stopStarPower), starPowerRemainingTime);
            }
        }

        try
        {
            var myDevices = GetComponent<PlayerInput>().devices;
            // set new mario's input device to the same as this mario's
            newMario.GetComponent<PlayerInput>().SwitchCurrentControlScheme(myDevices.ToArray());
        } catch {
            // this might error if only one controller is connected
            print("Could not transfer input device to new mario. This is probably fine.");
        }
        
        if (carrying && heldObjectPosition.transform.childCount > 0) {
            // We need to check if it actually exists because it might be a bomb that exploded while we were holding it
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

        // Set crouchPressed to false to ensure new character doesn't start crouching
        newMarioMovement.crouchPressed = false;

        // Set additional abilities to new Mario
        newMarioMovement.canCrawl = canCrawl;
        newMarioMovement.canWallJump = canWallJump;
        newMarioMovement.canWallJumpWhenHoldingObject = canWallJumpWhenHoldingObject;
        newMarioMovement.canSpinJump = canSpinJump;
        newMarioMovement.canGroundPound = canGroundPound;

        // Passing the sound effects
        newMarioMovement.yeahAudioClip = yeahAudioClip;

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
            StartCoroutine(InvokeDeath());
            // print("death success");
        }
    }

    private IEnumerator InvokeDeath()
    {
        yield return null; // Wait until the next frame
        Instantiate(deadMario, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    public void TransformIntoObject(GameObject newMario)
    {
        // Check for invincibility
        if (invincetimeremain > 0f || starPower)
        {
            Debug.Log("Mario is invincible. Transformation ignored.");
            return;
        }

        if (!dead)
        {
            // Drop the carried object
            if (carrying)
            {
                dropCarry();
            }

            dead = true;
            StartCoroutine(InvokeTransformationWithDelay(newMario));
        }
    }

    private IEnumerator InvokeTransformationWithDelay(GameObject newMario)
    {
        yield return null; // Wait for the next frame

        // Instantiate the new object at the current position and rotation
        GameObject m = Instantiate(newMario, transform.position, Quaternion.identity);

        // Flip the object based on the current facing direction
        m.GetComponent<SpriteRenderer>().flipX = !facingRight;
        // Adjust the x velocity based on facing direction
        m.GetComponent<DeadMario>().velocity.x = facingRight ? Mathf.Abs(m.GetComponent<DeadMario>().velocity.x) : -Mathf.Abs(m.GetComponent<DeadMario>().velocity.x);

        // Destroy the current GameObject
        Destroy(gameObject);
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

        if (yeahAudioClip != null)
        {
            // Play the audio clip
            audioSource.PlayOneShot(yeahAudioClip);

            // Wait for the audio clip to finish playing
            yield return new WaitForSeconds(yeahAudioClip.length + 0.2f);
        }
        else
        {
            yield return new WaitForSeconds(0.7f);
        }

        // Stop the "yeah" animation and set the parameter to false
        animator.SetBool("yeah", false);
        isYeahAnimationPlaying = false;
    }

    public void PlayCapeSound()
    {
        if (capeSound != null)
        {
            // Play the audio clip
            audioSource.PlayOneShot(capeSound);
        }
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
        DetectDamagingObject(other);

        if (other.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            if (groundPounding && !groundPoundInWater)
            {
                // If the ground pound started in the water, alllow it with a time limit
                groundPoundInWater = true;
                waterGroundPoundStartTime = Time.time;
            }

            if (groundPounding && !groundPoundRotating && groundPoundInWater)
            {
                // Cancel the ground pound if it exceeds the ground pound time allowed in the water
                if (Time.time - waterGroundPoundStartTime > waterGroundPoundDuration)
                {
                    CancelGroundPound();
                }
            }
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
            spinning = false;
            swimming = true;
            waterGroundPoundStartTime = 0f;
            animator.SetTrigger("enterWater");

            if (groundPounding)
            {
                // If player enters the water during a ground pound started in the air, cancel it
                CancelGroundPound();
                groundPoundInWater = false;
            }
        }

        DetectDamagingObject(other);
    }

    private void DetectDamagingObject(Collider2D other)
    {
        if (other.gameObject.tag == "Damaging")
        {
            damageMario();
        }
        if (other.gameObject.CompareTag("Deadly"))
        {
            toDead();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            // Check if still in water before setting swimming to false
            Collider2D[] overlappingColliders = Physics2D.OverlapCircleAll(transform.position, 0.1f);
            bool stillInWater = false;

            foreach (var collider in overlappingColliders)
            {
                if (collider.gameObject.layer == LayerMask.NameToLayer("Water"))
                {
                    stillInWater = true;
                    break;
                }
            }

            if (!stillInWater)
            {
                swimming = false;
                animator.SetTrigger("exitWater");

                // Reset water-specific ground pound state
                groundPoundInWater = false;
                waterGroundPoundStartTime = 0f;

                if (rb.velocity.y > 0 && !groundPounding)
                {
                    JumpOutOfWater();
                }
                else if (groundPounding)
                {
                    CancelGroundPound();
                }
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
        // if this script is disabled, don't draw gizmos
        if (!enabled) {
            return;
        }

        // Vertical raycast offset (based on crouch state)
        float raycastHeight = inCrouchState ? -(groundLength / 2) : 0f;
        float updGroundLength = inCrouchState ? groundLength / 2 : groundLength;
        float updCeilingLength = inCrouchState ? ceilingLength / 2 : ceilingLength;
        Vector3 HOffset = new(0, raycastHeight, 0);

        // Ground
        Gizmos.color = Color.red;
        Vector3 startpos = transform.position + raycastLeftOffset + HOffset;
        Gizmos.DrawLine(startpos, startpos + Vector3.down * updGroundLength);
        startpos = transform.position + raycastRightOffset + HOffset;
        Gizmos.DrawLine(startpos, startpos + Vector3.down * updGroundLength);

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
        startpos = transform.position + raycastLeftOffset + HOffset;
        Gizmos.DrawLine(startpos, startpos + Vector3.up * updCeilingLength);
        startpos = transform.position + HOffset;
        Gizmos.DrawLine(startpos, startpos + Vector3.up * ceilingLength);
        startpos = transform.position + raycastRightOffset + HOffset;
        Gizmos.DrawLine(startpos, startpos + Vector3.up * updCeilingLength);


        // Wall
        Gizmos.color = Color.magenta;
        float raycastlength = GetComponent<BoxCollider2D>().bounds.size.y / 2 + 0.03f;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left * raycastlength);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * raycastlength);

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
        spinJumpQueued = false;
    }
    public void onJumpReleased() {
        jumpPressed = false;
    }

    // Crouch
    public void Crouch(InputAction.CallbackContext context)
    {
        if (context.started){
            if (!onGround)
            {
                crouchPressedInAir = true; // Set if crouch started while in the air
            }
        }
        if (context.performed)
        {
            crouchPressed = true;
        }
        if (context.canceled)
        {
            crouchPressed = false;
        }
    }
    public void onMobileCrouchPressed() {
        if (!onGround) {
            crouchPressedInAir = true;
        }
        crouchPressed = true;
    }
    public void onMobileCrouchReleased() {
        crouchPressed = false;
    }

    // Spin
    public void Spin(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            onSpinPressed();
        }
        if (context.canceled)
        {
            onSpinReleased();
        }
    }

    public void onSpinPressed() {
        if (!canSpinJump) return;

        print("spin!");
        jumpTimer = Time.time + jumpDelay;
        spinPressed = true;
        spinJumpQueued = true;
    }

    public void onSpinReleased() {
        spinPressed = false;
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
        if (useableObjects.Count > 0) {
            useableObjects[^1].Use(this);
        }
    }

    // MarioAbility Actions
    // Shoot
    public void Shoot(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            onShootPressed();
        }
    }
    public void onShootPressed() {
        foreach (MarioAbility ability in abilities)
        {
            ability.onShootPressed();
        }
    }

    // ExtraAction
    public void ExtraAction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            onExtraActionPressed();
        }
    }
    public void onExtraActionPressed() {
        foreach (MarioAbility ability in abilities)
        {
            ability.onExtraActionPressed();
        }
    }

    public void Freeze() {
        // pause animations
        animator.enabled = false;
        // pause physics
        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic; 

        frozen = true;
    }

    public void Unfreeze() {
        // unpause animations
        animator.enabled = true;
        // unpause physics
        rb.bodyType = RigidbodyType2D.Dynamic; 

        frozen = false;
    }

    void checkForCarry() {
        //print("Check carry");

        if (dead) {
            return; // Fixes issue of picking an object back up on the frame you die
        }

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

    /* Useable Objects */
    // Objects like levers use these to let Mario know that they are near him
    // When the Use button is pressed, Mario will activate one of these objects
    public void AddUseableObject(UseableObject obj) {
        if (!useableObjects.Contains(obj)) {
            useableObjects.Add(obj);
        }
    }

    public void RemoveUseableObject(UseableObject obj) {
        if (useableObjects.Contains(obj)) {
            useableObjects.Remove(obj);
        }
    }

    /* Pushing */
    // Pushable objects use these to let Mario know that he is pushing them
    public void StartPushing(float speed) {
        pushing = true;
        pushingSpeed = speed;
    }

    public void StopPushing() {
        pushing = false;
    }
}