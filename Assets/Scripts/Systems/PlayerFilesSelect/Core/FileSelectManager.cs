using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class FileSelectManager : MonoBehaviour
{
    [Header("References")]
    public FileSelectMarioController mario;
    public SaveSlotManager slotManager;
    public UIInputLock uiInputLock;

    [Header("Cancel")]
    public Transform cancelAnchor;
    public UnityEvent onCancel;

    [Header("Sound Effects")]
    public AudioClip enterSlotSound;

    [Header("Cancel visuals")]
    public float cancelTransformDelay = 0.5f;

    [Header("Tuning")]
    [Tooltip("Small delay to let triggers/anim start before we call into SaveSlotManager.")]
    public float actionStartDelay = 0.05f;

    private bool isBusy;
    private bool isCancelling;
    private bool didInvokeOnCancel;

    private IEnumerator HandleAction(GameObject selected, FileSelectInteractable interactable, Transform anchor)
    {
        isBusy = true;
        uiInputLock?.Lock();

        try
        {
            // 1) Move Mario to that object's anchor (or do nothing if none)
            if (mario != null && anchor != null)
                yield return StartCoroutine(mario.MoveTo(anchor));

            // 2) Optional per-object custom sequence hook (pipe/bounce/parenting/etc)
            var ctx = new FileSelectSequenceContext
            {
                manager = this,
                slotManager = slotManager,
                mario = mario,
                selectedObject = selected,
                interactable = interactable,
                anchor = anchor,
                skipDefaultAction = false
            };

            // If you add a component implementing IFileSelectSequence on the selected object,
            // it will run here BEFORE the default action.
            var seq = selected != null ? selected.GetComponent<IFileSelectSequence>() : null;
            if (seq != null)
                yield return StartCoroutine(seq.Play(ctx));

            // 3) Default action (unless custom sequence chose to do it itself)
            if (!ctx.skipDefaultAction)
                yield return StartCoroutine(DoDefaultAction(interactable));
        }
        finally
        {
            if (uiInputLock != null && uiInputLock.GetLockCount() > 0)
            {
                bool restoreSelection = !ConfirmPopup.IsAnyPopupOpen;
                uiInputLock.Unlock(restoreSelection);
            }

            isBusy = false;
        }
    }

    private IEnumerator DoDefaultAction(FileSelectInteractable interactable)
    {
        if (slotManager == null || interactable == null)
            yield break;

        // Small delay so triggers like Jump/Bomb have a frame to start
        if (actionStartDelay > 0f)
            yield return new WaitForSeconds(actionStartDelay);

        switch (interactable.actionType)
        {
            // IMPORTANT:
            // Focus the slot, then call PlayFocusedSlot()
            // so Delete/Copy/Import/Export modes work automatically through SaveSlotManager.
            case FileSelectActionType.EnterSlot:
            {
                slotManager.FocusSlot(interactable.slotIndex);
                slotManager.PlayFocusedSlot();
                break;
            }

            // Buttons that change the current mode (toggle behavior already exists in SaveSlotManager)
            case FileSelectActionType.DeleteSlot:
            {
                slotManager.EnterDeleteMode();
                break;
            }

            case FileSelectActionType.CopySlot:
            {
                slotManager.EnterCopyMode();
                break;
            }

            case FileSelectActionType.Import:
            {
                slotManager.EnterImportMode();
                break;
            }

            case FileSelectActionType.Export:
            {
                slotManager.EnterExportMode();
                break;
            }
        }

        yield return null;
    }

    private IEnumerator HandleCancel()
    {
        isBusy = true;
        uiInputLock?.Lock();

        try
        {
            Debug.Log("FileSelectManager: HandleCancel");

            // If we are in a non-normal mode, cancel the mode first
            if (slotManager != null && slotManager.CurrentMode != SaveSlotManager.InteractionMode.Normal)
            {
                bool wasDelete = slotManager.CurrentMode == SaveSlotManager.InteractionMode.Delete;

                slotManager.CancelCurrentMode();

                if (mario != null)
                {
                    mario.SetFollowSelection(false);

                    if (wasDelete)
                    {
                        mario.SetTransformIntoObject();
                        if (cancelTransformDelay > 0f)
                            yield return new WaitForSecondsRealtime(cancelTransformDelay);
                    }

                    mario.SetIdle();
                    mario.SetFollowSelection(true);
                }

                yield break;
            }

            // Normal cancel behavior (move to cancel anchor + invoke onCancel)
            if (mario != null && cancelAnchor != null)
            {
                mario.SetFollowSelection(false);
                yield return StartCoroutine(mario.MoveTo(cancelAnchor));
                mario.SetIdle();
                mario.SetFollowSelection(true);
            }

            if (!didInvokeOnCancel)
            {
                didInvokeOnCancel = true;
                onCancel?.Invoke();
            }
        }
        finally
        {
            isCancelling = false;

            // Unlock only if still locked (same reason: sequences may have unlocked early)
            if (uiInputLock != null && TryGetUILockCount(uiInputLock) > 0)
            {
                bool restoreSelection = !ConfirmPopup.IsAnyPopupOpen;
                uiInputLock.Unlock(restoreSelection: restoreSelection);
            }

            isBusy = false;
        }
    }

    #region UI Buttons

    // Attach this to every action UI button onClick event
    public void OnUIButtonClicked(GameObject clicked)
    {
        // If a modal popup is open, do not allow underlying UI clicks to start sequences.
        if (ConfirmPopup.IsAnyPopupOpen) return;

        if (isBusy || clicked == null) return;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(clicked);

        var interactable = clicked.GetComponent<FileSelectInteractable>();
        if (interactable == null) return;

        if (interactable.actionType == FileSelectActionType.EnterSlot)
        {
            AudioManager.Instance?.Play(enterSlotSound, SoundCategory.SFX);
        }

        Transform anchor = clicked.GetComponent<MarioAnchorProvider>()?.marioAnchor;

        StartCoroutine(HandleAction(clicked, interactable, anchor));
    }

    public void CancelFromUIButton()
    {
        if (isBusy || isCancelling) return;

        isCancelling = true;
        StartCoroutine(HandleCancel());
    }

    #endregion

    /// <summary>
    /// Tries to read UIInputLock's current lock count without requiring a public API.
    /// Supports:
    /// - public/internal property: LockCount
    /// - public/internal method: GetLockCount()
    /// - private field: lockCount / _lockCount
    /// If nothing is found, returns 1 (conservative: assume locked).
    /// </summary>
    private static int TryGetUILockCount(UIInputLock locker)
    {
        if (locker == null) return 0;

        try
        {
            var t = locker.GetType();

            // Property: LockCount
            var prop = t.GetProperty("LockCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.PropertyType == typeof(int))
            {
                object val = prop.GetValue(locker, null);
                if (val is int i) return i;
            }

            // Method: GetLockCount()
            var method = t.GetMethod("GetLockCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null && method.ReturnType == typeof(int) && method.GetParameters().Length == 0)
            {
                object val = method.Invoke(locker, null);
                if (val is int i) return i;
            }

            // Field: lockCount
            var field = t.GetField("lockCount", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(int))
            {
                object val = field.GetValue(locker);
                if (val is int i) return i;
            }

            // Field: _lockCount
            field = t.GetField("_lockCount", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(int))
            {
                object val = field.GetValue(locker);
                if (val is int i) return i;
            }
        }
        catch
        {
            // Ignore and fall back below
        }

        // Conservative fallback: if we can't read it, assume it's still locked
        return 1;
    }
}