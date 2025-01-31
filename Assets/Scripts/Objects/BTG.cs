using UnityEngine;
using UnityEngine.Playables;

public class BTG : MonoBehaviour
{
    public Animator animator;
    public PlayableDirector btgTimeline;
    public GameObject particlePrefab;

    private bool hasTriggered = false; // Ensures animation only triggers once

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hasTriggered && other.CompareTag("Player")) // Check if it's the player and not triggered before
        {
            hasTriggered = true;
            if (animator != null)
            {
                animator.SetTrigger("Activate");
            }

            if (btgTimeline != null){
                btgTimeline.Play();
            }
        }
    }

    public void SpawnParticle()
    {
        if (particlePrefab != null)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, 0.25f, 0);
            Instantiate(particlePrefab, spawnPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("Particle prefab is not assigned in BTG.");
        }
    }
}