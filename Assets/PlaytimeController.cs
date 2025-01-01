using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;  // Make sure to include this for UnityEvent

public class PlaytimeController : EnemyAI
{
    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }

    public override void KnockAway(bool direction, bool sound = true, KnockAwayType? type = null, Vector2? velocity = null)
    {
        base.KnockAway(false, sound, type, velocity);
        releaseItem();
    }
}