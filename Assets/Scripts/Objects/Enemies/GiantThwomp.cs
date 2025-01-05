using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;
using UnityEngine.Playables;

[System.Serializable]
public struct RaycastConfiguration
{
    public Vector2 offset;       // Offset from the object's position
    public Vector2 size;         // Size of the box (if using boxcast)
    public Vector2 effectSpawnOffset; // Offset where the effect will spawn
    public List<GiantThwomp.FallDirections> allowedDirections; // Allowed directions for this configuration
}

public class GiantThwomp : EnemyAI, IGroundPoundable
{
    [Header("Giant Thwomp")]
    private List<Material> materials;  // Should be the special Thwomp Color Overlay material
    public Color hitColor = Color.red;
    private Color defaultColor;
    public BoxCollider2D mainCollider;
    public BoxCollider2D vulnerableCollider;
    public BoxCollider2D fallBackCollider;
    public GameObject hurtEffectPrefab;
    public float addDetectionRange = 0f; // Additional range to detect Mario added to the width/height of the Thwomp
    public float landWaitTime = 1f; // Time the Thwomp waits after landing before rising back up
    public float riseSpeed = 1f; // The speed the Thwomp rises back up after landing
    public float vulnerableTime = 3f; // The time the Thwomp remains vulnerable after being hit by Mario's cape
    public int health = 3; // Number of hits the Thwomp can take before falling back
    public GameObject hitEffect;
    private Animator animator;

    // Sounds
    private AudioSource audioSource;
    public AudioClip thwompLandSound;
    public AudioClip thwompHurtSound;

    [Header("Hit Effect Settings")]
    public List<RaycastConfiguration> raycastConfigurations; // Configurable raycasts
    public LayerMask hitEffectTriggerLayers;                // Layers that trigger hit effects

    public enum ThwompStates {
        Idle, // The Thwomp remains stationary, waiting for the player to come into range
        Falling, // The Thwomp falls rapidly towards the player or a specific target area
        Landed, // The Thwomp has landed on the ground or wall (or stops after falling up) and is waiting to move back
        Rising, // The Thwomp rises back to its original position after completing its fall and stun duration
        Vulnerable, // The Thwomp becomes vulnerable after being hit by Mario's cape, remaining in this state for a certain period
        FallBack, // After losing all health, the Thwomp falls backward and Mario can ground pound it
    }
    private ThwompStates currentState = ThwompStates.Idle;
    private bool checkSides = false; // Changes to true after first fall
    private Vector2 initialPosition; // The initial position of the Thwomp
    public GameObject poofEffectPrefab;
    public PlayableDirector defeatTimeline;

    public enum FallDirections {
        Down,
        Left,
        Right,
        Up,
    }

    private FallDirections fallDirection = FallDirections.Down; // The direction the Thwomp is falling in (if rising, opposite of this direction)

    private float internalGravity;  // The Thwomp's gravity value set in the inspector (saved because gravity is set to 0 when the Thwomp is idle)
    public LayerMask shakeTriggerLayers;
    public LayerMask audioTriggerLayers;  
    public float spinAttackBouncePower = 4f;
    public AudioClip marioLaunched;
    [SerializeField] private CameraFollow cameraFollow;

    public SpikesFlagPole flagpole;

    protected override void Start()
    {
        base.Start();
        internalGravity = gravity;
        gravity = 0f;
        initialPosition = transform.position;
        audioSource = GetComponent<AudioSource>();
        materials = new List<Material>();
        animator = GetComponent<Animator>();
        cameraFollow = FindObjectOfType<CameraFollow>();
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
        {
            materials.Add(renderer.material);
        }
        defaultColor = materials[0].GetColor("_Color");
    }

    private void DetectPlayer() {
        MarioMovement player = GameManager.Instance.GetPlayer(0);

        if (player != null)
        {
            // Check if Mario is underneath, to the side, or above the Thwomp
            if (player.transform.position.y < transform.position.y && Mathf.Abs(player.transform.position.x - transform.position.x) < width / 2 + addDetectionRange)
            {
                fallDirection = FallDirections.Down;
                ChangeState(ThwompStates.Falling);
            }
            else if (checkSides && player.transform.position.x < transform.position.x && Mathf.Abs(player.transform.position.y - transform.position.y) < height / 2 + addDetectionRange)
            {
                fallDirection = FallDirections.Left;
                ChangeState(ThwompStates.Falling);
            }
            else if (checkSides && player.transform.position.x > transform.position.x && Mathf.Abs(player.transform.position.y - transform.position.y) < height / 2 + addDetectionRange)
            {
                fallDirection = FallDirections.Right;
                ChangeState(ThwompStates.Falling);
            }
            else if (player.transform.position.y > transform.position.y && Mathf.Abs(player.transform.position.x - transform.position.x) < width / 2 + addDetectionRange)
            {
                fallDirection = FallDirections.Up;
                ChangeState(ThwompStates.Falling);
            }
        }
    }

