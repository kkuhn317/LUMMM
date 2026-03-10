using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Disables moving platform momentum when Mario is inside the area
public class MomentumOverride : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // check if player enters the trigger
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            MarioMovement playerscript = other.gameObject.GetComponent<MarioMovement>();
            playerscript.doMovingPlatformMomentum = false;
        }
    }

    // check if player exits the trigger
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            MarioMovement playerscript = other.gameObject.GetComponent<MarioMovement>();
            playerscript.doMovingPlatformMomentum = true;
        }
    }


}
