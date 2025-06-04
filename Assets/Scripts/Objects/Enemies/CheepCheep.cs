using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheepCheep : EnemyAI
{
    [Header("Cheep Cheep")]
    public float bobSpeed = 2f;
    public float bobHeight = 0.5f;

    protected override void Update()
    {
        base.Update();

        // Update vertical speed so it bobs
        velocity.y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
    }
}