    public void DetectFlagpole()
    {
        // Ensure the flagpole exists
        if (flagpole != null)
        {
            // Adjust fall direction to target the flagpole along the x-axis
            float directionToFlagpoleX = Mathf.Sign(flagpole.transform.position.x + 2f - transform.position.x);

            if (directionToFlagpoleX > 0)
            {
                fallDirection = FallDirections.Right;
            }
            else if (directionToFlagpoleX < 0)
            {
                fallDirection = FallDirections.Left;
            }
            else
            {
                // Optional: Handle case where Thwomp and flagpole are perfectly aligned on the x-axis
                Debug.Log("Thwomp and flagpole are aligned on the x-axis.");
            }

            // Ensure the Thwomp is not in the FallBack state before changing state
            if (currentState != ThwompStates.FallBack)
            {
                ChangeState(ThwompStates.Falling);
            }

            Debug.Log("Flagpole detected and Thwomp is reacting!");
        }
        else
        {
            Debug.LogWarning("Flagpole reference is missing.");
        }
    }

    protected override void Update()
    {
        base.Update();
        
        switch (currentState)
        {
            case ThwompStates.Idle:
                DetectPlayer();
                break;
            case ThwompStates.Falling:
                break;
            case ThwompStates.Landed:
                break;
            case ThwompStates.Rising:
                if ((transform.position.y >= initialPosition.y && fallDirection == FallDirections.Down)
                    || (transform.position.x >= initialPosition.x && fallDirection == FallDirections.Left)
                    || (transform.position.x <= initialPosition.x && fallDirection == FallDirections.Right)
                    || (transform.position.y <= initialPosition.y && fallDirection == FallDirections.Up))
                {
                    ChangeState(ThwompStates.Idle);
                }
                break;
            default:
                break;
        }
    }

    private void ChangeState(ThwompStates newState)
    {
        ThwompStates oldState = currentState;
        currentState = newState;
        print("Changing state to " + newState + " from " + oldState);

        switch (newState)
        {
            case ThwompStates.Idle:
                animator.SetBool("angry", false);
                gravity = 0f;
                velocity = Vector2.zero;
                break;
            case ThwompStates.Falling:
                if (fallDirection != FallDirections.Up) {
                    animator.SetBool("angry", true);
                }
                
                gravity = internalGravity;
                checkSides = true;
                break;
            case ThwompStates.Landed:
                animator.SetBool("flip", false);
                gravity = 0f;
                velocity = Vector2.zero;

                if (oldState != ThwompStates.Vulnerable) {
                    InstantiateHitEffect();
                }            

                if (oldState == ThwompStates.Falling)
                {
                    if (audioSource != null && audioTriggerLayers != 0)
                    {
                        // Check for collision below (ground) and sides
                        Collider2D hitCollider = Physics2D.OverlapBox(
                            transform.position + Vector3.down * mainCollider.size.y / 2, // Adjust position to check below
                            new Vector2(mainCollider.size.x, 0.1f), // Slim horizontal box to detect ground
                            0,
                            audioTriggerLayers
                        );

                        if (hitCollider == null)
                        {
                            // Check sides (current box alignment)
                            hitCollider = Physics2D.OverlapBox(
                                transform.position,
                                mainCollider.size,
                                0,
                                audioTriggerLayers
                            );
                        }

                        if (hitCollider != null)
                        {
                            audioSource.PlayOneShot(thwompLandSound);
                        }
                    }          
                }

                // Wait at the bottom for a bit
                Invoke(nameof(ThwompRise), landWaitTime);
                break;
            case ThwompStates.Rising:
                animator.SetBool("angry", false);
                animator.SetBool("flip", false);
                switch (fallDirection)
                {
                    case FallDirections.Down:
                        velocity = new Vector2(0, riseSpeed);
                        break;
                    case FallDirections.Left:
                        realVelocity = new Vector2(riseSpeed, 0);
                        break;
                    case FallDirections.Right:
                        realVelocity = new Vector2(-riseSpeed, 0);
                        break;
                    case FallDirections.Up:
                        velocity = new Vector2(0, -riseSpeed);
                        break;
                    default:
                        break;
                }
                gravity = 0f;
                break;
            case ThwompStates.Vulnerable:
                animator.SetBool("flip", true);
                gravity = 0f;
                velocity = Vector2.zero;
                mainCollider.enabled = false;
                vulnerableCollider.enabled = true;
                Invoke(nameof(FlipBack), vulnerableTime);
                break;
            case ThwompStates.FallBack:
                mainCollider.enabled = false;
                vulnerableCollider.enabled = false;
                fallBackCollider.enabled = true;
                gameObject.layer = LayerMask.NameToLayer("Ground");
                animator.SetBool("fall", true);
                break;
            default:
                break;
        }
    }

