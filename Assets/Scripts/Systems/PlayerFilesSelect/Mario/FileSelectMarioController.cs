using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class FileSelectMarioController : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("Animator Triggers")]
    public string idleTrigger = "Idle";
    public string jumpTrigger = "Jump";
    public string bombTrigger = "Bomb";
    public string transformIntoObjectTrigger = "TransformIntoObject";
    public string bombJump = "BombJump";
    public string celebrationTrigger = "Celebrate";

    [Header("Sound Effects")]
    public AudioClip jumpSound;
    public AudioClip transformSound;
    public AudioClip bombSound;
    public AudioClip celebrationSound;

    public bool IsBusy { get; private set; }

    private Transform marioTransform;
    private Transform target;
    private bool followSelection = true;
    private Transform originalParent;

    void Awake()
    {
        marioTransform = transform;
        originalParent = marioTransform.parent;
    }

    void Update()
    {
        if (!followSelection || IsBusy) return;
        if (EventSystem.current == null) return;

        var current = EventSystem.current.currentSelectedGameObject;
        if (current == null) return;

        var provider = current.GetComponent<MarioAnchorProvider>();
        if (provider == null || provider.marioAnchor == null) return;

        if (target != provider.marioAnchor)
        {
            target = provider.marioAnchor;
            marioTransform.position = target.position; // instant snap
        }
    }

    public void SetFollowSelection(bool value) => followSelection = value;

    public IEnumerator MoveTo(Transform destination)
    {
        IsBusy = true;
        followSelection = false;

        marioTransform.position = destination.position;
        yield return null;

        followSelection = true;
        IsBusy = false;
    }

    public void SetIdle()
    {
        ResetAllOneShotTriggers();
        TriggerIfValid(idleTrigger);
    }

    public void SetJump(bool playSfx = true)
    {
        ResetTriggerIfValid(idleTrigger);
        ResetTriggerIfValid(bombTrigger);
        ResetTriggerIfValid(transformIntoObjectTrigger);
        ResetTriggerIfValid(bombJump);
        ResetTriggerIfValid(celebrationTrigger);

        TriggerIfValid(jumpTrigger);
        if (playSfx) PlaySfx(jumpSound);
    }

    public void SetBomb()
    {
        ResetTriggerIfValid(idleTrigger);
        ResetTriggerIfValid(jumpTrigger);
        ResetTriggerIfValid(transformIntoObjectTrigger);
        ResetTriggerIfValid(bombJump);
        ResetTriggerIfValid(celebrationTrigger);

        TriggerIfValid(bombTrigger);
    }

    public void SetTransformIntoObject(bool playSfx = true)
    {
        ResetTriggerIfValid(idleTrigger);
        ResetTriggerIfValid(jumpTrigger);
        ResetTriggerIfValid(bombTrigger);
        ResetTriggerIfValid(bombJump);
        ResetTriggerIfValid(celebrationTrigger);

        TriggerIfValid(transformIntoObjectTrigger);
        if (playSfx) PlaySfx(transformSound);
    }

    public void SetBombJump(bool playSfx = true)
    {
        ResetTriggerIfValid(idleTrigger);
        ResetTriggerIfValid(jumpTrigger);
        ResetTriggerIfValid(bombTrigger);
        ResetTriggerIfValid(transformIntoObjectTrigger);
        ResetTriggerIfValid(celebrationTrigger);

        TriggerIfValid(bombJump);
        if (playSfx) PlaySfx(jumpSound);
    }

    public void SetCelebrate(bool playSfx = true)
    {
        ResetTriggerIfValid(idleTrigger);
        ResetTriggerIfValid(jumpTrigger);
        ResetTriggerIfValid(bombTrigger);
        ResetTriggerIfValid(transformIntoObjectTrigger);
        ResetTriggerIfValid(bombJump);

        TriggerIfValid(celebrationTrigger);
        if (playSfx) PlaySfx(celebrationSound);
    }

    public void PlayExplosionSound()
    {
        PlaySfx(bombSound);
    }

    private void ResetAllOneShotTriggers()
    {
        ResetTriggerIfValid(jumpTrigger);
        ResetTriggerIfValid(bombTrigger);
        ResetTriggerIfValid(transformIntoObjectTrigger);
        ResetTriggerIfValid(bombJump);
        ResetTriggerIfValid(celebrationTrigger);
        ResetTriggerIfValid(idleTrigger);
    }

    private void ResetTriggerIfValid(string trigger)
    {
        if (animator == null || string.IsNullOrEmpty(trigger)) return;
        animator.ResetTrigger(trigger);
    }

    private void TriggerIfValid(string trigger)
    {
        if (animator == null || string.IsNullOrEmpty(trigger)) return;
        animator.SetTrigger(trigger);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        AudioManager.Instance?.Play(clip, SoundCategory.SFX);
    }

    public void AttachTo(Transform parent, bool worldPositionStays = true)
    {
        if (parent == null) return;
        originalParent = marioTransform.parent;
        marioTransform.SetParent(parent, worldPositionStays);
    }

    public void Detach(bool worldPositionStays = true)
    {
        if (originalParent != null)
            marioTransform.SetParent(originalParent, worldPositionStays);
        else
            marioTransform.SetParent(null, worldPositionStays);
    }
}