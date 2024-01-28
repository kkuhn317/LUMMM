using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerBananaScreech : MonoBehaviour
{
    private Animator animator; // Animator component reference
    private AudioSource audioSource;
    public AudioClip screech;

    void Start()
    {
        animator = GetComponent<Animator>(); // Animator component attached to this game object
        audioSource = GetComponent<AudioSource>(); // Animator component attached to this game object

        if (animator == null) {
            Debug.LogError("Animator component not found on the object.");
        }

        if (audioSource == null) {
            Debug.LogError("AudioSource component not found on the object.");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("OnlyCutsceneUse")) // If Luigi collides with an object tagged OnlyCutsceneUse
        {
            // Trigger the banana screech animation state
            if (animator != null) {
                animator.SetTrigger("screech");
            } else {
                Debug.LogError("Animator component not found.");
            }

            audioSource.PlayOneShot(screech);
        }
    }
}
