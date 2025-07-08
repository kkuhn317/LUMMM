using System.Collections;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private GameObject[] players;
    public float smoothDampTime = 0.15f;
    private Vector3 smoothDampVelocity = Vector3.zero;

    private CameraZone[] zones;

    private CameraZone currentZone;
    private CameraZone previousZone;
    private bool justEnteredSnapZone = false;

    private Camera cam;
    public float camHeight => cam.orthographicSize * 2;
    public float camWidth => camHeight * cam.aspect;

    private Vector3 originalPosition;

    // Camera Shake variables
    private bool isShaking = false;
    private float shakeDuration = 0f;
    private float shakeIntensity = 0.1f;
    private float shakeDecreaseFactor = 1.0f;

    // Camera moving up variables
    private bool isLookingUp = false;
    public Vector3 offset;
    private int ongoingShakes = 0;

    [Header("Change Camera Size")]
    private float originalOrthographicSize;
    private float targetOrthographicSize;
    private float zoomSmoothTime = 0.2f;

    private Vector2 lastTargetPosition;

    void Start()
    {
        originalPosition = transform.position;
        offset = new Vector3(0f, 2f, 0f);
        cam = GetComponent<Camera>();

        originalOrthographicSize = cam.orthographicSize;
        targetOrthographicSize = originalOrthographicSize;

        zones = GetComponents<CameraZone>();

        if (zones.Length == 0)
        {
            Debug.LogWarning("No CameraZones found on camera. You should add at least one CameraZone to the camera.");
        }
    }

    void Update()
    {
        players = GameManager.Instance.GetPlayerObjects();
        if (players.Length == 0) return;

        Vector2 target = Vector2.zero;
        foreach (var player in players)
        {
            target += (Vector2)player.transform.position;
        }
        target /= players.Length;

        CameraZone zone = GetCurrentZone(target);
        currentZone = zone;

        if (zone == null) return;

        // Detect entry into a new snap zone
        justEnteredSnapZone = false;
        if (zone != previousZone)
        {
            if (zone.snapToBounds)
            {
                justEnteredSnapZone = true;
            }
            previousZone = zone;
        }

        float posOffsetX = -zone.cameraPosOffset.x * camWidth / 2;
        float posOffsetY = -zone.cameraPosOffset.y * camHeight / 2;

        float targetX = Mathf.Max(zone.cameraMinX, Mathf.Min(zone.cameraMaxX, target.x)) + posOffsetX;
        float targetY = Mathf.Max(zone.cameraMinY, Mathf.Min(zone.cameraMaxY, target.y)) + posOffsetY;

        bool tooSmallHorizontal = false;
        bool tooSmallVertical = false;

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
            targetY += offset.y;
        }

        // SNAP if entering a snap zone, else smooth
        Vector3 newPos;
        if (justEnteredSnapZone)
        {
            newPos = new Vector3(targetX, targetY, transform.position.z);
            smoothDampVelocity = Vector3.zero;
        }
        else
        {
            float x = Mathf.SmoothDamp(transform.position.x, targetX, ref smoothDampVelocity.x, smoothDampTime);
            float y = Mathf.SmoothDamp(transform.position.y, targetY, ref smoothDampVelocity.y, smoothDampTime);
            newPos = new Vector3(x, y, transform.position.z);
        }

        transform.position = newPos;
        lastTargetPosition = new Vector2(targetX, targetY);

        // Smoothly zoom
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetOrthographicSize, ref smoothDampVelocity.z, zoomSmoothTime);

        // Clamp position
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
        CameraZone currentBestZone = null;
        foreach (var zone in zones)
        {
            if (target.x >= zone.topLeft.x && target.x <= zone.bottomRight.x &&
                target.y >= zone.bottomRight.y && target.y <= zone.topLeft.y)
            {
                if (currentBestZone == null || zone.priority > currentBestZone.priority)
                {
                    currentBestZone = zone;
                }
            }
        }
        return currentBestZone;
    }

    public void ChangeCameraSize(float newSize)
    {
        targetOrthographicSize = newSize;
    }

    public void StartCameraMoveUp()
    {
        if (Time.timeScale == 0) return;
        isLookingUp = true;
    }

    public void StopCameraMoveUp()
    {
        if (Time.timeScale == 0) return;
        isLookingUp = false;
    }

    public void ShakeCameraRepeatedlyDefault()
    {
        ShakeCameraRepeatedly(0.1f, 1.0f, 1.0f, new Vector3(0, 1, 0), 2, 0.1f);
    }

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
            if (Time.timeScale > 0)
            {
                float shakeOffset = Random.Range(-0.5f, 0.5f);
                transform.localPosition = originalPosition + axis * shakeOffset * intensity;
                elapsed += Time.deltaTime;
            }
            yield return null;
        }
        ongoingShakes--;
        if (ongoingShakes == 0)
        {
            isShaking = false;
            transform.localPosition = originalPosition;
        }
    }

    public void ShakeCameraRepeatedly(float duration, float intensity, float decreaseFactor, Vector3 axis, int numberOfShakes, float delayBetweenShakes)
    {
        StartCoroutine(ShakeCameraRepeatedlyCoroutine(duration, intensity, decreaseFactor, axis, numberOfShakes, delayBetweenShakes));
    }

    private IEnumerator ShakeCameraRepeatedlyCoroutine(float duration, float intensity, float decreaseFactor, Vector3 axis, int numberOfShakes, float delayBetweenShakes)
    {
        for (int i = 0; i < numberOfShakes; i++)
        {
            ShakeCamera(duration, intensity, decreaseFactor, axis);
            yield return new WaitForSeconds(delayBetweenShakes);
        }
    }

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

    public Vector2 GetLockOffset()
    {
        return currentZone.lockOffset;
    }
}