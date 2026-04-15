/// <summary>
/// Defines how Mario holds a carried object.
/// Extracted from MarioMovement (was a nested enum) so MarioCarry
/// and other scripts can reference it without depending on MarioMovement.
/// </summary>
public enum CarryMethod
{
    inFront,
    onHand,
    Normal = onHand   // Alias used by MarioCarry inspector default
}
