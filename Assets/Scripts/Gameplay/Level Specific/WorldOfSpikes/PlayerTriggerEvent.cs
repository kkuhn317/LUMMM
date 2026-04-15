using UnityEngine;
using UnityEngine.Playables;

public class PlayerTriggerEvent : MonoBehaviour
{
    public PlayableDirector timeline;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // MarioCore is on the ROOT — destroy the root, not the child collider
        var core = other.GetComponent<MarioCore>() ?? other.GetComponentInParent<MarioCore>();
        if (core == null) return;

        Destroy(core.gameObject);

        if (timeline != null)
            timeline.Play();
        else
            Debug.LogWarning("PlayableDirector is not assigned!");
    }
}