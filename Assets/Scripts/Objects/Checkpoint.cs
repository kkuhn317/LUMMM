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

    public void ActivateCheckpoint()
    {
        gameObject.SetActive(true);
        checkpointSet = false;
    }

    public void DeactivateCheckpoint()
    {
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !checkpointSet)
        {
            Debug.Log("Checkpoint!", gameObject);

            // Change the sprite to an "active" sprite
            int activeSpriteIndex = 0;
            spriteRenderer.sprite = active[activeSpriteIndex];
            audioSource.PlayOneShot(CheckpointSound);
            GameManager.Instance.AddScorePoints(2000);

            if (checkpointParticles != null)
            {
                checkpointParticles.Play();
            }

            checkpointSet = true;
        }
    }
}
