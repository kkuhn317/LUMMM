using System.Collections;
using UnityEngine;


// TODO: not sure if this is used. Remove if not needed
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
        if (spinePrefab == null)
        {
            return;
        }
        
        for (int i = 0; i < numberOfSpines; i++)
        {
            Vector3 spawnPosition = transform.position + Vector3.right * i * spineOffset;
            GameObject newSpine = Instantiate(spinePrefab, spawnPosition, Quaternion.identity);
            newSpine.transform.parent = transform; // Set the SpineController as the parent of the new spine
        }
    }
}
