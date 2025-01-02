
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;

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

    public enum FallDirections {
        Down,
        Left,
        Right,
        Up,
    }

    private FallDirections fallDirection = FallDirections.Down; // The direction the Thwomp is falling in (if rising, opposite of this direction)

    private float internalGravity;  // The Thwomp's gravity value set in the inspector (saved because gravity is set to 0 when the Thwomp is idle)
    public LayerMask shakeTriggerLayers;
    public float spinAttackBouncePower = 4f;
    public AudioClip marioLaunched;
    [SerializeField] private CameraFollow cameraFollow;

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
                animator.SetBool("angry", true);
                gravity = internalGravity;
                checkSides = true;
                break;
            case ThwompStates.Landed:
                animator.SetBool("flip", false);
                gravity = 0f;
                velocity = Vector2.zero;

                InstantiateHitEffect();

                if (oldState == ThwompStates.Falling)
                {
                    // Play the landing sound
                    if (thwompLandSound != null)
                    {
                        audioSource.PlayOneShot(thwompLandSound);
                    }
                }

                // Wait at the bottom for a bit
                Invoke(nameof(ThwompRise), landWaitTime);
                break;
            case ThwompStates.Rising:
                animator.SetBool("flip", false);
                animator.SetBool("angry", false);
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
        if (hitEffect != null)
        {
            Vector2 hitPosition = transform.position;

            // Adjust the position based on the fall direction
            switch (fallDirection)
            {
                case FallDirections.Down:
                    hitPosition.y -= GetComponent<Collider2D>().bounds.extents.y;
                    break;
                case FallDirections.Left:
                    hitPosition.x -= GetComponent<Collider2D>().bounds.extents.x;
                    break;
                case FallDirections.Right:
                    hitPosition.x += GetComponent<Collider2D>().bounds.extents.x;
                    break;
                case FallDirections.Up:
                    hitPosition.y += GetComponent<Collider2D>().bounds.extents.y;
                    break;
            }

            // Instantiate the effect at the hit position
            Instantiate(hitEffect, hitPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("HitEffect prefab is not assigned.");
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
            ChangeState(ThwompStates.Landed);
        }        
    }

    public override void Land() {
        if (currentState == ThwompStates.Falling && (fallDirection == FallDirections.Down || fallDirection == FallDirections.Up))
        {
            ChangeState(ThwompStates.Landed);
        }
    }

    protected override void HitCeiling()
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
            // Play the poof effect
            if (poofEffectPrefab != null)
            {
                Instantiate(poofEffectPrefab, transform.position, Quaternion.identity);
            }
            Destroy(gameObject);
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other) {
        base.OnTriggerEnter2D(other);

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
}