#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class MissingScriptFinder
{
    [MenuItem("Tools/Find Missing Scripts in Scene")]
    static void FindMissingScripts()
    {
        GameObject[] go = GameObject.FindObjectsOfType<GameObject>();
        int missingCount = 0;

        foreach (GameObject g in go)
        {
            Component[] components = g.GetComponents<Component>();

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    Debug.LogWarning("Missing script in: " + g.name, g);
                    missingCount++;
                }
            }
        }

        Debug.Log("Total GameObjects with missing scripts: " + missingCount);
    }
}
#endif