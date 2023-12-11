using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyGroup
{
    public List<EnemyAI> enemies;
}

public class AmbushTrigger : MonoBehaviour
{
    public float delayBetweenGroups = 0.025f; // Adjust as needed
    public float delayBeforeAmbush = 1f; // Adjust as needed 

    public List<EnemyGroup> enemyGroups = new List<EnemyGroup>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(TriggerAmbush());
        }
    }

    private IEnumerator TriggerAmbush()
    {
        // Disable the collider
        GetComponent<Collider2D>().enabled = false;

        foreach (EnemyGroup group in enemyGroups)
        {
            if (group == null)
                yield break;

            // The delay only affects the first group
            if (group == enemyGroups[0])
                yield return new WaitForSeconds(delayBeforeAmbush);

            foreach (Goomba enemy in group.enemies)
            {
                if (enemy != null)
                    enemy.state = Goomba.EnemyState.ambush; // Each enemy on the list will change their state to Ambush
            }

            // Wait a little to do the same for the next group (if there is another)
            yield return new WaitForSeconds(delayBetweenGroups * Time.deltaTime);
        }
    }
}
