using System.Collections;
using UnityEngine;

public class StarCollector : MonoBehaviour
{
    public GameObject particle; // The particle to spawn
    public float particleSpeed = 2f; // Base speed of the particles' movement
    public Vector3 particleScale = new Vector3(1f, 1f, 1f); // Default scale for particles
    public bool randomizeDirection = false; // Whether to randomize the particle spawn direction
    public Vector3[] spawnPositions; // Array of positions to spawn particles (relative to the object)
    public float scaleVariation = 0.5f; // Amount of scale variation (0 = no variation)
    public float speedVariation = 0.5f; // Amount of speed variation (0 = no variation)

    void Start()
    {
        SpawnStars();
    }

    // Method to spawn stars around the object
    void SpawnStars()
    {
        // Loop through the custom spawn positions
        foreach (Vector3 spawnOffset in spawnPositions)
        {
            // Instantiate the particle at the given offset position
            GameObject newParticle = Instantiate(particle, transform.position + spawnOffset, Quaternion.identity);

            // Apply scale variation
            float randomScale = Random.Range(1f - scaleVariation, 1f + scaleVariation);
            newParticle.transform.localScale = particleScale * randomScale;

            // Apply random speed variation
            float randomSpeed = particleSpeed + Random.Range(-speedVariation, speedVariation);

            // Use GetComponent to get the StarMoveOutward component and set its properties
            StarMoveOutward starMove = newParticle.GetComponent<StarMoveOutward>();
            if (starMove != null)
            {
                // If randomizeDirection is true, assign a random direction
                Vector2 randomDirection = randomizeDirection
                    ? new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized
                    : new Vector2(spawnOffset.x, spawnOffset.y).normalized;

                starMove.direction = randomDirection;
                starMove.speed = randomSpeed;
            }
            else
            {
                Debug.LogError("StarMoveOutward script missing on particle.");
            }
        }
    }

    // Gizmo to visualize spawn positions in the Scene View
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        foreach (Vector3 position in spawnPositions)
        {
            Vector3 spawnPoint = transform.position + position;
            Gizmos.DrawSphere(spawnPoint, 0.5f);
        }
    }
}