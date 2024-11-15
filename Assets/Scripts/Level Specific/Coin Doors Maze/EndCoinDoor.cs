using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class EndCoinDoor : CoinDoor
{
    private bool coinTouchedDoor = false;

    public GameObject pushCoin;     // The coin that can be used to open the door

    protected override void Start()
    {
        base.Start();
    }

    protected override bool CheckForKey() {
        // if the push coin is within 1 unit of the door, the door can be opened
        return coinTouchedDoor;
    }

    protected override void SubtractOneCoin()
    {
        Destroy(pushCoin);
    }

    protected override bool PlayerAtDoor(MarioMovement playerScript)
    {
        return CheckForKey() || (playerInRange && (!mustBeStanding || playerScript.onGround));
    }

    protected override void Close()
    {
        if (destination)
        {
            base.Close();
        }
    }

    protected override void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject == pushCoin) {
            coinTouchedDoor = true;
        }
        base.OnTriggerEnter2D(other);
    }
}
