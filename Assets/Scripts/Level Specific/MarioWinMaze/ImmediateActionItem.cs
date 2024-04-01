using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

// When mario touches this object, it will do the unity event, and also play its cutscene if it has one
public class ImmediateActionItem : MonoBehaviour
{
    public UnityEngine.Events.UnityEvent unityEvent;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<BoxCollider2D>().enabled = false;
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.Play();
            }
            unityEvent.Invoke();
            // start playable director
            PlayableDirector pd = GetComponent<PlayableDirector>();
            if (pd != null)
            {
                pd.Play();
            }
        }
    }
}
