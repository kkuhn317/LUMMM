using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinySpike : EnemyAI
{
    protected override void onTouchWall(GameObject other)
    {
        base.onTouchWall(other);

        KnockAway(!movingLeft);
        gravity = 20;
    }
}
