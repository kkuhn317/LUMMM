using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonSelector : MonoBehaviour
{
    [SerializeField] private RectTransform selectorImage; // The selector highlight
    [SerializeField] private float padding = 30f; // Padding around the selector
    [SerializeField] private float animationTime = 0.2f; // Animation time for transitions
    [SerializeField] private LeanTweenType tweenType = LeanTweenType.easeOutQuad; // Type of easing animation

    private RectTransform currentTarget;
    private GameObject lastSelectedObject;
    private Canvas canvas;

    void OnEnable()
    {
        ValidateComponents();
    }

    void Start()
    {
        if (!ValidateComponents()) return;

        // Get the Canvas component
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("The script must be attached to a GameObject inside a Canvas.");
            enabled = false;
        }
    }

    void Update()
    {
        // Track changes in position/size of the current target
        if (currentTarget != null)
        {
            UpdateSelector(); // Adjust the selector to match the visible part of the target
        }

        // Check for selection changes
        GameObject currentSelected = EventSystem.current?.currentSelectedGameObject;

        if (currentSelected == lastSelectedObject) return; // No change in selection

        lastSelectedObject = currentSelected;

        if (currentSelected != null)
        {
            RectTransform targetRect = currentSelected.GetComponent<RectTransform>();
            if (targetRect != null)
            {
                currentTarget = targetRect;
                AnimateSelector(); // Animate to the new target
            }
            else
            {
                Debug.LogWarning($"Selected object '{currentSelected.name}' does not have a RectTransform.");
                currentTarget = null; // Clear the current target
            }
        }
        else
        {
            currentTarget = null; // No selection
        }
    }

    private void UpdateSelector()
    {
        if (currentTarget == null) return;

        // Prevent updates while animation is running
        if (LeanTween.isTweening(selectorImage.gameObject)) return;

        Vector3[] worldCorners = new Vector3[4];
        currentTarget.GetWorldCorners(worldCorners);

        // Convert world corners to local canvas space
        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;

        for (int i = 0; i < 4; i++)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(selectorImage.transform.parent as RectTransform,
                RectTransformUtility.WorldToScreenPoint(null, worldCorners[i]), 
                null, 
                out localPoint);

            min = Vector2.Min(min, localPoint);
            max = Vector2.Max(max, localPoint);
        }

        // Compute width and height correctly in local space
        float width = max.x - min.x;
        float height = max.y - min.y;

        Vector2 targetSize = new Vector2(width + padding * 2, height + padding * 2);
        Vector3 localPosition = (min + max) / 2;

        // Directly set the size without interpolation
        selectorImage.sizeDelta = targetSize;
        selectorImage.localPosition = localPosition;
    }

    private void AnimateSelector()
    {
        if (currentTarget == null) return;

        Vector3[] worldCorners = new Vector3[4];
        currentTarget.GetWorldCorners(worldCorners);

        // Convert world corners to local canvas space
        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;

        for (int i = 0; i < 4; i++)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(selectorImage.transform.parent as RectTransform,
                RectTransformUtility.WorldToScreenPoint(null, worldCorners[i]), 
                null, 
                out localPoint);

            min = Vector2.Min(min, localPoint);
            max = Vector2.Max(max, localPoint);
        }

        // Compute width and height correctly in local space
        float width = max.x - min.x;
        float height = max.y - min.y;

        Vector2 targetSize = new Vector2(width + padding * 2, height + padding * 2);
        Vector3 localPosition = (min + max) / 2;

        LeanTween.cancel(selectorImage.gameObject);

        // Animate size and position
        LeanTween.size(selectorImage, targetSize, animationTime)
            .setEase(tweenType)
            .setIgnoreTimeScale(true) // Ensures animations still play when paused
            .setOnComplete(() => UpdateSelector());

        LeanTween.moveLocal(selectorImage.gameObject, localPosition, animationTime)
        .setEase(tweenType)
        .setIgnoreTimeScale(true);
    }

    private Rect GetVisibleRect(RectTransform rectTransform)
    {
        if (rectTransform == null) return new Rect();

        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Rect canvasRect = GetCanvasRect(canvas);

        // Clip the corners to the canvas bounds
        Vector3[] clippedCorners = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            clippedCorners[i] = new Vector3(
                Mathf.Clamp(corners[i].x, canvasRect.xMin, canvasRect.xMax),
                Mathf.Clamp(corners[i].y, canvasRect.yMin, canvasRect.yMax),
                corners[i].z
            );
        }

        // Calculate the visible rectangle in world space
        float minX = Mathf.Min(clippedCorners[0].x, clippedCorners[1].x, clippedCorners[2].x, clippedCorners[3].x);
        float maxX = Mathf.Max(clippedCorners[0].x, clippedCorners[1].x, clippedCorners[2].x, clippedCorners[3].x);
        float minY = Mathf.Min(clippedCorners[0].y, clippedCorners[1].y, clippedCorners[2].y, clippedCorners[3].y);
        float maxY = Mathf.Max(clippedCorners[0].y, clippedCorners[1].y, clippedCorners[2].y, clippedCorners[3].y);

        return new Rect(new Vector2(minX, minY), new Vector2(maxX - minX, maxY - minY));
    }

    private Rect GetCanvasRect(Canvas canvas)
    {
        RectTransform canvasRectTransform = canvas.GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4];
        canvasRectTransform.GetWorldCorners(corners);

        float minX = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float maxX = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float minY = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
        float maxY = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private bool ValidateComponents()
    {
        if (selectorImage == null)
        {
            Debug.LogError("Selector image is not assigned in the Inspector.");
            enabled = false;
            return false;
        }

        if (EventSystem.current == null)
        {
            Debug.LogError("No EventSystem found in the scene. Please add one via GameObject > UI > EventSystem.");
            enabled = false;
            return false;
        }

        if (selectorImage.transform.parent == null)
        {
            Debug.LogError("Selector image must have a parent Transform.");
            enabled = false;
            return false;
        }

        return true;
    }
}