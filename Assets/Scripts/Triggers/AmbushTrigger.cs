using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

[System.Serializable]
public class EnemyGroup
{
    public List<Goomba> enemies;
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
    private bool hasTriggered = false;

    public UnityEvent onAmbushBefore;
    public UnityEvent onAmbushStart;
    public UnityEvent onAmbushEnd;
    private bool isPlayerInTrigger = false;

    private void Start()
    {
        onAmbushBefore.Invoke();

        foreach (EnemyGroup group in enemyGroups)
        {
            if (group == null) continue;
            foreach (Goomba enemy in group.enemies)
            {
                if (enemy == null) continue;
                AudioSource src = enemy.GetComponent<AudioSource>();
                if (src != null) allAudioSources.Add(src);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInTrigger = true;
        if (hasTriggered) return;

        hasTriggered = true;
        spikeygoombasgodown?.Play();

        // MarioCore is on the ROOT — walk up from the child collider
        var core = other.GetComponent<MarioCore>() ?? other.GetComponentInParent<MarioCore>();
        StartCoroutine(TriggerAmbush(core));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        isPlayerInTrigger = false;

        var core = other.GetComponent<MarioCore>() ?? other.GetComponentInParent<MarioCore>();
        var anim = core != null
            ? core.GetComponentInChildren<Animator>()
            : other.GetComponentInChildren<Animator>();
        anim?.SetBool("isScared", false);
    }

    private IEnumerator TriggerAmbush(MarioCore core)
    {
        yield return new WaitForSeconds(delayBeforeAmbush);

        if (core == null) yield break;

        Animator playerAnimator = core.GetComponentInChildren<Animator>();

        if (playerAnimator != null && isPlayerInTrigger)
        {
            onAmbushStart.Invoke();
            playerAnimator.SetBool("isScared", true);

            if (!hasPlayedMariowhoaaa)
            {
                hasPlayedMariowhoaaa = true;
                AudioSource playerAudio = core.GetComponent<AudioSource>();
                if (playerAudio != null && Mariowhoaaa != null)
                    playerAudio.PlayOneShot(Mariowhoaaa);
            }
        }

        StartCoroutine(EnemyAudio());

        foreach (EnemyGroup group in enemyGroups)
        {
            if (group == null) continue;
            foreach (Goomba enemy in group.enemies)
            {
                if (enemy == null) continue;
                enemy.movement = ObjectPhysics.ObjectMovement.bouncing;
                if (enemy.movement != ObjectPhysics.ObjectMovement.still)
                    StartCoroutine(BounceEnemy(enemy));
            }

            // FIX: delayBetweenGroups is already in seconds — multiplying by
            // Time.deltaTime made it near-zero (~0.004s), so all groups fired
            // almost simultaneously instead of staggered.
            yield return new WaitForSeconds(delayBetweenGroups);
        }

        StartCoroutine(CompleteAmbush(playerAnimator));
    }

    private IEnumerator CompleteAmbush(Animator playerAnimator)
    {
        yield return new WaitForSeconds(1.5f);

        if (playerAnimator != null)
            playerAnimator.SetBool("isScared", false);

        // FIX: The ArgumentException was thrown here because onAmbushEnd had a
        // listener in the Inspector wired to a method that previously accepted a
        // GameObject but whose signature changed. Re-wire onAmbushEnd in the
        // Inspector to a no-argument method, or a method accepting no parameters.
        onAmbushEnd.Invoke();
    }

    private IEnumerator BounceEnemy(Goomba enemy)
    {
        enemy.velocity.x  = 3;
        enemy.bounceHeight = 8;

        yield return new WaitForSeconds(1.25f);

        enemy.velocity.x  = 1;
        enemy.bounceHeight = 0;
    }

    private IEnumerator EnemyAudio()
    {
        foreach (AudioSource audioSource in allAudioSources)
        {
            if (audioSource != null && ambushAudioClip != null)
                audioSource.PlayOneShot(ambushAudioClip);

            // FIX: same as delayBetweenGroups — remove Time.deltaTime multiplication.
            yield return new WaitForSeconds(delayBetweenAudio);
        }
    }
}