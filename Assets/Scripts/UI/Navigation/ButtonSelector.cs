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

    [Header("Selection Following")]
    [Tooltip("When false, navigation still changes the EventSystem selection, but the selector stops following it: it finishes any in-flight glide onto its current target, then holds there and ignores further selection changes. Toggle at runtime via SetFollowSelection().")]
    [SerializeField] private bool followSelection = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = false;

    // What the selector is CURRENTLY, successfully positioned on. This only
    // advances when placement actually FINISHED (snap, or animation reached t=1).
    private GameObject positionedOn;
    private bool needsReposition;

    // Manual animation state. We drive the move ourselves on unscaled time instead
    // of handing off to an external tween, so positioning can never silently no-op
    // (and "done" is recorded on arrival, not when an animation is merely scheduled).
    private bool animating;
    private GameObject animatingTo;
    private float animStartTime;
    private Vector2 animStartSize;
    private Vector3 animStartPos;

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
        animating = false;
        animatingTo = null;
        ignoreNextSelectionChangeSfx = true;

        CacheCanvasCamera();
        StartCoroutine(EnsureSelection());
    }

    private void OnDisable()
    {
        animating = false;
        animatingTo = null;
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
    /// Positioning is owned by Update, which retries until the selector is laid out.
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

        // --- 1) SFX / event: fire on a genuine selection change. ---
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

        // --- 2) Positioning ---
        // When following, the selector tracks the live selection. When NOT following,
        // it tracks a HELD target: whatever it was last heading for (animatingTo) or
        // resting on (positionedOn). So an in-flight glide still finishes onto its
        // objective, then holds — and subsequent selection changes (which still fire
        // above and still move the EventSystem selection) no longer move the selector.
        GameObject trackSel = followSelection ? sel : (animating ? animatingTo : positionedOn);

        // Idle: resting on the tracked target, nothing animating, nothing dirty.
        if (!needsReposition && !animating && positionedOn == trackSel)
            return;

        if (trackSel == null)
        {
            currentTargetCleanup();
            positionedOn = null;
            needsReposition = false;
            return;
        }

        // Selector hidden by an intro/transition: stay dirty, retry next frame.
        if (!selectorImage.gameObject.activeInHierarchy)
            return;

        RectTransform rect = trackSel.GetComponent<RectTransform>();
        if (rect == null)
        {
            currentTargetCleanup();
            positionedOn = trackSel;   // nothing we can do for this target; stop retrying it
            needsReposition = false;
            return;
        }

        // Need valid bounds before placing/animating. If not ready, retry next frame.
        if (!TryGetTargetBounds(rect, out Vector2 targetSize, out Vector3 targetPos))
            return;

        // Guard against a degenerate box before layout has built.
        if (targetSize.x <= padding * 2f + 0.01f && targetSize.y <= padding * 2f + 0.01f)
            return;

        // First acquisition (or recovery from a null selection): snap, no animation.
        if (positionedOn == null && !animating)
        {
            selectorImage.sizeDelta = targetSize;
            selectorImage.localPosition = targetPos;
            positionedOn = trackSel;
            needsReposition = false;
            Log($"Positioned selector on '{trackSel.name}' (animate=False).");
            return;
        }

        // If where we're headed (or resting) isn't the tracked target, (re)start
        // an animation from wherever the selector physically is RIGHT NOW. This also
        // redirects cleanly if the player moves again mid-animation.
        GameObject currentDest = animating ? animatingTo : positionedOn;
        if (currentDest != trackSel)
        {
            animating = true;
            animatingTo = trackSel;
            animStartTime = Time.unscaledTime;
            animStartSize = selectorImage.sizeDelta;
            animStartPos = selectorImage.localPosition;
            needsReposition = false;
        }

        // Drive the manual animation toward the (re-evaluated each frame) target,
        // so layout shifts during the move are tracked. Commit ONLY on arrival.
        if (animating && animatingTo == trackSel)
        {
            float dur = Mathf.Max(0.0001f, animationTime);
            float t = Mathf.Clamp01((Time.unscaledTime - animStartTime) / dur);
            float e = ApplyEase(t);

            selectorImage.sizeDelta = Vector2.LerpUnclamped(animStartSize, targetSize, e);
            selectorImage.localPosition = Vector3.LerpUnclamped(animStartPos, targetPos, e);

            if (t >= 1f)
            {
                selectorImage.sizeDelta = targetSize;
                selectorImage.localPosition = targetPos;
                animating = false;
                positionedOn = trackSel;
                needsReposition = false;
                Log($"Positioned selector on '{trackSel.name}' (animate=True).");
            }
        }
    }

    /// <summary>
    /// Toggle whether the selector follows the current selection. When set to false,
    /// navigation continues to change the selection normally, but the selector finishes
    /// its current glide and then holds — it stops chasing further selection changes.
    /// Set back to true to resume following (it animates to catch up to the selection).
    /// Wire this to a Button's OnClick (it accepts a static bool argument).
    /// </summary>
    public void SetFollowSelection(bool follow)
    {
        if (followSelection == follow) return;
        followSelection = follow;
        // Resuming: mark dirty so Update animates toward the live selection again.
        if (follow) needsReposition = true;
    }

    private void currentTargetCleanup()
    {
        animating = false;
        animatingTo = null;
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
    /// Computes the selector size/position (in the selector parent's local space) that
    /// frames the given target, padded. Returns false only if references are missing.
    /// </summary>
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

    private float ApplyEase(float t)
    {
        switch (tweenType)
        {
            case LeanTweenType.linear:        return t;
            case LeanTweenType.easeInQuad:    return t * t;
            case LeanTweenType.easeOutQuad:   return 1f - (1f - t) * (1f - t);
            case LeanTweenType.easeInOutQuad: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
            case LeanTweenType.easeInCubic:   return t * t * t;
            case LeanTweenType.easeOutCubic:  return 1f - Mathf.Pow(1f - t, 3f);
            default:                          return 1f - (1f - t) * (1f - t); // easeOutQuad
        }
    }

    private void CancelSelectorTweens()
    {
        if (selectorImage == null) return;
        LeanTween.cancel(selectorImage.gameObject); // harmless safety if anything else tweened it
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