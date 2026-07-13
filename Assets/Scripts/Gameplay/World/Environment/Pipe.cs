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
        None,    // Don't align the player at all. He will just move in the direction of the pipe
        Slow,    // Slowly align the player with the pipe by moving him directly towards the enter position
        Fast,    // Quickly align the player with the pipe while he is still moving
        Instant  // Instantly teleport the player to be aligned with the pipe
    }
    public Direction enterDirection = Direction.Down;
    public Direction exitDirection = Direction.Up;

    public float enterDistance = 1f;
    public AlignType enterAlignType = AlignType.Slow;
    public float insideExitDistance = 1f;
    public float exitDistance = 1f;

    public bool instantExit = false;
    public AudioClip warpEnterSound;
    public bool requireGroundToEnter = true;
    private AudioSource audioSource;
    private bool isEnteringPipe = false;

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
            if (!requireGroundToEnter || IsPlayerOnGround(other.gameObject))
            {
                // MarioCore is on the ROOT, not the child collider (Body_Collider)
                var marioCore = other.GetComponent<MarioCore>()
                             ?? other.GetComponentInParent<MarioCore>();

                if (marioCore != null && CorrectDirectionPressed(marioCore))
                    StartCoroutine(Enter(marioCore.transform));
            }
        }
    }

    private bool IsPlayerOnGround(GameObject player)
    {
        var core = player.GetComponent<MarioCore>() ?? player.GetComponentInParent<MarioCore>();
        return core != null && core.State.OnGround;
    }

    public bool IsMarioExited() => isMarioExited;
    public void MarioExitsPipe() => isMarioExited = true;

    /// <summary>
    /// Finds the visually topmost SpriteRenderer among the given renderers
    /// (comparing by sorting layer index first, then by order within the layer)
    /// and places the player sprite one step behind it.
    /// </summary>
    private void ApplyPipeSorting(SpriteRenderer playerSprite, SpriteRenderer[] pipeRenderers)
    {
        if (playerSprite == null || pipeRenderers == null || pipeRenderers.Length == 0) return;

        SpriteRenderer highest = pipeRenderers[0];
        foreach (var r in pipeRenderers)
        {
            int rLayer    = SortingLayer.GetLayerValueFromID(r.sortingLayerID);
            int bestLayer = SortingLayer.GetLayerValueFromID(highest.sortingLayerID);
            if (rLayer > bestLayer || (rLayer == bestLayer && r.sortingOrder > highest.sortingOrder))
                highest = r;
        }

        playerSprite.sortingLayerName = highest.sortingLayerName;
        playerSprite.sortingOrder     = highest.sortingOrder - 1;
    }

    private IEnumerator Enter(Transform player)
    {
        bool AnimatorHas(Animator a, string p) { if (!a) return false; foreach (var x in a.parameters) if (x.name == p) return true; return false; }

        /* ENTERING */
        OnAnyEnterStarted?.Invoke(this, player);
        onPlayerEnter?.Invoke();

        var marioCore  = player.GetComponent<MarioCore>();
        // Rb and Collider are on the root, then cached by MarioCore
        var playerRb     = marioCore != null ? marioCore.Rb                    : player.GetComponent<Rigidbody2D>();
        var playerCol    = marioCore != null ? (Collider2D)marioCore.Collider  : player.GetComponentInChildren<Collider2D>();
        // Animator and SpriteRenderer live inside the Visual child hierarchy
        var playerAnim   = player.GetComponentInChildren<Animator>();
        var playerSprite = player.GetComponentInChildren<SpriteRenderer>();

        // Cache the player's original sorting so we can restore it after exiting
        int originalSortingOrder    = playerSprite != null ? playerSprite.sortingOrder    : 0;
        string originalSortingLayer = playerSprite != null ? playerSprite.sortingLayerName : "Default";

        while (marioCore != null && marioCore.State.GroundPoundRotating) yield return null;

        isEnteringPipe = true;
        PlayWarpEnterSound();

        // Place Mario behind the entry pipe for the enter animation
        ApplyPipeSorting(playerSprite, GetComponentsInChildren<SpriteRenderer>());

        if (marioCore != null)
        {
            marioCore.State.MoveInput      = Vector2.zero;
            marioCore.State.Direction      = Vector2.zero;
            marioCore.State.GroundPounding = false;
            marioCore.State.InputLocked    = true;
            marioCore.DisableInputs();
            // Disable physics/input modules and the StateMachine (to stop
            // CheckTransitions from snapping Mario to Idle), but leave the
            // AnimatorController alone so the walking sprite keeps playing
            marioCore.Input.enabled           = false;
            marioCore.GroundDetection.enabled = false;
            marioCore.WallDetection.enabled   = false;
            marioCore.Rb.gravityScale         = 0f;
            // Disable StateMachine (stops CheckTransitions snapping to Idle)
            // and AnimatorController (stops UpdateContinuousParams overwriting
            // isRunning=false because velocity is zero during the pipe slide)
            marioCore.StateMachine.enabled        = false;
            marioCore.AnimatorController.enabled  = false;
        }

        if (playerRb)  { playerRb.velocity = Vector2.zero; playerRb.isKinematic = true; }
        if (playerCol) playerCol.enabled = false;

        if (playerAnim)
        {
            playerAnim.SetBool("onGround", enterDirection != Direction.Up);
            playerAnim.SetBool("isSkidding", false);
            playerAnim.SetBool("isCrouching", enterDirection == Direction.Down);

            bool horizEnter = enterDirection == Direction.Left || enterDirection == Direction.Right;
            playerAnim.SetBool("isRunning", horizEnter);

            float enterHorizontal =
                enterDirection == Direction.Right ? 1f :
                enterDirection == Direction.Left  ? -1f : 0f;

            playerAnim.SetFloat("Horizontal", enterHorizontal);
        }

        if (marioCore != null) marioCore.State.GroundPounding = false;

        // enter movement — same logic as the original, just using player.position directly
        Vector2 enterDirVec     = DirectionToVector(enterDirection) * enterDistance;
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

        if (marioCore != null)
        {
            if (exitDirection == Direction.Left)
                marioCore.Physics.FlipTo(false); // Face left
            else if (exitDirection == Direction.Right)
                marioCore.Physics.FlipTo(true);  // Face right
        }

        if (playerAnim)
        {
            playerAnim.SetBool("isSkidding", false);
            playerAnim.SetBool("isCrouching", false);
        }

        if (!instantExit)
        {
            if (playerAnim)
            {
                bool horizExit = exitDirection == Direction.Left || exitDirection == Direction.Right;
                playerAnim.SetBool("isRunning", horizExit);

                float exitHorizontal =
                    exitDirection == Direction.Right ? 1f :
                    exitDirection == Direction.Left  ? -1f : 0f;

                playerAnim.SetFloat("Horizontal", exitHorizontal);
            }

            Vector2 outDir    = DirectionToVector(exitDirection);
            Vector2 insideOff = outDir * -insideExitDistance;
            Vector2 exitOff   = outDir *  exitDistance;

            if (marioCore != null && PowerStates.IsBig(marioCore.State.PowerupState) &&
                (exitDirection == Direction.Down || exitDirection == Direction.Up))
                exitOff += outDir * 0.5f;

            // Teleport to inside the exit pipe, then slide out.
            // Apply the exit pipe's sorting so Mario stays behind it during the exit animation.
            player.position = connection.position + (Vector3)insideOff;
            var connectionPipe = connection.GetComponent<Pipe>() ?? connection.GetComponentInParent<Pipe>();
            ApplyPipeSorting(playerSprite, connectionPipe?.GetComponentsInChildren<SpriteRenderer>());

            Vector3 finalExitPos  = connection.position + (Vector3)exitOff + (Vector3)(outDir * 0.02f);
            bool exitingIntoWater = Physics2D.OverlapPoint(finalExitPos, LayerMask.GetMask("Water"));

            if (marioCore != null) marioCore.State.Swimming = exitingIntoWater;
            if (playerAnim)
            {
                playerAnim.SetBool("onGround", !exitingIntoWater);
                if (AnimatorHas(playerAnim, "swim"))       playerAnim.SetBool("swim", exitingIntoWater);
                if (AnimatorHas(playerAnim, "enterWater")) playerAnim.SetBool("enterWater", exitingIntoWater);
                if (AnimatorHas(playerAnim, "exitWater"))  playerAnim.SetBool("exitWater", !exitingIntoWater);
            }

            yield return Move(player, connection.position + (Vector3)exitOff, true);
        }
        else
        {
            player.position = connection.position;

            // instant exit — apply the connection pipe's sorting for the brief moment Mario is visible there,
            // then restore immediately below since there's no exit slide animation
            var connectionPipe = connection.GetComponent<Pipe>() ?? connection.GetComponentInParent<Pipe>();
            ApplyPipeSorting(playerSprite, connectionPipe?.GetComponentsInChildren<SpriteRenderer>());

            bool inWater = Physics2D.OverlapPoint(player.position, LayerMask.GetMask("Water"));
            if (marioCore != null) marioCore.State.Swimming = inWater;
            if (playerAnim) playerAnim.SetBool("onGround", !inWater);
        }

        // Restore Mario's original sorting order now that he has fully exited the pipe
        if (playerSprite != null)
        {
            playerSprite.sortingLayerName = originalSortingLayer;
            playerSprite.sortingOrder     = originalSortingOrder;
        }

        // unfreeze — mirror of the lock above
        if (playerRb)  playerRb.isKinematic = false;
        if (playerCol) playerCol.enabled    = true;
        if (marioCore != null)
        {
            marioCore.Input.enabled              = true;
            marioCore.GroundDetection.enabled    = true;
            marioCore.WallDetection.enabled      = true;
            marioCore.Rb.gravityScale            = 1f;
            marioCore.AnimatorController.enabled = true;
            marioCore.StateMachine.enabled       = true;
            marioCore.State.InputLocked          = false;
            marioCore.EnableInputs();

            // MoveInput was cleared when Mario entered the pipe. If the player kept holding a
            // direction, the Input System has no new press edge to report here, so read the
            // current Move action value directly before handing control back to the state machine.
            marioCore.Input?.SyncHeldMovement();
            marioCore.StateMachine.ForceTransition(MarioStateID.Idle);
        }

        OnAnyExitFinished?.Invoke(this, player);
        isEnteringPipe = false;
    }

    private Vector2 DirectionToVector(Direction direction)
    {
        switch (direction)
        {
            case Direction.Down:  return Vector2.down;
            case Direction.Right: return Vector2.right;
            case Direction.Up:    return Vector2.up;
            case Direction.Left:  return Vector2.left;
            default:              return Vector2.zero;
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

            if (enterAlignType == AlignType.Fast && !exiting)
            {
                bool leftRight = enterDirection == Direction.Left || enterDirection == Direction.Right;
                Vector3 halfPosition = leftRight
                    ? new Vector3((endPosition.x + startPosition.x) / 2, endPosition.y, startPosition.z)
                    : new Vector3(endPosition.x, (endPosition.y + startPosition.y) / 2, startPosition.z);

                if (t < 0.5f) player.position = Vector3.Lerp(startPosition, halfPosition, t * 2);
                else          player.position = Vector3.Lerp(halfPosition, endPosition, (t - 0.5f) * 2);
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

    private bool CorrectDirectionPressed(MarioCore marioCore)
    {
        Vector2 input = marioCore.State.MoveInput;
        switch (enterDirection)
        {
            case Direction.Down:  return input.y < 0;
            case Direction.Right: return input.x > 0;
            case Direction.Up:    return input.y > 0;
            case Direction.Left:  return input.x < 0;
            default:              return false;
        }
    }

    private void PlayWarpEnterSound()
    {
        if (warpEnterSound != null && audioSource != null)
            audioSource.PlayOneShot(warpEnterSound);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector2 enterDirectionVector = DirectionToVector(enterDirection) * enterDistance;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + enterDirectionVector);
        Gizmos.DrawRay(transform.position, enterDirectionVector);

        if (connection != null)
        {
            Vector2 exitDirectionVector = DirectionToVector(exitDirection) * exitDistance;
            Gizmos.DrawLine(connection.position, (Vector2)connection.position + exitDirectionVector);
            Gizmos.DrawRay(connection.position, exitDirectionVector);
        }
    }

    public void StartRideFor(Transform rider)
    {
        if (connection == null) return;
        StartCoroutine(EnterNPC(rider));
    }

    private IEnumerator EnterNPC(Transform rider)
    {
        var rb   = rider.GetComponent<Rigidbody2D>();
        var col  = rider.GetComponent<Collider2D>();
        var ai   = rider.GetComponent<EnemyAI>();
        var phys = rider.GetComponent<ObjectPhysics>();

        if (ai   != null) ai.enabled   = false;
        if (phys != null) phys.enabled = false;
        if (rb   != null) { rb.velocity = Vector2.zero; rb.isKinematic = true; }
        if (col  != null) col.enabled  = false;

        Vector2 enterVec = DirectionToVector(enterDirection) * enterDistance;
        Vector3 enterPos = transform.position + (Vector3)enterVec;

        Vector3 temp = rider.position;
        switch (enterDirection)
        {
            case Direction.Down:
            case Direction.Up:    temp.x = enterPos.x; break;
            case Direction.Left:
            case Direction.Right: temp.y = enterPos.y; break;
        }
        rider.position = temp;

        yield return Move(rider, enterPos, false);
        yield return new WaitForSeconds(1f);

        if (!instantExit)
        {
            Vector2 outDir  = DirectionToVector(exitDirection);
            Vector3 inside  = (Vector3)(outDir * -insideExitDistance);
            Vector3 outside = (Vector3)(outDir *  exitDistance);

            rider.position = connection.position + inside;
            yield return Move(rider, connection.position + outside, true);
        }
        else
        {
            rider.position = connection.position;
        }

        if (rb   != null) rb.isKinematic = false;
        if (col  != null) col.enabled    = true;
        if (phys != null) phys.enabled   = true;
        if (ai   != null) ai.enabled     = true;
    }
}