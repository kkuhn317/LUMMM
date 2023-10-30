using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint")]
    public AudioClip CheckpointSound;
    public Sprite passive;
    public Sprite[] active;
    public ParticleSystem checkpointParticles;
    public GameObject particle;

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
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Checkpoint!");
            checkpointCollider.enabled = false;

            // Change the sprite to an "active" sprite
            int activeSpriteIndex = 0;
            spriteRenderer.sprite = active[activeSpriteIndex];
            audioSource.PlayOneShot(CheckpointSound);

            if (checkpointParticles != null)
            {
                checkpointParticles.Play();
            }

            if (particle != null)
            {
                spawnParticles();
            }
        }
    }

    public void ActivateCheckpoint()
    {
        gameObject.SetActive(true);
        checkpointCollider.enabled = true;
        spriteRenderer.sprite = passive;
    }

    public void DeactivateCheckpoint()
    {
        gameObject.SetActive(false);
        checkpointCollider.enabled = false;
    }

    #region particles
    void spawnParticles()
    {
        // spawn 8 of them around the key and make them move outwards in specific directions
        int[] vertdirections = new int[] { -1, 0, 1 };
        int[] horizdirections = new int[] { -1, 0, 1 };
        for (int i = 0; i < vertdirections.Length; i++)
        {
            for (int j = 0; j < horizdirections.Length; j++)
            {
                if (vertdirections[i] == 0 && horizdirections[j] == 0)
                {
                    continue;
                }
                float distance;
                if (vertdirections[i] != 0 && vertdirections[j] != 0)
                {
                    distance = 0.7f;
                }
                else
                {
                    distance = 1f;
                }
                Vector3 startoffset = new Vector3(horizdirections[i] * distance, vertdirections[j] * distance, 0);

                GameObject newParticle = Instantiate(particle, transform.position + startoffset, Quaternion.identity);

                // make the particles move outwards at constant speed
                newParticle.GetComponent<StarMoveOutward>().direction = new Vector2(vertdirections[i], horizdirections[j]);
                newParticle.GetComponent<StarMoveOutward>().speed = 2f;
            }
        }
    }
    #endregion
}
