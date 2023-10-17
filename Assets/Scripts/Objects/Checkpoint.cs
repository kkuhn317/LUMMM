using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Position")]
    public Transform CheckPointPosition;

    [Header("Checkpoint Audio")]
    public AudioClip CheckpointSound;

    [Header("Checkpoint Sprites")]
    public Sprite passive;
    public Sprite[] active;

    [Header("Particles")]
    public ParticleSystem checkpointParticles;

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private bool checkpointSet = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !checkpointSet)
        {
            Debug.Log("Checkpoint!", gameObject);

            // Change the sprite to an "active" sprite
            int activeSpriteIndex = 0;
            spriteRenderer.sprite = active[activeSpriteIndex];
            audioSource.PlayOneShot(CheckpointSound); // Play an audio 
            GameManager.Instance.AddScorePoints(2000); // Give 2000 points

            if (checkpointParticles != null) // If there're particles attached
            {
                checkpointParticles.Play(); // Play the particles
            }

            checkpointSet = true; // The checkpoint has been set
        }
    }
}

