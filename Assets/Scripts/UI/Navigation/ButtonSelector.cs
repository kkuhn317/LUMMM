using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.UI;

public class ButtonSelector : MonoBehaviour
{
    [SerializeField] private RectTransform selectorImage;
    [SerializeField] private float padding = 30f;
    [SerializeField] private float animationTime = 0.2f;
    [SerializeField] private LeanTweenType tweenType = LeanTweenType.easeOutQuad;

    [Header("SFX")]
    [SerializeField] private AudioClip selectionSfx;
    [SerializeField] private UnityEvent onSelectionChanged;

    // How many frames to keep retrying if no selection is found on enable
    [SerializeField] private int maxRefreshRetries = 10;

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = false;

    // What the selector is CURRENTLY, successfully positioned on. Distinct from
    // lastSelectedObject: this only advances when placement actually happened.
    private GameObject positionedOn;
    private bool needsReposition;

    // Edge-detection for SFX / events only.
    private GameObject lastSelectedObject;

    // Ignore exactly the first selection change after this component is enabled.
    private bool ignoreNextSelectionChangeSfx;

    private Camera canvasCamera;

    private string SceneTag => $"[ButtonSelector | {gameObject.scene.name} | {name}]";
    private void Log(string m)  { if (verboseLogging) Debug.Log($"{SceneTag} {m}", this); }
    private void Warn(string m) { if (verboseLogging) Debug.LogWarning($"{SceneTag} {m}", this); }

    private void OnEnable()
    {
        if (!ValidateComponents()) return;

        lastSelectedObject = null;
        positionedOn = null;
        needsReposition = true;
        ignoreNextSelectionChangeSfx = true;

        CacheCanvasCamera();
        StartCoroutine(EnsureSelection());
    }

    private void OnDisable()
    {
        CancelSelectorTweens();
    }

    private void CacheCanvasCamera()
    {
        Canvas canvas = selectorImage.GetComponentInParent<Canvas>();
        canvasCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera
            : null;
    }

    /// <summary>
    /// Only responsibility: make sure SOMETHING is selected at startup.
    /// Positioning is deliberately NOT done here — Update owns that, so it can
    /// keep retrying until the selector is actually active and laid out.
    /// </summary>
    private IEnumerator EnsureSelection()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();

        for (int attempt = 0; attempt < maxRefreshRetries; attempt++)
        {
            if (TryGetSelectedObject(out GameObject selected))
            {
                if (EventSystem.current.currentSelectedGameObject == null)
                    EventSystem.current.SetSelectedGameObject(selected);

                needsReposition = true; // hand off to Update; it retries until placed
                Log($"EnsureSelection: selection is '{selected.name}' after {attempt + 1} attempt(s).");
                yield break;
            }
            yield return null;
            Canvas.ForceUpdateCanvases();
        }

