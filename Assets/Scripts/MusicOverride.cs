using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicOverride : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameObject[] musics = GameObject.FindGameObjectsWithTag("Music");
        foreach(GameObject music in musics) {
            music.GetComponent<AudioSource>().mute = true;
        }
    }

    public void stopPlayingAfterTime(float time) {
        Invoke("stopPlaying", time);
    }

    public void stopPlaying() {
        GameObject[] musics = GameObject.FindGameObjectsWithTag("Music");
        foreach(GameObject music in musics) {
            music.GetComponent<AudioSource>().mute = false;
        }
        Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
