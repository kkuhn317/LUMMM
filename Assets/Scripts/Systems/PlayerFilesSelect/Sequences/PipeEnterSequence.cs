using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;

public class PipeEnterSequence : MonoBehaviour, IFileSelectSequence
{
    [Header("References")]
    public Animator pipeAnimator;
    public Transform pipeContainer;
    public UIShake uiShake;
    public GameObject explosionObject;

    [SerializeField] private RectTransform uiRootToShake;

    [Header("Optional extra input lock (FileSelectManager already locks during HandleAction)")]
    public UIInputLock uiInputLock;

    [Header("Sound Effects")]
    public AudioClip pipeActionSound;

    [Header("Pipe triggers")]
    public string enterTrigger = "EnterPipe";
    public string exitTrigger = "ExitPipe";

    [Header("Wait settings")]
    public string enterStateName = "PipeEnter";
    public string exitStateName = "PipeExit";
    public float fallbackAnimWait = 0.35f;

    [Header("Delete mode extras")]
    public float explosionDelayAfterEnter = 0.05f;
    public float shakeDuration = 0.25f;
    public float shakeStrength = 10f;

    [Header("Confirm Popup")]
    public ConfirmPopup confirmPopup;
    public LocalizedString deleteConfirmMsg;
    public LocalizedString yesBtn;
    public LocalizedString noBtn;

    void Start()
    {
        if (uiInputLock == null)
        {
            uiInputLock = FindObjectOfType<UIInputLock>();
            Debug.Log($"PipeEnterSequence: Auto-assigned UIInputLock: {uiInputLock != null}");
        }
    }

