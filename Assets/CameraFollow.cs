using System.Collections;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public string playerTag = "Player";
    public float smoothDampTime = 0.15f;
    private Vector3 smoothDampVelocity = Vector3.zero;

    public bool canMoveHorizontally = true;
    public float leftBounds;
    public float rightBounds;

    public bool canMoveVertically = false;
    public float topBounds;
    public float bottomBounds;

    private float camWidth, camHeight, levelMinX, levelMaxX, levelMinY, levelMaxY;
    private Vector3 originalPosition;

    // Camera Shake variables
    private bool isShaking = false;
    private float shakeDuration = 0f;
    private float shakeIntensity = 0.1f;
    private float shakeDecreaseFactor = 1.0f;

    // Camera moving up variables
    private bool isLookingUp = false;
    private Vector3 offset;
    private float returnSpeed = 5.0f; // Adjust the return speed as needed

    // Start is called before the first frame update
    void Start()
    {
        camHeight = Camera.main.orthographicSize * 2;
        camWidth = camHeight * Camera.main.aspect;

        levelMinX = leftBounds + (camWidth / 2);
        levelMaxX = rightBounds - (camWidth / 2);

        levelMinY = bottomBounds + (camHeight / 2);
        levelMaxY = topBounds - (camHeight / 2);

        originalPosition = transform.position;
        offset = new Vector3(0f, 2.3f, 0f);
    }

    // Update is called once per frame
    void Update()
    {
        GameObject[] Players = GameObject.FindGameObjectsWithTag("Player");

        if (Players.Length > 0)
        {
            GameObject target = Players[0];
            float targetX = Mathf.Max(levelMinX, Mathf.Min(levelMaxX, target.transform.position.x));
            float targetY = Mathf.Max(levelMinY, Mathf.Min(levelMaxY, target.transform.position.y));

            if (isLookingUp)
            {
                // Calculate desired position with an offset when looking up
                targetY += offset.y;
            }
            else
            {
                // Gradually move the camera back to its original position
                targetY = Mathf.MoveTowards(transform.position.y, originalPosition.y, returnSpeed * Time.deltaTime);
            }

            float x = canMoveHorizontally ? Mathf.SmoothDamp(transform.position.x, targetX, ref smoothDampVelocity.x, smoothDampTime) : transform.position.x;
            float y = canMoveVertically ? Mathf.SmoothDamp(transform.position.y, targetY, ref smoothDampVelocity.y, smoothDampTime) : transform.position.y;
            transform.position = new Vector3(x, y, transform.position.z);
        }

        if (isShaking)
        {
            ShakeCamera();
        }
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
        if (!isShaking)
        {
            originalPosition = transform.position;
        }

        isShaking = true;
        shakeDuration = duration;
        shakeIntensity = intensity;
        shakeDecreaseFactor = decreaseFactor;

        StartCoroutine(ShakeCoroutine(axis));
    }

    // Coroutine to handle the camera shake effect
    private IEnumerator ShakeCoroutine(Vector3 axis)
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float shakeOffset = Mathf.PerlinNoise(Time.time * shakeIntensity, 0f) - 0.5f;
            Vector3 offset = axis * shakeOffset * shakeIntensity;
            transform.localPosition = originalPosition + offset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        isShaking = false;
        transform.localPosition = originalPosition;
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

    private void OnDrawGizmos ()
    {
        // draw a square around the camera bounds
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(leftBounds, topBounds, 0), new Vector3(rightBounds, topBounds, 0));
        Gizmos.DrawLine(new Vector3(rightBounds, topBounds, 0), new Vector3(rightBounds, bottomBounds, 0));
        Gizmos.DrawLine(new Vector3(rightBounds, bottomBounds, 0), new Vector3(leftBounds, bottomBounds, 0));
        Gizmos.DrawLine(new Vector3(leftBounds, bottomBounds, 0), new Vector3(leftBounds, topBounds, 0));

    }
}

