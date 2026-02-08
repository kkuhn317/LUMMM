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

    [Header("Name/Rename Popup (SaveSlotRename)")]
    public SaveSlotRename renamePopup;

    void Start()
    {
        if (uiInputLock == null)
        {
            uiInputLock = FindObjectOfType<UIInputLock>();
            Debug.Log($"PipeEnterSequence: Auto-assigned UIInputLock: {uiInputLock != null}");
        }

        if (renamePopup == null)
        {
            renamePopup = FindObjectOfType<SaveSlotRename>(true);
            Debug.Log($"PipeEnterSequence: Auto-assigned SaveSlotRename: {renamePopup != null}");
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

        var mode = ctx.slotManager.CurrentMode;

        // Modes where pipe animation should NOT run
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

        int slotIndex = ctx.interactable.slotIndex;

        bool isRenameMode = mode == SaveSlotManager.InteractionMode.RenameSelectTarget;
        bool isDelete = mode == SaveSlotManager.InteractionMode.Delete;
        
        // RENAME MODE:
        // Press slot -> open popup ONLY
        // Confirm -> rename ONLY (no celebrate, no pipe, no fade)
        // Cancel -> return to normal
        if (isRenameMode)
        {
            ctx.skipDefaultAction = true; // NEVER enter slot / never fade

            ctx.mario.SetFollowSelection(false);

            // Ensure input is enabled for popup
            if (uiInputLock != null && uiInputLock.GetLockCount() > 0)
            {
                uiInputLock.ForceUnlockAll(false);
                yield return null;
            }

            var prevSelected = EventSystem.current?.currentSelectedGameObject;

            // Don't rename empty slots
            if (!SaveManager.SlotExists(slotIndex))
            {
                Debug.Log("PipeEnterSequence: Cannot rename an empty slot. Returning to normal.");

                ctx.slotManager.CancelCurrentMode();
                ctx.mario.SetIdle();
                ctx.mario.SetFollowSelection(true);

                if (EventSystem.current != null && prevSelected != null)
                    EventSystem.current.SetSelectedGameObject(prevSelected);

                yield break;
            }

            if (renamePopup == null)
            {
                Debug.LogWarning("PipeEnterSequence: renamePopup is missing. Cancelling rename mode.");
                ctx.slotManager.CancelCurrentMode();
                ctx.mario.SetIdle();
                ctx.mario.SetFollowSelection(true);
                yield break;
            }

            bool? confirmed = null;
            string chosenName = null;

            void OnConfirmed(int i, string name)
            {
                if (i != slotIndex) return;
                chosenName = name;
                confirmed = true;
            }

            void OnCancelled()
            {
                confirmed = false;
            }

            renamePopup.onNameConfirmed.AddListener(OnConfirmed);
            renamePopup.onRenameCancelled.AddListener(OnCancelled);

            // Open popup (NO PIPE)
            renamePopup.OpenForRename(slotIndex, GetSlotProfileName(slotIndex));

            while (confirmed == null)
                yield return null;

            renamePopup.onNameConfirmed.RemoveListener(OnConfirmed);
            renamePopup.onRenameCancelled.RemoveListener(OnCancelled);

            // Cancel -> just exit rename mode
            if (confirmed == false)
            {
                ctx.slotManager.CancelCurrentMode();

                ctx.mario.SetIdle();
                ctx.mario.SetFollowSelection(true);

                if (EventSystem.current != null && prevSelected != null)
                    EventSystem.current.SetSelectedGameObject(prevSelected);

                yield break;
            }

            // Confirm -> rename ONLY, then return to normal (NO CELEBRATE, NO PIPE)
            RenameSlotNameImmediately(slotIndex, chosenName);
            ctx.slotManager.RefreshAllSlots();
            ctx.slotManager.CancelCurrentMode();

            ctx.mario.SetIdle();
            ctx.mario.SetFollowSelection(true);

            if (EventSystem.current != null && prevSelected != null)
                EventSystem.current.SetSelectedGameObject(prevSelected);

            yield break;
        }

        // ============================================================
        // NORMAL MODE:
        // If empty slot -> ask name first.
        // Confirm -> create name, then play pipe, THEN allow default (fade)
        // ============================================================
        if (!isDelete)
        {
            bool isNew = !SaveManager.SlotExists(slotIndex);

            // If new slot, name first (and do NOT allow default action yet)
            if (isNew && renamePopup != null)
            {
                ctx.skipDefaultAction = true;

                ctx.mario.SetFollowSelection(false);

                // Ensure input is enabled for popup
                if (uiInputLock != null && uiInputLock.GetLockCount() > 0)
                {
                    uiInputLock.ForceUnlockAll(false);
                    yield return null;
                }

                var prevSelected = EventSystem.current?.currentSelectedGameObject;

                bool? confirmed = null;
                string chosenName = null;

                void OnConfirmed(int i, string name)
                {
                    if (i != slotIndex) return;
                    chosenName = name;
                    confirmed = true;
                }

                void OnCancelled()
                {
                    confirmed = false;
                }

                renamePopup.onNameConfirmed.AddListener(OnConfirmed);
                renamePopup.onRenameCancelled.AddListener(OnCancelled);

                renamePopup.OpenForNewSlot(slotIndex);

                while (confirmed == null)
                    yield return null;

                renamePopup.onNameConfirmed.RemoveListener(OnConfirmed);
                renamePopup.onRenameCancelled.RemoveListener(OnCancelled);

                // Cancel -> no pipe, no enter
                if (confirmed == false)
                {
                    ctx.skipDefaultAction = true;

                    ctx.mario.SetIdle();
                    ctx.mario.SetFollowSelection(true);

                    if (EventSystem.current != null && prevSelected != null)
                        EventSystem.current.SetSelectedGameObject(prevSelected);

                    yield break;
                }

                // Confirm -> create slot name now
                CreateSlotNameImmediately(slotIndex, chosenName);
                ctx.slotManager.RefreshAllSlots();

                // Restore selection after popup
                if (EventSystem.current != null && prevSelected != null)
                    EventSystem.current.SetSelectedGameObject(prevSelected);
            }
            else
            {
                // Existing slot: we will still delay default until after pipe anim
                ctx.skipDefaultAction = true;
            }

            // PIPE CINEMATIC (enter slot)
            if (pipeContainer != null)
                ctx.mario.AttachTo(pipeContainer, true);

            ctx.mario.SetCelebrate(true);

            yield return new WaitForSecondsRealtime(1.35f);

            Trigger(pipeAnimator, enterTrigger);
            PlayPipeSfx();

            // IMPORTANT: wait for enter animation BEFORE allowing fade/scene change
            yield return WaitForAnim(pipeAnimator, enterStateName, fallbackAnimWait);

            // Now allow default action (PlayFocusedSlot -> fade)
            ctx.skipDefaultAction = false;

            yield return null;
            yield break;
        }

        // ============================================================
        // DELETE MODE (unchanged from your old working flow)
        // ============================================================
        ctx.skipDefaultAction = true;

        ctx.mario.SetFollowSelection(false);

        if (uiInputLock != null && uiInputLock.GetLockCount() > 0)
        {
            uiInputLock.ForceUnlockAll(false);
            yield return null;
        }

        var prevSelectedDel = EventSystem.current?.currentSelectedGameObject;

        // If slot empty, cancel delete
        if (!SaveManager.SlotExists(slotIndex))
        {
            Debug.Log("PipeEnterSequence: Cannot delete an empty slot. Cancelling delete mode.");

            ctx.slotManager.CancelCurrentMode();

            ctx.mario.SetTransformIntoObject();
            yield return new WaitForSecondsRealtime(0.5f);
            ctx.mario.SetIdle();
            ctx.mario.SetFollowSelection(true);

            if (EventSystem.current != null && prevSelectedDel != null)
                EventSystem.current.SetSelectedGameObject(prevSelectedDel);

            yield break;
        }

        if (confirmPopup != null)
        {
            bool? decision = null;

            confirmPopup.Show(
                deleteConfirmMsg,
                yes: () => decision = true,
                no: () => decision = false,
                selectYes: false,
                yesText: yesBtn,
                noText: noBtn
            );

            while (decision == null)
                yield return null;

            if (decision == false)
            {
                ctx.slotManager.CancelCurrentMode();
                ctx.mario.SetTransformIntoObject();
                yield return new WaitForSecondsRealtime(0.5f);
                ctx.mario.SetIdle();
                ctx.mario.SetFollowSelection(true);

                if (EventSystem.current != null && prevSelectedDel != null)
                    EventSystem.current.SetSelectedGameObject(prevSelectedDel);

                yield break;
            }

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

        if (EventSystem.current != null && prevSelectedDel != null)
            EventSystem.current.SetSelectedGameObject(prevSelectedDel);

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

        while (anim.GetCurrentAnimatorStateInfo(layer).normalizedTime < 1f)
            yield return null;
    }

    private void CreateSlotNameImmediately(int slotIndex, string chosenName)
    {
        int prevSlot = SaveManager.CurrentSlot;

        SaveManager.Load(slotIndex); // creates if missing

        if (SaveManager.Current != null)
        {
            string trimmed = string.IsNullOrWhiteSpace(chosenName)
                ? SaveSlotNaming.DefaultNameFor((SaveSlotId)slotIndex)
                : chosenName.Trim();

            SaveManager.Current.profileName = trimmed;
            SaveManager.Save();
        }

        if (prevSlot != slotIndex)
            SaveManager.Load(prevSlot);
    }

    private void RenameSlotNameImmediately(int slotIndex, string chosenName)
    {
        if (!SaveManager.SlotExists(slotIndex)) return;

        int prevSlot = SaveManager.CurrentSlot;

        SaveManager.Load(slotIndex);

        if (SaveManager.Current != null)
        {
            string trimmed = string.IsNullOrWhiteSpace(chosenName)
                ? SaveSlotNaming.DefaultNameFor((SaveSlotId)slotIndex)
                : chosenName.Trim();

            SaveManager.Current.profileName = trimmed;
            SaveManager.Save();
        }

        if (prevSlot != slotIndex)
            SaveManager.Load(prevSlot);
    }

    private string GetSlotProfileName(int slotIndex)
    {
        if (!SaveManager.SlotExists(slotIndex))
            return SaveSlotNaming.DefaultNameFor((SaveSlotId)slotIndex);

        int prevSlot = SaveManager.CurrentSlot;

        SaveManager.Load(slotIndex);
        string name = SaveManager.Current != null ? SaveManager.Current.profileName : "";

        if (string.IsNullOrWhiteSpace(name))
            name = SaveSlotNaming.DefaultNameFor((SaveSlotId)slotIndex);

        if (prevSlot != slotIndex)
            SaveManager.Load(prevSlot);

        return name;
    }
}