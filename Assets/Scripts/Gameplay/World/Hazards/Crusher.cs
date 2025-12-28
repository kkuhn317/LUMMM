using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

// Crusher that gets the crushing tag once it is low enough to the ground
public class Crusher : ObjectMovement
{
    public float crusherTagDistance = 1f; // Distance from the ground when to add the crushing tag
    string originalTag;

    protected override void Start()
    {
        base.Start();
        originalTag = gameObject.tag;
    }

    protected override void Update()
    {
        base.Update();

        // if the distance away from the where it is to final position is less than the crusherTagDistance, add the Crushing tag
        if (Vector3.Distance(transform.position, originalPosition) >= moveDistance - crusherTagDistance)
        {
            if (gameObject.tag != "Crushing")
            {
                gameObject.tag = "Crushing";
            }
        } else if (gameObject.tag == "Crushing")
        {
            gameObject.tag = originalTag;
        }
    } 
}
