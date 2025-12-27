using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GiantSpiny : EnemyAI
{
    [Header("Giant Spiny")]
    public float moveDistance = 1f;
    public GameObject spinySpikePrefab; // Prefab for the spiny spike
    public GameObject spinyShellPrefab; // Prefab for the spiny shell

    public float spikeSpawnInterval = 2f; // Time interval to spawn spikes
    public float shellSpawnInterval = 2f; // Time interval to spawn shells
    public float randomOffset = 1f; // Random offset for spawning spikes and shells
    public float spikeSpawnInitialDelay = 1f; // Initial delay before spawning spikes
    public float shellSpawnInitialDelay = 1f; // Initial delay before spawning shells

    private Vector3 startPosition;
    private float nextSpikeSpawnTime;
    private float nextShellSpawnTime;
    public AudioClip spinySpikeSound; // Sound for spiny spike
    public AudioClip spinyShellSound; // Sound for spiny shell
    private AudioSource audioSource;

    private bool paused = false; // Flag to check if the spiny is paused

    protected override void Start()
    {
        base.Start();
        startPosition = transform.position;
        nextSpikeSpawnTime = Time.time + spikeSpawnInitialDelay;
        nextShellSpawnTime = Time.time + shellSpawnInitialDelay;
        audioSource = GetComponent<AudioSource>();
    }

    protected override void Update()
    {
        base.Update();

        if (movement == ObjectPhysics.ObjectMovement.still)
        {
            paused = true; // Set paused to true if movement is still
        }

        if (paused)
        {
            return; // If paused, do not update further
        }

        // Turn the spiny around when it reaches the move distance
        if (movingLeft && transform.position.x <= startPosition.x - moveDistance)
        {
            Flip();
        }
        else if (!movingLeft && transform.position.x >= startPosition.x + moveDistance)
        {
            Flip();
        }

        // Spawn spikes at intervals
        if (Time.time >= nextSpikeSpawnTime)
        {
            SpawnSpinySpike();
            nextSpikeSpawnTime = Time.time + spikeSpawnInterval + Random.Range(-randomOffset, randomOffset);
        }
        // Spawn shells at intervals
        if (Time.time >= nextShellSpawnTime)
        {
            StartCoroutine(SpawnShellsWithDelay());
            nextShellSpawnTime = Time.time + shellSpawnInterval + Random.Range(-randomOffset, randomOffset);
        }
    }

    private void SpawnSpinySpike()
    {
        if (!isVisible) return; // Only spawn if the spiny is visible on screen
        Vector3 spawnPosition = transform.position + new Vector3(-2, -0.5f, 0); // Adjust spawn position as needed
        GameObject spinySpike = Instantiate(spinySpikePrefab, spawnPosition, Quaternion.identity);
        audioSource.PlayOneShot(spinySpikeSound);
    }

    private IEnumerator SpawnShellsWithDelay()
    {
        if (!isVisible) yield break; // Only spawn if the spiny is visible on screen
        for (int i = 0; i < 3; i++)
        {
            Vector3 spawnPosition = transform.position + new Vector3(0, 1, 0);
            GameObject spinyShell = Instantiate(spinyShellPrefab, spawnPosition, Quaternion.identity);
            audioSource.PlayOneShot(spinyShellSound);
            yield return new WaitForSeconds(0.15f); // Delay between each shell
        }
    }
}