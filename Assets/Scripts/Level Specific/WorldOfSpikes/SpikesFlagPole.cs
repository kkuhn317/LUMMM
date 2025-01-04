using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpikesFlagPole : Flag, IDestructible
{
    private bool playerReachedFlag = false;
    public GameObject brokenFlag; 
    public GameObject deadMario;

    protected override void Start()
    {
        base.Start(); // Ensure base class Start logic is executed
        brokenFlag.SetActive(false);
        Debug.Log("SpikesFlagPole specific Start logic.");
    }

    protected override void Update()
    {
        base.Update(); // Ensure base class Update logic is executed
        Debug.Log("SpikesFlagPole specific Update logic.");
    }

    public void OnDestruction()
    {
        Debug.Log("SpikesFlagPole destroyed!");

        if (playerReachedFlag == true && deadMario != null) {
            deadMario.transform.position = cutsceneMario.transform.position; 
            Instantiate(deadMario, cutsceneMario.transform.position, Quaternion.identity);
        }
        brokenFlag.SetActive(true);
        // Destroy the object
        Destroy(gameObject);
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        base.OnTriggerEnter2D(other);  
        
        if (other.CompareTag("Player") && !playerReachedFlag)
        {
            playerReachedFlag = true;
            Debug.Log("Player reached the flagpole!");
        }
    }
}
