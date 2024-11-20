using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

[System.Serializable]
public class EnemyGroup
{
    public List<Goomba> enemies; // Assuming Goomba is the correct type, adjust if needed
}

public class AmbushTrigger : MonoBehaviour
{
    public float delayBeforeAmbush = 1f;
    public float delayBetweenGroups = 0.25f;
    public PlayableDirector spikeygoombasgodown;
    public float delayBetweenAudio = 0.025f;
    public AudioClip ambushAudioClip;
    public AudioClip Mariowhoaaa;

    public List<EnemyGroup> enemyGroups = new List<EnemyGroup>();
    private List<AudioSource> allAudioSources = new List<AudioSource>();
    private bool hasPlayedMariowhoaaa = false;
    private bool hasTriggered = false; // This is to ensure ambush happens only once

    public UnityEvent onAmbushBefore;
    public UnityEvent onAmbushStart;
    public UnityEvent onAmbushEnd;

    private void Start()
    {
        onAmbushBefore.Invoke();

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
        if (hasTriggered) return; // Prevent multiple triggers

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;
            if (spikeygoombasgodown != null)
            {
                spikeygoombasgodown.Play();
            }

            StartCoroutine(TriggerAmbush(other.gameObject));
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Animator playerAnimator = other.GetComponent<Animator>();
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("isScared", false);
        }
    }

    private IEnumerator TriggerAmbush(GameObject player)
    {
        yield return new WaitForSeconds(delayBeforeAmbush);

        if (player == null)
            yield break;

        onAmbushStart.Invoke();

        // Set the "isScared" parameter of the player's animator
        Animator playerAnimator = player.GetComponent<Animator>();
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("isScared", true);
        }

        // Play the Mariowhoaaa audio clip
        if (!hasPlayedMariowhoaaa)
        {
            AudioSource playerAudioSource = player.GetComponent<AudioSource>();
            playerAudioSource.PlayOneShot(Mariowhoaaa);
            hasPlayedMariowhoaaa = true;
        }

        StartCoroutine(EnemyAudio());

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
                        // Start the bouncing coroutine
                        StartCoroutine(BounceEnemy(enemy));
                    }
                }
            }

            // Wait before the next group
            yield return new WaitForSeconds(delayBetweenGroups * Time.deltaTime);
        }

        StartCoroutine(CompleteAmbush(playerAnimator));   
    }

    private IEnumerator CompleteAmbush(Animator playerAnimator)
    {
        // Disable "isScared" animation
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("isScared", false);
        }

        yield return new WaitForSeconds(1.5f);

        // Now invoke the end of the ambush event
        onAmbushEnd.Invoke();
    }

    private IEnumerator BounceEnemy(Goomba enemy)
    {
        // Second jump
        enemy.velocity.x = 3;
        enemy.bounceHeight = 8;

        // Wait for a short duration
        yield return new WaitForSeconds(1.25f);

        enemy.velocity.x = 1;
        // To stop the bounce
        enemy.bounceHeight = 0;
    }

    private IEnumerator EnemyAudio()
    {
        // Play audio sources with a delay   
        foreach (AudioSource audioSource in allAudioSources)
        {
            audioSource.PlayOneShot(ambushAudioClip);
            yield return new WaitForSeconds(delayBetweenAudio * Time.deltaTime);
        }
    }
}