using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Axe : MonoBehaviour
{
    public enum AxeSize { Big, Small }
    public AxeSize Size;

    public enum AxeRotation { Right, Left }
    public AxeRotation Rotation;

    public enum BridgeDestructionType
    {
        Instant,    // Tiles are destroyed instantly tile per tile.
        FallAndDestroy      // Tiles fall down and then get destroyed one by one.
    }
    public BridgeDestructionType destructionType;

    [Header("Big Axe")]
    public float rotationSpeed = 90f; // The rotation speed in degrees per second.
    private bool isRotating;

    [Header("Bridge")]
    public GameObject bridge;
    public float bridgeDestroyDelay = 1.0f; // Adjust this value to set the delay in seconds before destroying the bridge when the axe rotation is done.
    public List<GameObject> bridgeTiles; // Assign the bridge tiles to this list in the Inspector.
    public bool timerStop;

    [Header("Bridge Destruction")]
    public bool startFromLastTile = true; // Set this to true if you want to start destruction from the last tile.
    public int tilesPerStep = 1; // Number of tiles to destroy at once.
    public BoxCollider2D playerCantPass;

    [Header("Instant Bridge Destroy Option")]
    public float tilesDestroyDelay = 0.1f;

    [Header("Fall And Destroy Bridge Options")]
    public float fallDistance = 2.0f; // Adjust this value to determine how far the tiles should fall.
    public float fallDuration = 0.1f;
    public float tileFallDestroyDelay = 0.2f; // delay between tiles falling and destroying.
    
    [Header("Audio Clips")]
    public AudioClip bigAxeGrab;
    public AudioClip smallAxeDestroy;

    [Header("Camera Shake")]
    public CameraFollow cameraFollow;
    public int numberOfShakes = 5; // Number of times the camera will shake up and down
    public float delayBetweenShakes = 0.5f; // Time delay between each shake

    public float duration = 0.7f;
    public float intensity = 0.3f;
    public float decreaseFactor = 1.2f;
    public Vector3 axisUp = new Vector3(0f, 1f, 0f); // Y-axis

    [Header("Components")]
    private AudioSource audioSource;
    private BoxCollider2D axeCollider;
    private Quaternion targetRotation;

    [Header("Stop Object Animation")]
    public AnimatedSprite animatedSprite;

    private void Start()
    {
        // Get the AudioSource component attached to the same GameObject or add one if missing.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        axeCollider = GetComponent<BoxCollider2D>();
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
                if (Size == AxeSize.Big && bigAxeGrab != null)
                {
                    audioSource.PlayOneShot(bigAxeGrab);
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
                // Play the smallAxeDestroy sound.
                if (smallAxeDestroy != null)
                {
                    audioSource.PlayOneShot(smallAxeDestroy);
                }

                Destroy(this.gameObject);
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

        // Destroy the bridge based on the selected destruction type.
        switch (destructionType)
        {
            // Destroy the bridge tiles one by one.
            case BridgeDestructionType.Instant:
                if (bridgeTiles.Count > 0)
                {
                    Debug.Log("Starting Instant Bridge Destruction...");
                    StartBridgeDestruction();
                }
                break;
            // Bridge tiles fall one by one and destroy.
            case BridgeDestructionType.FallAndDestroy:
                StartCoroutine(DestroyBridgeTilesFallAndDestroy(startFromLastTile));
                break;
        }
    }

    // Instant

    private void StartBridgeDestruction()
    {
        int startTileIndex = startFromLastTile ? bridgeTiles.Count - 1 : 0;
        StartCoroutine(DestroyBridgeTiles(startTileIndex));
    }

    private IEnumerator DestroyBridgeTiles(int startTileIndex)
    {
        int tileCount = bridgeTiles.Count;
        // Make sure startTileIndex is within the valid range.
        startTileIndex = Mathf.Clamp(startTileIndex, 0, tileCount - 1);

        if (tileCount > 0)
        {
            // Determine the direction of destruction based on startTileIndex and endTileIndex.
            int step = startFromLastTile ? -1 : 1;
            int endTileIndex = startFromLastTile ? -1 : tileCount;

            for (int i = startTileIndex; i != endTileIndex; i += step)
            {
                GameObject tile = bridgeTiles[i];

                // Play a destruction animation or sound if desired.
                // For example, you can use "tile.GetComponent<AnimatedSprite>()?.PlayDestructionAnimation();"

                // Destroy the current tile instantly.
                Destroy(tile);

                // Wait for a short time before destroying the next tile.
                yield return new WaitForSeconds(tilesDestroyDelay); // Adjust the time delay as needed.
            }
        }

        // Set playerCantPass to non-trigger after the bridge destruction is complete.
        playerCantPass.isTrigger = false;

        // Resume the pauseable objects after the bridge destruction is complete.
        GameManager.Instance.ResumePauseableObjects();
    }


    //  FallAndDestroy

    private IEnumerator DestroyTileWithDelay(GameObject tile)
    {
        yield return new WaitForSeconds(tileFallDestroyDelay);
        Destroy(tile);
    }

    private IEnumerator FallTile(Rigidbody2D tileRigidbody, float fallStep)
    {
        float currentFallDistance = 0f;
        Collider2D tileCollider = tileRigidbody.GetComponent<Collider2D>();

        // Disable the collider to prevent interactions while the tile is falling.
        tileCollider.enabled = false;
        while (currentFallDistance < fallDistance)
        {
            // Move the tile downward.
            float deltaY = -fallStep * Time.deltaTime;
            tileRigidbody.MovePosition(tileRigidbody.position + new Vector2(0f, deltaY));

            currentFallDistance += Mathf.Abs(deltaY);
            yield return null;
        }

        // Once the tile has fallen, destroy it with a delay.
        StartCoroutine(DestroyTileWithDelay(tileRigidbody.gameObject));
    }

    private IEnumerator DestroyBridgeTilesFallAndDestroy(bool startFromLastTile)
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

            // Calculate the fall distance for each tile.
            float fallStep = fallDistance / (fallDuration / tilesDestroyDelay);

            for (int i = startTileIndex; i != endTileIndex; i += step)
            {
                GameObject tile = bridgeTiles[i];
                Rigidbody2D tileRigidbody = tile.GetComponent<Rigidbody2D>();

                // Enable the tile's rigidbody to make it fall.
                tileRigidbody.bodyType = RigidbodyType2D.Dynamic;
                tileRigidbody.gravityScale = 0; // We'll control the fall speed manually.
                tileRigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

                StartCoroutine(FallTile(tileRigidbody, fallStep));

                // Wait for a short time before starting to destroy the next tile.
                yield return new WaitForSeconds(tileFallDestroyDelay);
            }
        }

        playerCantPass.isTrigger = false;
        // Resume the pauseable objects after the bridge destruction is complete.
        GameManager.Instance.ResumePauseableObjects();
    }
}