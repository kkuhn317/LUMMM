using UnityEngine;

public abstract class CutsceneCondition : ScriptableObject
{
    public abstract bool IsMet(in CutsceneContext ctx);
}