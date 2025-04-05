using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class CreditsScroller : MonoBehaviour
{
    public GameObject creditsText;
    public float scrollSpeed = 50f; // Default auto scroll speed
    public float scrollUpSpeed = 50f; // When scrolling up manually
    public float fastScrollSpeed = 100f; // When scrolling down manually
    private float currentScrollSpeed = 0f; // Actual scroll speed applied each frame
    private float targetScrollSpeed = 0f; // Target speed to smoothly return to
    private float startPositionY; // Initial Y position
    public float stopPositionY; // Limit Y position

    public GameObject middleOfScreen;
    public InputActionAsset inputActions;

    private InputAction navigateAction;
    private InputAction touchScrollAction;

    void Awake()
    {
        if (inputActions != null)
        {
            navigateAction = inputActions.FindActionMap("UI").FindAction("Navigate");
            touchScrollAction = inputActions.FindActionMap("UI").FindAction("TouchScroll");
        }
        else
        {
            Debug.LogError("Input Action Asset is not assigned!");
        }
    }

    void OnEnable()
    {
        if (navigateAction != null)
        {
            navigateAction.performed += OnNavigatePerformed;
            navigateAction.Enable();
        }

        if (touchScrollAction != null)
        {
            touchScrollAction.performed += OnTouchScrollPerformed;
            touchScrollAction.Enable();
        }
    }

    void OnDisable()
    {
        if (navigateAction != null)
        {
            navigateAction.performed -= OnNavigatePerformed;
            navigateAction.Disable();
        }

        if (touchScrollAction != null)
        {
            touchScrollAction.performed -= OnTouchScrollPerformed;
            touchScrollAction.Disable();
        }
    }

    void Start()
    {
        currentScrollSpeed = scrollSpeed;
        targetScrollSpeed = scrollSpeed;
        startPositionY = creditsText.transform.position.y;
    }

    void Update()
    {
        // Apply scrolling
        Vector3 newPos = creditsText.transform.position + Vector3.up * currentScrollSpeed * Time.deltaTime * Screen.height / 1080;

        float adjustedStopPositionY = stopPositionY * Screen.height / 1080;
        newPos.y = Mathf.Clamp(newPos.y, startPositionY, adjustedStopPositionY);

        creditsText.transform.position = newPos;
    }

    private void OnNavigatePerformed(InputAction.CallbackContext context)
    {
        Vector2 navigateValue = context.ReadValue<Vector2>();

        if (navigateValue.y > 0)
        {
            targetScrollSpeed = -scrollUpSpeed;
        }
        else if (navigateValue.y < 0)
        {
            targetScrollSpeed = fastScrollSpeed;
        }
        else
        {
            targetScrollSpeed = scrollSpeed;
        }
    }

    private void OnTouchScrollPerformed(InputAction.CallbackContext context)
    {
        Vector2 delta = context.ReadValue<Vector2>();

        if (Mathf.Abs(delta.y) > 0.1f)
        {
            float currentY = creditsText.transform.position.y;
            float adjustedStopPositionY = stopPositionY * Screen.height / 1080;

            // Prevent scrolling up when already at the top
            if (delta.y > 0 && currentY <= startPositionY + 0.01f)
            {
                targetScrollSpeed = 0f;
                return;
            }

            // Prevent scrolling down when already at the bottom
            if (delta.y < 0 && currentY >= adjustedStopPositionY - 0.01f)
            {
                targetScrollSpeed = 0f;
                return;
            }

            // Otherwise, apply input normally
            targetScrollSpeed = -delta.y;
        }
        else
        {
            targetScrollSpeed = scrollSpeed;
        }
    }

    void LateUpdate()
    {
        // Final correction to avoid jitter caused by input at limits
        float adjustedStopPositionY = stopPositionY * Screen.height / 1080;
        Vector3 currentPos = creditsText.transform.position;

        float clampedY = Mathf.Clamp(currentPos.y, startPositionY, adjustedStopPositionY);
        if (!Mathf.Approximately(currentPos.y, clampedY))
        {
            currentPos.y = clampedY;
            creditsText.transform.position = currentPos;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        if (middleOfScreen == null)
            return;

        float adjustedStopPositionY = stopPositionY * Camera.main.pixelHeight / 1080;
        float drawPosY = creditsText.transform.position.y - adjustedStopPositionY + middleOfScreen.transform.position.y;

        Vector3 drawPos = new(creditsText.transform.position.x, drawPosY, creditsText.transform.position.z);

        Gizmos.DrawSphere(drawPos, 50f);
    }
}