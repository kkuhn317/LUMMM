using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private bool checkpointSet = false;

    [Header("Position")]
    public Transform checkpointPosition;

    [Header("Checkpoint Audio")]
    public AudioClip CheckpointSound;

    [Header("Checkpoint Sprites")]
    public Sprite passive;
    public Sprite[] active;

    [Header("Particles")]
    public ParticleSystem checkpointParticles;

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private BoxCollider2D checkpointCollider;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        checkpointCollider = GetComponent<BoxCollider2D>();
    }   

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !checkpointSet)
        {
            Debug.Log("Checkpoint!", gameObject);
            checkpointCollider.enabled = false;

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
            CheckpointManager.Instance.lastCheckpointPosition = new Vector2(transform.position.x, transform.position.y);
            Debug.Log("Checkpoint has been set at" + CheckpointManager.Instance.lastCheckpointPosition);
        }
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
}