        Warn($"EnsureSelection: no selectable object after {maxRefreshRetries} attempts.");
    }

    private void Update()
    {
        if (selectorImage == null) return;

        GameObject sel = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject
            : null;

        // --- 1) SFX / event: fire on a genuine selection change, regardless of
        //         whether we can position yet. ---
        if (sel != lastSelectedObject)
        {
            lastSelectedObject = sel;
            needsReposition = true;

            if (sel != null)
            {
                if (ignoreNextSelectionChangeSfx)
                {
                    ignoreNextSelectionChangeSfx = false;
                }
                else
                {
                    if (selectionSfx != null && AudioManager.Instance != null)
                        AudioManager.Instance.Play(selectionSfx, SoundCategory.SFX);
                    onSelectionChanged?.Invoke();
                }
            }
        }

        // --- 2) Positioning: retried every frame until it actually succeeds. ---
        // Nothing left to do if we're already correctly placed on the current selection.
        if (!needsReposition && positionedOn == sel)
            return;

        if (sel == null)
        {
            currentTargetCleanup();
            positionedOn = null;
            needsReposition = false;
            return;
        }

        // Not ready yet (selector hidden by an intro/transition): stay dirty, retry next frame.
        if (!selectorImage.gameObject.activeInHierarchy)
            return;

        RectTransform rect = sel.GetComponent<RectTransform>();
        if (rect == null)
        {
            currentTargetCleanup();
            positionedOn = sel;        // nothing we can do for this target; stop retrying it
            needsReposition = false;
            return;
        }

        // Snap when first acquiring (or recovering), animate when moving between buttons.
        bool animate = positionedOn != null && positionedOn != sel;
        bool placed = PositionSelector(rect, animate);

        if (placed)
        {
            Log($"Positioned selector on '{sel.name}' (animate={animate}).");
            positionedOn = sel;
            needsReposition = false;
        }
        // If not placed (bounds not ready / zero-size layout), remain dirty and retry next frame.
    }

    private RectTransform currentTarget;

    private void currentTargetCleanup()
    {
        currentTarget = null;
        CancelSelectorTweens();
    }

    private bool TryGetSelectedObject(out GameObject selected)
    {
        selected = null;
        if (EventSystem.current == null) return false;

        selected = EventSystem.current.currentSelectedGameObject
                   ?? EventSystem.current.firstSelectedGameObject;

        if (selected == null || !selected.activeInHierarchy) { selected = null; return false; }
        if (selected.GetComponent<RectTransform>() == null)  { selected = null; return false; }
        return true;
    }

    /// <summary>
    /// Returns true ONLY if the selector was actually moved/sized to the target.
    /// Returns false if the selector isn't active or the target has no usable
    /// bounds yet (e.g. layout not built, async-localized text still arriving) —
    /// the caller treats false as "retry next frame".
    /// </summary>
    private bool PositionSelector(RectTransform target, bool animate)
    {
        if (target == null) return false;
        if (!selectorImage.gameObject.activeInHierarchy) return false;

        if (!TryGetTargetBounds(target, out Vector2 size, out Vector3 pos))
            return false;

        // Guard against positioning to a degenerate box before layout has built.
        if (size.x <= padding * 2f + 0.01f && size.y <= padding * 2f + 0.01f)
            return false;

        currentTarget = target;
        CancelSelectorTweens();

        if (animate)
        {
            LeanTween.size(selectorImage, size, animationTime)
                .setEase(tweenType)
                .setIgnoreTimeScale(true)
                .setOnComplete(() =>
                {
                    if (selectorImage != null && currentTarget == target &&
                        selectorImage.gameObject.activeInHierarchy)
                    {
                        // Final exact correction in case layout shifted mid-tween.
                        if (TryGetTargetBounds(target, out Vector2 s2, out Vector3 p2))
                        {
                            selectorImage.sizeDelta = s2;
                            selectorImage.localPosition = p2;
                        }
                    }
                });

            LeanTween.moveLocal(selectorImage.gameObject, pos, animationTime)
                .setEase(tweenType)
                .setIgnoreTimeScale(true);
        }
        else
        {
            selectorImage.sizeDelta = size;
            selectorImage.localPosition = pos;
        }

        return true;
    }

    private bool TryGetTargetBounds(RectTransform target, out Vector2 targetSize, out Vector3 targetPos)
    {
        targetSize = Vector2.zero;
        targetPos  = Vector3.zero;
        if (target == null) return false;

        RectTransform parentRect = selectorImage.transform.parent as RectTransform;
        if (parentRect == null) return false;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(target);
        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);

        Vector3[] worldCorners = new Vector3[4];
        target.GetWorldCorners(worldCorners);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < 4; i++)
        {
            Vector2 local = parentRect.InverseTransformPoint(worldCorners[i]);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        targetSize = new Vector2((max.x - min.x) + padding * 2f, (max.y - min.y) + padding * 2f);
        targetPos  = (min + max) / 2f;
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
            Debug.LogError($"{SceneTag} Selector image not assigned.", this);
            enabled = false;
            return false;
        }
        if (EventSystem.current == null)
        {
            Debug.LogError($"{SceneTag} No EventSystem in scene.", this);
            enabled = false;
            return false;
        }
        if (selectorImage.transform.parent == null)
        {
            Debug.LogError($"{SceneTag} Selector image must have a parent.", this);
            enabled = false;
            return false;
        }
        return true;
    }
}