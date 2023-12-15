using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class EnemyGroup
{
    public List<EnemyAI> enemies;
}

public class AmbushTrigger : MonoBehaviour
{
    public float delayBeforeAmbush = 1f;
    public float delayBetweenGroups = 0.25f;
    public PlayableDirector spikeygoombasgodown;
    public float delayBetweenAudio = 0.025f;
    public AudioClip ambushAudioClip;

    public List<EnemyGroup> enemyGroups = new List<EnemyGroup>();
    private List<AudioSource> allAudioSources = new List<AudioSource>();

    private void Start()
    {
        // Collect all AudioSources at the start
        foreach (EnemyGroup group in enemyGroups)
        {
            if (group == null)
                continue;

            foreach (Goomba enemy in group.enemies)
            {
                if (enemy != null)
                {
                    AudioSource enemyAudioSource = enemy.GetComponent<AudioSource>();
                    if (enemyAudioSource != null)
                    {
                        allAudioSources.Add(enemyAudioSource);
                    }
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (spikeygoombasgodown != null)
            {
                spikeygoombasgodown.Play();
            }
            StartCoroutine(TriggerAmbush());
        }
    }

    private IEnumerator TriggerAmbush()
    {
        yield return new WaitForSeconds(delayBeforeAmbush);

        // Set the movement and reset bounceHeight for all enemies
        foreach (EnemyGroup group in enemyGroups)
        {
            if (group == null)
                continue;

            foreach (Goomba enemy in group.enemies)
            {
                if (enemy != null)
                {
                    enemy.movement = ObjectPhysics.ObjectMovement.bouncing;

                    if (enemy.movement != ObjectPhysics.ObjectMovement.still)
                    {
                        enemy.bounceHeight = 0;
                    }
                }

                StartCoroutine(EnemyAudio());
            }

            // Wait before the next group
            yield return new WaitForSeconds(delayBetweenGroups * Time.deltaTime);
        }

        // Disable the collider
        GetComponent<Collider2D>().enabled = false;
    }

    private IEnumerator EnemyAudio()
    {
        // Play audio sources with a delay   
        foreach (AudioSource audioSource in allAudioSources)
        {
            audioSource.clip = ambushAudioClip;
            audioSource.Play();
            yield return new WaitForSeconds(delayBetweenAudio);
        }
    }
}
