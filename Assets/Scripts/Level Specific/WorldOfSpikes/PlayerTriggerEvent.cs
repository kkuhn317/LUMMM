using UnityEngine;
using UnityEngine.Playables;

public class PlayerTriggerEvent : MonoBehaviour
{
    public PlayableDirector timeline;  // Reference to the PlayableDirector for the timeline

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object entering the trigger is the player
        if (other.gameObject.CompareTag("Player"))
        {
            // Destroy the player
            Destroy(other.gameObject);

            // Play the timeline
            if (timeline != null)
            {
                timeline.Play();
            }
            else
            {
                Debug.LogWarning("PlayableDirector is not assigned!");
            }
        }
    }
}