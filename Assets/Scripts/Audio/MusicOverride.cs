using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// This is used on temporary music like the star music
public class MusicOverride : MonoBehaviour
{

    public float timeToStopPlaying = 1;

    // Start is called before the first frame update
    void Start()
    {
        MusicManager.Instance.PushMusicOverride(gameObject, MusicManager.MusicStartMode.Restart);
    }

    public void stopPlayingAfterTime(float time) {
        Invoke("stopPlaying", time);
    }

    public void stopPlaying()
    {
        MusicManager.Instance.PopMusicOverride(gameObject, MusicManager.MusicStartMode.Continue);
        Destroy(gameObject);
    }
}