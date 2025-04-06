using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script lets Mario enter a pipe
public class Pipe : MonoBehaviour
{
    public Transform connection;
    public enum Direction { Up, Down, Left, Right }
    public enum AlignType
    {
        None,   // Don't align the player at all. He will just move in the direction of the pipe
        Slow,   // Slowly align the player with the pipe by moving him directly towards the enter position
        Fast,   // Quickly align the player with the pipe while he is still moving
        Instant // Instantly teleport the player to be aligned with the pipe
    }
    public Direction enterDirection = Direction.Down;
    public Direction exitDirection = Direction.Up;

    // How far in/out of the pipe the player should be when they enter/exit
    // Should be smaller for tiny pipes
    public float enterDistance = 1f;    // How far into the pipe the player should go (relative to the position of the pipe)
    public AlignType enterAlignType = AlignType.Slow; // How the player should align when entering the pipe
    public float insideExitDistance = 1f;   // How far the player should be inside the pipe when they start to exit (relative to the position of the exit)
    public float exitDistance = 1f; // How far out of the pipe the player should be when they exit

    public bool instantExit = false; // If true, don't animate the player exiting the pipe (like 1-1 Underground)
    public AudioClip warpEnterSound;
    public bool requireGroundToEnter = true; // New option to require the player to be on the ground
    private AudioSource audioSource;
    private bool isEnteringPipe = false; // Track if the player is already entering the pipe

    public bool snapCameraX = false;
    public bool snapCameraY = false;

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

    private IEnumerator Enter(Transform player)
    {
        /* ENTERING */
        MarioMovement marioMovement = player.GetComponent<MarioMovement>();
        // Wait until the player finishes the ground pound rotation phase
        while (marioMovement != null && marioMovement.groundPoundRotating)
        {
            Debug.Log("Waiting for ground pound rotation to finish...");
            yield return null; // Wait for the next frame
        }

        isEnteringPipe = true; // Set the flag to prevent re-entry

        PlayWarpEnterSound();

        // Disable the player's movement and gravity
        Rigidbody2D playerRigidbody = player.GetComponent<Rigidbody2D>();
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector2.zero;
            playerRigidbody.isKinematic = true;
        }

        // Disable the player's collider
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

        // Set animation
        Animator playerAnimator = player.GetComponent<Animator>();
        SpriteRenderer playerSprite = player.GetComponent<SpriteRenderer>();
        playerAnimator.SetBool("onGround", enterDirection != Direction.Up);
        playerAnimator.SetBool("isSkidding", false);
        playerAnimator.SetBool("isCrouching", enterDirection == Direction.Down);
        playerAnimator.SetBool("isRunning", enterDirection == Direction.Left || enterDirection == Direction.Right);
        playerAnimator.SetFloat("Horizontal", enterDirection == Direction.Left || enterDirection == Direction.Right ? 1 : 0);

        // Reset ground pound state
       
        if (marioMovement != null)
        {
            marioMovement.StopGroundPound();
        }
        if (enterDirection == Direction.Left || enterDirection == Direction.Right)
        {
            playerSprite.flipX = enterDirection == Direction.Left;
        }

        // Disable player's movement
        marioMovement.enabled = false;

        Vector2 enterDirectionVector = DirectionToVector(enterDirection) * enterDistance; // Get the direction vector based on the enter direction

        Vector3 enteredPosition = transform.position + (Vector3)enterDirectionVector; // We get the place where the player will move based on the pipe position and the enter position
        switch (enterAlignType)
        {
            case AlignType.None:
                Vector3 tempPosition = transform.position;
                switch (enterDirection)
                {
                    case Direction.Down:
                    case Direction.Up:
                        tempPosition.x = player.position.x;
                        break;
                    case Direction.Right:
                    case Direction.Left:
                        tempPosition.y = player.position.y;
                        break;
                }
                enteredPosition = tempPosition + (Vector3)enterDirectionVector;  // The end position is based on the player's current position
                break;
            case AlignType.Slow:
                break;
            case AlignType.Fast:
                break;
            case AlignType.Instant:
                Vector3 tempPosition2 = player.position;
                switch (enterDirection)
                {
                    case Direction.Down:
                    case Direction.Up:
                        tempPosition2.x = enteredPosition.x;
                        break;
                    case Direction.Right:
                    case Direction.Left:
                        tempPosition2.y = enteredPosition.y;
                        break;
                }
                player.position = tempPosition2;
                break;
        }

        yield return Move(player, enteredPosition, false); // To enter the pipe, player moves based on the enteredPosition
        // Move the camera

        yield return new WaitForSeconds(1f);

        /* EXITING */

        PlayWarpEnterSound();

        // Move the camera
        Vector3 camPos = Camera.main.transform.position;
        if (snapCameraX)
        {
            camPos.x = connection.position.x;
        }
        if (snapCameraY)
        {
            camPos.y = connection.position.y;
        }
        Camera.main.transform.position = camPos;

        if (!instantExit)
        {
            // Set animation for exiting
            playerAnimator.SetBool("onGround", true);
            playerAnimator.SetBool("isSkidding", false);
            playerAnimator.SetBool("isCrouching", false);
            playerAnimator.SetBool("isRunning", exitDirection == Direction.Left || exitDirection == Direction.Right);
            playerAnimator.SetFloat("Horizontal", exitDirection == Direction.Left || exitDirection == Direction.Right ? 1 : 0);
            if (exitDirection == Direction.Left || exitDirection == Direction.Right)
            {
                playerSprite.flipX = exitDirection == Direction.Left;
            }

            Vector2 exitDirectionVector = DirectionToVector(exitDirection);
            Vector2 insidePipeOffset = exitDirectionVector * -insideExitDistance; // Get the inside position based on the exit direction
            Vector2 exitPipeOffset = exitDirectionVector * exitDistance; // Get the exit position based on the exit direction
            // If Mario is big and moving up or down, we need to move him a little bit more
            if (PowerStates.IsBig(player.GetComponent<MarioMovement>().powerupState) &&
                (exitDirection == Direction.Down || exitDirection == Direction.Up))
            {
                exitPipeOffset += exitDirectionVector * 0.5f;
            }
            player.position = connection.position + (Vector3)insidePipeOffset;   // Move the player to the exit position
            yield return Move(player, connection.position + (Vector3)exitPipeOffset, true);
        }
        else
        {
            player.position = connection.position;
        }

        // Re-enable the player's movement and gravity
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
        }

        // Re-enable the player's collider
        if (playerCollider != null)
        {
            playerCollider.enabled = true;
        }

        player.GetComponent<MarioMovement>().enabled = true;

        isEnteringPipe = false; // Reset the flag for the next entry
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
        // Set the Gizmos color to make it easily distinguishable
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
}