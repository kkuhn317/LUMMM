using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetBounceHeight : MonoBehaviour
{
    private Goomba goomba;

    private void Update()
    {
        goomba = GetComponent<Goomba>();

        if (goomba != null)
        {
            if (goomba.velocity.y > 1f)
            goomba.bounceHeight = 0f;
        }
    }
}
