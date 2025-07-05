using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HappyGoomba : MonoBehaviour
{
    public Goomba goomba; // Reference to the Goomba instance
    private Animator animator;

    void Start()
    {
        // Cache the Animator component on this GameObject
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Ensure the Goomba reference is valid before checking its state
        if (goomba != null && goomba.objectState == ObjectPhysics.ObjectState.grounded)
        {
            animator.SetTrigger("happy");
        }
    }
}
