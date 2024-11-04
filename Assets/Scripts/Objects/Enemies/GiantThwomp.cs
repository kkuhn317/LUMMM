using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GiantThwomp : EnemyAI
{
    [Header("Giant Thwomp")]
    public SpriteRenderer front;
    public Sprite idleSprite;
    public Sprite angrySprite;

    public float addDetectionRange = 0f; // Additional range to detect Mario added to the width/height of the Thwomp
    public float landWaitTime = 1f; // Time the Thwomp waits after landing before rising back up

    public float riseSpeed = 1f; // The speed the Thwomp rises back up after landing

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

    public enum FallDirections {
        Down,
        Left,
        Right,
        Up,
    }

    private FallDirections fallDirection = FallDirections.Down; // The direction the Thwomp is falling in (if rising, opposite of this direction)

    private float internalGravity;  // The Thwomp's gravity value set in the inspector (saved because gravity is set to 0 when the Thwomp is idle)

    protected override void Start()
    {
        base.Start();
        internalGravity = gravity;
        gravity = 0f;
        initialPosition = transform.position;
    }

    private void DetectPlayer() {
        MarioMovement player = GameManager.Instance.GetPlayer(0);
        // Check if Mario is underneath, to the side, or above the Thwomp
        if (player.transform.position.y < transform.position.y && Mathf.Abs(player.transform.position.x - transform.position.x) < width / 2 + addDetectionRange)
        {
            fallDirection = FallDirections.Down;
            ChangeState(ThwompStates.Falling);
        } else if (checkSides && player.transform.position.x < transform.position.x && Mathf.Abs(player.transform.position.y - transform.position.y) < height / 2 + addDetectionRange)
        {
            fallDirection = FallDirections.Left;
            ChangeState(ThwompStates.Falling);
        } else if (checkSides && player.transform.position.x > transform.position.x && Mathf.Abs(player.transform.position.y - transform.position.y) < height / 2 + addDetectionRange)
        {
            fallDirection = FallDirections.Right;
            ChangeState(ThwompStates.Falling);
        } else if (player.transform.position.y > transform.position.y && Mathf.Abs(player.transform.position.x - transform.position.x) < width / 2 + addDetectionRange)
        {
            fallDirection = FallDirections.Up;
            ChangeState(ThwompStates.Falling);
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

        print("Changing state to " + newState);

        currentState = newState;

        switch (newState)
        {
            case ThwompStates.Idle:
                front.sprite = idleSprite;
                gravity = 0f;
                velocity = Vector2.zero;
                break;
            case ThwompStates.Falling:
                front.sprite = angrySprite;
                gravity = internalGravity;
                checkSides = true;
                break;
            case ThwompStates.Landed:
                gravity = 0f;
                velocity = Vector2.zero;

                // Wait at the bottom for a bit
                Invoke(nameof(ThwompRise), landWaitTime);
                break;
            case ThwompStates.Rising:
                front.sprite = idleSprite;
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
                break;
            case ThwompStates.FallBack:
                break;
            default:
                break;
        }
    }

    private void ThwompRise()
    {
        ChangeState(ThwompStates.Rising);
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
            ChangeState(ThwompStates.Landed);
        }
    }

    public override void Land() {
        if (currentState == ThwompStates.Falling && (fallDirection == FallDirections.Down || fallDirection == FallDirections.Up))
        {
            ChangeState(ThwompStates.Landed);
        }
    }
}
