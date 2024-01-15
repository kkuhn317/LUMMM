using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldenGoomba : MonoBehaviour
{
    public float activationProbability = 0.5f; // Probability of activation (0 to 1)
    public Vector2[] activationPositions; // Array of positions where the GameObject can be activated
    private Vector2 startPosition; // The starting position (position 0)
    [SerializeField] private bool isActive = false;
    private int currentPositionIndex = 0;

    public GameObject smokePrefab;

    void Start()
    {
        // Store the starting position (position 0)
        startPosition = activationPositions[0];

        // Start the activation coroutine
        StartCoroutine(ActivateRandomly());
    }

    IEnumerator ActivateRandomly()
    {
        float randomValue = Random.Range(0f, 1f); // Store random value 
        // It will generate only one random number per coroutine call

        while (true)
        {
            Debug.Log("Random value: " + randomValue);

            if (!isActive && randomValue > activationProbability) 
            {
                gameObject.SetActive(false);
                isActive = true;
            } else {
                yield break;
            }

            yield return null;
        }
    }

    // Change the position of the GameObject based on an index
    public void ChangePosition(int positionIndex)
    {
        if (positionIndex >= 0 && positionIndex < activationPositions.Length)
        {
            // Set the position based on the provided index
            Vector2 newPosition = activationPositions[positionIndex];

            // Instantiate the Smoke prefab at the old position
            Instantiate(smokePrefab, new Vector3(transform.position.x, transform.position.y, transform.position.z), Quaternion.identity);

            // Set the new position for the GoldenGoomba
            transform.position = new Vector3(newPosition.x, newPosition.y, transform.position.z);   

            // Set isActive to true to prevent further activations until the GameObject is deactivated
            isActive = true;

            // Update the currentPositionIndex
            currentPositionIndex = positionIndex;
        }
    }

    // Visualize activation positions in the scene using Gizmos
    private void OnDrawGizmosSelected()
    {
        if (activationPositions != null)
        {
            Gizmos.color = Color.red;
            foreach (Vector2 position in activationPositions)
            {
                Gizmos.DrawSphere(new Vector3(position.x, position.y, 0), 0.05f);
            }
        }
    }
}
