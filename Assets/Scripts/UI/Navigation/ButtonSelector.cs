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

    private void OnEnable()
    {
        ValidateComponents();
    }

    private void Start()
    {
        if (!ValidateComponents()) return;

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("The script must be attached to a GameObject inside a Canvas.");
            enabled = false;
        }
    }

    private void Update()
    {
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
            Debug.LogWarning($"Selected object '{currentSelected.name}' does not have a RectTransform.");
            currentTarget = null;
            return;
        }

        currentTarget = targetRect;

        if (!UISfxGate.ConsumeSuppressNextSelectSfx())
        {
            if (selectionSfx != null && AudioManager.Instance != null)
                AudioManager.Instance.Play(selectionSfx, SoundCategory.SFX);

            onSelectionChanged?.Invoke();
        }

        AnimateSelector();
    }

    private void UpdateSelector()
    {
        if (currentTarget == null) return;
        if (LeanTween.isTweening(selectorImage.gameObject)) return;

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

        float width = max.x - min.x;
        float height = max.y - min.y;

        selectorImage.sizeDelta = new Vector2(width + padding * 2f, height + padding * 2f);
        selectorImage.localPosition = (min + max) / 2f;
    }

    private void AnimateSelector()
    {
        if (currentTarget == null) return;

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

        float width = max.x - min.x;
        float height = max.y - min.y;

        Vector2 targetSize = new Vector2(width + padding * 2f, height + padding * 2f);
        Vector3 localPosition = (min + max) / 2f;

        LeanTween.cancel(selectorImage.gameObject);

        LeanTween.size(selectorImage, targetSize, animationTime)
            .setEase(tweenType)
            .setIgnoreTimeScale(true)
            .setOnComplete(UpdateSelector);

        LeanTween.moveLocal(selectorImage.gameObject, localPosition, animationTime)
            .setEase(tweenType)
            .setIgnoreTimeScale(true);
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