    public IEnumerator Play(FileSelectSequenceContext ctx)
    {
        if (ctx == null || ctx.mario == null || ctx.slotManager == null || ctx.interactable == null)
            yield break;

        // Only for slots
        if (ctx.interactable.actionType != FileSelectActionType.EnterSlot)
        {
            ctx.skipDefaultAction = false;
            yield break;
        }

        // Modes where pipe animation should NOT run
        var mode = ctx.slotManager.CurrentMode;
        bool isNonPipeSlotSelect =
            mode == SaveSlotManager.InteractionMode.CopySelectSource ||
            mode == SaveSlotManager.InteractionMode.CopySelectDestination ||
            mode == SaveSlotManager.InteractionMode.ImportSelectDestination ||
            mode == SaveSlotManager.InteractionMode.ExportSelectSource;

        if (isNonPipeSlotSelect)
        {
            ctx.skipDefaultAction = false;
            yield break;
        }

        // From here: Normal and Delete modes only
        bool isDelete = mode == SaveSlotManager.InteractionMode.Delete;

        if (!isDelete)
        {
            // NORMAL MODE pipe entry
            if (pipeContainer != null)
                ctx.mario.AttachTo(pipeContainer, true);

            ctx.mario.SetCelebrate(true);

            yield return new WaitForSecondsRealtime(1.35f);

            Trigger(pipeAnimator, enterTrigger);
            PlayPipeSfx();

            ctx.skipDefaultAction = false;
            yield return null;
            yield break;
        }

        // Delete mode begins
        ctx.skipDefaultAction = true;

        // Freeze Mario follow
        ctx.mario.SetFollowSelection(false);
        
        // Ensure input is enabled for popup
        if (uiInputLock != null && uiInputLock.GetLockCount() > 0)
        {
            uiInputLock.ForceUnlockAll(false);
            yield return null; // Wait for input system to update
        }

        // Save current selection
        var prevSelected = EventSystem.current?.currentSelectedGameObject;
        Debug.Log($"PipeEnterSequence: Saving selection before popup: {prevSelected?.name}");

        // We MUST unlock input AFTER FileSelectManager locked it,
        // which happens THIS FRAME.
        // So we delay unlock until next frame.

        // Check if slot has a file before showing delete confirmation
        int slotIndex = ctx.interactable.slotIndex;

        // If slot is EMPTY, just cancel delete mode immediately (no popup needed)
        if (!SaveManager.SlotExists(slotIndex))
        {
            Debug.Log("PipeEnterSequence: Cannot delete an empty slot. Cancelling delete mode.");
            
            // Optionally show a different message or just cancel silently
            ctx.slotManager.CancelCurrentMode();
            
            // Reset Mario
            ctx.mario.SetTransformIntoObject();
            yield return new WaitForSecondsRealtime(0.5f);
            ctx.mario.SetIdle();
            ctx.mario.SetFollowSelection(true);
            
            // Restore selection
            if (EventSystem.current != null && prevSelected != null)
                EventSystem.current.SetSelectedGameObject(prevSelected);
            
            yield break;
        }
    
        if (confirmPopup != null)
        {
            bool? decision = null;

            // Open the popup with working input
            confirmPopup.Show(
                deleteConfirmMsg,
                yes: () => decision = true,
                no: () => decision = false,
                selectYes: false,
                yesText: yesBtn,
                noText: noBtn
            );

            // Wait for user to choose Yes or No
            while (decision == null)
                yield return null;
            
            // When No is pressed
            if (decision == false)
            {
                ctx.slotManager.CancelCurrentMode(); 
                ctx.mario.SetTransformIntoObject();
                yield return new WaitForSecondsRealtime(0.5f);
                ctx.mario.SetIdle();
                ctx.mario.SetFollowSelection(true);

                if (EventSystem.current != null && prevSelected != null)
                    EventSystem.current.SetSelectedGameObject(prevSelected);

                yield break;
            }

            // when yes is pressed, re-lock input for the cinematic
            uiInputLock?.Lock();
        }

        // Delete cinematic begins
        ctx.mario.SetFollowSelection(false);

        if (pipeContainer != null)
            ctx.mario.AttachTo(pipeContainer, true);

        Trigger(pipeAnimator, enterTrigger);
        PlayPipeSfx();

        yield return WaitForAnim(pipeAnimator, enterStateName, fallbackAnimWait);

        if (explosionDelayAfterEnter > 0f)
            yield return new WaitForSecondsRealtime(explosionDelayAfterEnter);

        if (explosionObject != null)
            explosionObject.SetActive(true);

        ctx.mario.PlayExplosionSound();

        if (uiShake != null)
            yield return StartCoroutine(uiShake.ShakeRect(uiRootToShake, shakeDuration, shakeStrength));

        ctx.slotManager.DeleteFocusedSlot();
        ctx.slotManager.CancelCurrentMode();

        ctx.mario.SetIdle();

        Trigger(pipeAnimator, exitTrigger);
        PlayPipeSfx();

        yield return WaitForAnim(pipeAnimator, exitStateName, fallbackAnimWait);

        if (explosionObject != null)
            explosionObject.SetActive(false);

        ctx.mario.Detach(true);
        ctx.mario.SetFollowSelection(true);

        if (EventSystem.current != null && prevSelected != null)
            EventSystem.current.SetSelectedGameObject(prevSelected);

        uiInputLock?.Unlock();
    }

    private void Trigger(Animator anim, string trigger)
    {
        if (anim == null || string.IsNullOrEmpty(trigger)) return;
        anim.SetTrigger(trigger);
    }

    private void PlayPipeSfx()
    {
        if (pipeActionSound == null) return;
        AudioManager.Instance?.Play(pipeActionSound, SoundCategory.SFX);
    }

    private IEnumerator WaitForAnim(Animator anim, string stateName, float fallbackSeconds)
    {
        if (anim == null)
        {
            if (fallbackSeconds > 0f)
                yield return new WaitForSecondsRealtime(fallbackSeconds);
            yield break;
        }

        if (string.IsNullOrEmpty(stateName))
        {
            if (fallbackSeconds > 0f)
                yield return new WaitForSecondsRealtime(fallbackSeconds);
            yield break;
        }

        int layer = 0;

        // allow trigger to activate animator state
        yield return null;

        float safety = 1.5f;
        float t = 0f;

        // Wait until correct state is entered
        while (!anim.GetCurrentAnimatorStateInfo(layer).IsName(stateName))
        {
            t += Time.unscaledDeltaTime;
            if (t >= safety)
            {
                if (fallbackSeconds > 0f)
                    yield return new WaitForSecondsRealtime(fallbackSeconds);
                yield break;
            }
            yield return null;
        }

        // Wait until animation finishes
        while (anim.GetCurrentAnimatorStateInfo(layer).normalizedTime < 1f)
            yield return null;
    }
}