using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// If an enemy doesn't have a sprite renderer in the same object as the script,
// The script will always be disabled!
// To fix this, use this script in the child object that has the sprite renderer
public class EnemyChildOnScreenDetect : MonoBehaviour
{
    private EnemyAI enemyAI;

    private void Start()
    {
        enemyAI = GetComponentInParent<EnemyAI>();
    }

    private void OnBecameVisible()
    {
        enemyAI.OnBecameVisible();
    }

    private void OnBecameInvisible()
    {
        enemyAI.OnBecameInvisible();
    }
}
