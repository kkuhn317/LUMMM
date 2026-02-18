using UnityEngine;

public class PauseableObject : MonoBehaviour
{
    [Header("During Pause")]
    public bool dontPauseObjectAnimator = false;

    [Header("During Resume")]
    public bool resumeObjectAnimator = false;

    private ObjectPhysics.ObjectMovement oldMovement;
    private ObjectPhysics objectPhysics;

    private Animator animator;
    private AnimatedSprite animatedSprite;

    private PauseableObjectsController controller;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animatedSprite = GetComponent<AnimatedSprite>();
        objectPhysics = GetComponent<ObjectPhysics>();

        controller = FindObjectOfType<PauseableObjectsController>();
        if (controller == null)
        {
            Debug.LogError($"{nameof(PauseableObject)} requires {nameof(PauseableObjectsController)} in the scene.");
        }
    }

    private void OnEnable()
    {
        controller?.Register(this);
    }

    private void OnDisable()
    {
        controller?.Unregister(this);
    }

    public void Pause()
    {
        if (objectPhysics == null) return;

        oldMovement = objectPhysics.movement;
        objectPhysics.movement = ObjectPhysics.ObjectMovement.still;

        if (animator != null && !dontPauseObjectAnimator)
            animator.enabled = false;

        if (animatedSprite != null && !dontPauseObjectAnimator)
            animatedSprite.PauseAnimation();
    }

    public void Resume()
    {
        if (objectPhysics == null) return;

        objectPhysics.movement = oldMovement;

        if (animator != null && !resumeObjectAnimator)
            animator.enabled = true;

        if (animatedSprite != null && !resumeObjectAnimator)
            animatedSprite.ResumeAnimation();
    }

    public void FallStraightDown()
    {
        if (objectPhysics == null) return;

        objectPhysics.velocity = new Vector2(0, 0);

        objectPhysics.floorMask = 0;
        objectPhysics.wallMask = 0;
        objectPhysics.movement = ObjectPhysics.ObjectMovement.sliding;
    }
}