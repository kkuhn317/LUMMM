using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseableObject : MonoBehaviour
{
    [Header("During Pause")]
    public bool dontPauseObjectAnimator = false;
    [Header("During Resume")]
    public bool resumeObjectAnimator = false;

    private ObjectPhysics.ObjectMovement oldMovement;
    private ObjectPhysics objectPhysics;

    private Animator animator; // Reference to the Animator component
    private AnimatedSprite animatedSprite; // Reference to the AnimatedSprite component

    private void Start()
    {
        // Get the Animator component
        animator = GetComponent<Animator>();
        // Get the AnimatedSprite component
        animatedSprite = GetComponent<AnimatedSprite>();

        // Ensure GameManager.Instance is not null before registering the PauseableObject
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPauseableObject(this);
            objectPhysics = GetComponent<ObjectPhysics>();
        }
        else
        {
            Debug.LogError("GameManager.Instance is null!");
        }
    }

    private void OnDestroy()
    {
        // Ensure GameManager.Instance is not null before unregistering the PauseableObject
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterPauseableObject(this);
        }
    }

    public void Pause()
    {
        oldMovement = objectPhysics.movement;
        objectPhysics.movement = ObjectPhysics.ObjectMovement.still;

        if (animator != null && !dontPauseObjectAnimator)
        {
            animator.enabled = false; // Disable the Animator to pause the animation
        }
        // Pause the animation if the AnimatedSprite component is available
        if (animatedSprite != null && !dontPauseObjectAnimator)
        {
            animatedSprite.PauseAnimation();
        }
    }

    public void Resume()
    {
        objectPhysics.movement = oldMovement;

        if (animator != null && !resumeObjectAnimator)
        {
            animator.enabled = true; // Disable the Animator to pause the animation
        }
        // Pause the animation if the AnimatedSprite component is available
        if (animatedSprite != null && !resumeObjectAnimator)
        {
            animatedSprite.ResumeAnimation();
        }
    }

    public void FallStraightDown()
    {
        objectPhysics.velocity = new Vector2(0, 0);
        
        objectPhysics.floorMask = 0;
        objectPhysics.wallMask = 0;
        objectPhysics.movement = ObjectPhysics.ObjectMovement.sliding;
    }
}
