using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    public SpriteSwapArea spriteswaparea;

    public List<EnemyGroup> enemyGroups = new List<EnemyGroup>();
    private List<AudioSource> allAudioSources = new List<AudioSource>();
    private bool hasPlayedMariowhoaaa = false;

    private void Start()
    {
        spriteswaparea.enabled = false;

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

            StartCoroutine(TriggerAmbush(other.gameObject));
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Set isScared to false on the Player's animator
            Animator playerAnimator = other.GetComponent<Animator>();
            if (playerAnimator != null)
            {
                playerAnimator.SetBool("isScared", false);
            }
        }
    }

    private IEnumerator TriggerAmbush(GameObject player)
    {
        yield return new WaitForSeconds(delayBeforeAmbush);

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

        // Enable scared player library
        spriteswaparea.enabled = true;

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

                StartCoroutine(EnemyAudio());
            }

            // Wait before the next group
            yield return new WaitForSeconds(delayBetweenGroups * Time.deltaTime);
        }

        // Disable the collider
        GetComponent<Collider2D>().enabled = false;
        // Disable scared animator
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("isScared", false);
        }
        // Return player to normal library
        spriteswaparea.enabled = false;
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
            audioSource.clip = ambushAudioClip;
            audioSource.Play();
            yield return new WaitForSeconds(delayBetweenAudio * Time.deltaTime);
        }
    }
}
