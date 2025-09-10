using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// This script lets Mario enter a pipe
public class Pipe : MonoBehaviour
{
    public Transform connection;
    public enum Direction { Up, Down, Left, Right }
    public enum AlignType
    {
        None, // Don't align the player at all. He will just move in the direction of the pipe
        Slow, // Slowly align the player with the pipe by moving him directly towards the enter position
        Fast, // Quickly align the player with the pipe while he is still moving
        Instant // Instantly teleport the player to be aligned with the pipe
    }
    public Direction enterDirection = Direction.Down;
    public Direction exitDirection = Direction.Up;

    // How far in/out of the pipe the player should be when they enter/exit
    // Should be smaller for tiny pipes
    public float enterDistance = 1f; // How far into the pipe the player should go (relative to the position of the pipe)
    public AlignType enterAlignType = AlignType.Slow; // How the player should align when entering the pipe
    public float insideExitDistance = 1f; // How far the player should be inside the pipe when they start to exit (relative to the position of the exit)
    public float exitDistance = 1f; // How far out of the pipe the player should be when they exit

    public bool instantExit = false; // If true, don't animate the player exiting the pipe (like 1-1 Underground)
    public AudioClip warpEnterSound;
    public bool requireGroundToEnter = true; // New option to require the player to be on the ground
    private AudioSource audioSource;
    private bool isEnteringPipe = false; // Track if the player is already entering the pipe

    public bool snapCameraX = false;
    public bool snapCameraY = false;

    public static System.Action<Pipe, Transform> OnAnyEnterStarted;
    public static System.Action<Pipe, Transform> OnAnyExitFinished;

    public UnityEvent onPlayerEnter;

