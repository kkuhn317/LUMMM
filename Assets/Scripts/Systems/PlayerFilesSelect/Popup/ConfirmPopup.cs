using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Xml.Serialization;
using UnityEngine.InputSystem;

public class ConfirmPopup : MonoBehaviour
{
    // Global pointer so FileSelectManager can detect "a modal is open"
    public static ConfirmPopup ActivePopup { get; private set; }
    public static bool IsAnyPopupOpen => ActivePopup != null && ActivePopup.IsOpen;

    public bool IsOpen => isOpen;

    [Header("UI")]
    public GameObject root; // entire popup root
    public TMP_Text messageText;
    public Button yesButton;
    public Button noButton;

    [Tooltip("Optional. If null, we auto-find TMP_Text inside Yes/No buttons.")]
    public TMP_Text yesLabel;
    [Tooltip("Optional. If null, we auto-find TMP_Text inside Yes/No buttons.")]
    public TMP_Text noLabel;

    [Header("Sound Effects")]
    public AudioClip pressSound;

    [Header("Animator")]
    public Animator animator;
    public string appearTrigger = "appear";
    public string disappearTrigger = "disappear";
    public int animatorLayer = 0;

    [Header("Animation Timing")]
    public float disappearAnimationDuration = 0.3f;

    [Header("Selection")]
    public bool defaultSelectYes = true;

    [Tooltip("How long we will wait (real time) for the desired button to become active + interactable before using fallback.")]
    public float selectWaitTimeout = 0.75f;

    [Header("Navigation Lock")]
    [Tooltip("If empty, uses the parent Canvas automatically. While open, disables other Selectables so navigation stays inside popup.")]
    public Transform[] lockScopes;

    [Tooltip("If true, Cancel acts like No.")]
    public bool cancelActsAsNo = true;

    [Header("Events")]
    public UnityEvent onPopupOpened;
    public UnityEvent onPopupClosed;

    [Header("Cancel Input")]
    [Tooltip("When popup is open, this action will trigger the No button")]
    public InputActionReference cancelAction;

    private Action onYes;
    private Action onNo;

    private bool isOpen;
    private Coroutine closeRoutine;
    private Coroutine selectRoutine;

    // Navigation lock state
    private GameObject prevSelected;

    private struct SelectableSnapshot
    {
        public Selectable selectable;
        public bool interactable;
    }

    private readonly List<SelectableSnapshot> locked = new List<SelectableSnapshot>();

    // Localization binding state
    private LocalizedString boundMessage;
    private LocalizedString boundYes;
    private LocalizedString boundNo;

