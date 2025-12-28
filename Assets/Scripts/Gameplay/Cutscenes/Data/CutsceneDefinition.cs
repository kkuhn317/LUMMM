// CutsceneDefinition.cs
using UnityEngine;
using UnityEngine.Playables;

[CreateAssetMenu(menuName = "Cutscenes/Cutscene Definition")]
public class CutsceneDefinition : ScriptableObject
{
    [Header("Timeline")]
    public PlayableAsset timeline;
    public float cutsceneTime = 10f;

    [Header("Behaviour")]
    public bool hideUI = true;
    public bool destroyPlayers = false;
    public bool stopMusic = false;

    [Header("Selection")]
    public int priority = 0;
    public CutsceneCondition[] conditions;

    public bool Matches(in CutsceneContext ctx)
    {
        if (conditions == null || conditions.Length == 0)
            return true;

        foreach (var cond in conditions)
        {
            if (cond == null) continue;
            if (!cond.IsMet(ctx))
                return false;
        }

        return true;
    }
}