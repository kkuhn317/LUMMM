using System.Collections;
using UnityEngine;
using System.Linq;

public class CameraFollow : MonoBehaviour
{
    private GameObject[] players;
    private PlayerRegistry playerRegistry;

    [Header("Follow Smoothing")]
    [Tooltip("Time constant used by SmoothDamp for panning.")]
    public float smoothDampTime = 0.15f;
    [Tooltip("Time constant used by SmoothDamp for zooming.")]
    [SerializeField] private float zoomSmoothTime = 0.20f;
    private Vector3 smoothDampVelocity = Vector3.zero; // x,y used for pan; z used for zoom

    private CameraZone[] zones;
    private CameraZone currentZone;
    private CameraZone previousZone;
    private bool justEnteredSnapZone = false;

    private bool snappedOnEntry = false;
    private bool clampInThisZoneStay = false;

    private Camera cam;
    public float camHeight => cam.orthographicSize * 2f;
    public float camWidth  => camHeight * cam.aspect;

    private Vector3 originalPosition;
    private bool isShaking = false;
    private float shakeDuration = 0f;
    private float shakeIntensity = 0.1f;
    private float shakeDecreaseFactor = 1.0f;

    private bool isLookingUp = false;
    public Vector3 offset;
    private int ongoingShakes = 0;

    [Header("Change Camera Size")]
    private float originalOrthographicSize;
    private float targetOrthographicSize;

    private Vector2 lastTargetPosition;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        originalPosition = transform.position;
        offset = new Vector3(0f, 2f, 0f);

        originalOrthographicSize = cam.orthographicSize;
        targetOrthographicSize = originalOrthographicSize;

        zones = FindObjectsOfType<CameraZone>(true);
        if (zones == null || zones.Length == 0)
        {
            Debug.LogWarning("No CameraZones found in scene. Add at least one CameraZone.");
        }

        CacheRegistry();
    }

    private void CacheRegistry()
    {
        if (GameManager.Instance != null)
            playerRegistry = GameManager.Instance.GetSystem<PlayerRegistry>();

        if (playerRegistry == null)
            playerRegistry = FindObjectOfType<PlayerRegistry>(true);
    }

    void Update()
    {
        // players = GameManager.Instance.GetPlayerObjects();
        
        if (playerRegistry == null) CacheRegistry();
        players = playerRegistry != null ? playerRegistry.GetAllPlayerObjects() : null;

        if (players == null || players.Length == 0) return;

        // 1) Compute target (players centroid)
        Vector2 target = Vector2.zero;
        int validCount = 0;

        foreach (var p in players)
        {
            if (!p) continue;
            target += (Vector2)p.transform.position;
            validCount++;
        }

        if (validCount == 0) return;
        target /= validCount;
        
        // 2) Select zone by target position
        var zone = GetCurrentZone(target);
        currentZone = zone;
        if (zone == null) return;

        // Detect zone change and decide entry snap + clamping for the stay
        justEnteredSnapZone = false;
        if (zone != previousZone)
        {
            snappedOnEntry = zone.ShouldSnapOnEnterFrom(previousZone);
            justEnteredSnapZone = snappedOnEntry;
            clampInThisZoneStay = zone.ShouldClampForStay(snappedOnEntry);
            previousZone = zone;
        }

        // 3) Compute desired position (targetX, targetY)
        // Compute min/max locally to avoid relying on CameraZone's internal camera reference
        float minX = zone.lockToVertical ? (zone.horizontalMiddle + zone.lockOffset.x)
                                         : zone.topLeft.x + (camWidth / 2f);
        float maxX = zone.lockToVertical ? (zone.horizontalMiddle + zone.lockOffset.x)
                                         : zone.bottomRight.x - (camWidth / 2f);
        float minY = zone.lockToHorizontal ? (zone.verticalMiddle + zone.lockOffset.y)
                                           : zone.bottomRight.y + (camHeight / 2f);
        float maxY = zone.lockToHorizontal ? (zone.verticalMiddle + zone.lockOffset.y)
                                           : zone.topLeft.y - (camHeight / 2f);

        float posOffsetX = -zone.cameraPosOffset.x * camWidth / 2f;
        float posOffsetY = -zone.cameraPosOffset.y * camHeight / 2f;

        float targetX = Mathf.Clamp(target.x, minX, maxX) + posOffsetX;
        float targetY = Mathf.Clamp(target.y, minY, maxY) + posOffsetY;

        bool tooSmallHorizontal = false;
        bool tooSmallVertical = false;

        if ((zone.bottomRight.x - zone.topLeft.x) < camWidth && !zone.lockToVertical)
        {
            tooSmallHorizontal = true;
            targetX = (zone.bottomRight.x + zone.topLeft.x) * 0.5f;
        }
        if ((zone.topLeft.y - zone.bottomRight.y) < camHeight && !zone.lockToHorizontal)
        {
            tooSmallVertical = true;
            targetY = (zone.topLeft.y + zone.bottomRight.y) * 0.5f;
        }

        if (isLookingUp)
        {
            targetY += offset.y;
        }

        // 4) Snap vs Smooth
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

        // 5) Smooth zoom
        cam.orthographicSize = Mathf.SmoothDamp(
            cam.orthographicSize, targetOrthographicSize, ref smoothDampVelocity.z, zoomSmoothTime);

        // 6) Clamp position for the stay (policy-driven)
        if (clampInThisZoneStay)
        {
            Vector2 clampedPos = new(
                Mathf.Clamp(transform.position.x, minX, maxX),
                Mathf.Clamp(transform.position.y, minY, maxY)
            );
            if (tooSmallHorizontal)
            {
                clampedPos.x = (zone.bottomRight.x + zone.topLeft.x) * 0.5f;
            }
            if (tooSmallVertical)
            {
                clampedPos.y = (zone.topLeft.y + zone.bottomRight.y) * 0.5f;
            }
            transform.position = new Vector3(clampedPos.x, clampedPos.y, transform.position.z);
        }

        if (isShaking)
        {
            ShakeCamera();
        }
    }

    private CameraZone GetCurrentZone(Vector2 worldPoint)
    {
        if (zones == null || zones.Length == 0) return null;

        // Choose the smallest-area zone that contains the point (most specific)
        CameraZone best = null;
        float bestArea = float.MaxValue;

        foreach (var z in zones)
        {
            if (worldPoint.x >= z.topLeft.x && worldPoint.x <= z.bottomRight.x &&
                worldPoint.y >= z.bottomRight.y && worldPoint.y <= z.topLeft.y)
            {
                float area = Mathf.Abs((z.bottomRight.x - z.topLeft.x) * (z.topLeft.y - z.bottomRight.y));
                if (best == null || area < bestArea)
                {
                    best = z;
                    bestArea = area;
                }
            }
        }
        return best;
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
        if (currentZone != null) currentZone.lockOffset.x = offset;
    }

    public void SetLockOffsetY(float offset)
    {
        if (currentZone != null) currentZone.lockOffset.y = offset;
    }

    public void SetLockedHorizontal(bool locked)
    {
        if (currentZone != null) currentZone.lockToHorizontal = locked;
    }

    public void SetLockedVertical(bool locked)
    {
        if (currentZone != null) currentZone.lockToVertical = locked;
    }

    public Vector2 GetLockOffset()
    {
        return currentZone != null ? currentZone.lockOffset : Vector2.zero;
    }
}