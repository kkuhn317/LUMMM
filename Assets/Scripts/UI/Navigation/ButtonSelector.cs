using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ButtonSelector : MonoBehaviour
{
    [SerializeField] private RectTransform selectorImage;
    [SerializeField] private float padding = 30f;
    [SerializeField] private float animationTime = 0.2f;
    [SerializeField] private LeanTweenType tweenType = LeanTweenType.easeOutQuad;

    [Header("SFX")]
    [SerializeField] private AudioClip selectionSfx;
    [SerializeField] private UnityEvent onSelectionChanged;

    private RectTransform currentTarget;
    private GameObject lastSelectedObject;
    private Canvas canvas;

    // We control animation ourselves (avoid LeanTween.isTweening)
    private bool isAnimating;

    // ignore exactly the first selection change after enable
    private bool ignoreNextSelectionChangeSfx;

    private void OnEnable()
    {
        if (!ValidateComponents()) return;

        // Whatever is selected right now is "baseline"
        lastSelectedObject = EventSystem.current?.currentSelectedGameObject;

        // But EventSystem may assign a new selection AFTER OnEnable
        // so ignore the next selection change SFX once.
        ignoreNextSelectionChangeSfx = true;

        // Snap selector to whatever is currently selected (if any)
        ForceRefreshToCurrentSelection();
    }

    private void OnDisable()
    {
        isAnimating = false;
    }

    private void ForceRefreshToCurrentSelection()
    {
        if (selectorImage == null || !selectorImage.gameObject.activeInHierarchy)
            return;

        GameObject selected = EventSystem.current?.currentSelectedGameObject;
        if (selected == null)
        {
            currentTarget = null;
            return;
        }

        RectTransform rect = selected.GetComponent<RectTransform>();
        if (rect == null)
        {
            currentTarget = null;
            return;
        }

        currentTarget = rect;
        UpdateSelector(); // snap, no audio
    }

    private void Start()
    {
        if (!ValidateComponents()) return;

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("ButtonSelector must be inside a Canvas.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (selectorImage == null || !selectorImage.gameObject.activeInHierarchy)
            return;
        
        /*if (isAnimating && !LeanTween.isTweening(selectorImage.gameObject))
        {
            isAnimating = false;
        }*/

        if (currentTarget != null)
            UpdateSelector();

        GameObject currentSelected = EventSystem.current?.currentSelectedGameObject;
        if (currentSelected == lastSelectedObject) return;

        lastSelectedObject = currentSelected;

        if (currentSelected == null)
        {
            currentTarget = null;
            return;
        }

        RectTransform targetRect = currentSelected.GetComponent<RectTransform>();
        if (targetRect == null)
        {
            currentTarget = null;
            return;
        }

        currentTarget = targetRect;

        // Always move the selector on any selection change
        AnimateSelector();

        // Suppress audio exactly once after enable (covers EventSystem's initial selection assignment)
        if (ignoreNextSelectionChangeSfx)
        {
            ignoreNextSelectionChangeSfx = false;
            return;
        }

        // Now it's truly user navigation
        if (selectionSfx != null && AudioManager.Instance != null)
            AudioManager.Instance.Play(selectionSfx, SoundCategory.SFX);

        onSelectionChanged?.Invoke();
    }

    private void UpdateSelector()
    {
        if (currentTarget == null) return;
        // if (isAnimating) return;
        if (!selectorImage.gameObject.activeInHierarchy) return;

        Vector3[] worldCorners = new Vector3[4];
        currentTarget.GetWorldCorners(worldCorners);

        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;

        for (int i = 0; i < 4; i++)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                selectorImage.transform.parent as RectTransform,
                RectTransformUtility.WorldToScreenPoint(null, worldCorners[i]),
                null,
                out Vector2 localPoint);

            min = Vector2.Min(min, localPoint);
            max = Vector2.Max(max, localPoint);
        }

        selectorImage.sizeDelta = new Vector2(
            (max.x - min.x) + padding * 2f,
            (max.y - min.y) + padding * 2f
        );

        selectorImage.localPosition = (min + max) / 2f;
    }

    private void AnimateSelector()
    {
        if (currentTarget == null) return;
        if (!selectorImage.gameObject.activeInHierarchy) return;

        Vector3[] worldCorners = new Vector3[4];
        currentTarget.GetWorldCorners(worldCorners);

        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;

        for (int i = 0; i < 4; i++)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                selectorImage.transform.parent as RectTransform,
                RectTransformUtility.WorldToScreenPoint(null, worldCorners[i]),
                null,
                out Vector2 localPoint);

            min = Vector2.Min(min, localPoint);
            max = Vector2.Max(max, localPoint);
        }

        Vector2 targetSize = new Vector2(
            (max.x - min.x) + padding * 2f,
            (max.y - min.y) + padding * 2f
        );

        Vector3 targetPos = (min + max) / 2f;

        LeanTween.cancel(selectorImage.gameObject);

        isAnimating = true;

        LeanTween.size(selectorImage, targetSize, animationTime)
            .setEase(tweenType)
            .setIgnoreTimeScale(true)
            .setOnComplete(() =>
            {
                isAnimating = false;
                UpdateSelector();
            });

        LeanTween.moveLocal(selectorImage.gameObject, targetPos, animationTime)
            .setEase(tweenType)
            .setIgnoreTimeScale(true);
    }

    private bool ValidateComponents()
    {
        if (selectorImage == null)
        {
            Debug.LogError("Selector image not assigned.");
            enabled = false;
            return false;
        }

        if (EventSystem.current == null)
        {
            Debug.LogError("No EventSystem in scene.");
            enabled = false;
            return false;
        }

        if (selectorImage.transform.parent == null)
        {
            Debug.LogError("Selector image must have a parent.");
            enabled = false;
            return false;
        }

        return true;
    }
}