using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pipeSystem : MonoBehaviour
{
    public Transform warpDestination;
    public float centerOffset = -0.05f;
    public AudioClip warpEnterSound;
    public bool isWarpEnabled = true;
    public float warpCenteringDuration = 0.5f;
    public float delayTimeBeforeWarping = 0.5f;
    public Vector3 exitDirection = Vector3.zero;

    private Collider2D playerCollider;
    private AudioSource audioSource;
    private KeyCode lastKeyPressed;
    private bool canWarp;
    private bool isWarping;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isWarpEnabled && other.CompareTag("Player"))
        {
            canWarp = true;
            playerCollider = other;
            Debug.Log("Press the corresponding key to enter the pipe or warp.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canWarp = false;
        }
    }

    private void Update()
    {
        if (canWarp)
        {
            KeyCode keyToWarp = GetWarpKeyFromRotation();
            if (Input.GetKeyDown(keyToWarp))
            {
                if (isWarpEnabled && warpEnterSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(warpEnterSound);
                }

                StartCoroutine(CenterPlayer());
                lastKeyPressed = keyToWarp;
            }
        }

        if (isWarping)
        {
            playerCollider.enabled = true;
            isWarping = false;
            WarpPlayer();
        }
    }

    private IEnumerator CenterPlayer()
    {
        playerCollider.enabled = false;

        float elapsedTime = 0f;
        Vector3 startPosition = playerCollider.transform.position;
        Vector3 centerPosition = CalculateCenterPosition();

        while (elapsedTime < warpCenteringDuration)
        {
            playerCollider.transform.position = Vector3.Lerp(startPosition, centerPosition, elapsedTime / warpCenteringDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        playerCollider.transform.position = centerPosition;

        yield return new WaitForSeconds(delayTimeBeforeWarping);

        isWarping = true;
    }

    private void WarpPlayer()
    {
        if (isWarpEnabled && warpDestination != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = warpDestination.position;
                // You can add additional animations or effects here to simulate the warp
            }
        }

        Debug.Log("Entered warp with key: " + lastKeyPressed.ToString());
    }

    private KeyCode GetWarpKeyFromRotation()
    {
        float angle = transform.eulerAngles.z;

        if (angle < 45f || angle >= 315f)
        {
            return KeyCode.DownArrow; // Facing down
        }
        else if (angle >= 45f && angle < 135f)
        {
            return KeyCode.RightArrow; // Facing right
        }
        else if (angle >= 135f && angle < 225f)
        {
            return KeyCode.UpArrow; // Facing up
        }
        else
        {
            return KeyCode.LeftArrow; // Facing left
        }
    }

    private Vector3 CalculateCenterPosition()
    {
        Vector3 centerPosition = transform.position;
        centerPosition.z = playerCollider.transform.position.z;
        centerPosition += (Vector3)playerCollider.offset;
        centerPosition -= playerCollider.bounds.center - playerCollider.transform.position;
        centerPosition += playerCollider.transform.up * centerOffset;
        return centerPosition;
    }
}