    private LocalizedString.ChangeHandler msgHandler;
    private LocalizedString.ChangeHandler yesHandler;
    private LocalizedString.ChangeHandler noHandler;

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);

        if (yesButton != null) yesButton.onClick.AddListener(ClickYes);
        if (noButton != null) noButton.onClick.AddListener(ClickNo);

        // Auto-find labels if not assigned
        if (yesLabel == null && yesButton != null) yesLabel = yesButton.GetComponentInChildren<TMP_Text>(true);
        if (noLabel == null && noButton != null) noLabel = noButton.GetComponentInChildren<TMP_Text>(true);
    }

    private void OnEnable()
    {
        if (cancelAction != null && cancelAction.action != null)
        {
            cancelAction.action.performed += OnCancelPerformed;
            if (!cancelAction.action.enabled)
                cancelAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        // Make sure we never leave subscriptions or locks around
        UnbindLocalization();
        UnlockOutsideSelectables();

        if (cancelAction != null && cancelAction.action != null)
        {
            cancelAction.action.performed -= OnCancelPerformed;
        }
        
        if (ActivePopup == this)
            ActivePopup = null;

        if (selectRoutine != null)
        {
            StopCoroutine(selectRoutine);
            selectRoutine = null;
        }

        isOpen = false;
    }

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        // Only respond if this popup is active and No button exists
        if (gameObject.activeInHierarchy && noButton != null && noButton.interactable)
        {
            Debug.Log($"ConfirmPopup: Cancel action triggered on {gameObject.name}, clicking No");
            noButton.onClick.Invoke();
        }
    }


    public void Show(string message, Action yes, Action no, bool? selectYes = null, string yesText = "Yes", string noText = "No")
    {
        UnbindLocalization();

        onYes = yes;
        onNo = no;

        if (messageText != null) messageText.text = message ?? "";
        if (yesLabel != null) yesLabel.text = string.IsNullOrEmpty(yesText) ? "Yes" : yesText;
        if (noLabel != null) noLabel.text = string.IsNullOrEmpty(noText) ? "No" : noText;

        Open(selectYes);
    }

    public void Show(LocalizedString message, Action yes, Action no, bool? selectYes = null, LocalizedString yesText = null, LocalizedString noText = null)
    {
        UnbindLocalization();

        onYes = yes;
        onNo = no;

        BindLocalization(message, yesText, noText);

        // Apply immediately once (do not wait for StringChanged event)
        if (boundMessage != null && messageText != null) messageText.text = boundMessage.GetLocalizedString();
        if (boundYes != null && yesLabel != null) yesLabel.text = boundYes.GetLocalizedString();
        if (boundNo != null && noLabel != null) noLabel.text = boundNo.GetLocalizedString();

        Open(selectYes);
    }

    public void Open(bool? focusYes = null)
    {
        // If something is already open, close/ignore to avoid double-state issues
        if (isOpen)
            return;

        // STOP ANY EXISTING SELECTION ROUTINE BEFORE PROCEEDING
        if (selectRoutine != null)
        {
            StopCoroutine(selectRoutine);
            selectRoutine = null;
        }

        onPopupOpened?.Invoke();

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);

            // Reset triggers
            animator.ResetTrigger(appearTrigger);
            animator.ResetTrigger(disappearTrigger);
        }

        // Cache what was selected BEFORE the popup takes over
        var current = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (current != null)
            prevSelected = current;

        // Register global pointer so other systems know a modal is open
        ActivePopup = this;

        // Ensure popup is active BEFORE we attempt any selection
        if (root != null)
            root.SetActive(true);

        isOpen = true;

        // Decide which button should be focused first
        bool pickYes = focusYes ?? false;
        Button desired = pickYes ? yesButton : noButton;
        Button fallback = noButton;

        Debug.Log($"[POPUP] OPEN frame={Time.frameCount} t={Time.time:F3} " +
                $"prevSel={(prevSelected != null ? prevSelected.name : "NULL")} " +
                $"desired={(desired != null ? desired.name : "NULL")} " +
                $"fallback={(fallback != null ? fallback.name : "NULL")} " +
                $"rootActive={(root != null && root.activeInHierarchy)}");

        // Lock outside UI
        LockOutsideSelectables();

        // Play appear animation
        if (animator != null && !string.IsNullOrEmpty(appearTrigger))
        {
            animator.ResetTrigger(disappearTrigger);
            animator.SetTrigger(appearTrigger);
        }

        // START the animation-aware selection logic
        selectRoutine = StartCoroutine(WaitForAppearAnimation(desired, fallback));
    }

    private IEnumerator WaitForAppearAnimation(Button desired, Button fallback)
    {
        // If no animator, do immediate selection
        if (animator == null)
        {
            yield return null;
            selectRoutine = StartCoroutine(ForceSelectWhenReady(desired, fallback));
            yield break;
        }

        // Wait for animation to start (any state)
        yield return new WaitForEndOfFrame();
        yield return null;
        
        // Get current state
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(animatorLayer);
        float animationStartTime = Time.unscaledTime;
        
        // Wait for ANY animation to complete (not just "Popup_Appear")
        // Or wait for a maximum of 1 second
        while ((animator.IsInTransition(animatorLayer) || stateInfo.normalizedTime < 1f) && 
            (Time.unscaledTime - animationStartTime) < 1f)
        {
            yield return null;
            stateInfo = animator.GetCurrentAnimatorStateInfo(animatorLayer);
        }

        // One extra frame for UI to settle
        yield return null;

        // Now safely select buttons
        selectRoutine = StartCoroutine(ForceSelectWhenReady(desired, fallback));
    }

    public void Hide()
    {
        if (!isOpen) return;

        isOpen = false;

        if (selectRoutine != null)
        {
            StopCoroutine(selectRoutine);
            selectRoutine = null;
        }

        if (closeRoutine != null)
            StopCoroutine(closeRoutine);

        PlayTrigger(disappearTrigger);
        closeRoutine = StartCoroutine(CloseAfterDisappear());
    }

    public void PlayUISfx(AudioClip clip)
    {
        AudioManager.Instance?.Play(clip, SoundCategory.SFX);
    }

    private void ClickYes()
    {
        if (!isOpen) return;

        if (selectRoutine != null)
        {
            StopCoroutine(selectRoutine);
            selectRoutine = null;
        }

        if (yesButton != null)
        {
            Animator yesAnimator = yesButton.GetComponent<Animator>();
            if (yesAnimator != null)
            {
                Debug.Log($"[POPUP] Forcing yes button to Normal state");
                yesAnimator.Play("Normal", 0, 0f);
                yesAnimator.Update(0f);
            }
        }

        AudioManager.Instance?.Play(pressSound, SoundCategory.SFX);

        var cb = onYes;
        ClearCallbacks();

        Hide();
        cb?.Invoke();
    }

    private void ClickNo()
    {
        if (!isOpen) return;

        if (selectRoutine != null)
        {
            StopCoroutine(selectRoutine);
            selectRoutine = null;
        }

        if (noButton != null)
        {
            Animator noAnimator = noButton.GetComponent<Animator>();
            if (noAnimator != null)
            {
                Debug.Log($"[POPUP] Forcing no button to Normal state");
                noAnimator.Play("Normal", 0, 0f);
                noAnimator.Update(0f);
            }
        }

        AudioManager.Instance?.Play(pressSound, SoundCategory.SFX);

        var cb = onNo;
        ClearCallbacks();

        Hide();
        cb?.Invoke();
    }

    private void ClearCallbacks()
    {
        onYes = null;
        onNo = null;
    }

    private void BindLocalization(LocalizedString message, LocalizedString yesText, LocalizedString noText)
    {
        boundMessage = message;
        boundYes = yesText;
        boundNo = noText;

        msgHandler = (s) => { if (messageText != null) messageText.text = s ?? ""; };
        yesHandler = (s) => { if (yesLabel != null) yesLabel.text = s ?? ""; };
        noHandler  = (s) => { if (noLabel != null)  noLabel.text  = s ?? ""; };

        if (boundMessage != null) boundMessage.StringChanged += msgHandler;
        if (boundYes != null) boundYes.StringChanged += yesHandler;
        if (boundNo != null) boundNo.StringChanged += noHandler;
    }

    private void UnbindLocalization()
    {
        if (boundMessage != null && msgHandler != null) boundMessage.StringChanged -= msgHandler;
        if (boundYes != null && yesHandler != null) boundYes.StringChanged -= yesHandler;
        if (boundNo != null && noHandler != null) boundNo.StringChanged -= noHandler;

        boundMessage = null;
        boundYes = null;
        boundNo = null;

        msgHandler = null;
        yesHandler = null;
        noHandler = null;
    }

    private void PlayTrigger(string trigger)
    {
        if (animator == null || string.IsNullOrEmpty(trigger)) return;

        animator.ResetTrigger(appearTrigger);
        animator.ResetTrigger(disappearTrigger);
        animator.SetTrigger(trigger);
    }

    private IEnumerator CloseAfterDisappear()
    {
        // Wait for the disappear animation to play
        if (animator != null && !string.IsNullOrEmpty(disappearTrigger))
        {
            // Wait for animation to start
            yield return null;
            
            // Get the current state
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(animatorLayer);
            float animationStartTime = Time.unscaledTime;
            
            // Wait for animation to complete or timeout after 1 second
            while (stateInfo.normalizedTime < 1f && (Time.unscaledTime - animationStartTime) < 1f)
            {
                yield return null;
                stateInfo = animator.GetCurrentAnimatorStateInfo(animatorLayer);
            }
        }
        else
        {
            // No animator, use the configured duration
            yield return new WaitForSecondsRealtime(disappearAnimationDuration);
        }

        if (root != null)
            root.SetActive(false);

        UnlockOutsideSelectables();

        // Always restore prevSelected unless the current selection is outside the popup
        if (EventSystem.current != null && prevSelected != null)
        {
            var currentSelection = EventSystem.current.currentSelectedGameObject;
            bool currentIsPopupButton = currentSelection != null &&
                                       (currentSelection == yesButton?.gameObject ||
                                        currentSelection == noButton?.gameObject);

            if (currentSelection == null || currentIsPopupButton)
            {
                EventSystem.current.SetSelectedGameObject(prevSelected);
            }
        }

        UnbindLocalization();

        onPopupClosed?.Invoke();

        if (ActivePopup == this)
            ActivePopup = null;

        closeRoutine = null;

        Debug.Log($"[POPUP] CLOSE frame={Time.frameCount} t={Time.unscaledTime:0.000} " +
          $"currentSel={(EventSystem.current?.currentSelectedGameObject ? EventSystem.current.currentSelectedGameObject.name : "NULL")} " +
          $"restorePrev={(prevSelected ? prevSelected.name : "NULL")}");
    }

    private IEnumerator ForceSelectWhenReady(Button desired, Button fallback)
    {
        if (EventSystem.current == null) yield break;

        Debug.Log($"[POPUP] ForceSelectWhenReady START - desired: {desired?.name}, fallback: {fallback?.name}");

        float start = Time.unscaledTime;

        // Wait until root is active AND the appear animation has completed
        while (isOpen && root != null && !root.activeInHierarchy)
        {
            // EARLY EXIT: if popup closes while waiting, stop
            if (!isOpen) yield break;
            yield return null;
        }
        
        if (!isOpen) yield break;

        // NEW: Also wait for animation to complete if we have an animator
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(animatorLayer);
            Debug.Log($"[POPUP] Waiting for animation... state: {stateInfo.fullPathHash}, normalizedTime: {stateInfo.normalizedTime}");
            
            // Wait for animation to start (state named "Popup_Appear" or similar)
            while (isOpen && !stateInfo.IsName("Popup_Appear"))
            {
                yield return null;
                stateInfo = animator.GetCurrentAnimatorStateInfo(animatorLayer);
            }
            
            // Wait for animation to complete
            while (isOpen && (animator.IsInTransition(animatorLayer) || stateInfo.normalizedTime < 1f))
            {
                yield return null;
                stateInfo = animator.GetCurrentAnimatorStateInfo(animatorLayer);
            }
        }

        // One extra frame for UI to settle after animation
        yield return null;

        // 1) Prefer selecting the desired button
        if (isOpen && desired != null && IsSelectableReady(desired))
        {
            Debug.Log($"[POPUP] SELECT desired={desired.name} frame={Time.frameCount} t={Time.unscaledTime:0.000}");
            EventSystem.current.SetSelectedGameObject(desired.gameObject);
            
            // Reassert if selection was lost
            Debug.Log($"[POPUP] After selection - EventSystem.currentSelectedGameObject: {EventSystem.current?.currentSelectedGameObject?.name}");
            yield return null;

            if (isOpen && EventSystem.current != null && 
                EventSystem.current.currentSelectedGameObject != desired.gameObject)
            {   
                Debug.Log($"[POPUP] Selection was lost! Restoring to desired button");
                EventSystem.current.SetSelectedGameObject(desired.gameObject);
            }
            
            selectRoutine = null;
            Debug.Log($"[POPUP] ForceSelectWhenReady END");
            yield break;
        }

        // 2) Fallback
        if (isOpen && fallback != null && IsSelectableReady(fallback))
        {
            EventSystem.current.SetSelectedGameObject(fallback.gameObject);
        }

        selectRoutine = null;
    }

    private static bool IsSelectableReady(Button b)
    {
        if (b == null) return false;
        if (!b.gameObject.activeInHierarchy) return false;
        if (!b.IsInteractable()) return false;

        return true;
    }

    private void LockOutsideSelectables()
    {
        locked.Clear();

        Transform[] scopes = lockScopes;

        if (scopes == null || scopes.Length == 0)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) scopes = new[] { canvas.transform };
            else scopes = new[] { transform.root };
        }

        Transform popupRoot = (root != null) ? root.transform : transform;

        for (int s = 0; s < scopes.Length; s++)
        {
            var scope = scopes[s];
            if (scope == null) continue;

            var selectables = scope.GetComponentsInChildren<Selectable>(includeInactive: true);
            for (int i = 0; i < selectables.Length; i++)
            {
                var sel = selectables[i];
                if (sel == null) continue;

                // Keep popup's own selectables
                if (IsUnder(sel.transform, popupRoot))
                    continue;

                // Skip ones already not interactable
                if (!sel.interactable)
                    continue;

                locked.Add(new SelectableSnapshot { selectable = sel, interactable = sel.interactable });
                sel.interactable = false;
            }
        }
    }

    private void UnlockOutsideSelectables()
    {
        for (int i = 0; i < locked.Count; i++)
        {
            var snap = locked[i];
            if (snap.selectable != null)
                snap.selectable.interactable = snap.interactable;
        }

        locked.Clear();
    }

    private static bool IsUnder(Transform t, Transform rootT)
    {
        if (t == null || rootT == null) return false;

        var cur = t;
        while (cur != null)
        {
            if (cur == rootT) return true;
            cur = cur.parent;
        }
        return false;
    }
}