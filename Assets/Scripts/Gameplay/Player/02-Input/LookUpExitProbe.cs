using UnityEngine;

/// <summary>
/// Drop this on the "Small Mario" root (same object as the Animator).
/// It logs — only when a value CHANGES — the animator state, transition
/// status, which visual container is active, and the SpriteSimple sprite.
///
/// A one-frame artifact shows up as two log lines in consecutive frames
/// with the wrong value sandwiched between the correct ones. Read the
/// console right after you release Up.
///
/// Optional: set slowMoWhileLookingUp to stretch the look-up exit across
/// more real time so it's easier to eyeball in the Game view.
/// </summary>
public class LookUpExitProbe : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject spriteBaseRoot;      // Visual/SpriteBaseRoot
    [SerializeField] private GameObject spriteSwapContainer; // Visual/SpriteBaseRoot/SpriteSwapContainer
    [SerializeField] private SpriteRenderer spriteSimple;    // .../SpriteSimple

    [Header("Optional capture aid")]
    [SerializeField] private bool slowMoWhileLookingUp = false;
    [SerializeField] private float slowMoScale = 0.15f;

    // Last-seen values for change detection
    private int    _lastStateHash;
    private bool   _lastInTransition;
    private int    _lastNextHash;
    private bool   _lastBaseActive;
    private bool   _lastSwapActive;
    private Sprite _lastSprite;
    private bool   _primed;

    private void Reset()
    {
        animator = GetComponent<Animator>();
        // Best-effort auto-wire by name so you don't have to drag everything.
        var t = transform.Find("Visual/SpriteBaseRoot");
        if (t) spriteBaseRoot = t.gameObject;
        var s = transform.Find("Visual/SpriteBaseRoot/SpriteSwapContainer");
        if (s) spriteSwapContainer = s.gameObject;
        var sr = transform.Find("Visual/SpriteBaseRoot/SpriteSimple");
        if (sr) spriteSimple = sr.GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (animator == null) return;

        var st            = animator.GetCurrentAnimatorStateInfo(0);
        bool inTransition = animator.IsInTransition(0);
        int  nextHash     = inTransition ? animator.GetNextAnimatorStateInfo(0).fullPathHash : 0;
        bool baseActive   = spriteBaseRoot      && spriteBaseRoot.activeInHierarchy;
        bool swapActive   = spriteSwapContainer && spriteSwapContainer.activeInHierarchy;
        Sprite sprite     = spriteSimple ? spriteSimple.sprite : null;

        bool changed =
            !_primed ||
            st.fullPathHash != _lastStateHash ||
            inTransition    != _lastInTransition ||
            nextHash        != _lastNextHash ||
            baseActive      != _lastBaseActive ||
            swapActive      != _lastSwapActive ||
            sprite          != _lastSprite;

        if (changed)
        {
            // Note: state hashes aren't human-readable; use Animator window
            // "live link" alongside, or add names to a lookup if you want.
            Debug.Log(
                $"[Probe f{Time.frameCount}] state={st.fullPathHash} " +
                $"inTransition={inTransition} next={nextHash} " +
                $"BaseActive={baseActive} SwapActive={swapActive} " +
                $"sprite={(sprite ? sprite.name : "null")}");

            _lastStateHash    = st.fullPathHash;
            _lastInTransition = inTransition;
            _lastNextHash     = nextHash;
            _lastBaseActive   = baseActive;
            _lastSwapActive   = swapActive;
            _lastSprite       = sprite;
            _primed           = true;
        }

        if (slowMoWhileLookingUp)
        {
            bool lookingUp = swapActive; // swap container only on during limbs look-up
            Time.timeScale = lookingUp ? slowMoScale : 1f;
        }
    }
}
