using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BigSpikeWOS : MonoBehaviour
{
    private bool rose = false;

    // check if player enters the trigger
    void OnTriggerEnter2D(Collider2D other)
    {
        if (rose)
        {
            return;
        }

        if (other.gameObject.CompareTag("Player"))
        {
            GetComponent<Animator>().SetTrigger("Rise");
            rose = true;
            // TODO: sound
        }
    }
}