    private bool isMarioExited = false;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isEnteringPipe && connection != null && other.CompareTag("Player"))
        {
            if (!requireGroundToEnter || (requireGroundToEnter && IsPlayerOnGround(other.gameObject)))
            {
                if (CorrectDirectionPressed(other.GetComponent<MarioMovement>()))
                {
                    StartCoroutine(Enter(other.transform));
                }
            }
        }
    }

    private bool IsPlayerOnGround(GameObject player)
    {
        return player.GetComponent<MarioMovement>().onGround;
    }

    public bool IsMarioExited()
    {
        return isMarioExited;
    }

    public void MarioExitsPipe()
    {
        isMarioExited = true;
    }

    private IEnumerator Enter(Transform player)
    {
        // helpers
        bool AnimatorHas(Animator a, string p){ if(!a) return false; foreach (var x in a.parameters) if (x.name==p) return true; return false; }

        /* ENTERING */
        OnAnyEnterStarted?.Invoke(this, player);
        onPlayerEnter?.Invoke();

        var marioMovement  = player.GetComponent<MarioMovement>();
        var playerRb = player.GetComponent<Rigidbody2D>();
        var playerCol = player.GetComponent<Collider2D>();
        var playerAnim = player.GetComponent<Animator>();
        var playerSprite = player.GetComponent<SpriteRenderer>();

        while (marioMovement && marioMovement.groundPoundRotating) yield return null;

        isEnteringPipe = true;
        PlayWarpEnterSound();

        if (playerRb){ playerRb.velocity = Vector2.zero; playerRb.isKinematic = true; }
        if (playerCol) playerCol.enabled = false;

        if (playerAnim)
        {
            playerAnim.SetBool("onGround", enterDirection != Direction.Up);
            playerAnim.SetBool("isSkidding", false);
            playerAnim.SetBool("isCrouching", enterDirection == Direction.Down);
            bool horizEnter = (enterDirection == Direction.Left || enterDirection == Direction.Right);
            playerAnim.SetBool("isRunning", horizEnter);
            playerAnim.SetFloat("Horizontal", horizEnter ? 1f : 0f);
        }
        if (marioMovement) marioMovement.StopGroundPound();
        if (playerSprite && (enterDirection == Direction.Left || enterDirection == Direction.Right))
            playerSprite.flipX = (enterDirection == Direction.Left);

        if (marioMovement) marioMovement.enabled = false;

        // enter movement
        Vector2 enterDirVec = DirectionToVector(enterDirection) * enterDistance;
        Vector3 enteredPosition = transform.position + (Vector3)enterDirVec;
        switch (enterAlignType)
        {
            case AlignType.None:
                var t = transform.position;
                if (enterDirection == Direction.Up || enterDirection == Direction.Down) t.x = player.position.x; else t.y = player.position.y;
                enteredPosition = t + (Vector3)enterDirVec;
                break;
            case AlignType.Instant:
                var p2 = player.position;
                if (enterDirection == Direction.Up || enterDirection == Direction.Down) p2.x = enteredPosition.x; else p2.y = enteredPosition.y;
                player.position = p2;
                break;
        }
        yield return Move(player, enteredPosition, false);

        yield return new WaitForSeconds(1f);

        /* EXITING */
        PlayWarpEnterSound();

        var camPos = Camera.main.transform.position;
        if (snapCameraX) camPos.x = connection.position.x;
        if (snapCameraY) camPos.y = connection.position.y;
        Camera.main.transform.position = camPos;

        if (!instantExit)
        {
            // common setup that doesn't force swim/ground yet
            if (playerAnim)
            {
                playerAnim.SetBool("isSkidding", false);
                playerAnim.SetBool("isCrouching", false);
                bool horizExit = exitDirection == Direction.Left || exitDirection == Direction.Right;
                playerAnim.SetBool("isRunning", horizExit);
                playerAnim.SetFloat("Horizontal", horizExit ? 1f : 0f);
                if (playerSprite && horizExit) playerSprite.flipX = exitDirection == Direction.Left;
            }

            Vector2 outDir = DirectionToVector(exitDirection);
            Vector2 insideOff = outDir * -insideExitDistance;
            Vector2 exitOff = outDir *  exitDistance;

            if (PowerStates.IsBig(marioMovement.powerupState) &&
                (exitDirection == Direction.Down || exitDirection == Direction.Up))
            {
                exitOff += outDir * 0.5f;
            }

            // position just before pipe mouth
            player.position = connection.position + (Vector3)insideOff;

            // decide destination environment BEFORE moving out
            // sample a little *past* the final exit point to avoid being inside the pipe collider
            Vector3 finalExitPos = connection.position + (Vector3)exitOff + (Vector3)(outDir * 0.02f);
            bool exitingIntoWater = Physics2D.OverlapPoint(finalExitPos, LayerMask.GetMask("Water"));

            // sync gameplay + animator NOW so the tween shows the correct pose
            if (marioMovement) marioMovement.swimming = exitingIntoWater;
            if (playerAnim)
            {
                playerAnim.SetBool("onGround", !exitingIntoWater);
                if (AnimatorHas(playerAnim,"swim")) playerAnim.SetBool("swim", exitingIntoWater);
                if (AnimatorHas(playerAnim,"enterWater")) playerAnim.SetBool("enterWater", exitingIntoWater);
                if (AnimatorHas(playerAnim,"exitWater")) playerAnim.SetBool("exitWater", !exitingIntoWater);
            }

            // now move out while already in the correct state
            yield return Move(player, connection.position + (Vector3)exitOff, true);
        }
        else
        {
            player.position = connection.position;

            // instant exit: still decide env at the final spot
            bool inWater = Physics2D.OverlapPoint(player.position, LayerMask.GetMask("Water"));
            if (marioMovement) marioMovement.swimming = inWater;
            var a = player.GetComponent<Animator>();
            if (a) a.SetBool("onGround", !inWater);
        }

        // unfreeze
        if (playerRb)  playerRb.isKinematic = false;
        if (playerCol) playerCol.enabled    = true;
        if (marioMovement) marioMovement.enabled = true;

        OnAnyExitFinished?.Invoke(this, player);
        isEnteringPipe = false;
    }

    private Vector2 DirectionToVector(Direction direction)
    {
        switch (direction)
        {
            case Direction.Down:
                return Vector2.down;
            case Direction.Right:
                return Vector2.right;
            case Direction.Up:
                return Vector2.up;
            case Direction.Left:
                return Vector2.left;
            default:
                return Vector2.zero;
        }
    }

    private IEnumerator Move(Transform player, Vector3 endPosition, bool exiting)
    {
        float elapsed = 0f;
        float duration = 1f;

        Vector3 startPosition = player.position;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            // If fast aligning, align the player's x or y position to the pipe's x or y position in HALF the time
            if (enterAlignType == AlignType.Fast && !exiting)
            {
                bool leftRight = enterDirection == Direction.Left || enterDirection == Direction.Right;
                Vector3 halfPosition = leftRight ? new Vector3((endPosition.x + startPosition.x) / 2, endPosition.y, startPosition.z) : new Vector3(endPosition.x, (endPosition.y + startPosition.y) / 2, startPosition.z);
                if (t < 0.5f)
                {
                    player.position = Vector3.Lerp(startPosition, halfPosition, t * 2);
                }
                else
                {
                    player.position = Vector3.Lerp(halfPosition, endPosition, (t - 0.5f) * 2);
                }
            }
            else
            {
                player.position = Vector3.Lerp(startPosition, endPosition, t);
            }
            elapsed += Time.deltaTime;

            yield return null;
        }
        player.position = endPosition;
    }

    private bool CorrectDirectionPressed(MarioMovement playerMovement)
    {
        Vector2 moveInput = playerMovement.moveInput;

        switch (enterDirection)
        {
            case Direction.Down:
                return playerMovement.crouchPressed; // Holding down
            case Direction.Right:
                return moveInput.x > 0; // Holding right
            case Direction.Up:
                return moveInput.y > 0; // Holding up
            case Direction.Left:
                return moveInput.x < 0; // Holding left
            default:
                return false;
        }
    }

    private void PlayWarpEnterSound()
    {
        if (warpEnterSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(warpEnterSound);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        // Draw the enter direction as a line from the pipe's position
        Vector2 enterDirectionVector = DirectionToVector(enterDirection) * enterDistance;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + enterDirectionVector);
        Gizmos.DrawRay(transform.position, enterDirectionVector); // Draw the arrow for the enter direction

        // Draw the exit direction as a line from the connection point
        if (connection != null)
        {
            Vector2 exitDirectionVector = DirectionToVector(exitDirection) * exitDistance;
            Gizmos.DrawLine(connection.position, (Vector2)connection.position + exitDirectionVector);
            Gizmos.DrawRay(connection.position, exitDirectionVector); // Draw the arrow for the exit direction
        }
    }
    
    public void StartRideFor(Transform rider)
    {
        if (connection == null) return;
        StartCoroutine(EnterNPC(rider));
    }
    
    // Generic entry for non-player riders (CheepCheep etc.)
    private IEnumerator EnterNPC(Transform rider)
    {
        // Disable rider movement/collisions while traveling
        var rb = rider.GetComponent<Rigidbody2D>();
        var col = rider.GetComponent<Collider2D>();
        var ai = rider.GetComponent<EnemyAI>();
        var phys = rider.GetComponent<ObjectPhysics>();

        if (ai != null) ai.enabled = false;
        if (phys != null) phys.enabled = false;

        if (rb != null) { rb.velocity = Vector2.zero; rb.isKinematic = true; }
        if (col != null) col.enabled = false;

        // ENTER: align/slide just like Mario
        Vector2 enterVec = DirectionToVector(enterDirection) * enterDistance;
        Vector3 enterPos = transform.position + (Vector3)enterVec;

        // If your pipe supports Slow/Fast/Instant, you can branch here like your player coroutine.
        // For simplicity, we "Instant" align on the axis perpendicular to the movement:
        Vector3 temp = rider.position;
        switch (enterDirection)
        {
            case Direction.Down:
            case Direction.Up: temp.x = enterPos.x; break;
            case Direction.Left:
            case Direction.Right: temp.y = enterPos.y; break;
        }
        rider.position = temp;

        yield return Move(rider, enterPos, false);

        yield return new WaitForSeconds(1f);

        // EXIT: same offsets as player, just skip camera logic
        if (!instantExit)
        {
            Vector2 outDir = DirectionToVector(exitDirection);
            Vector3 inside = (Vector3)(outDir * -insideExitDistance);
            Vector3 outside = (Vector3)(outDir * exitDistance);

            rider.position = connection.position + inside;
            yield return Move(rider, connection.position + outside, true);
        }
        else
        {
            rider.position = connection.position;
        }

        // Re-enable rider systems
        if (rb != null) rb.isKinematic = false;
        if (col != null) col.enabled = true;

        if (phys != null) phys.enabled = true;
        if (ai != null) ai.enabled = true;
    }
}