using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// TODO: Likely unused. Remove if not needed
public class CollisionHandler : MonoBehaviour
{
    public GameObject objectToSpawn;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Instantiate(objectToSpawn, collision.transform.position, Quaternion.identity);
        }
    }
}
