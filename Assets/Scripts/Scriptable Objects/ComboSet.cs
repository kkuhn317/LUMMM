using UnityEngine;

[CreateAssetMenu(menuName = "Combos/Combo Set")]
public class ComboSet : ScriptableObject
{
    public ComboStep[] steps;
}