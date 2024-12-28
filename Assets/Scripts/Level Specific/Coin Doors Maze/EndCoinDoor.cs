using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class EndCoinDoor : CoinDoor
{
    private bool coinTouchedDoor = false;

    public GameObject pushCoin; // The coin that can be used to open the door
    public float maxPlayerCoinDistance = 0.85f; // Maximum distance between player and coin to open the door

    protected override void Start()
    {
        base.Start();
    }

    protected override bool CheckForKey()
    {
        // If the push coin is within the door range, the door can be opened
        return coinTouchedDoor;
    }

    protected override void SubtractOneCoin()
    {
        if (pushCoin != null)
        {
            // Detach only the player if they are a child of pushCoin
            foreach (Transform child in pushCoin.transform)
            {
                if (child.gameObject.CompareTag("Player")) // Assuming the player has the "Player" tag
                {
                    child.parent = null; // Detach the player
                }
            }

            Destroy(pushCoin); // Safely destroy the pushCoin
        }
    }

    protected override bool PlayerAtDoor(MarioMovement playerScript)
    {
        // Check if the coin is near the door
        if (coinTouchedDoor && pushCoin != null)
        {
            // Ensure the player is near the coin as well
            float distanceToCoin = Vector2.Distance(player.transform.position, pushCoin.transform.position);
            Debug.Log($"Player to coin distance: {distanceToCoin}");

            if (distanceToCoin <= maxPlayerCoinDistance)
            {
                return true;
            }
        }

        // Fallback to normal player proximity check
        return playerInRange && (!mustBeStanding || playerScript.onGround);
    }

    protected override void Close()
    {
        if (destination)
        {
            base.Close();
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == pushCoin)
        {
            coinTouchedDoor = true;
        }
        base.OnTriggerEnter2D(other);
    }
}