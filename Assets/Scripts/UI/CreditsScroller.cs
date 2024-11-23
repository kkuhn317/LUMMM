using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class CreditsScroller : MonoBehaviour
{
    public GameObject creditsText;
    public float scrollSpeed = 50f; // Speed at which the credits scroll
    public float scrollUpSpeed = 50f; // Speed at which the credits scroll upwards when up arrow is pressed
    public float fastScrollSpeed = 100f; // Speed at which the credits scroll when down arrow is pressed
    private float currentScrollSpeed = 0f; // Current scroll speed
    private float startPositionY; // Initial position of credits
    public float stopPositionY; // Position where credits stop scrolling

    public GameObject middleOfScreen;
    public InputActionAsset inputActions; // Reference to the Input Action Asset in the Inspector

    private InputAction navigateAction; // Input action for navigation

    void Awake()
    {
        // Retrieve the Navigate action from the Input Action Asset
        if (inputActions != null)
        {
            navigateAction = inputActions.FindActionMap("UI").FindAction("Navigate"); // This finds the named action map and retrieves the named action from the referenced Input Action Asset
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
            // Enable and register callback for the Navigate action
            navigateAction.performed += OnNavigatePerformed;
            navigateAction.Enable();
        }
    }

    void OnDisable()
    {
        if (navigateAction != null)
        {
            // Unregister callback and disable the Navigate action
            navigateAction.performed -= OnNavigatePerformed;
            navigateAction.Disable();
        }
    }

    void Start()
    {
        currentScrollSpeed = scrollSpeed;
        startPositionY = creditsText.transform.position.y;
    }

    void Update()
    {
        // Move the credits based on the current scroll speed
        Vector3 newPos = creditsText.transform.position + Vector3.up * currentScrollSpeed * Time.deltaTime * Screen.height / 1080;

        // Adjust the stop position based on screen resolution
        float adjustedStopPositionY = stopPositionY * Screen.height / 1080;

        // Clamp the position between startPositionY and adjustedStopPositionY
        newPos.y = Mathf.Clamp(newPos.y, startPositionY, adjustedStopPositionY);

        creditsText.transform.position = newPos;
    }

    private void OnNavigatePerformed(InputAction.CallbackContext context)
    {
        Vector2 navigateValue = context.ReadValue<Vector2>();

        if (navigateValue.y > 0) // Scroll up
        {
            currentScrollSpeed = -scrollUpSpeed;
        }
        else if (navigateValue.y < 0) // Scroll down
        {
            currentScrollSpeed = fastScrollSpeed;
        }
        else // No input (navigateValue.y == 0)
        {
            currentScrollSpeed = scrollSpeed;
        }
    }

    // Show the stop position in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        if (middleOfScreen == null)
        {
            return;
        }

        float adjustedStopPositionY = stopPositionY * Camera.main.pixelHeight / 1080;
        float drawPosY = creditsText.transform.position.y - adjustedStopPositionY + middleOfScreen.transform.position.y;

        Vector3 drawPos = new(creditsText.transform.position.x, drawPosY, creditsText.transform.position.z);

        Gizmos.DrawSphere(drawPos, 50f);
    }
}