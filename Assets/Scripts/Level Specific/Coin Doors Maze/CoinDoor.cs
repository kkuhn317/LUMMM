using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class CoinDoor : Door
{

    public int coinsRequired = 1;

    private bool playerInRange = false;

    protected override bool CheckForKey() {
        return GlobalVariables.coinCount >= coinsRequired;
    }

    protected override void SpendKey()
    {
        GameManager.Instance.RemoveCoins(coinsRequired);
    }

    protected override bool PlayerAtDoor(MarioMovement playerScript)
    {
        return playerInRange;
    }

    protected void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.CompareTag("Player")) {
            playerInRange = true;
        }
    }

    protected void OnTriggerExit2D(Collider2D other) {
        if (other.gameObject.CompareTag("Player")) {
            playerInRange = false;
        }
    }
}
