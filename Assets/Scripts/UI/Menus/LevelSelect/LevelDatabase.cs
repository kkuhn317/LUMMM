using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "Game/Level Database")]
public class LevelDatabase : ScriptableObject
{
    [SerializeField] private LevelInfo[] levels;

    public bool TryGetByScene(string sceneName, out LevelInfo level)
    {
        if (levels != null)
        {
            for (int i = 0; i < levels.Length; i++)
            {
                var li = levels[i];
                if (li != null && li.levelScene == sceneName)
                {
                    level = li;
                    return true;
                }
            }
        }

        level = null;
        return false;
    }
}