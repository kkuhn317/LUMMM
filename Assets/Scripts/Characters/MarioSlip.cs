using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Behavior for when Mario slips on a banana
public class MarioSlip : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator animator;
    private MarioMovement marioMovement;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        marioMovement = GetComponent<MarioMovement>();

        // Disable mario movement when slipping
        marioMovement.enabled = false;

        bool movingRight = rb.velocity.x >= 0;

        float horizVel = movingRight ? 5 : -5;

        // Remove constraints
        rb.constraints = RigidbodyConstraints2D.None;

        // Set rotation
        rb.AddTorque(movingRight ? 100 : -100);

        // Set his vertical velocity
        rb.velocity = new Vector2(horizVel, 15);

        // Set gravity scale to the in air gravity
        rb.gravityScale = marioMovement.fallgravity;
        
        // Set animation
        animator.SetBool("onGround", false);
        animator.SetTrigger("slip");
    }

    // Update is called once per frame
    void FixedUpdate() {
        RaycastHit2D? hit = marioMovement.CheckGround();

        if (hit != null && rb.velocity.y <= 0) {
            // If mario is on the ground, stop slipping
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            transform.rotation = Quaternion.identity;
            rb.velocity = new Vector2(0, 0);
            animator.SetBool("onGround", true);
            marioMovement.enabled = true;
            Destroy(this);
        }
    }

}
