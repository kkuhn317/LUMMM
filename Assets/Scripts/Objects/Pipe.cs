using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script lets Mario enter a pipe
public class Pipe : MonoBehaviour
{
    public Transform connection;
    public enum Direction { Up, Down, Left, Right }
    public Direction enterDirection = Direction.Down;
    public Direction exitDirection = Direction.Up;

    // How far in/out of the pipe the player should be when they enter/exit
    // Should be smaller for tiny pipes
    public float enterDistance = 1f;    // How far into the pipe the player should go (relative to the position of the pipe)
    public float insideExitDistance = 1f;   // How far the player should be inside the pipe when they start to exit (relative to the position of the exit)
    public float exitDistance = 1f; // How far out of the pipe the player should be when they exit

    public bool instantExit = false; // If true, don't animate the player exiting the pipe (like 1-1 Underground)
    public AudioClip warpEnterSound;
    public bool requireGroundToEnter = true; // New option to require the player to be on the ground
    private Vector3 originalScale;
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

        // Disable player's movement
        player.GetComponent<MarioMovement>().enabled = false;

        Vector2 enterDirectionVector = DirectionToVector(enterDirection) * enterDistance; // Get the direction vector based on the enter direction

        Vector3 enteredPosition = transform.position + (Vector3)enterDirectionVector; // We get the place where the player will move based on the pipe position and the enter position
        originalScale = player.localScale; // Store the original scale
        Vector3 enteredScale = Vector3.one * 0.5f; // Reduces the player's scale

        yield return Move(player, enteredPosition, enteredScale); // To enter the pipe, player moves based on the enteredPosition and starts decreasing it's scale
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
            yield return Move(player, connection.position + (Vector3)exitPipeOffset, originalScale);
        }
        else
        {
            player.position = connection.position;
            player.localScale = originalScale; // Restore the original scale
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

    private IEnumerator Move(Transform player, Vector3 endPosition, Vector3 endScale)
    {
        float elapsed = 0f;
        float duration = 1f;

        Vector3 startPosition = player.position;
        Vector3 startScale = player.localScale;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            player.position = Vector3.Lerp(startPosition, endPosition, t);
            player.localScale = Vector3.Lerp(startScale, endScale, t);
            elapsed += Time.deltaTime;

            yield return null;
        }
        player.position = endPosition;
        player.localScale = endScale;
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
}
