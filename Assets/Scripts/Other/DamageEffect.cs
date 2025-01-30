using System.Runtime.CompilerServices;
using UnityEngine;


// Shows an effect when the attached object damages Mario.
// In the future it might be best to move the collision handling somewhere else
// and just have this script handle the effect
public class DamageEffect : MonoBehaviour
{
    public GameObject objectToSpawn; // Yellow burst prefab
    public AudioClip hit;           // Hit sound
    private AudioSource audioSource;
    private Collider2D col;
    bool hasExistingCollision = false;

    private void Start()
    {
        // Audio Source might be in parent object (spike bullet bill)
        audioSource = GetComponentInParent<AudioSource>();
        col = GetComponentInChildren<Collider2D>();

        //! TODO!! Rework this so I dont have to check for all the different scripts!
        EnemyAI enemyAI = GetComponent<EnemyAI>();
        SpikeyHazard spikeyHazard = GetComponent<SpikeyHazard>();

        if (enemyAI != null) {
            hasExistingCollision = true;
            enemyAI.onPlayerDamaged.AddListener(ShowEffect);
        }
        if (spikeyHazard != null) {
            hasExistingCollision = true;
            spikeyHazard.onPlayerDamaged.AddListener(ShowEffect);
        }

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExistingCollision || !isActiveAndEnabled) return;

        if (other.CompareTag("Player"))
        {
            var mario = other.GetComponent<MarioMovement>();

            // Check if Mario can be damaged
            if (mario != null && mario.invincetimeremain <= 0)
            {
                DamageMario(mario);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasExistingCollision || !isActiveAndEnabled) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            var mario = collision.gameObject.GetComponent<MarioMovement>();

            // Check if Mario can be damaged
            if (mario != null && mario.invincetimeremain <= 0)
            {
                DamageMario(mario);
            }
        }
    }

    private void DamageMario(MarioMovement mario)
    {
        // Damage Mario
        mario.damageMario();

        // Show effect
        ShowEffect(mario.gameObject);
    }

    private void ShowEffect(GameObject player)
    {
        Vector2 hitPoint = col.ClosestPoint(player.transform.position);
        Debug.Log($"Effect showing at position: {hitPoint}");

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
    }
}