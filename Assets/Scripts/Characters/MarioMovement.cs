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
using System;

public class MarioMovement : MonoBehaviour
{
    public int playerNumber = 0;    // 0 = player 1, 1 = player 2, etc.
    private Vector3 originalPosition;

    /* Input System */
    // Other scripts can access these variables to get the player's input
    // Do not use the old input system or raw keyboard input anywhere in the game
    public bool inputLocked = false;
    [HideInInspector] public Vector2 moveInput; // The raw directional input from the player's controller
    private const float lowerDeadzone = 0.3f; // The lower limit of the deadzone
    private const float upperDeadzone = 0.9f; // The upper limit of the deadzone
    [HideInInspector] public bool groundPoundInWater = false;
    private float waterGroundPoundDuration = 1f; // Duración permitida en el agua
    [HideInInspector] public float waterGroundPoundStartTime;
    private float lastCancelTime = -1f; // Tracks the time of the last cancel
    private bool jumpPressed = false;
    private bool runPressed = false;
    private bool spinPressed = false;
    private bool shootPressed = false;

    [Header("Horizontal Movement")]
    public float moveSpeed = 10f;
    public float runSpeed = 20f;
    public float slowDownForce = 5f;

    public Vector2 direction;
    public bool facingRight = true;
    private bool inCrouchState = false;
    private bool isCrawling = false; // Currently small mario only
    private float floorAngle = 0f; // -45 = \, 0 = _, 45 = /

    [Header("Vertical Movement")]
    public float jumpSpeed = 11f; // Standing jump speed (4 block jump)
    public float walkJumpSpeed = 12f; // Jump speed when moving at least a little (5 block jump)
    public float walkJumpSpeedRequired = 1f; // How fast you need to be moving to get the walk jump speed
    public float jumpDelay = 0.25f; // How early you can press jump before landing
    public float terminalvelocity = 10f;
    public float startfallingspeed = 1f; // If you are moving slower than this, you will start falling (stops you from floating in the air)
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
    public float riseGravity = 0;
    public float peakGravity = 1f;  // Gravity at the top of your jump
    public float fallgravity = 5f;
    public float airtime = 1f;  // Max time you can rise in the air after jumping
    public float walkJumpAirtime = 1.5f; // Max time you can rise in the air after a walk jump
    private float airtimer = 0;
    private bool changingDirections
    {
        get
        {
            return (direction.x > 0 && rb.velocity.x < 0) || (direction.x < 0 && rb.velocity.x > 0);
        }
    }
    private bool maxSpeedSkidding = false;  // Whether the player is skidding after running at max speed

    [Header("Swimming")]
    public float bubbleSpawnDelay = 2.5f;
    public GameObject bubblePrefab;
    public GameObject splashWaterPrefab;
    public bool swimming = false;
    public float swimForce = 5f;
    public float swimGravity = 1f;
    public float swimDrag = 3f;
    public float swimTerminalVelocity = 2f;

    [Header("Climbing")]
    private bool climbing = false;
    public float climbSpeed = 2f;
    private bool canClimb = false; // Whether the player can climb

    [Header("Collision")]
    public bool onGround = false;
    public bool wasGrounded = false;
    public float groundLength = 0.6f;
    public float groundSink = 0.1f; // how much the end of the ground raycast sinks into the ground
    public bool onMovingPlatform = false;
    [HideInInspector] public bool doMovingPlatformMomentum = true;  // Whether to add momentum to Mario when he leaves a moving platform
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

    /// <summary>
    /// <para>This value is changed by animations to change Mario's scale.</para>
    /// <para>Because of this, you cannot change it via the inspector or code.</para>
    /// </summary>
    public Vector3 animationScale = new(1, 1, 1);   // animate this instead of the scale directly

    /// <summary>
    /// <para>The base scale that's treated as default.</para>
    /// <para>It's set at the start to the initial scale of the player.</para>
    /// <para>Since the actual scale is overriden every frame, change this instead.</para>
    /// </summary>
    public Vector3 baseScale;
    public float animationRotation = 0;  // If not 0, his z rotation will be set to this
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
    public bool isTransforming = false;

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
    public enum CarryMethod
    {
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
    private const float wallJumpHoldTime = 0.25f;
    private float wallJumpHoldTimer;
    public bool canSpinJump = false;

    [Header("Midair Spin")]
    public bool canMidairSpin = true;
    public float midairSpinDuration = 0.4f; // How long the twirl lasts
    public float midairSpinFallSpeedCap = 3f; // Max fall speed during twirl
    public float midairSpinGravityMult = 0.3f; // Gravity multiplier while twirling

    private bool isMidairSpinning = false;
    private bool midairSpinUsedThisJump = false;
    private float midairSpinEndTime = 0f;

    [HideInInspector] public bool isCapeActive = false;
    [HideInInspector] public bool wallSliding = false;
    private bool pushing = false;
    private ObjectPhysics pushingObject;
    private float pushingSpeed = 0f;
    [HideInInspector] public bool spinning = false;
    private bool spinJumpQueued = false;    // If the next jump should be a spin jump
    public bool canGroundPound = false;
    public GameObject groundPoundParticles;
    [HideInInspector] public bool groundPounding = false;
    [HideInInspector] public bool groundPoundRotating = false;
    private bool groundPoundLanded = false; // If the ground pound has landed but you are still in the animation and can't move
    private bool wasPressingDown = false;
    // Made private to not clog inspector
    private float groundPoundSpinTime = 0.5f; // How long the player will be frozen in the air when ground pound starts

    public AudioClip spinJumpBounceSound;
    public AudioClip spinJumpPoofSound;
    public GameObject spinJumpBouncePrefab; // Spikey effect
    public GameObject spinJumpPoofPrefab;   // Puff of smoke
    public AudioClip groundPoundSound;
    public AudioClip groundPoundLandSound;

    /* Levers */
    private List<UseableObject> useableObjects = new();

    // use this in other scripts to check if mario is moving (walking or jumping)
    public bool isMoving
    {
        get
        {
            return rb.velocity.x > 0.01 || !onGround;
        }
    }

    public PlayerInput playerInput;

    public void DisableInputs()
    {
        if (this == null) return;   // Can happen when pausing while taking damage
        if (!gameObject.activeSelf) return;

        if (playerInput != null)
        {
            playerInput.DeactivateInput(); // Disable input actions
            playerInput.enabled = false; // Disable the PlayerInput component to fully block input
        }
    }

