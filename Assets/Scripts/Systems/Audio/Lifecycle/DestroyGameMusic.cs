using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyGameMusic : MonoBehaviour
{
    private void Start()
    {
        DestroyGameMusicObjects();
    }

    public static void DestroyGameMusicObjects()
    {
        GameObject[] musicObjs = GameObject.FindGameObjectsWithTag("GameMusic");
        foreach (GameObject musicObj in musicObjs)
        {
            Destroy(musicObj);
        }
    }
}
