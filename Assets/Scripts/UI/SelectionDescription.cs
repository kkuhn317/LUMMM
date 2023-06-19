using UnityEngine;

[CreateAssetMenu(fileName = "Select Level", menuName = "Selection Menu/item")]
public class SelectionDescription : ScriptableObject
{
    public string levelName;
    public string videoYear;
    public string videoLink;
    [Multiline(10)]
    public string levelDescription;
    
}
