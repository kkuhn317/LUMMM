using System.Collections;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private GameObject[] players;
    public float smoothDampTime = 0.15f;
    private Vector3 smoothDampVelocity = Vector3.zero;

    private CameraZone[] zones;

    private CameraZone currentZone;

    public float camHeight => Camera.main.orthographicSize * 2;
    public float camWidth => camHeight * Camera.main.aspect;

    private Vector3 originalPosition;

    // Camera Shake variables
    private bool isShaking = false;
    private float shakeDuration = 0f;
    private float shakeIntensity = 0.1f;
    private float shakeDecreaseFactor = 1.0f;

    // Camera moving up variables
    private bool isLookingUp = false;
    public Vector3 offset;  // Vertical offset when looking up
    private int ongoingShakes = 0;

    [Header("Change Camera Size")]
    private float originalOrthographicSize;
    private float targetOrthographicSize;
    private float zoomSmoothTime = 0.2f;

    // Start is called before the first frame update
    void Start()
    {
        originalPosition = transform.position;
        offset = new Vector3(0f, 2f, 0f);

        // Store the original orthographic size of the camera
        originalOrthographicSize = Camera.main.orthographicSize;

        // Set the initial target orthographic size to match the original size
        targetOrthographicSize = originalOrthographicSize;

        // Get all the camera zones attached to this camera
        zones = GetComponents<CameraZone>();

        if (zones.Length == 0)
        {
            Debug.LogWarning("No CameraZones found on camera. You should add at least one CameraZone to the camera.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Get all the players in the scene
        players = GameManager.Instance.GetPlayerObjects();

        if (players.Length == 0)
        {
            return;
        }

        // Get middle of all players
        Vector2 target = Vector2.zero;
        foreach (var player in players)
        {
            target += (Vector2)player.transform.position;
        }
        target /= players.Length;

        CameraZone zone = GetCurrentZone(target);
        currentZone = zone;

        if (zone == null)
        {
            return;
        }

        float targetX = Mathf.Max(zone.cameraMinX, Mathf.Min(zone.cameraMaxX, target.x));
        float targetY = Mathf.Max(zone.cameraMinY, Mathf.Min(zone.cameraMaxY, target.y));

        bool tooSmallHorizontal = false;
        bool tooSmallVertical = false;

        // In case the bounds are too small for the camera, center the camera between the bounds
        if (zone.bottomRight.x - zone.topLeft.x < camWidth && !zone.lockToVertical)
        {
            tooSmallHorizontal = true;
            targetX = (zone.bottomRight.x + zone.topLeft.x) / 2;
        }
        if (zone.topLeft.y - zone.bottomRight.y < camHeight && !zone.lockToHorizontal)
        {
            tooSmallVertical = true;
            targetY = (zone.topLeft.y + zone.bottomRight.y) / 2;
        }

        if (isLookingUp)
        {
            // Calculate desired position with an offset when looking up
            targetY += offset.y;
        }

        float x = Mathf.SmoothDamp(transform.position.x, targetX, ref smoothDampVelocity.x, smoothDampTime);
        float y = Mathf.SmoothDamp(transform.position.y, targetY, ref smoothDampVelocity.y, smoothDampTime);
        transform.position = new Vector3(x, y, transform.position.z);

        // Smoothly change the camera's orthographic size
        Camera.main.orthographicSize = Mathf.SmoothDamp(Camera.main.orthographicSize, targetOrthographicSize, ref smoothDampVelocity.z, zoomSmoothTime);

        // Clamp the camera position to the bounds
        if (zone.snapToBounds)
        {
            Vector2 clampedPos = new(
                Mathf.Clamp(transform.position.x, zone.cameraMinX, zone.cameraMaxX),
                Mathf.Clamp(transform.position.y, zone.cameraMinY, zone.cameraMaxY)
            );
            if (tooSmallHorizontal)
            {
                clampedPos.x = (zone.bottomRight.x + zone.topLeft.x) / 2;
            }
            if (tooSmallVertical)
            {
                clampedPos.y = (zone.topLeft.y + zone.bottomRight.y) / 2;
            }
            transform.position = new Vector3(clampedPos.x, clampedPos.y, transform.position.z);
        }

        if (isShaking)
        {
            ShakeCamera();
        }
    }

    private CameraZone GetCurrentZone(Vector2 target)
    {
        // Check if the middle of the players is within any of the camera zones
        foreach (var zone in zones)
        {
            if (target.x >= zone.topLeft.x && target.x <= zone.bottomRight.x && target.y >= zone.bottomRight.y && target.y <= zone.topLeft.y)
            {
                return zone;
            }
        }

        return null;
    }

    // Change the camera size to a specific value
    public void ChangeCameraSize(float newSize/*, float zoomTime = 0.2f*/)
    {
        // Set the target orthographic size for the camera
        targetOrthographicSize = newSize;
        //zoomSmoothTime = zoomTime;
    }

    // Call this method from the player script when the player looks up
    public void StartCameraMoveUp()
    {
        isLookingUp = true;
    }

    // Call this method from the player script when the player stops looking up
    public void StopCameraMoveUp()
    {
        isLookingUp = false;
    }

    // Call this method to trigger camera shake on any specified axis
    public void ShakeCamera(float duration = 0.5f, float intensity = 0.1f, float decreaseFactor = 1.0f, Vector3 axis = default)
    {
        if (ongoingShakes == 0)
        {
            originalPosition = transform.position;
        }
        ongoingShakes++;
        StartCoroutine(ShakeCoroutine(duration, intensity, decreaseFactor, axis));
    }

    private IEnumerator ShakeCoroutine(float duration, float intensity, float decreaseFactor, Vector3 axis)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float shakeOffset = Random.Range(-0.5f, 0.5f);
            transform.localPosition = originalPosition + axis * shakeOffset * intensity;
            elapsed += Time.deltaTime;
            yield return null;
        }
        ongoingShakes--;
        if (ongoingShakes == 0)
        {
            isShaking = false;
            transform.localPosition = originalPosition;
        }
    }

    // Call this method to trigger camera shake on any specified axis repeatedly
    public void ShakeCameraRepeatedly(float duration, float intensity, float decreaseFactor, Vector3 axis, int numberOfShakes, float delayBetweenShakes)
    {
        StartCoroutine(ShakeCameraRepeatedlyCoroutine(duration, intensity, decreaseFactor, axis, numberOfShakes, delayBetweenShakes));
    }

    // Coroutine to handle the camera shake effect repeatedly
    private IEnumerator ShakeCameraRepeatedlyCoroutine(float duration, float intensity, float decreaseFactor, Vector3 axis, int numberOfShakes, float delayBetweenShakes)
    {
        for (int i = 0; i < numberOfShakes; i++)
        {
            ShakeCamera(duration, intensity, decreaseFactor, axis);
            yield return new WaitForSeconds(delayBetweenShakes);
        }
    }

    // Sets the current zone's lock offset
    public void SetLockOffsetX(float offset)
    {
        currentZone.lockOffset.x = offset;
    }
    
    public void SetLockOffsetY(float offset)
    {
        currentZone.lockOffset.y = offset;
    }

    public void SetLockedHorizontal(bool locked)
    {
        currentZone.lockToHorizontal = locked;
    }

    public void SetLockedVertical(bool locked)
    {
        currentZone.lockToVertical = locked;
    }

    // Gets the current zone's lock offset
    public Vector2 GetLockOffset()
    {
        return currentZone.lockOffset;
    }
}