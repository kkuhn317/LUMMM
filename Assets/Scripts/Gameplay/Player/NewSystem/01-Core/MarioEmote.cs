/// <summary>
/// All named emote states Mario can be in.
/// Each value drives both a sprite library swap (via SpriteSwapArea)
/// and an animator bool (via MarioAnimatorController).
///
/// To add a new emote:
/// 1. Add the value here
/// 2. Add a case in MarioAnimatorController.OnEmoteStarted
/// 3. Add an entry in each character's CharacterData.EmoteLibraries
/// </summary>
public enum MarioEmote
{
    Normal,
    Worried,     // Mild reaction — e.g. sees something approaching
    Scared,      // Strong reaction — e.g. ambush, sudden danger
    Celebrating, // Yeah animation — plays once then returns to Normal
    Angry,       // When Mario ground pounds the giant thwomp
}