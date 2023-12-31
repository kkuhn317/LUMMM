using UnityEngine;

[CreateAssetMenu(fileName = "Select Level", menuName = "Selection Menu/item")]
public class LevelInfo : ScriptableObject
{
    public string levelID;
    public string levelScene;
    public string levelName;
    public string videoYear;
    public string videoLink;
    [Multiline(10)]
    public string levelDescription;
    public int lives;
    
}
