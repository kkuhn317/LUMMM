/// <summary>
/// Interface for solid objects that react to being ground pounded
/// (e.g. a Giant Thwomp after it has fallen back down).
///
/// Signature changed from MarioMovement to MarioCore so implementors
/// have access to the full new API without a shim.
/// </summary>
public interface IGroundPoundable
{
    void OnGroundPound(MarioCore player);
}
