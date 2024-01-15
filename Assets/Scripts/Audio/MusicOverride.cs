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
        GameManager.Instance.OverrideMusic(this.gameObject);
    }

    public void stopPlayingAfterTime(float time) {
        Invoke("stopPlaying", time);
    }

    public void stopPlaying() {
        GameManager.Instance.ResumeMusic(this.gameObject);
        Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
