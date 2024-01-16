using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script lets Mario enter a pipe
public class Pipe : MonoBehaviour
{
    public Transform connection;
    public Vector3 enterDirection = Vector3.down;
    public Vector3 exitDirection = Vector3.zero;
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

        Vector3 enteredPosition = transform.position + enterDirection; // We get the place where the player will move based on the pipe position and the enter position
        originalScale = player.localScale; // Store the original scale
        Vector3 enteredScale = Vector3.one * 0.5f; // Reduces the player's scale

        yield return Move(player, enteredPosition, enteredScale); // To enter the pipe, player moves based on the enteredPosition and starts decreasing it's scale
        // Move the camera

        yield return new WaitForSeconds(1f);

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

        if (exitDirection != Vector3.zero)
        {
            player.position = connection.position - exitDirection;
            yield return Move(player, connection.position + exitDirection, originalScale);
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
        float angle = transform.eulerAngles.z;
        Vector2 moveInput = playerMovement.moveInput;

        if (angle < 45f || angle >= 315f)
        {
            return playerMovement.crouchPressed; // Facing down
        }
        else if (angle >= 45f && angle < 135f)
        {
            return moveInput.x > 0; // Facing right
        }
        else if (angle >= 135f && angle < 225f)
        {
            return moveInput.y > 0; // Facing up
        }
        else
        {
            return moveInput.x < 0; // Facing left
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
