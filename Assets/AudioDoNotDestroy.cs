using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioDoNotDestroy : MonoBehaviour
{
    private void Awake()
    {
        DestroyDuplicateGameMusicObjects();
        DontDestroyOnLoad(this.gameObject);
    }

    private void DestroyDuplicateGameMusicObjects()
    {
        GameObject[] musicObjs = GameObject.FindGameObjectsWithTag("GameMusic");
        foreach (GameObject musicObj in musicObjs)
        {
            // Make sure not to destroy the current game object (this.gameObject)
            if (musicObj != this.gameObject)
            {
                Destroy(musicObj);
            }
        }
    }
}
