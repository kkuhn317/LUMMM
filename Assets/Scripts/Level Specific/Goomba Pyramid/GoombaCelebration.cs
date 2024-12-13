using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class EnemiesGroup
{
    public List<EnemyAI> enemiesAI; // Group of enemies
    public List<GameObject> parents; // Parent GameObjects (whose SpriteRenderers will be disabled)
    public List<GameObject> childrenToActivate; // Child GameObjects to activate

    public UnityEvent onThisGroupHappen; // Event triggered when this group is processed
}

public class GoombaCelebration : MonoBehaviour
{
    [Header("Enemy Management")]
    public List<EnemiesGroup> enemyGroups = new List<EnemiesGroup>();

    [Header("Group Activation Timing")]
    public float delayBeforeDeactivatingGoombas = 0.1f;
    public float delayBeforeGroupActivation = 1f; // Timer before starting group activations
    public float delayBetweenGroups = 0.25f;

    [Header("Events")]
    public UnityEvent onCelebrationStart;
    public UnityEvent onCelebrationEnd;

    public void TriggerCelebration()
    {
        StartCoroutine(DisableAllEnemiesAndStartCelebration());
    }

    private IEnumerator DisableAllEnemiesAndStartCelebration()
    {
        yield return new WaitForSeconds(delayBeforeDeactivatingGoombas);
        yield return DisableAllEnemyScripts();
        yield return new WaitForSeconds(delayBeforeGroupActivation);
        StartCoroutine(CelebrationSequence());
    }

    private IEnumerator DisableAllEnemyScripts()
{
    foreach (EnemiesGroup group in enemyGroups)
    {
        for (int i = group.enemiesAI.Count - 1; i >= 0; i--)
        {
            EnemyAI enemy = group.enemiesAI[i];

            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                group.enemiesAI.RemoveAt(i);
                continue;
            }

            // Skip Goombas in the Crushed state
            if (enemy is Goomba goomba && goomba.state == Goomba.EnemyState.crushed)
            {
                continue;
            }

            // Disable active enemies
            enemy.enabled = false;
            yield return null;
        }
    }
}

    private IEnumerator CelebrationSequence()
    {
        onCelebrationStart.Invoke();

        foreach (EnemiesGroup group in enemyGroups)
        {
            ProcessEnemyGroup(group);
            yield return new WaitForSeconds(delayBetweenGroups);
        }

        onCelebrationEnd.Invoke();
    }

    private void ProcessEnemyGroup(EnemiesGroup group)
    {
        group.onThisGroupHappen.Invoke();

        foreach (GameObject parent in group.parents)
        {
            if (parent != null)
            {
                SpriteRenderer spriteRenderer = parent.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = false;
                }
            }
        }

        foreach (GameObject child in group.childrenToActivate)
        {
            if (child != null)
            {
                child.SetActive(true);
            }
        }
    }
}