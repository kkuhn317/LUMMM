using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections;

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

    // Ignore exactly the first selection change after this component is enabled
    private bool ignoreNextSelectionChangeSfx;

    private void OnEnable()
    {
        if (!ValidateComponents()) return;

        lastSelectedObject = EventSystem.current?.currentSelectedGameObject;
        ignoreNextSelectionChangeSfx = true;

        StartCoroutine(DelayedRefresh());
    }

    private IEnumerator DelayedRefresh()
    {
        // Wait for UI layout and EventSystem to fully initialize
        yield return null;
        yield return new WaitForEndOfFrame();

        ForceRefreshToCurrentSelection();
    }

    private void OnDisable()
    {
        CancelSelectorTweens();
    }

    private void Update()
    {
        if (selectorImage == null || !selectorImage.gameObject.activeInHierarchy)
            return;

        GameObject currentSelected = EventSystem.current?.currentSelectedGameObject;

        if (currentSelected == lastSelectedObject)
            return;

        lastSelectedObject = currentSelected;

        if (currentSelected == null)
        {
            currentTarget = null;
            CancelSelectorTweens();
            return;
        }

        RectTransform targetRect = currentSelected.GetComponent<RectTransform>();
        if (targetRect == null)
        {
            currentTarget = null;
            CancelSelectorTweens();
            return;
        }

        currentTarget = targetRect;

        AnimateSelectorToCurrentTarget();

        if (ignoreNextSelectionChangeSfx)
        {
            ignoreNextSelectionChangeSfx = false;
            return;
        }

        if (selectionSfx != null && AudioManager.Instance != null)
            AudioManager.Instance.Play(selectionSfx, SoundCategory.SFX);

        onSelectionChanged?.Invoke();
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
        SnapSelectorToCurrentTarget();
    }

    private void SnapSelectorToCurrentTarget()
    {
        if (currentTarget == null) return;
        if (!selectorImage.gameObject.activeInHierarchy) return;

        if (!TryGetTargetBounds(currentTarget, out Vector2 targetSize, out Vector3 targetPos))
            return;

        selectorImage.sizeDelta = targetSize;
        selectorImage.localPosition = targetPos;
    }

    private void AnimateSelectorToCurrentTarget()
    {
        if (currentTarget == null) return;
        if (!selectorImage.gameObject.activeInHierarchy) return;

        if (!TryGetTargetBounds(currentTarget, out Vector2 targetSize, out Vector3 targetPos))
            return;

        // Cancel any previous tweens before starting a new one
        CancelSelectorTweens();

        LeanTween.size(selectorImage, targetSize, animationTime)
            .setEase(tweenType)
            .setIgnoreTimeScale(true)
            .setOnComplete(() =>
            {
                // Final correction to ensure the selector ends exactly on target
                if (selectorImage != null && currentTarget != null && selectorImage.gameObject.activeInHierarchy)
                {
                    SnapSelectorToCurrentTarget();
                }
            });

        LeanTween.moveLocal(selectorImage.gameObject, targetPos, animationTime)
            .setEase(tweenType)
            .setIgnoreTimeScale(true);
    }

    private bool TryGetTargetBounds(RectTransform target, out Vector2 targetSize, out Vector3 targetPos)
    {
        targetSize = Vector2.zero;
        targetPos = Vector3.zero;

        if (target == null) return false;

        RectTransform parentRect = selectorImage.transform.parent as RectTransform;
        if (parentRect == null) return false;

        Vector3[] worldCorners = new Vector3[4];
        target.GetWorldCorners(worldCorners);

        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;

        for (int i = 0; i < 4; i++)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                RectTransformUtility.WorldToScreenPoint(null, worldCorners[i]),
                null,
                out Vector2 localPoint
            );

            min = Vector2.Min(min, localPoint);
            max = Vector2.Max(max, localPoint);
        }

        targetSize = new Vector2(
            (max.x - min.x) + padding * 2f,
            (max.y - min.y) + padding * 2f
        );

        targetPos = (min + max) / 2f;

        return true;
    }

    private void CancelSelectorTweens()
    {
        if (selectorImage == null) return;

        LeanTween.cancel(selectorImage.gameObject);
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