using UnityEngine;

/// <summary>
/// Identifies a cause of death — completely open-ended.
/// Create assets via: right-click → Game/Death Cause
///
/// Examples: DC_Fire, DC_Enemy, DC_Fall, DC_Crush, DC_Default
/// </summary>
[CreateAssetMenu(fileName = "DeathCause", menuName = "Game/Death Cause")]
public class DeathCause : ScriptableObject { }
