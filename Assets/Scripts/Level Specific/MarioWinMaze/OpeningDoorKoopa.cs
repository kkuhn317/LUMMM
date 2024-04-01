using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OpeningDoorKoopa : KoopaController
{
    public GameObject door;
    public UnityEvent onDoorHit;

    protected override void onTouchWall(GameObject other)
    {
        base.onTouchWall(other);

        if (state == EnemyState.movingShell)
        {
            // if the other object is the door, then open it
            if (other == door)
            {
                onDoorHit.Invoke();
            }
        }
    }
}
