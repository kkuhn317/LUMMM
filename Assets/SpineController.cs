using System.Collections;
using UnityEngine;

public class SpineController : MonoBehaviour
{
    public GameObject spinePrefab; // Reference to the spike prefab
    public int numberOfSpines = 7;
    public float spineOffset = 2.0f; // Offset between spines on the x-axis

    private void Start()
    {
        CreateSpines();
    }

    private void CreateSpines()
    {
        for (int i = 0; i < numberOfSpines; i++)
        {
            Vector3 spawnPosition = transform.position + Vector3.right * i * spineOffset;
            GameObject newSpine = Instantiate(spinePrefab, spawnPosition, Quaternion.identity);
            newSpine.transform.parent = transform; // Set the SpineController as the parent of the new spine
        }
    }
}
