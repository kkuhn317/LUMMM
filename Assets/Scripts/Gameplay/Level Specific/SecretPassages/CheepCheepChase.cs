using System.Collections;
using UnityEngine;

public class CheepCheepChase : MonoBehaviour
{
    private Transform target;
    public CheepCheep cheepCheepScript;
    private KeyInventorySystem keyInventory;

    private void CacheSystems()
    {
        if (keyInventory != null) return;

        if (GameManagerRefactored.Instance != null)
            keyInventory = GameManagerRefactored.Instance.GetSystem<KeyInventorySystem>();

        if (keyInventory == null)
            keyInventory = FindObjectOfType<KeyInventorySystem>(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            target = other.transform;

            // check if the player has a key (using GameManager) and if the player is within the follow radius
            bool playerHasKey = CheckForKey();
            if (playerHasKey)
            {
                // Player has a key, so don't start chasing
                Debug.Log("Player has the key, CheepCheep won't chase.");
                return;
            }

            // notify the CheepCheep to start chasing if the player doesn't have the key
            cheepCheepScript.StartChasing(target);
        }
    }

    // Optional: Trigger when the player exits the chase area
    /*private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            target = null;
            cheepCheepScript.StopChasing();
        }
    }*/

    // Check if the player has a key
    private bool CheckForKey()
    {
        /*if (GameManager.Instance.keys.Count > 0)
        {
            return true; // player has a key
        }
        return false; // player does not have a key*/

        if (keyInventory == null) CacheSystems();
        return keyInventory != null && keyInventory.HasKey();
    }
}