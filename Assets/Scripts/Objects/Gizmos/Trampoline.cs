using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trampoline : MonoBehaviour
{
    public bool objectBounce = false;
    public float playerBouncePower = 10;
    public float objectBouncePower = 25;

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
        for(int i = 0; i < contactCount; i++) {
            var contact = other.GetContact(i);
            impulse += contact.normal * contact.normalImpulse;
            impulse.x += contact.tangentImpulse * contact.normal.y;
            impulse.y -= contact.tangentImpulse * contact.normal.x;
        }

        if (impulse.y < 0) {
            if (other.gameObject.tag == "Player") {
                MarioMovement playerScript = other.gameObject.GetComponent<MarioMovement>();
                playerScript.Jump();
                other.gameObject.GetComponent<Rigidbody2D>().velocity += new Vector2(0, playerBouncePower);
                Bounce();
            }
        }

        GameObject otherObject = other.gameObject;

        if (objectBounce && other.transform.position.y > transform.position.y && other.transform.position.x > transform.position.x - 1 && other.transform.position.x < transform.position.x + 1 && otherObject.GetComponent<ObjectPhysics>()) {
            otherObject.GetComponent<ObjectPhysics>().velocity = new Vector2(otherObject.GetComponent<ObjectPhysics>().velocity.x, objectBouncePower);
            Bounce();
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (objectBounce && other.transform.position.y > transform.position.y && other.transform.position.x > transform.position.x - 1 && other.transform.position.x < transform.position.x + 1 && other.GetComponent<ObjectPhysics>()) {
            other.GetComponent<ObjectPhysics>().velocity = new Vector2(other.GetComponent<ObjectPhysics>().velocity.x, objectBouncePower);
            Bounce();
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