    private void InstantiateHitEffect()
    {
        foreach (var config in raycastConfigurations)
        {
            // Skip this configuration if the current direction is not allowed
            if (!config.allowedDirections.Contains(fallDirection))
            {
                continue;
            }

            Vector2 origin = (Vector2)transform.position + config.offset;

            // Perform a boxcast to detect objects in the specified area
            RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, config.size, 0, Vector2.zero, 0);

            foreach (var hit in hits)
            {
                if (hit.collider != null && (hitEffectTriggerLayers.value & (1 << hit.collider.gameObject.layer)) != 0)
                {
                    // Calculate the position to spawn the effect
                    Vector2 effectPosition = (Vector2)transform.position + config.effectSpawnOffset;
                    Instantiate(hitEffect, effectPosition, Quaternion.identity);
                }
            }
        }
    }

    private void ThwompRise()
    {
        if (currentState == ThwompStates.Landed) {
            ChangeState(ThwompStates.Rising);
        }
    }

    private void FlipBack()
    {
        if (currentState == ThwompStates.FallBack)
        {
            return;
        }
        mainCollider.enabled = true;
        vulnerableCollider.enabled = false;

        // Ensure the Thwomp does not reset to Landed if targeting the flagpole
        if (flagpole != null && Mathf.Abs(flagpole.transform.position.x - transform.position.x) < mainCollider.size.x / 2)
        {
            Debug.Log("Thwomp is targeting the flagpole, staying in current state.");
            return;
        }  

        ChangeState(ThwompStates.Landed);
    }

    private void TriggerScreenShake(GameObject other) {
        // Check if the current object's layer is in the shakeTriggerLayers mask
        if (other != null && (shakeTriggerLayers.value & (1 << other.layer)) != 0)
        {
            cameraFollow.ShakeCameraRepeatedlyDefault();
        }
    }

    // We are overriding HorizontalMovement and VerticalMovement because the gravity can be applied horizontally instead of just vertically
    protected override Vector3 HorizontalMovement(Vector3 pos)
    {
        // Velocity
        pos += (movingLeft ? -1 : 1) * adjDeltaTime * velocity.x * Vector3.right;

        // Sideways Gravity
        if (gravity != 0 && (fallDirection == FallDirections.Left || fallDirection == FallDirections.Right))
        {
            // realvelocity automatically handles moving left/right
            realVelocity = realVelocity + (gravity * adjDeltaTime * (fallDirection == FallDirections.Left ? -1 : 1) * Vector2.right);
        }

        return pos;
    }

    protected override Vector3 VerticalMovement(Vector3 pos)
    {
        // Velocity
        pos.y += adjDeltaTime * velocity.y;

        // Gravity
        if (gravity != 0 && (fallDirection == FallDirections.Down || fallDirection == FallDirections.Up))
        {
            velocity.y += gravity * adjDeltaTime * (fallDirection == FallDirections.Down ? -1 : 1);
        }

        return pos;
    }

    protected override void onTouchWall(GameObject other)
    {
        if (currentState == ThwompStates.Falling && (fallDirection == FallDirections.Left || fallDirection == FallDirections.Right))
        {
            TriggerScreenShake(other);
            InstantiateHitEffect(); // Now uses multiple raycasts
            ChangeState(ThwompStates.Landed);
        }        
    }

    public override void Land(GameObject other = null) {
        if (currentState == ThwompStates.Falling && (fallDirection == FallDirections.Down || fallDirection == FallDirections.Up))
        {
            TriggerScreenShake(other);
            InstantiateHitEffect(); // Now uses multiple raycasts
            ChangeState(ThwompStates.Landed);
        }
    }

    protected override void HitCeiling(GameObject other = null)
    {
        if (currentState == ThwompStates.Falling && fallDirection == FallDirections.Up)
        {
            ChangeState(ThwompStates.Landed);
        } else if (currentState == ThwompStates.Rising && fallDirection == FallDirections.Down)
        {
            ChangeState(ThwompStates.Idle);
        }
    }

    public override void OnCapeAttack(bool hitFromLeft)
    {
        if (currentState == ThwompStates.Landed)
        {
            ChangeState(ThwompStates.Vulnerable);
        }
    }

    protected override void hitByStomp(GameObject player)
    {
        switch (currentState)
        {
            case ThwompStates.Vulnerable:
                // Damage the Thwomp
                health--;
                if (health <= 0)
                {
                    ChangeState(ThwompStates.FallBack);
                }
                // Change the color of the Thwomp
                foreach (Material material in materials)
                {
                    material.SetColor("_Color", hitColor);
                }
                Invoke(nameof(StopTint), 0.2f);
                // Play the hurt sound
                if (thwompHurtSound != null)
                {
                    audioSource.PlayOneShot(thwompHurtSound);
                }
                player.GetComponent<MarioMovement>().Jump();

                // Spawn the hurt effect
                if (hurtEffectPrefab != null)
                {
                    Instantiate(hurtEffectPrefab, GetComponent<Collider2D>().ClosestPoint(player.transform.position), Quaternion.identity);
                }
                break;
            default:
                base.hitByStomp(player);
                break;
        }
    }

    private void StopTint()
    {
        foreach (Material material in materials)
        {
            material.SetColor("_Color", defaultColor);
        }
    }

    public void OnGroundPound()
    {
        if (currentState == ThwompStates.FallBack)
        {
            GameManager.Instance.StopTimer();
            GameManager.Instance.StopAllMusic();

            // Play the poof effect
            if (poofEffectPrefab != null)
            {
                Instantiate(poofEffectPrefab, transform.position, Quaternion.identity);
            }

            // Play the defeat timeline
            if (defeatTimeline != null)
            {
                defeatTimeline.Play();
            }
            else
            {
                Debug.LogWarning("Defeat timeline not assigned!");
            }

            Destroy(gameObject);
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other) {
        base.OnTriggerEnter2D(other);
        Debug.Log($"Collision with {other.name}");
        IDestructible destructible = other.GetComponent<IDestructible>();
        if (destructible != null)
        {
            destructible.OnDestruction();
            InstantiateHitEffect();

            // Use ObjectPhysics for knock away logic if available
            ObjectPhysics objectPhysics = other.GetComponent<ObjectPhysics>();
            if (objectPhysics != null)
            {
                // Determine the knock away direction based on Thwomp's current direction
                bool knockAwayDirection = other.transform.position.x < transform.position.x;
                objectPhysics.KnockAway(knockAwayDirection, true, KnockAwayType.rotate);
            }
        }

        if (other.gameObject.CompareTag("Player"))
        {
            MarioMovement playerScript = other.gameObject.GetComponent<MarioMovement>();

            if (playerScript != null)
            {
                // Only apply the trampoline effect if the Thwomp is moving up and the player is performing a spin attack
                if (fallDirection == FallDirections.Up && currentState == ThwompStates.Falling && playerScript.spinning)
                {
                    // Apply the trampoline-like bounce with increased power
                    Rigidbody2D playerRb = other.gameObject.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        // Apply a stronger upward force if the player is in a spin attack
                        playerRb.velocity = new Vector2(playerRb.velocity.x, playerRb.velocity.y * spinAttackBouncePower);
                    }

                    if (marioLaunched != null){
                        audioSource.PlayOneShot(marioLaunched);
                    }
                } 
                else
                {
                    Rigidbody2D playerRb = other.gameObject.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        playerRb.velocity = new Vector2(playerRb.velocity.x, playerRb.velocity.y);
                    }
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        foreach (var config in raycastConfigurations)
        {
            Vector2 origin = (Vector2)transform.position + config.offset;

            // Draw the detection area
            Gizmos.DrawWireCube(origin, config.size);

            // Draw the effect spawn position
            Gizmos.color = Color.green;
            Vector2 effectPosition = (Vector2)transform.position + config.effectSpawnOffset;
            Gizmos.DrawSphere(effectPosition, 0.1f);

            // Reset color for consistency in loops
            Gizmos.color = Color.red;
        }
    }
}