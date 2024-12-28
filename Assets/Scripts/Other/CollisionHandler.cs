using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    public GameObject objectToSpawn; // Yellow burst prefab
    public AudioClip hit;           // Hit sound
    private AudioSource audioSource;
    private Collider2D col;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        col = GetComponentInChildren<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var mario = other.GetComponent<MarioMovement>();

            // Check if Mario can be damaged
            if (mario != null && mario.invincetimeremain <= 0)
            {
                DamageMario(mario, col.ClosestPoint(other.transform.position));
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var mario = collision.gameObject.GetComponent<MarioMovement>();

            // Check if Mario can be damaged
            if (mario != null && mario.invincetimeremain <= 0)
            {
                DamageMario(mario, col.ClosestPoint(collision.transform.position));
            }
        }
    }

    private void DamageMario(MarioMovement mario, Vector2 hitPoint)
    {
        // Damage Mario
        mario.damageMario();

        // Spawn yellow burst at the hit position
        if (objectToSpawn != null)
        {
            Instantiate(objectToSpawn, hitPoint, Quaternion.identity);
        }

        // Play hit sound
        if (audioSource != null && hit != null)
        {
            audioSource.PlayOneShot(hit);
        }

        Debug.Log($"Mario damaged at position: {hitPoint}");
    }
}