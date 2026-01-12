using UnityEngine;

// This is just a container for "all levels" so the save system
// can know how many levels / coins exist.
public class SaveFileConfig : MonoBehaviour
{
    // Fill this from the Inspector with ALL your levels
    public LevelDefinition[] allLevels;

    public static SaveFileConfig Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}