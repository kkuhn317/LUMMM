using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecretPassagesPow : MonoBehaviour
{
    public GameObject spiny;
    private AudioSource audioSource;

    private bool triggered = false;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == spiny)
        {
            if (triggered) return; // Prevent multiple triggers
            triggered = true;

            // Remove collision and disable rendering of myself
            gameObject.GetComponent<Collider2D>().enabled = false;
            gameObject.GetComponent<SpriteRenderer>().enabled = false;

            // Play the sound effect
            audioSource.Play();

            // Screen shake
            CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
            if (cameraFollow != null) {
                cameraFollow.ShakeCameraRepeatedly(0.1f, 0.25f, 1.0f, new Vector3(0, 1, 0), 2, 0.1f);
            } else {
                Debug.LogWarning("No CameraFollow component found on the main camera.");
            }

            // Kill the spiny
            spiny.GetComponent<EnemyAI>().KnockAway(false);

            // Destry me after a delay
            Destroy(gameObject, 0.5f);
        }
    }
}
