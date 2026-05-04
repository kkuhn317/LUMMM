/// <summary>
/// Interface for solid objects that react to being ground pounded
/// (e.g. a Giant Thwomp after it has fallen back down).
/// </summary>
public interface IGroundPoundable
{
    void OnGroundPound(MarioCore player);
}
