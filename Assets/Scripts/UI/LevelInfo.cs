using UnityEngine;

[CreateAssetMenu(fileName = "Select Level", menuName = "Selection Menu/item")]
public class LevelInfo : ScriptableObject
{
    public string levelID;
    public string levelScene;
    public string videoYear;
    public string videoLink;
    public int lives;
    public bool beta;
}
