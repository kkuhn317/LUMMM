using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trampoline : MonoBehaviour
{
    public bool objectBounce = false;
    public float playerBouncePower = 10;
    public float objectBouncePower = 25;
    public bool sideways = false; // If the spring is sideways

    private bool isVisible = false; // If the spring is visible to the camera

    public void Bounce () {

        Animator animator = GetComponent<Animator>();
        if (animator != null) // If there's an animation
            animator.SetTrigger("Bounce");

        if (!isVisible) // If it's not visible, don't play the sound
            return;

        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null) // If it contains an audio
            audioSource.Play();
    }

    private void OnCollisionEnter2D(Collision2D other) {
        
        Vector2 impulse = Vector2.zero;

        int contactCount = other.contactCount;
        for (int i = 0; i < contactCount; i++)
        {
            var contact = other.GetContact(i);
            impulse += contact.normal * contact.normalImpulse;
#if !UNITY_ANDROID
            impulse.x += contact.tangentImpulse * contact.normal.y;
            impulse.y -= contact.tangentImpulse * contact.normal.x;
#endif
            // NOTE: The tiny spring could bounce mario up from the side on mobile if the above lines are uncommented
            // ALSO, on Web version, springs might have a chance to not work if those 2 lines are not present (at least that's my theory...)
            // So, for now we will only run those lines on Android and not on Web or Windows
        }
        
        if (other.gameObject.tag == "Player") {
            MarioMovement playerScript = other.gameObject.GetComponent<MarioMovement>();
            if (playerScript != null)
                // Cancel ground pound when bouncing on trampoline
                playerScript.CancelGroundPound();
        }

        if (impulse.y < 0 && !sideways) {
            if (other.gameObject.tag == "Player") {
                MarioMovement playerScript = other.gameObject.GetComponent<MarioMovement>();
                playerScript.Jump();
                other.gameObject.GetComponent<Rigidbody2D>().velocity += new Vector2(0, playerBouncePower);
                Bounce();
            }
        } else if (impulse.x < 0 && sideways) {
            if (other.gameObject.tag == "Player") {
                MarioMovement playerScript = other.gameObject.GetComponent<MarioMovement>();
                other.gameObject.GetComponent<Rigidbody2D>().velocity += new Vector2(playerBouncePower, 0);
                Bounce();
            }
        } else if (impulse.x > 0 && sideways) {
            if (other.gameObject.tag == "Player") {
                MarioMovement playerScript = other.gameObject.GetComponent<MarioMovement>();
                other.gameObject.GetComponent<Rigidbody2D>().velocity += new Vector2(-playerBouncePower, 0);
                Bounce();
            }
        }

        GameObject otherObject = other.gameObject;

        if (objectBounce && otherObject.GetComponent<ObjectPhysics>()) {
            if (other.transform.position.y > transform.position.y && other.transform.position.x > transform.position.x - 1 && other.transform.position.x < transform.position.x + 1 && !sideways) {
                otherObject.GetComponent<ObjectPhysics>().velocity = new Vector2(otherObject.GetComponent<ObjectPhysics>().velocity.x, objectBouncePower);
                Bounce();
            } else if (other.transform.position.x > transform.position.x && other.transform.position.y > transform.position.y - 1 && other.transform.position.y < transform.position.y + 1 && sideways) {
                otherObject.GetComponent<ObjectPhysics>().velocity = new Vector2(objectBouncePower, otherObject.GetComponent<ObjectPhysics>().velocity.y);
                Bounce();
            } else if (other.transform.position.x < transform.position.x && other.transform.position.y > transform.position.y - 1 && other.transform.position.y < transform.position.y + 1 && sideways) {
                otherObject.GetComponent<ObjectPhysics>().velocity = new Vector2(-objectBouncePower, otherObject.GetComponent<ObjectPhysics>().velocity.y);
                Bounce();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        // Check if the triggering object is the player
        if (other.gameObject.tag == "Player")
        {
            MarioMovement playerScript = other.gameObject.GetComponent<MarioMovement>();

            if (playerScript != null)
            {
                // Cancel ground pound when bouncing on trampoline (trigger version)
                playerScript.CancelGroundPound();
            }
        }
        
        if (objectBounce && other.GetComponent<ObjectPhysics>()) {
            if (other.transform.position.y > transform.position.y && other.transform.position.x > transform.position.x - 1 && other.transform.position.x < transform.position.x + 1 && !sideways) {
                other.GetComponent<ObjectPhysics>().velocity = new Vector2(other.GetComponent<ObjectPhysics>().velocity.x, objectBouncePower);
                Bounce();
            } else if (other.transform.position.x > transform.position.x && other.transform.position.y > transform.position.y - 1 && other.transform.position.y < transform.position.y + 1 && sideways) {
                other.GetComponent<ObjectPhysics>().velocity = new Vector2(objectBouncePower, other.GetComponent<ObjectPhysics>().velocity.y);
                Bounce();
            } else if (other.transform.position.x < transform.position.x && other.transform.position.y > transform.position.y - 1 && other.transform.position.y < transform.position.y + 1 && sideways) {
                other.GetComponent<ObjectPhysics>().velocity = new Vector2(-objectBouncePower, other.GetComponent<ObjectPhysics>().velocity.y);
                Bounce();
            }
        }
    }

    private void OnBecameVisible()
    {
        isVisible = true;
    }

    private void OnBecameInvisible()
    {
        isVisible = false;
    }
}
