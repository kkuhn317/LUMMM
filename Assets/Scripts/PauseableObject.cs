using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseableObject : MonoBehaviour
{

    private ObjectPhysics.ObjectMovement oldMovement;
    private ObjectPhysics objectPhysics;

    private void Start()
    {
        // Ensure GameManager.Instance is not null before registering the PauseableObject
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterPauseableObject(this);
            objectPhysics = GetComponent<ObjectPhysics>();
        }
        else
        {
            Debug.LogError("GameManager.Instance is null!");
        }
    }

    private void OnDestroy()
    {
        // Ensure GameManager.Instance is not null before unregistering the PauseableObject
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterPauseableObject(this);
        }
    }

    public void Pause()
    {
        oldMovement = objectPhysics.movement;
        objectPhysics.movement = ObjectPhysics.ObjectMovement.still;
    }

    public void Resume()
    {
        objectPhysics.movement = oldMovement;
    }

    public void FallStraightDown()
    {
        objectPhysics.velocity = new Vector2(0, 0);
        
        objectPhysics.floorMask = 0;
        objectPhysics.wallMask = 0;
        objectPhysics.movement = ObjectPhysics.ObjectMovement.sliding;
    }
}
