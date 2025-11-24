using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayAudioAfterDelay : MonoBehaviour
{
    public float delay = 1.25f; // Delay time in seconds

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        StartCoroutine(PlayDelayedAudio());
    }

    private IEnumerator PlayDelayedAudio()
    {
        yield return new WaitForSeconds(delay);

        if (audioSource != null)
        {
            audioSource.Play();
        }
    }
}