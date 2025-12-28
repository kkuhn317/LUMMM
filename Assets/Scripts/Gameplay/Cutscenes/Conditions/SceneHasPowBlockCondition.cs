using UnityEngine;

[CreateAssetMenu(menuName = "Cutscenes/Conditions/Scene Doesnt Have POWBlock")]
public class SceneHasPowBlockCondition : SceneHasComponentConditionBase
{
    protected override System.Type ComponentType => typeof(POWBlock);
}