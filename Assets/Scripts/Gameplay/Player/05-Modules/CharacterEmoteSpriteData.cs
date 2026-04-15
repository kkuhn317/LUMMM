using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

/// <summary>
/// One asset per character (Mario, Luigi, etc.).
/// Maps each MarioEmote to the SpriteLibraryAsset that represents it
/// for that specific character.
///
/// Setup:
/// 1. Create asset: right-click → Game → Character Emote Sprite Data
/// 2. Fill in one entry per emote you use (Normal is the baseline).
/// 3. Assign this asset on the character's MarioPowerup component
///    in the CharacterEmoteSpriteData field.
///
/// SpriteSwapArea reads this at runtime — no per-zone character mapping needed.
/// </summary>
[CreateAssetMenu(fileName = "CharacterEmoteSpriteData", menuName = "Game/Character Emote Sprite Data")]
public class CharacterEmoteSpriteData : ScriptableObject
{
    [System.Serializable]
    public class EmoteEntry
    {
        public MarioEmote emote;
        public SpriteLibraryAsset library;
    }

    [Tooltip("One entry per emote. Normal should always be present.")]
    public List<EmoteEntry> entries = new();

    public SpriteLibraryAsset GetLibrary(MarioEmote emote)
    {
        foreach (var e in entries)
            if (e.emote == emote) return e.library;

        // Fallback to Normal if requested emote isn't defined
        foreach (var e in entries)
            if (e.emote == MarioEmote.Normal) return e.library;

        return null;
    }
}