    public void EnableInputs()
    {
        if (this == null) return;   // Can happen when unpausing while taking damage
        if (!gameObject.activeSelf) return; // Can happen after the level is completed
        if (playerInput == null) return;    // Can happen when transitioning between powerup states

        if (playerInput != null && !playerInput.enabled)
        {
            playerInput.enabled = true; // Ensure PlayerInput is enabled first
        }
        playerInput.ActivateInput(); // Re-enables input after unpausing
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
        baseScale = transform.lossyScale;
        abilities.AddRange(GetComponents<MarioAbility>());
        normalSpriteLibrary = GetComponent<SpriteLibrary>().spriteLibraryAsset;

        // Store player's position at the beginning of the level (respawn)
        originalPosition = transform.position;

        // Abilities cheat
        if (GlobalVariables.cheatAllAbilities)
        {
            canCrawl = true;
            canWallJump = true;
            canSpinJump = true;
            canGroundPound = true;
            // Add cape attack script
            if (GetComponent<CapeAttack>() == null)
            {
                CapeAttack capeAttack = gameObject.AddComponent<CapeAttack>();
                abilities.Add(capeAttack);
            }
        }

        // Should happen after everything else so that the player transformation cheats work
        GameManager.Instance.SetPlayer(this, playerNumber);

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
        // set my scale to the animation scale (relative to the original scale)
        transform.localScale = Vector3.Scale(baseScale, animationScale);
        transform.parent = myParent;

        // Set rotation based on animation
        // NOTE: We are not using rigidbody rotation because it can cause movement to be choppy if it is set frequently
        animationRotation = facingRight ? -Mathf.Abs(animationRotation) : Mathf.Abs(animationRotation); // Update rotation based on where the player is facing
        transform.rotation = Quaternion.Euler(0, 0, animationRotation);

        if (invincetimeremain > 0f)
        {
            invincetimeremain -= Time.deltaTime;
            if (!flashing)
            {
                StartCoroutine(FlashDuringInvincibility());
            }
        }
        else
        {
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
        if (!pressRunToGrab && runPressed && (!crouchToGrab || direction.y < -0.5f) && !carrying)
        {
            checkForCarry();
        }

        // Throwing item
        if (carrying && !runPressed)
        {
            if (direction.y < -0.5f)
            {
                dropCarry();
            }
            else
            {
                throwCarry();
            }
        }

    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (groundLayer == (groundLayer | (1 << other.gameObject.layer)))
        {
            Vector2 impulse = Vector2.zero;

            int contactCount = other.contactCount;
            for (int i = 0; i < contactCount; i++)
            {
                var contact = other.GetContact(i);
                impulse += contact.normal * contact.normalImpulse;
                impulse.x += contact.tangentImpulse * contact.normal.y;
                impulse.y -= contact.tangentImpulse * contact.normal.x;
            }

            if (impulse.y < 0)
            {
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

    public void startStarPower(float time)
    {
        // stop any current star power
        CancelInvoke(nameof(stopStarPower));
        CancelInvoke(nameof(changeRainbowColor));

        // start new star power
        InvokeRepeating(nameof(changeRainbowColor), 0, 0.1f);
        starPower = true;
        starPowerRemainingTime = time;
        if (time != -1)
        {
            Invoke(nameof(stopStarPower), time);
        }
    }

    public void stopStarPower()
    {
        CancelInvoke(nameof(changeRainbowColor));
        starPower = false;
        starPowerRemainingTime = 0f; // Reset remaining time

        if (ComboManager.Instance != null)
        {
            ComboManager.Instance.ResetAll();
        }
        
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
        if (frozen)
        {
            return;
        }
        
        // cancel spin every frame while climbing, unless spin jump is queued
        if (climbing && !spinJumpQueued)
        {
            spinning = false;
            spinPressed = false;
            animator.SetBool("isSpinning", false);
        }

        // Movement
        if (climbing)
        {
            ClimbMove(direction);
        }
        else
        {
            MoveCharacter(direction.x);
        }


        // Jumping/Swimming
        bool jumpBlocked = false;
        foreach (MarioAbility ability in abilities)
        {
            if (ability.isBlockingJump)
            {
                jumpBlocked = true;
            }
        }
        
        bool wallJumpCheck = (wallSliding || (spinning && direction.x != 0 && CheckWall(direction.x > 0))) && !onGround;
        if (Time.time < jumpTimer && (onGround || swimming || wallSliding || wallJumpCheck || climbing) && !jumpBlocked && !groundPoundLanded)
        {
            if (swimming)
            {
                if (!groundPounding)
                {
                    audioSource.PlayOneShot(swimSound);
                    Swim();
                }
            }
            else if (climbing && spinJumpQueued)
            {
                // spin jump from climbing
                StopClimbing();
                SpinJump();
            }
            else if (wallSliding || (spinning && wallJumpCheck))
            {
                // Make sure Mario faces the wall before walljumping
                if (wallJumpCheck && !wallSliding)
                {
                    FlipTo(direction.x > 0);
                }
                audioSource.Play();
                WallJump();
            }
            else if (spinJumpQueued)
            {
                SpinJump();
            }
            else
            {
                audioSource.Play();
                Jump(1, false);
            }
        }

        // Ground Pound Cancel
        if (groundPounding && !groundPoundRotating && direction.y > 0)
        {
            CancelGroundPound();
        }

        bool isPressingDown = direction.y < -0.5f;

        // Ground Pound
        if (canGroundPound && !onGround && isPressingDown && !wasPressingDown && !groundPounding && !wallSliding && !climbing)
        {
            GroundPound();
        }

        // Update state for next frame
        wasPressingDown = isPressingDown;

        bool wasInAir = !onGround;   // store if mario was in the air last frame

        RaycastHit2D? hitRayMaybe = null;

        // If climbing, check for ground if climbing down
        if (!climbing || direction.y < 0f)
        {
            hitRayMaybe = CheckGround();
        }

        bool wasOnMovingPlatform = onMovingPlatform;

        if (onGround && hitRayMaybe != null)
        {

            if (climbing)
            {
                StopClimbing();
            }

            RaycastHit2D hitRay = (RaycastHit2D)hitRayMaybe;

            bool firstFrameMovingPlatform = false;
            if (hitRay.transform.gameObject.tag == "MovingPlatform")
            {
                if (!onMovingPlatform)
                {
                    firstFrameMovingPlatform = true;
                }
                onMovingPlatform = true;
            }

            groundPos = hitRay.point;

            if (onMovingPlatform)
            {
                //onMovingPlatform = true;
                //transform.parent = hitRay.transform;
                if (transform.parent != hitRay.transform)
                {
                    transform.parent = hitRay.transform; // Only set parent if not already the moving platform
                }
            }
            else
            {
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
            if (groundObject.CompareTag("Slope") && groundObject.TryGetComponent(out Slope slope))
            {
                newAngle = slope.angle;
            }
            else
            {
                newAngle = 0f;
            }

            Vector2 slopeVector = new Vector2(Mathf.Cos(newAngle * Mathf.Deg2Rad), Mathf.Sin(newAngle * Mathf.Deg2Rad)).normalized;

            // if the angle has changed, change mario's velocity to match the slope
            if (newAngle != floorAngle && !wasInAir)
            {
                float moveMag = rb.velocity.magnitude;
                if (rb.velocity.x < 0)
                {
                    moveMag *= -1;
                }
                rb.velocity = slopeVector * moveMag;
            }

            // If landing on a slope, also change mario's velocity so he doesn't slide down the slope immediately
            // TODO: Make this not slow you down if you jump onto a slope while moving downhill
            if (newAngle != 0 && wasInAir)
            {
                float moveMag = rb.velocity.x;
                rb.velocity = slopeVector * moveMag;
            }

            floorAngle = newAngle;

            // Stick to ground (but not moving platforms, unless it's the first frame on the platform)
            if (!onMovingPlatform || firstFrameMovingPlatform)
            {
                transform.position = new Vector3(transform.position.x, groundPos.y + groundLength - groundSink, transform.position.z);    // Modified from 0.01f to 0.1f
            }

            // remove any off-the-ground velocity (accounting for slope)
            // https://stackoverflow.com/questions/72494915/how-do-you-get-the-component-of-a-vector-in-the-direction-of-a-ray
            rb.velocity = Vector2.Dot(rb.velocity, slopeVector) * slopeVector;

            if (hitRay.transform.gameObject.tag == "Damaging")
            {
                damageMario();
            }

            // Stop spinning, stop ground pounding
            spinning = false;
            if (groundPounding && !groundPoundLanded)
            {
                GroundPoundLand(hitRay.transform.gameObject);
            }

            // If we landed AND we are not holding left or right, face the direction we are moving
            if (direction.x == 0 && wasInAir && Mathf.Abs(rb.velocity.x) > 0.01f)
            {
                FlipTo(rb.velocity.x > 0);
            }

        }
        else
        {
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

        bool didHorizontalCornerCorrection = TryHorizontalCornerCorrection();

        // Corner correction
        if (!didHorizontalCornerCorrection && rb.velocity.y > 0 && doCornerCorrection)
        {

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
            }
            else
            {
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
                        float newPositionX = hitLeft.point.x + (playerWidth / 2 * 1.2f);
                        transform.position = new Vector3(newPositionX, transform.position.y, transform.position.z);
                    }
                    else if (hitRight.collider != null && hitRight.point.x > playerright - cornerCorrection && hitRight.point.x < playerright)
                    {
                        Debug.Log("CORNER CORRECTION ACTIVE RIGHT");
                        float newPositionX = hitRight.point.x - (playerWidth / 2 * 1.2f);
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

        if (ceilLeft.collider != null || ceilMid.collider != null || ceilRight.collider != null)
        {

            RaycastHit2D hitRay = ceilMid;

            if (ceilMid)
            {
                hitRay = ceilMid;
            }
            else if (ceilLeft)
            {
                hitRay = ceilLeft;
            }
            else if (ceilRight)
            {
                hitRay = ceilRight;
            }
        }

        // Wall detection (for wall sliding)
        if ((direction.x != 0 || wallSliding) && rb.velocity.y < 0)
        {
            bool checkRight = wallSliding ? facingRight : direction.x > 0;
            bool hitWall = CheckWall(checkRight);

            if (hitWall && !pushing && !inCrouchState && !isCrawling)
            {
                if (!wallSliding)
                {
                    // flip mario to face the wall
                    FlipTo(checkRight);
                    wallSliding = true; // cancel spinning when starting to wall slide
                    spinning = false;
                }
            }
            else
            {
                wallSliding = false;
            }
        }
        else
        {
            wallSliding = false;
        }

        // Climbing
        // if (!climbing && canClimb && direction.y > 0.8f)
        if (!climbing && canClimb && Mathf.Abs(direction.y) > 0.5f)
        {
            StartClimbing();
        }

        animator.SetBool("isWallSliding", wallSliding);
        animator.SetBool("isSpinning", spinning);
        animator.SetBool("isLookingUp", isLookingUp);
        animator.SetBool("isClimbing", climbing);

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

        // Detect landing to reset stomp combo,
        // but only if Mario is NOT in star power mode
        if (onGround && !wasGrounded && !starPower)
        {
            if (ComboManager.Instance != null)
            {
                // Stomp combo should ALWAYS reset when Mario touches the ground
                ComboManager.Instance.ResetStomp();

                // Shell combo only resets if there is NO active moving shell chain
                if (!ComboManager.Instance.ShellChainActive)
                {
                    ComboManager.Instance.ResetShellChain();
                }
            }
        }

        wasGrounded = onGround;

        // Physics
        ModifyPhysics();
    }

    /// <summary>
    /// Try to slide Mario horizontally into a small gap when moving sideways in the air.
    /// Returns true if we snapped Mario and vertical corner correction should be skipped.
    /// </summary>
    private bool TryHorizontalCornerCorrection()
    {
        // Only in air and only if we’re actually trying to move
        if (onGround) return false;
        if (!doCornerCorrection) return false;

        float horizontalInput = direction.x;
        if (Mathf.Abs(rb.velocity.x) < 0.01f && Mathf.Abs(horizontalInput) < 0.01f)
            return false;

        // Direction we’re “pushing into” (prefer velocity, fallback to input)
        float dirX = Mathf.Abs(rb.velocity.x) > 0.01f
            ? Mathf.Sign(rb.velocity.x)
            : Mathf.Sign(horizontalInput);

        if (dirX == 0f) return false;

        var box = GetComponent<BoxCollider2D>();
        float playerWidth = box.bounds.size.x;
        float playerHeight = box.bounds.size.y;

        // Predict a little bit ahead in the horizontal direction (like the vertical code does for Y)
        float startWidth = playerWidth / 2f + (rb.velocity.x * Time.fixedDeltaTime) + 0.01f;

        // Ray origin at the side we are moving towards, centered vertically
        Vector2 origin = (Vector2)transform.position + new Vector2(dirX * startWidth, 0f);

        // Ray length based on height
        float rayLength = playerHeight / 2f * 1.1f;

        // Two rays: one up, one down from the side
        RaycastHit2D hitUp = Physics2D.Raycast(origin, Vector2.up,   rayLength, groundLayer);
        RaycastHit2D hitDown = Physics2D.Raycast(origin, Vector2.down, rayLength, groundLayer);

        if (hitUp.collider == null || hitDown.collider == null)
        {
            // No walls forming a "gap" on this side
            return false;
        }

        float distUp = hitUp.distance;
        float distDown = hitDown.distance;
        float totalDistance = distUp + distDown;
        float gapHeight = totalDistance - playerHeight;

        // Not enough room to fit Mario vertically
        if (gapHeight < 0f)
            return false;

        // Similar idea to vertical corner correction:
        // Only correct if the corner we’re touching is very close to Mario’s top/bottom.
        float playerTop = transform.position.y + playerHeight / 2f;
        float playerBottom = transform.position.y - playerHeight / 2f;

        float cornerMargin = cornerCorrection * playerHeight;

        bool touchingTopCorner =
            hitDown.point.y > playerBottom &&
            hitDown.point.y < playerBottom + cornerMargin;

        bool touchingBottomCorner =
            hitUp.point.y < playerTop &&
            hitUp.point.y > playerTop - cornerMargin;

        if (!touchingTopCorner && !touchingBottomCorner)
            return false;

        Vector3 oldPos = transform.position;

        // Center Mario vertically in the gap
        float gapCenterY = (hitUp.point.y + hitDown.point.y) * 0.5f;
        float newY = gapCenterY;

        // Nudge him a little *into* the gap horizontally
        float newX = transform.position.x + dirX * (playerWidth * 0.2f);

        transform.position = new Vector3(newX, newY, transform.position.z);

        Debug.Log(
            $"[HorizontalCornerCorrection] Applied. dirX={dirX}, " +
            $"oldPos={oldPos}, newPos={transform.position}, " +
            $"distUp={distUp:F3}, distDown={distDown:F3}, gapHeight={gapHeight:F3}"
        );

        return true;
    }

    private void StartClimbing()
    {
        climbing = true;
        rb.velocity = Vector2.zero; // Stop all movement
        rb.gravityScale = 0; // Disable gravity
        rb.drag = 0; // Disable drag

        spinning = false;
        spinJumpQueued = false;
        spinPressed = false;

        // Reset ground pound rotation states when starting to climb
        groundPounding = false;
        groundPoundRotating = false;
        groundPoundLanded = false;
        groundPoundInWater = false;
        waterGroundPoundStartTime = 0f;

        // Reset ground pound animations
        animator.SetBool("isDropping", false);
        animator.SetBool("cancelDropping", false);
        animationRotation = 0f;

        animator.SetBool("isSpinning", false);
        //rb.isKinematic = true; // Make the rigidbody kinematic
    }

    private void StopClimbing()
    {
        climbing = false;
        rb.gravityScale = 1; // Enable gravity
        rb.drag = 0; // Disable drag
        //rb.isKinematic = false; // Make the rigidbody non-kinematic

        FlipTo(rb.velocity.x > 0); // Face the direction of movement
    }

    private void TransferMovingPlatformMomentum()
    {
        if (!doMovingPlatformMomentum) return;
        if (onMovingPlatform)
        {
            // TODO!! Somehow group all kinds of moving platforms together so we don't need to check for each kind
            // Using an Interface for all moving platforms seems like a good solution
            // For now, just check for each kind of moving platform
            if (transform.parent == null)
            {
                print("HMM... onMovingPlatform is true but transform.parent is null");
                return;
            }
            if (transform.parent.GetComponent<MovingPlatform>() != null)
            {
                rb.velocity += transform.parent.GetComponent<MovingPlatform>().velocity;
            }
            else if (transform.parent.GetComponent<ConveyorBelt>() != null)
            {
                rb.velocity += transform.parent.GetComponent<ConveyorBelt>().velocity;
            }
        }
    }

    public RaycastHit2D? CheckGround()
    {
        // Vertical raycast offset (based on crouch state)
        float updGroundLength = inCrouchState ? groundLength / 2 : groundLength;

        // Floor detection
        RaycastHit2D groundHit1 = Physics2D.Raycast(transform.position + raycastLeftOffset + HOffset, Vector2.down, updGroundLength, groundLayer);
        RaycastHit2D groundHit2 = Physics2D.Raycast(transform.position + raycastRightOffset + HOffset, Vector2.down, updGroundLength, groundLayer);

        // Don't stand on the object Mario is pushing
        // TODO: Might not work correctly for future pushable objects where their collider is not on the same object as their ObjectPhysics
        bool hit1Valid = (groundHit1.collider != null) && (!pushingObject || groundHit1.transform.gameObject != pushingObject.gameObject);
        bool hit2Valid = (groundHit2.collider != null) && (!pushingObject || groundHit2.transform.gameObject != pushingObject.gameObject);

        onGround = (hit1Valid || hit2Valid) && (rb.velocity.y <= 0.01f || onGround);

        if (onGround)
        {
            RaycastHit2D hitRay = groundHit1;

            // print("ground1: " + groundHit1.transform.gameObject.tag);
            // print("ground2: " + groundHit2.transform.gameObject.tag);

            // instead, choose the higher of the two ground hits
            if (hit1Valid && hit2Valid)
            {
                hitRay = groundHit1.point.y > groundHit2.point.y ? groundHit1 : groundHit2;
            }
            else if (hit1Valid)
            {
                hitRay = groundHit1;
            }
            else if (hit2Valid)
            {
                hitRay = groundHit2;
            }
            return hitRay;
        }
        return null;
    }

    bool CheckWall(bool right)
    {
        // Wall detection (for wall jumping and wall sliding)
        if (canWallJump && !onGround && !swimming && (!carrying || canWallJumpWhenHoldingObject))
        {

            float raycastlength = GetComponent<BoxCollider2D>().bounds.size.x / 2 + 0.03f;

            // raycast in the direction the stick is pointing or the direction Mario is facing if already wall sliding
            Vector2 raycastDirection = right ? Vector2.right : Vector2.left;
            RaycastHit2D wallHit = Physics2D.Raycast(transform.position, raycastDirection, raycastlength, groundLayer);

            if (wallHit.collider != null)
            {
                return true;
            }
        }
        return false;
    }

    void MoveCharacter(float horizontal)
    {
        bool crouch = direction.y < -0.5f && canCrouch && onGround;

        // Crouching
        if (crouch && onGround && !carrying && !swimming)
        {

            // Start Crouch
            animator.SetBool("isCrouching", true);
            inCrouchState = true;
            GetComponent<BoxCollider2D>().size = new Vector2(GetComponent<BoxCollider2D>().size.x, crouchColHeight);
            GetComponent<BoxCollider2D>().offset = new Vector2(GetComponent<BoxCollider2D>().offset.x, crouchColOffset);

        }
        else if ((!crouch && onGround) || carrying || groundPounding)
        {

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
        isCrawling = inCrouchState && onGround && !carrying && !swimming && !groundPounding && powerupState == PowerupState.small && canCrawl
                     && math.abs(horizontal) > 0.5 && (math.abs(rb.velocity.x) < 0.05f || isCrawling);
        bool regularMoving = (!inCrouchState || !onGround) && !groundPounding && !wallSliding;

        // use the angle of the slope if on the ground
        Vector2 moveDir = onGround ? new Vector2(Mathf.Cos(floorAngle * Mathf.Deg2Rad), Mathf.Sin(floorAngle * Mathf.Deg2Rad)) : Vector2.right;

        maxSpeedSkidding = (maxSpeedSkidding && changingDirections) || (Mathf.Abs(rb.velocity.x) >= (maxRunSpeed * 0.9f) && changingDirections);

        float speedMult = 1f;
        // If turning around, apply a speed multiplier
        if (changingDirections)
        {
            if (onGround)
            {
                if (maxSpeedSkidding)
                {
                    speedMult = 0.7f;
                }
                else
                {
                    speedMult = 1f;
                }
            }
            else
            {
                speedMult = 1.5f;
            }
        }
        // If crawling, move faster so you speed up faster
        if (isCrawling)
        {
            speedMult = 2f;
        }

        float maxSpeedMult = 1f;
        if (isCrawling)
        {
            maxSpeedMult = 0.5f;
        }

        //print("moving in " + moveDir);
        if (regularMoving || isCrawling)
        {
            //print("regular moving");
            if (runPressed && !swimming && !isCrawling)
            {
                // Running
                rb.AddForce(horizontal * runSpeed * moveDir * speedMult);
            }
            else
            {
                // Walking
                if (Mathf.Abs(rb.velocity.x) <= (maxSpeed * maxSpeedMult) || (Mathf.Sign(horizontal) != Mathf.Sign(rb.velocity.x)))
                {
                    rb.AddForce(horizontal * moveSpeed * moveDir * speedMult);
                }
                else if (onGround)
                {
                    // Slow down if you are going too fast (only on the ground)
                    rb.AddForce(Mathf.Sign(rb.velocity.x) * slowDownForce * -moveDir);
                }
            }
        }

        // Wall slide horizontal movement
        if (wallSliding)
        {
            // Stop moving horizontally when wall sliding
            rb.velocity = new Vector2(0, rb.velocity.y);

            // If holding the direction of the wall or not at all, reset the timer
            if (horizontal == 0 || (facingRight && horizontal > 0) || (!facingRight && horizontal < 0))
            {
                wallJumpHoldTimer = Time.time + wallJumpHoldTime;
            }
            else
            {
                // If holding the opposite direction of the wall for too long, stop wall sliding
                if (Time.time > wallJumpHoldTimer)
                {
                    wallSliding = false;
                }
            }
        }

        if ((onGround || swimming) && !isCapeActive && horizontal != 0)
        {
            // Face the direction we are holding, if on the ground 
            FlipTo(horizontal > 0);
        }

        // Max Speed (Horizontal)
        if (runPressed)
        {
            if (Mathf.Abs(rb.velocity.x) > maxRunSpeed)
            {
                rb.velocity = new Vector2(Mathf.Sign(rb.velocity.x) * maxRunSpeed, rb.velocity.y);
            }
        }

        // Max Speed (Vertical)
        if (!onGround)
        {
            // Terminal Velocity
            float tvel = swimming ? swimTerminalVelocity : terminalvelocity;

            if (wallSliding)
            {  // slide down wall slower
                tvel /= 3;
            }
            if (groundPounding)
            {  // fall faster during ground pound
                tvel *= 1.5f;
            }

            if (-rb.velocity.y > tvel)
            {
                rb.velocity = new Vector2(rb.velocity.x, -tvel);
            }
            if (rb.velocity.y > (tvel * 2) && swimming)
            { // swimming up speed limit
                rb.velocity = new Vector2(rb.velocity.x, tvel * 2);
            }
        }

        // Infinite fireballs cheat
        if (GlobalVariables.cheatFlamethrower && shootPressed)
        {
            foreach (MarioAbility ability in abilities)
            {
                ability.onShootPressed();
            }
        }

        // Animation
        animator.SetFloat("Horizontal", Mathf.Abs(rb.velocity.x) * walkAnimatorSpeed);
        animator.SetBool("isRunning", Mathf.Abs(rb.velocity.x) > 0.2f);

        if (onGround)
        {
            if (!inCrouchState && canSkid)
            {
                animator.SetBool("isSkidding", maxSpeedSkidding);
            }
            animator.SetBool("onGround", true);
        }
        else
        {
            animator.SetBool("isSkidding", false);
            animator.SetBool("onGround", false);
        }

        animator.SetBool("isCrawling", isCrawling);
    }

    private void ClimbMove(Vector2 dir)
    {
        rb.velocity = new Vector2(dir.x * climbSpeed, dir.y * climbSpeed);
        animator.SetFloat("climbSpeed", rb.velocity.magnitude);
    }

    // for jumping and also stomping enemies (which always make mario use his walkJumpSpeed)
    public void Jump(float jumpMultiplier = 1f, bool forceWalkJumpSpeed = true)
    {
        if (climbing)
        {
            StopClimbing();
        }
        rb.velocity = new Vector2(rb.velocity.x, 0);
        bool useWalkJumpSpeed = forceWalkJumpSpeed || Mathf.Abs(rb.velocity.x) > walkJumpSpeedRequired;
        rb.AddForce(Vector2.up * (useWalkJumpSpeed ? walkJumpSpeed : jumpSpeed) * (swimming ? 0.5f : 1f) * jumpMultiplier, ForceMode2D.Impulse);
        onGround = false;
        jumpTimer = 0;
        airtimer = Time.time + ((useWalkJumpSpeed ? walkJumpAirtime : airtime) * jumpMultiplier);
    }

    // for swimming
    public void Swim()
    {
        if (groundPounding) return;

        onGround = false;
        rb.AddForce(Vector2.up * swimForce, ForceMode2D.Impulse);
        animator.SetTrigger("swim");
        jumpTimer = 0;
    }

    // jump out of water
    public void JumpOutOfWater()
    {
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
    public void WallJump()
    {
        spinning = false;
        Jump(0.75f);
        // add horizontal force in the opposite direction of the wall (where you are facing)
        int dirToSign = facingRight ? -1 : 1;
        rb.AddForce(dirToSign * Vector2.right * jumpSpeed, ForceMode2D.Impulse);
        // flip mario to face away from the wall
        FlipTo(!facingRight);
    }

    // Spin Jump
    public void SpinJump()
    {
        // If we're climbing, we need to exit climbing state first
        if (climbing)
        {
            StopClimbing();
            // Ensure physics are properly reset
            rb.gravityScale = 1;
            rb.drag = 0;
            rb.bodyType = RigidbodyType2D.Dynamic;
        }

        audioSource.PlayOneShot(spinJumpSound);
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(Vector2.up * jumpSpeed * 1.1f, ForceMode2D.Impulse);
        onGround = false;
        jumpTimer = 0;
        airtimer = Time.time + (airtime * 0.6f);
        spinning = true;

        foreach (MarioAbility ability in abilities)
        {
            ability.onSpinPressed();
        }
    }

    // Called from enemy script when mario spin bounces on an enemy
    public void SpinJumpBounce(GameObject enemy)
    {
        print("bouncing off");
        audioSource.PlayOneShot(spinJumpBounceSound);
        // Instantiate the spin jump bounce effect where they are colliding
        Vector3 effectSpawnPos = enemy.GetComponentInChildren<Collider2D>().ClosestPoint(transform.position);
        Instantiate(spinJumpBouncePrefab, effectSpawnPos, Quaternion.identity);
        Jump();
    }

    public void SpinJumpPoof(GameObject enemy)
    {
        audioSource.PlayOneShot(spinJumpPoofSound);
        // Instantiate the spin jump poof effect where they are colliding
        Vector3 effectSpawnPos = enemy.GetComponentInChildren<Collider2D>().ClosestPoint(transform.position);
        Instantiate(spinJumpPoofPrefab, effectSpawnPos, Quaternion.identity);
        // Destroy the enemy
        Destroy(enemy);
        Jump();
    }

    private void GroundPound()
    {
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

    private void GroundPoundFall()
    {
        if (!groundPounding) return; // Skip if ground pound is canceled

        // Start the ground pound fall
        groundPoundRotating = false;
        rb.velocity = new Vector2(rb.velocity.x, -jumpSpeed * 1.5f);
    }

    private void GroundPoundLand(GameObject hitObject)
    {
        //groundPounding = false;
        groundPoundLanded = true;
        groundPoundRotating = false;
        groundPoundInWater = false;
        waterGroundPoundStartTime = 0f; // Reset timer
        audioSource.PlayOneShot(groundPoundLandSound);
        IGroundPoundable groundPoundable = hitObject.GetComponent<IGroundPoundable>();
        if (groundPoundable != null)
        {
            groundPoundable.OnGroundPound(this);
        }

        if (groundPoundParticles != null)
        {
            Vector3 particlePosition = new Vector3(transform.position.x, transform.position.y - (colliderY / 2), transform.position.z);
            Instantiate(groundPoundParticles, particlePosition, Quaternion.identity);
        }

        animator.SetBool("isDropping", false);
        animator.SetBool("cancelDropping", false);

        // Wait a bit before finishing the ground pound
        Invoke(nameof(FinishGroundPoundLand), 0.25f);
    }

    private void FinishGroundPoundLand()
    {
        groundPounding = false;
        groundPoundLanded = false;

        // start swim idle if you're swimming when the ground pound lands
        if (swimming)
        {
            animator.SetTrigger("enterWater");
        }
    }

    public void CancelGroundPound()
    {
        if (!groundPounding || groundPoundRotating) // Only cancel during the fall phase
            return;

        groundPounding = false;  // Exit ground pound state
        groundPoundRotating = false;  // Stop rotation effect
        groundPoundInWater = false;
        waterGroundPoundStartTime = 0f;

        // Allow normal air movement
        rb.gravityScale = fallgravity;

        // Play cancel animation or sound if needed
        animator.SetBool("cancelDropping", true);
        animator.SetBool("isDropping", false);

        if (swimming)
        {
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
        rb.gravityScale = riseGravity; // Reset gravity to normal

        // Reset input flags (to prevent lingering input re-triggering the ground pound)
        spinPressed = false;
    }

    void ModifyPhysics()
    {
        // Special ground pound physics
        if (groundPounding)
        {
            rb.drag = 0;
            if (groundPoundRotating)
            {
                rb.gravityScale = 0; // Freeze during rotation phase
                rb.velocity = new Vector2(0, 0);
            }
            else
            {
                rb.gravityScale = fallgravity; // Normal gravity during fall phase
            }
            return;
        }


        // TODO: Properly handle pushing underwater (animation and functionality) (Waterfall Caverns)
        animator.SetBool("isPushing", pushing);

        // special swimming physics
        if (swimming)
        {
            rb.gravityScale = swimGravity;
            rb.drag = swimDrag;
            return;
        }

        // Special pushing physics
        if (pushing && !changingDirections)
        {
            int pushDir = facingRight ? 1 : -1;
            rb.velocity = new Vector2(pushingSpeed * pushDir, rb.velocity.y);
            if (onGround)
            {
                // Fix for falling inside moving platforms while pushing
                rb.gravityScale = 0;
                rb.drag = 0f;
            }
            else
            {
                rb.gravityScale = fallgravity;
                rb.drag = 0f;
            }
            return;
        }

        // Special Climbing physics
        if (climbing)
        {
            rb.gravityScale = 0; // Disable gravity
            rb.drag = 0; // Disable drag
            return;
        }

        Vector2 physicsInput = direction;   // So we can modify it without changing the direction variable

        if (onGround)
        { // Regular physics for air or ground
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

            // If holding direction of movement
            if ((Mathf.Abs(physicsInput.x) > 0 && !changingDirections) || isCrawling)
            {
                rb.drag = 0f;
            }
            else
            {
                // Changing directions, not holding any direction, or crouching
                float spd = Mathf.Abs(rb.velocity.x);
                float newDrag = 100000000;
                // if (spd > 0) {
                //     newDrag = 10f / spd * (inCrouchState ? 1.5f : 1f);
                // }

                switch (spd)
                {
                    case float n when n < 0.5f:
                        newDrag = 100000000;
                        break;
                    case float n when n < 5f:
                        newDrag = 8f / spd;
                        break;
                    default:
                        newDrag = 1.5f;
                        break;
                }

                float dragMult = 1f;
                if (inCrouchState)
                {
                    // If crouching, set a drag multiplier
                    dragMult = 1.5f;
                }
                else if (facingRight != (rb.velocity.x > 0))
                {
                    // If facing the opposite direction, apply a drag multiplier
                    dragMult = 1.5f;
                }


                newDrag *= dragMult;

                if (!float.IsInfinity(newDrag))
                {
                    rb.drag = newDrag;
                }
                else
                {
                    rb.drag = 100000000;
                }

            }

            // 0 gravity on the ground
            rb.gravityScale = 0;

        }
        else
        {
            // in the air
            rb.gravityScale = riseGravity;  // Rising Gravity
            rb.drag = 0;
            //if(rb.velocity.y < startfallingspeed){

            // Falling
            if (airtimer < Time.time || rb.velocity.y < startfallingspeed)
            {
                if (rb.velocity.y > 0)
                {    // Still going up
                    rb.gravityScale = peakGravity;
                }
                else
                {
                    rb.gravityScale = fallgravity;
                }

                // Rising but not pressing jump/spin anymore
            }
            else if (rb.velocity.y > 0 && !(jumpPressed || (spinPressed && spinning)))
            {
                rb.gravityScale = fallgravity;
                airtimer = Time.time - 1f;
                //rb.gravityScale = gravity * fallMultiplier;
            }
        }
    }

    void Flip()
    {
        if (groundPounding || groundPoundRotating)
        {
            return; // Prevent flipping during ground pound phases
        }

        facingRight = !facingRight;
        //transform.rotation = Quaternion.Euler(0, facingRight ? 0 : 180, 0);
        if (sprite)
        {
            sprite.flipX = !facingRight;
        }
        else
        {
            // If flip is called before start, it might not be assigned yet
            // So we assign it here
            sprite = GetComponent<SpriteRenderer>();
            if (sprite)
            {
                sprite.flipX = !facingRight;
            }
        }
        float relScaleX = facingRight ? 1 : -1;

        // flip might be called before start, this fixes that
        if (relPosObj == null)
        {
            relPosObj = transform.GetChild(0).gameObject;
        }

        relPosObj.transform.localScale = new Vector3(relScaleX, 1, 1);
    }

    public void FlipTo(bool right)
    {
        if (facingRight != right)
        {
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
        float flashSpeed = 1 / 20f;   // 20 fps flash speed
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

    public void damageMario(bool force = false)
    {
        if (GlobalVariables.cheatInvincibility)
        {
            return;
        }

        if (invincetimeremain == 0f)
        {
            if (damaged && !force)
            {
                return;
            }
            // are we invincible mario?
            if (starPower && !force)
            {
                return;
            }
            damaged = true;

            // If you comment this, the tranformIntoPig will work without instantiating the deadMario with the pigMario
            // but the player will not be harmed by the enemies, only the wizard goomba's magic attack
            if (PowerStates.IsSmall(powerupState))
            {
                toDead();
            }
            else
            {
                powerDown();
            }
        }
    }

    private void powerDown()
    {
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

    public void ChangePowerup(GameObject newMarioObject)
    {
        if (isTransforming)
            return;
        
        isTransforming = true;
        
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

    public MarioMovement transferProperties(GameObject newMario)
    {
        newMario.GetComponent<Rigidbody2D>().velocity = gameObject.GetComponent<Rigidbody2D>().velocity;
        var newMarioMovement = newMario.GetComponent<MarioMovement>();

        // Transfer the parent relationship
        if (transform.parent != null)
        {
            newMario.transform.SetParent(transform.parent, true); // Maintain the parent's relationship
        }

        newMarioMovement.FlipTo(facingRight);

        /*SpriteRenderer newMarioSprite = newMario.GetComponent<SpriteRenderer>();
        if (newMarioSprite != null)
        {
            newMarioSprite.maskInteraction = GetComponent<SpriteRenderer>().maskInteraction;
        }
        else
        {
            Debug.LogWarning("New Mario object is missing a SpriteRenderer!");
        }*/

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
        }
        catch
        {
            // this might error if only one controller is connected
            print("Could not transfer input device to new mario. This is probably fine.");
        }

        if (carrying && heldObjectPosition.transform.childCount > 0)
        {
            // We need to check if it actually exists because it might be a bomb that exploded while we were holding it
            // move carried object to new mario
            GameObject carriedObject = heldObjectPosition.transform.GetChild(0).gameObject;
            carriedObject.transform.parent = newMarioMovement.heldObjectPosition.transform;
            carriedObject.transform.localPosition = Vector3.zero;
            newMarioMovement.carrying = true;
        }

        // transfer pressed buttons (for mobile controls)
        newMarioMovement.jumpPressed = jumpPressed;
        newMarioMovement.runPressed = runPressed;
        newMarioMovement.moveInput = moveInput;

        // Set additional abilities to new Mario
        newMarioMovement.canCrawl = canCrawl;
        newMarioMovement.canWallJump = canWallJump;
        newMarioMovement.canWallJumpWhenHoldingObject = canWallJumpWhenHoldingObject;
        newMarioMovement.canSpinJump = canSpinJump;
        newMarioMovement.canGroundPound = canGroundPound;

        // Passing the sound effects
        newMarioMovement.yeahAudioClip = yeahAudioClip;

        // Passing ground layer values
        newMarioMovement.groundLayer = groundLayer;

        return newMarioMovement;
    }

    public void playDamageSound()
    {
        GetComponent<AudioSource>().PlayOneShot(damageSound);
    }

    private void toDead()
    {
        if (GlobalVariables.cheatInvincibility)
        {
            return;
        }

        // print("death attempt");
        if (!dead)
        {
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
        if (invincetimeremain > 0f || starPower || GlobalVariables.cheatInvincibility)
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
        // TODO!! The crush detector can trigger this method too!
        // This could cause some triggers to trigger TWICE as mario enters them
        // This particularly affects water animations with big mario
        // Please find some solution to this issue

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

        if (other.gameObject.layer == LayerMask.NameToLayer("Vine"))
        {
            canClimb = true;
        }

        DetectDamagingObject(other);
    }

    private void DetectDamagingObject(Collider2D other)
    {
        if (other.gameObject.CompareTag("Damaging"))
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
            // Note: this might have been written in an attempt to get around the crush detector issue mentioned above
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

        if (other.gameObject.layer == LayerMask.NameToLayer("Vine"))
        {
            canClimb = false;
            if (climbing)
            {
                StopClimbing();
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

    private void OnDrawGizmos()
    {
        // if this script is disabled, don't draw gizmos
        if (!enabled)
        {
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

        // Horizontal corner correction
        if (doCornerCorrection && !onGround && rb != null)
        {
            // Figure out which side we are checking (prefer velocity, fall back to input)
            float dirX = 0f;

            if (Mathf.Abs(rb.velocity.x) > 0.01f)
            {
                dirX = Mathf.Sign(rb.velocity.x);
            }
            else if (Mathf.Abs(direction.x) > 0.01f)
            {
                dirX = Mathf.Sign(direction.x);
            }

            if (dirX != 0f)
            {
                BoxCollider2D box = GetComponent<BoxCollider2D>();
                float playerHeight = box.bounds.size.y;

                // start a little bit ahead of the collider on the side we're moving into
                float startWidth = playerWidth / 2f + (rb.velocity.x * Time.fixedDeltaTime) + 0.01f;
                Vector3 sideOrigin = transform.position + new Vector3(dirX * startWidth, 0f, 0f);

                float vertRayLength = playerHeight / 2f * 1.1f;

                // UP ray
                Gizmos.color = Color.green;
                RaycastHit2D upHit = Physics2D.Raycast(sideOrigin, Vector2.up, vertRayLength, groundLayer);
                if (upHit.collider != null)
                {
                    Gizmos.color = Color.yellow;
                }
                Gizmos.DrawLine(sideOrigin, sideOrigin + Vector3.up * vertRayLength);

                // DOWN ray
                Gizmos.color = Color.cyan;
                RaycastHit2D downHit = Physics2D.Raycast(sideOrigin, Vector2.down, vertRayLength, groundLayer);
                if (downHit.collider != null)
                {
                    Gizmos.color = Color.yellow;
                }
                Gizmos.DrawLine(sideOrigin, sideOrigin + Vector3.down * vertRayLength);

                // Optional: draw the origin as a small sphere so you can see the exact side point
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(sideOrigin, 0.02f);
            }
        }

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
        Vector2 moveRawIn = context.ReadValue<Vector2>();
        // Deadzone (separate for x and y, taking direction into account)
        if (Mathf.Abs(moveRawIn.x) < lowerDeadzone)
        {
            moveRawIn.x = 0;
        }
        else if (Mathf.Abs(moveRawIn.x) > upperDeadzone)
        {
            moveRawIn.x = Mathf.Sign(moveRawIn.x);
        }
        if (Mathf.Abs(moveRawIn.y) < lowerDeadzone)
        {
            moveRawIn.y = 0;
        }
        else if (Mathf.Abs(moveRawIn.y) > upperDeadzone)
        {
            moveRawIn.y = Mathf.Sign(moveRawIn.y);
        }
        moveInput = moveRawIn;
    }
    public void onMobileLeftPressed()
    {
        moveInput = new Vector2(-1, moveInput.y);
    }
    public void onMobileLeftReleased()
    {
        moveInput = new Vector2(0, moveInput.y);
    }
    public void onMobileRightPressed()
    {
        moveInput = new Vector2(1, moveInput.y);
    }
    public void onMobileRightReleased()
    {
        moveInput = new Vector2(0, moveInput.y);
    }
    public void onMobileUpPressed()
    {
        moveInput = new Vector2(moveInput.x, 1);
    }
    public void onMobileUpReleased()
    {
        moveInput = new Vector2(moveInput.x, 0);
    }
    public void onMobileDownPressed()
    {
        moveInput = new Vector2(moveInput.x, -1);
    }
    public void onMobileDownReleased()
    {    // might not be needed because of crouch button
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
    public void onRunPressed()
    {
        //print("run");
        runPressed = true;

        if (pressRunToGrab && (!crouchToGrab || direction.y < -0.5f) && !carrying)
        {
            checkForCarry();
        }
    }
    public void onRunReleased()
    {
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
    public void onJumpPressed()
    {
        jumpTimer = Time.time + jumpDelay;
        jumpPressed = true;
        spinJumpQueued = false;
    }
    public void onJumpReleased()
    {
        jumpPressed = false;
    }

    // Spin
    public void Spin(InputAction.CallbackContext context)
    {
        if (inputLocked)
            return;

        if (context.performed)
        {
            onSpinPressed();
        }
        if (context.canceled)
        {
            onSpinReleased();
        }
    }

    public void onSpinPressed()
    {
        if (!canSpinJump) return;

        print("spin!");
        jumpTimer = Time.time + jumpDelay;
        spinPressed = true;
        spinJumpQueued = true;
    }

    public void onSpinReleased()
    {
        spinPressed = false;
    }

    // Use
    public void Use(InputAction.CallbackContext context)
    {
        // use lever
        if (context.performed)
        {
            onUsePressed();
        }
    }
    public void onUsePressed()
    {
        // for right now, use the NEWEST lever we entered
        if (useableObjects.Count > 0)
        {
            useableObjects[^1].Use(this);
        }
    }

    // MarioAbility Actions
    // Shoot
    public void Shoot(InputAction.CallbackContext context)
    {
        if (inputLocked)
            return;
        
        if (context.performed)
        {
            onShootPressed();
        }
        if (context.canceled)
        {
            onShootReleased();
        }
    }
    public void onShootPressed()
    {
        foreach (MarioAbility ability in abilities)
        {
            shootPressed = true;
            ability.onShootPressed();
        }
    }
    public void onShootReleased()
    {
        foreach (MarioAbility ability in abilities)
        {
            shootPressed = false;
            //ability.onShootReleased();    // Add when needed
        }
    }

    // ExtraAction
    public void ExtraAction(InputAction.CallbackContext context)
    {
        if (inputLocked)
            return;
        
        if (context.performed)
        {
            onExtraActionPressed();
        }
    }
    public void onExtraActionPressed()
    {
        foreach (MarioAbility ability in abilities)
        {
            ability.onExtraActionPressed();
        }
    }

    public void Freeze()
    {
        // pause animations
        animator.enabled = false;
        // pause physics
        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        frozen = true;
    }

    public void Unfreeze()
    {
        // unpause animations
        animator.enabled = true;
        // unpause physics
        rb.bodyType = RigidbodyType2D.Dynamic;

        frozen = false;
    }

    void checkForCarry()
    {
        //print("Check carry");

        if (dead)
        {
            return; // Fixes issue of picking an object back up on the frame you die
        }

        // raycast in front of feet of mario
        RaycastHit2D[] hit = Physics2D.RaycastAll(transform.position + new Vector3(0, grabRaycastHeight, 0), facingRight ? Vector2.right : Vector2.left, 0.6f);


        foreach (RaycastHit2D h in hit)
        {
            // if object has objectphysics script
            if (h.collider.gameObject.GetComponent<ObjectPhysics>() != null)
            {
                ObjectPhysics obj = h.collider.gameObject.GetComponent<ObjectPhysics>();
                // not carried and carryable
                if (!obj.carried && obj.carryable)
                {
                    carry(obj);
                    return;
                }
            }
        }
    }

    void carry(ObjectPhysics obj)
    {
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

    public void dropCarry()
    {
        //print("drop!");
        carrying = false;

        animator.SetTrigger("grab");

        // get object from mario's object holder
        ObjectPhysics obj = heldObjectPosition.transform.GetChild(0).gameObject.GetComponent<ObjectPhysics>();
        obj.transform.parent = null;
        obj.transform.rotation = Quaternion.identity;

        //obj.transform.position = new Vector3(transform.position.x + (facingRight ? 1 : -1), transform.position.y + (powerupState == PowerupState.small ? 0f : -.5f), transform.position.z);
        float halfwidth = obj.width / 2;
        float offset = powerupState == PowerupState.small ? 0f : -0.5f;
        Vector2? raycastPoint = ThrowRaycast(offset, 1f + halfwidth, obj.wallMask);
        if (raycastPoint != null)
        {
            obj.transform.position = (Vector2)raycastPoint + new Vector2(facingRight ? -halfwidth : halfwidth, 0);
            // move mario back (todo: mario might get stuck in a wall if he throws an object in a one block gap)
            transform.position = new Vector3(facingRight ? (raycastPoint.Value.x - obj.width - 0.5f) : (raycastPoint.Value.x + obj.width + 0.5f), transform.position.y, transform.position.z);
        }
        else
        {
            obj.transform.position = transform.position + new Vector3(facingRight ? 1 : -1, offset, 0);
        }

        // sound
        if (dropSound != null)
            audioSource.PlayOneShot(dropSound);

        obj.getDropped(facingRight);
    }

    void throwCarry()
    {
        //print("throw!");

        carrying = false;

        // check if heldObjectPosition has an object
        if (heldObjectPosition.transform.childCount == 0)
        {
            return;
        }

        // todo: throw animation

        // get object from mario's object holder
        ObjectPhysics obj = heldObjectPosition.transform.GetChild(0).gameObject.GetComponent<ObjectPhysics>();
        obj.transform.parent = null;
        obj.transform.rotation = Quaternion.identity;

        float halfwidth = obj.width / 2;
        float offset = powerupState == PowerupState.small ? 0.1f : -0.1f;
        Vector2? raycastPoint = ThrowRaycast(offset, 1f + halfwidth, obj.wallMask);
        if (raycastPoint != null)
        {
            obj.transform.position = (Vector2)raycastPoint + new Vector2(facingRight ? -halfwidth : halfwidth, 0);
            // move mario back (todo: mario might get stuck in a wall if he throws an object in a one block gap)
            transform.position = new Vector3(facingRight ? (raycastPoint.Value.x - obj.width - 0.5f) : (raycastPoint.Value.x + obj.width + 0.5f), transform.position.y, transform.position.z);
        }
        else
        {
            obj.transform.position = transform.position + new Vector3(facingRight ? 1 : -1, offset, 0);
        }

        // sound
        if (throwSound != null)
            audioSource.PlayOneShot(throwSound);

        obj.GetThrown(facingRight);
    }

    // Raycasts from the specified vertical offset and returns the point of contact (if any)
    // TODO: maybe change it to 2 raycasts (one on top, one on bottom) to make sure that the object wont go inside a wall
    Vector2? ThrowRaycast(float offset, float distance, int layerMask)
    {
        layerMask &= ~(1 << gameObject.layer);  // remove mario's layer from the layermask
        Vector3 start = transform.position + new Vector3(0, offset, 0);
        RaycastHit2D hit = Physics2D.Raycast(start, facingRight ? Vector2.right : Vector2.left, distance, layerMask);

        if (hit.collider != null)
        {
            return hit.point;
        }
        else
        {
            return null;
        }
    }

    public void resetSpriteLibrary()
    {
        GetComponent<SpriteLibrary>().spriteLibraryAsset = normalSpriteLibrary;
    }

    /* Useable Objects */
    // Objects like levers use these to let Mario know that they are near him
    // When the Use button is pressed, Mario will activate one of these objects
    public void AddUseableObject(UseableObject obj)
    {
        if (!useableObjects.Contains(obj))
        {
            useableObjects.Add(obj);
        }
    }

    public void RemoveUseableObject(UseableObject obj)
    {
        if (useableObjects.Contains(obj))
        {
            useableObjects.Remove(obj);
        }
    }

    /* Pushing */
    // Pushable objects use these to let Mario know that he is pushing them
    public void StartPushing(ObjectPhysics pushObject, float speed)
    {
        pushing = true;
        pushingObject = pushObject;
        pushingSpeed = speed;
    }

    public void StopPushing()
    {
        print("Stopped pushing");
        pushing = false;
        pushingObject = null;
    }

}