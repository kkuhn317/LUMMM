using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class GoombaGroup
{
    public List<Goomba> enemies; // Group of Goomba enemies
    public List<GameObject> parents; // Parent GameObjects (whose SpriteRenderers will be disabled)
    public List<GameObject> childrenToActivate; // Child GameObjects to activate

    public UnityEvent onThisGroupHappen; // Event triggered when this group is processed
}

public class GoombaCelebration : MonoBehaviour
{
    [Header("Goomba Management")]
    public List<GoombaGroup> goombaGroups = new List<GoombaGroup>();

    [Header("Group Activation Timing")]
    public float delayBeforeDeactivatingGoombas = 0.1f;
    public float delayBeforeGroupActivation = 1f; // Timer before starting group activations
    public float delayBetweenGroups = 0.25f;

    [Header("Events")]
    public UnityEvent onCelebrationStart;
    public UnityEvent onCelebrationEnd;

    // Public method to trigger the celebration
    public void TriggerCelebration()
    {
        StartCoroutine(DisableAllGoombasAndStartCelebration());
    }

    private IEnumerator DisableAllGoombasAndStartCelebration()
    {
        // Step 1: Wait a little before deactivating all goomba scripts
        yield return new WaitForSeconds(delayBeforeDeactivatingGoombas);
        
        // Step 2: Disable all Goomba scripts
        yield return DisableAllGoombaScripts();

        // Step 3: Wait before starting the group activation sequence
        yield return new WaitForSeconds(delayBeforeGroupActivation);

        // Step 4: Proceed with the celebration sequence
        StartCoroutine(CelebrationSequence());
    }

    private IEnumerator DisableAllGoombaScripts()
    {
        foreach (GoombaGroup group in goombaGroups)
        {
            foreach (Goomba goomba in group.enemies)
            {
                if (goomba != null)
                {
                    goomba.enabled = false; // Disable Goomba script
                    yield return null; // Wait for the next frame (optional for smoother processing)
                }
            }
        }
    }

    private IEnumerator CelebrationSequence()
    {
        // Invoke the start of the celebration
        onCelebrationStart.Invoke();

        // Activate each group in sequence
        foreach (GoombaGroup group in goombaGroups)
        {
            ProcessGoombaGroup(group);
            yield return new WaitForSeconds(delayBetweenGroups);
        }

        // Invoke the end of the celebration
        onCelebrationEnd.Invoke();
    }

    private void ProcessGoombaGroup(GoombaGroup group)
    {
        // Invoke the group-specific UnityEvent
        group.onThisGroupHappen.Invoke();

        // Disable the SpriteRenderers of all parent GameObjects
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

        // Activate all child GameObjects
        foreach (GameObject child in group.childrenToActivate)
        {
            if (child != null)
            {
                child.SetActive(true);
            }
        }
    }
}
