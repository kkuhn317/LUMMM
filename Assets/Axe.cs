using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Axe : MonoBehaviour
{
    public enum AxeSize { Small, Big }
    public AxeSize Size;
    public enum AxeRotation { Left, Right }
    public AxeRotation Rotation;
    public enum BridgeDestructionType
    {
        Instant,    // Tiles are destroyed instantly tile per tile.
        FallAndDestroy      // Tiles fall down and then get destroyed one by one.
    }
    public BridgeDestructionType destructionType;

    [Header("Big Axe")]
    public float rotationSpeed = 120f; // The rotation speed in degrees per second.
    private bool isRotating;

    [Header("Bridge")]
    public List<GameObject> bridgeTiles; // Assign the bridge tiles to this list in the Inspector.
    public bool timerStop;

    [Header("Bridge Destruction")]
    public bool startFromLastTile = true; // Set this to true if you want to start destruction from the last tile.
    public int tilesPerStep = 1; // Number of tiles to destroy at once.

    [Header("Fall And Destroy Bridge Options")]
    public float fallDistance = 2.0f; // Adjust this value to determine how far the tiles should fall.
    public float tileFallDestroyDelay = 0.2f; // delay between tiles falling/destroying.
    
    [Header("Audio Clips")]
    public AudioClip grabSound;
    public AudioClip hitGroundSound;
    public AudioClip bridgeBreakSound;
    public AudioClip enemyFallSound;

    [Header("Camera Shake")]
    public CameraFollow cameraFollow;
    public int numberOfShakes = 3; // Number of times the camera will shake up and down
    public float delayBetweenShakes = 0.1f; // Time delay between each shake

    public float duration = 0.1f;
    public float intensity = 2.0f;
    public float decreaseFactor = 1.0f;
    public Vector3 axisUp = new Vector3(0f, 1f, 0f); // Y-axis

    [Header("Components")]
    private AudioSource audioSource;
    private BoxCollider2D axeCollider;
    private Quaternion targetRotation;

    [Header("Stop Object Animation")]
    private AnimatedSprite animatedSprite;
    private GameObject player;

    [Header("Timing")]
    // these are timed from the time the axe hits the ground or disappears
    public float bridgeDestroyDelay = 0.5f; // Adjust this value to set the delay in seconds before destroying the bridge when the axe rotation is done.
    public float enemyFallDelay = 2.0f; // how long until the enemies fall
    public float playerResumeDelay = 3.0f; // how long until the player can move again
    private void Start()
    {
        // Get the AudioSource component attached to the same GameObject or add one if missing.
        audioSource = GetComponent<AudioSource>();

        axeCollider = GetComponent<BoxCollider2D>();

        animatedSprite = GetComponent<AnimatedSprite>();
    }
    private void Update()
    {
        if (isRotating)
        {
            // Call StopAnimation() on the referenced AnimatedSprite component.
            if (animatedSprite != null)
            {
                animatedSprite.StopAnimation();
            }

            // Calculate the rotation step based on the rotation speed and Time.deltaTime.
            float step = rotationSpeed * Time.deltaTime;

            // Rotate towards the target rotation angle over time.
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, step);

            // Check if the rotation is complete and set isRotating to false.
            if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
            {
                isRotating = false;

                // Play the audio clip after the rotation is complete.
                if (Size == AxeSize.Big && hitGroundSound != null)
                {
                    audioSource.PlayOneShot(hitGroundSound);
                }

                // Camera Shake
                cameraFollow.ShakeCameraRepeatedly(duration, intensity, decreaseFactor, axisUp, numberOfShakes, delayBetweenShakes);

                // Handle shared behavior for both axes (destroying the bridge and stopping the timer).
                HandleSharedBehavior();
            }
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !isRotating)
        {
            // Pause the pauseable objects when starting the bridge destruction.
            GameManager.Instance.PausePauseableObjects();

            // freeze player
            player = collision.gameObject;
            player.GetComponent<MarioMovement>().Freeze();

            // Handle behavior based on the axe size.
            if (Size == AxeSize.Big)
            {
                isRotating = true;

                // Calculate the target rotation based on the current rotation direction.
                float targetRotationAngle = (Rotation == AxeRotation.Right) ? -90f : 90f;
                targetRotation = Quaternion.Euler(0f, 0f, targetRotationAngle);

                // Deactivate the collider to prevent triggering rotation again.
                axeCollider.enabled = false;
            }
            else // Axe is small
            {
                // play sound
                if (grabSound != null)
                    audioSource.PlayOneShot(grabSound);
                // disable renderer
                GetComponent<SpriteRenderer>().enabled = false;
                // disable collider
                GetComponent<BoxCollider2D>().enabled = false;

                // Handle shared behavior for both axes (destroying the bridge and stopping the timer).
                HandleSharedBehavior();
            }
            
        }
    }
    private void HandleSharedBehavior()
    {
        // Check if timer should be stopped (shared behavior for both axes).
        if (timerStop)
        {
            GameManager.Instance.StopTimer();
        }

        Invoke(nameof(DestroyBridge), bridgeDestroyDelay);
        Invoke(nameof(makeObjectsFall), enemyFallDelay);
        Invoke(nameof(resumePlayer), playerResumeDelay);
    }

    private IEnumerator FallTile(GameObject tile)
    {
        float startHeight = tile.transform.position.y;

        // Disable the collider to prevent interactions while the tile is falling.
        while (tile.transform.position.y > startHeight - fallDistance)
        {
            yield return null;
        }

        // Once the tile has fallen, destroy it
        Destroy(tile);
    }

    private void DestroyBridge()
    {
        // play sound
        if (bridgeBreakSound != null)
            audioSource.PlayOneShot(bridgeBreakSound);

        // Destroy the bridge.
        StartCoroutine(DestroyBridgeTiles(startFromLastTile));
    }

    private IEnumerator DestroyBridgeTiles(bool startFromLastTile)
    {
        int tileCount = bridgeTiles.Count;

        // Make sure startTileIndex is within the valid range.
        int startTileIndex = startFromLastTile ? tileCount - 1 : 0;

        if (tileCount > 0)
        {
            // Calculate the ending tile index based on the start direction.
            int endTileIndex = startFromLastTile ? -1 : tileCount;

            // Determine the direction of destruction based on startTileIndex and endTileIndex.
            int step = startFromLastTile ? -1 : 1;

            for (int i = startTileIndex; i != endTileIndex; i += step)
            {
                GameObject tile = bridgeTiles[i];
                Rigidbody2D tileRigidbody = tile.GetComponent<Rigidbody2D>();

                switch (destructionType)
                {
                    case BridgeDestructionType.Instant:
                        // Destroy the tile instantly.
                        Destroy(tile);
                        break;
                    case BridgeDestructionType.FallAndDestroy:
                        // Enable the tile's rigidbody to make it fall.
                        tileRigidbody.bodyType = RigidbodyType2D.Dynamic;
                        //tileRigidbody.gravityScale = 0; // We'll control the fall speed manually.
                        tileRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
                        tile.GetComponent<Collider2D>().isTrigger = true; // disable collisions while falling.

                        StartCoroutine(FallTile(tile));
                        break;
                }

                // Wait for a short time before starting to destroy the next tile.
                yield return new WaitForSeconds(tileFallDestroyDelay);
            }
        }
    }

    private void makeObjectsFall() {
        // play sound
        if (enemyFallSound != null)
            audioSource.PlayOneShot(enemyFallSound);

        // Resume the pauseable objects
        GameManager.Instance.ResumePauseableObjects();
    }

    private void resumePlayer() {
        // resume player
        player.GetComponent<MarioMovement>().Unfreeze();
    }
}