using UnityEngine;

[System.Flags]
public enum MarioMoves
{
    None = 0,
    Cape = 1 << 0,       // 1
    Spin = 1 << 1, // 2
    WallJump = 1 << 2,  // 4
    GroundPound = 1 << 3,       // 8
    Crawl = 1 << 4       // 16
}

public enum MarioType
{
    Normal,
    Tiny,
    NES,
    None
}

[CreateAssetMenu(fileName = "Select Level", menuName = "Selection Menu/item")]
public class LevelInfo : ScriptableObject
{
    public string levelID;
    public string levelScene;
    public string videoYear;
    public string videoLink;
    public int lives;
    public bool beta;

    public MarioType marioType; // The type of Mario (Normal, Tiny, NES)
    public MarioMoves marioMoves; // The available moves for Mario (bitmask)
    public Sprite xImage;
    public Sprite conditionIconImage; // condition icon image for the intro screen
    [TextArea]
    public string condition; // Long string for level condition
    public AudioClip transitionAudio; // It'll play when the fade in happens
}
