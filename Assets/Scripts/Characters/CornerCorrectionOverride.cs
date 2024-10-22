using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// TODO: GET RID OF THIS SCRIPT WHEN ITS NOT NEEDED ANYMORE
// MOST LIKELY WHEN HORIZONTAL CORNER CORRECTION IS IMPLEMENTED

public class CornerCorrectionOverride : MonoBehaviour
{

    public bool enableCornerCorrection = false; // false: disable corner correction, true: enable corner correction


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
            playerscript.doCornerCorrection = enableCornerCorrection;
        }
    }

    // check if player exits the trigger
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            MarioMovement playerscript = other.gameObject.GetComponent<MarioMovement>();
            playerscript.doCornerCorrection = !enableCornerCorrection;
        }
    }


}
