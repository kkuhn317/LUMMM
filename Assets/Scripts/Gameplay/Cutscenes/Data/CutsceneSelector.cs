// CutsceneSelector.cs
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class CutsceneSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayableDirector director;

    [Header("Available Cutscenes for this trigger")]
    [SerializeField] private CutsceneDefinition[] candidates;

    public CutsceneDefinition Select(in CutsceneContext ctx)
    {
        CutsceneDefinition best = null;

        foreach (var def in candidates)
        {
            if (def == null || def.timeline == null)
                continue;

            if (!def.Matches(ctx))
                continue;

            if (best == null || def.priority > best.priority)
            {
                best = def;
            }
        }

        return best;
    }

    public IEnumerator PlaySelectedCutscene(CutsceneContext ctx)
    {
        var def = Select(ctx);
        if (def == null)
        {
            Debug.LogWarning("CutsceneSelector: No matching cutscene found.");
            yield break;
        }

        if (director == null)
            director = GetComponent<PlayableDirector>();
        if (director == null)
        {
            Debug.LogWarning("CutsceneSelector: No PlayableDirector assigned.");
            yield break;
        }

        director.playableAsset = def.timeline;

        var levelFlow = GameManagerRefactored.Instance != null
            ? GameManagerRefactored.Instance.GetSystem<LevelFlowController>()
            : FindObjectOfType<LevelFlowController>(true);

        if (levelFlow == null)
        {
            Debug.LogError("CutsceneSelector: LevelFlowController not found.");
            yield break;
        }

        // delay (you were passing 0f before)
        yield return null;

        // Use your existing GameManager coroutine
        /*yield return GameManager.Instance.TriggerEndLevelCutscene(
            director,
            0f,                // extra delay before cutscene
            def.cutsceneTime,
            def.destroyPlayers,
            def.stopMusic,
            def.hideUI
        );*/

        levelFlow.TriggerCutsceneEnding(
            director,
            def.cutsceneTime,
            def.destroyPlayers,
            def.stopMusic
        );
    }
}