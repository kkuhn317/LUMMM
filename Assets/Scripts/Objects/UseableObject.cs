using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class UseableObject : MonoBehaviour
{
    public bool hasUsed = false;
    public GameObject keyActivate;
    public bool resettable = true;  // using again reset the object
    public bool reuseable = false;  // "hasUsed" is not set to false after use
    public bool playerInArea = false;

    public virtual void Use(MarioMovement player)
    {
        print("UseableObject Use");
        if (!CanUseObject())
        {
            return;
        }

        if (!resettable && !reuseable) {
            player.RemoveUseableObject(this);
        }

        if (!hasUsed)
        {
            if (!reuseable)
            {
                hasUsed = true;
            }
            
            if (!resettable && !reuseable) {
                if (keyActivate != null)
                keyActivate.SetActive(false);
            }
            UseObject();
        }
        else if (resettable)
        {
            hasUsed = false;
            if (keyActivate != null)
            keyActivate.SetActive(true);
            ResetObject();
        }

    }

    protected virtual bool CanUseObject()
    {
        return true;    // Override for additional conditions
    }

    protected virtual void UseObject()
    {
        // Override this method in the child class
    }

    protected virtual void ResetObject()
    {
        // Override this method in the child class
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<MarioMovement>().AddUseableObject(this);
            playerInArea = true;

            // Activate the keyActivate when the player enters the trigger zone and haven't pull the level 
            if (keyActivate != null && (!hasUsed || resettable) && CanUseObject()) {
                keyActivate.SetActive(true);
            } 
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<MarioMovement>().RemoveUseableObject(this);
            playerInArea = false;

            // Deactivate the keyActivate when the player exits the trigger zone
            if (keyActivate != null)
            {
                keyActivate.SetActive(false);
            }
        }
    }
    
}
