using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class CreditsScroller : MonoBehaviour
{
    public GameObject creditRoot;
    public TextMeshProUGUI creditsText;

    public float scrollSpeed = 50f;
    public float scrollUpSpeed = 250f;
    public float fastScrollSpeed = 250f;

    private float currentScrollSpeed = 0f;
    private float targetScrollSpeed = 0f;

    private float startPositionY;
    private float stopPositionY;

    public GameObject middleOfScreen;
    public InputActionAsset inputActions;

    private InputAction navigateAction;
    private InputAction touchScrollAction;

    private string lastText;
    private Vector2 lastRectSize;
    private bool recalcQueued;
    private Coroutine recalcRoutine;

    // Calculate stopPositionY in WORLD SPACE (works at start AND mid-scroll)
    private void CalculateStopPosition()
    {
        if (creditRoot == null || middleOfScreen == null)
        {
            Debug.LogError("creditRoot or middleOfScreen is not assigned!");
            return;
        }

        if (creditsText == null)
        {
            Debug.LogError("creditsText is not assigned!");
            return;
        }

        creditsText.ForceMeshUpdate(true);

        Bounds textBounds = creditsText.textBounds;

        // Bottom of rendered text in WORLD space (accounts for canvas scaling)
        float textBottomWorld = creditsText.transform
            .TransformPoint(new Vector3(0f, textBounds.min.y, 0f)).y;

        float middleScreenY = middleOfScreen.transform.position.y;

        // How much we must move FROM CURRENT POSITION so bottom aligns with middle marker
        float movementNeeded = middleScreenY - textBottomWorld;

        // Baseline is current creditRoot position (so it readjusts when language changes at the end)
        float baselineY = creditRoot.transform.position.y;
        stopPositionY = baselineY + movementNeeded;

        // Never allow stop below start
        if (stopPositionY < startPositionY)
            stopPositionY = startPositionY;
    }

    void Awake()
    {
        if (inputActions != null)
        {
            var uiMap = inputActions.FindActionMap("UI");
            navigateAction = uiMap.FindAction("Navigate");
            touchScrollAction = uiMap.FindAction("TouchScroll");
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

        if (recalcRoutine != null)
        {
            StopCoroutine(recalcRoutine);
            recalcRoutine = null;
        }

        recalcQueued = false;
    }

    void Start()
    {
        currentScrollSpeed = scrollSpeed;
        targetScrollSpeed = scrollSpeed;

        startPositionY = creditRoot.transform.position.y;

        CacheTextState();
        CalculateStopPosition();
    }

    void Update()
    {
        DetectAndQueueRecalc();

        currentScrollSpeed = Mathf.Lerp(currentScrollSpeed, targetScrollSpeed, Time.deltaTime * 5f);

        Vector3 newPos = creditRoot.transform.position + Vector3.up * currentScrollSpeed * Time.deltaTime;
        newPos.y = Mathf.Clamp(newPos.y, startPositionY, stopPositionY);
        creditRoot.transform.position = newPos;
    }

    private void DetectAndQueueRecalc()
    {
        if (creditsText == null) return;

        var rt = creditsText.rectTransform;
        bool textChanged = !string.Equals(lastText, creditsText.text);
        bool rectChanged = rt != null && (rt.rect.size != lastRectSize);

        if ((textChanged || rectChanged) && !recalcQueued)
        {
            recalcQueued = true;

            if (recalcRoutine != null)
                StopCoroutine(recalcRoutine);

            recalcRoutine = StartCoroutine(RecalculateNextFrame());
        }
    }

    private IEnumerator RecalculateNextFrame()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();
        creditsText.ForceMeshUpdate(true);

        CacheTextState();
        CalculateStopPosition();

        // If the new stop is LOWER than current, snap down to the new limit
        Vector3 pos = creditRoot.transform.position;
        pos.y = Mathf.Clamp(pos.y, startPositionY, stopPositionY);
        creditRoot.transform.position = pos;

        recalcQueued = false;
        recalcRoutine = null;
    }

    private void CacheTextState()
    {
        lastText = creditsText != null ? creditsText.text : "";
        lastRectSize = creditsText != null ? creditsText.rectTransform.rect.size : Vector2.zero;
    }

    private void OnNavigatePerformed(InputAction.CallbackContext context)
    {
        Vector2 navigateValue = context.ReadValue<Vector2>();

        if (navigateValue.y > 0) targetScrollSpeed = -scrollUpSpeed;
        else if (navigateValue.y < 0) targetScrollSpeed = fastScrollSpeed;
        else targetScrollSpeed = scrollSpeed;
    }

    private void OnTouchScrollPerformed(InputAction.CallbackContext context)
    {
        Vector2 delta = context.ReadValue<Vector2>();

        if (Mathf.Abs(delta.y) > 0.1f)
        {
            float currentY = creditRoot.transform.position.y;

            if (delta.y > 0 && currentY <= startPositionY + 0.01f)
            {
                targetScrollSpeed = 0f;
                return;
            }

            if (delta.y < 0 && currentY >= stopPositionY - 0.01f)
            {
                targetScrollSpeed = 0f;
                return;
            }

            targetScrollSpeed = -delta.y;
        }
        else
        {
            targetScrollSpeed = scrollSpeed;
        }
    }

    void LateUpdate()
    {
        Vector3 currentPos = creditRoot.transform.position;

        float clampedY = Mathf.Clamp(currentPos.y, startPositionY, stopPositionY);
        if (!Mathf.Approximately(currentPos.y, clampedY))
        {
            currentPos.y = clampedY;
            creditRoot.transform.position = currentPos;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (creditRoot == null || creditsText == null || middleOfScreen == null)
            return;

        // Keep bounds accurate in editor
        creditsText.ForceMeshUpdate(true);
        Bounds b = creditsText.textBounds;

        // Bottom of rendered text in WORLD space (current)
        float textBottomWorld = creditsText.transform
            .TransformPoint(new Vector3(0f, b.min.y, 0f)).y;

        // Draw bottom of text
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(new Vector3(creditsText.transform.position.x, textBottomWorld, creditsText.transform.position.z), 25f);

        // Draw target middle marker
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(middleOfScreen.transform.position, 25f);

        // Draw stopPositionY for creditRoot
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(new Vector3(creditRoot.transform.position.x, stopPositionY, creditRoot.transform.position.z), 50f);

        // Draw line from bottom, target for quick visual debugging
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            new Vector3(creditsText.transform.position.x, textBottomWorld, creditsText.transform.position.z),
            middleOfScreen.transform.position
        );
    }
}