using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinyShellAttack : EnemyAI
{
    public float rotationSpeed = 200f; // Speed of rotation

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        // Rotate the spiny shell around its center
        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
    }

    // When leaving the screen, destroy the spiny shell
    public override void OnBecameInvisible()
    {
        base.OnBecameInvisible();
        Destroy(gameObject);
    }
}
