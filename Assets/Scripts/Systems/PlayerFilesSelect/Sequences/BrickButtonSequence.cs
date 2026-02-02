using System.Collections;
using UnityEngine;

public class BrickButtonSequence : MonoBehaviour, IFileSelectSequence
{
    [Header("Jump target")]
    [Tooltip("Where Mario should reach at the top of the jump. If null, uses startPos + (Vector3.up * 1.2f).")]
    public Transform jumpTarget;

    [Header("Timings")]
    public float jumpUpDuration = 0.12f;
    public float fallDownDuration = 0.10f;

    [Header("Brick bounce")]
    public Animator brickAnimator;
    public string brickBounceTrigger = "Bounce";
    public float bounceDelay = 0.02f; // small delay after reaching apex
    public float postBounceDelay = 0.02f;
    public AudioClip brickBounceSound;

    [Header("Delete behavior")]
    public float transformIntoBombDelay = 0.15f; // time to let transform anim read

    public IEnumerator Play(FileSelectSequenceContext ctx)
    {
        if (ctx == null || ctx.mario == null || ctx.slotManager == null || ctx.interactable == null)
            yield break;

        // Only for action buttons
        var type = ctx.interactable.actionType;
        bool isActionButton =
            type == FileSelectActionType.DeleteSlot ||
            type == FileSelectActionType.CopySlot ||
            type == FileSelectActionType.Import ||
            type == FileSelectActionType.Export;

        if (!isActionButton)
        {
            ctx.skipDefaultAction = false;
            yield break;
        }

        // We handle the mode change ourselves (so FileSelectManager shouldn't do it again)
        ctx.skipDefaultAction = true;

        // Lock follow snapping while we animate
        ctx.mario.SetFollowSelection(false);

        Transform marioT = ctx.mario.transform;

        // LAND TARGET: Prefer MarioAnchorProvider.marioAnchor on this action button (or parent)
        Vector3 landPos = marioT.position;
        var anchorProvider = ctx.interactable.GetComponentInParent<MarioAnchorProvider>();
        if (anchorProvider != null && anchorProvider.marioAnchor != null)
        {
            landPos = anchorProvider.marioAnchor.position;

            // Ensure we start exactly on the intended land/anchor point
            marioT.position = landPos;
        }

        // Start position is the land target (anchor)
        Vector3 startPos = landPos;

        // Determine apex (safe fallback if jumpTarget is null)
        Vector3 apexPos = (jumpTarget != null)
            ? jumpTarget.position
            : (startPos + Vector3.up * 1.2f);

        // Delete-mode state BEFORE we act
        bool wasDelete = ctx.slotManager.CurrentMode == SaveSlotManager.InteractionMode.Delete;
        bool isDeleteButton = type == FileSelectActionType.DeleteSlot;

        // What the delete mode SHOULD BE after this action:
        // - Delete button toggles delete on/off
        // - Other action buttons always exit delete mode
        bool willBeDelete = isDeleteButton ? !wasDelete : false;

        bool enteringDelete = !wasDelete && willBeDelete;
        bool leavingDelete = wasDelete && !willBeDelete;

        // START feedback (Mario is a bomb -> always bomb jump)
        if (wasDelete)
            ctx.mario.SetBombJump();
        else
            ctx.mario.SetJump();

        // 1) Jump up
        yield return LerpWorldPosition(marioT, startPos, apexPos, jumpUpDuration);

        if (bounceDelay > 0f)
            yield return new WaitForSecondsRealtime(bounceDelay);

        // 2) Brick bounce
        if (brickAnimator != null && !string.IsNullOrEmpty(brickBounceTrigger))
            brickAnimator.SetTrigger(brickBounceTrigger);

        AudioManager.Instance?.Play(brickBounceSound, SoundCategory.SFX);

        // 3) At apex: if changing delete mode (entering OR leaving), play transform,
        // then set correct airborne state AFTER transforming.
        // NOTE: This may replay jump SFX depending on your controller implementation.
        // If you later add no-SFX overloads, replace these calls with SetJump(false) / SetBombJump(false).
        if (enteringDelete || leavingDelete)
        {
            ctx.mario.SetTransformIntoObject();
            if (transformIntoBombDelay > 0f)
                yield return new WaitForSecondsRealtime(transformIntoBombDelay);

            if (enteringDelete)
                ctx.mario.SetBombJump(false); // now bomb -> airborne bomb
            else
                ctx.mario.SetJump(false); // now normal -> airborne normal
        }

        if (postBounceDelay > 0f)
            yield return new WaitForSecondsRealtime(postBounceDelay);

        // 4) Fall back down (LAND to MarioAnchorProvider.marioAnchor)
        yield return LerpWorldPosition(marioT, apexPos, startPos, fallDownDuration);

        // 5) Enter / toggle mode
        switch (type)
        {
            case FileSelectActionType.DeleteSlot:
                ctx.slotManager.EnterDeleteMode(); // toggles delete on/off
                break;

            case FileSelectActionType.CopySlot:
                ctx.slotManager.EnterCopyMode();
                break;

            case FileSelectActionType.Import:
                ctx.slotManager.EnterImportMode();
                break;

            case FileSelectActionType.Export:
                ctx.slotManager.EnterExportMode();
                break;
        }

        // After toggle, set final Mario state
        if (ctx.slotManager.CurrentMode == SaveSlotManager.InteractionMode.Delete)
            ctx.mario.SetBomb();
        else
            ctx.mario.SetIdle();

        // Unlock follow snapping again
        ctx.mario.SetFollowSelection(true);
    }

    private IEnumerator LerpWorldPosition(Transform t, Vector3 a, Vector3 b, float duration)
    {
        if (duration <= 0f)
        {
            t.position = b;
            yield break;
        }

        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float x = Mathf.Clamp01(time / duration);
            x = x * x * (3f - 2f * x); // SmoothStep feel

            t.position = Vector3.LerpUnclamped(a, b, x);
            yield return null;
        }

        t.position = b;
    }
}