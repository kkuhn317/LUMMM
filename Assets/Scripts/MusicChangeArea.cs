using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicChangeArea : MonoBehaviour
{
    public bool restartOldMusicOnExit = false;
    public bool permanent = false;
    bool entered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (entered) return;

        if (other.gameObject.tag == "Player")
        {
            entered = true;
            GetComponent<AudioSource>().Play();
            GameManager.Instance.OverrideMusic(gameObject);       
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (permanent) return;
        if (other.gameObject.tag == "Player")
        {
            entered = false;
            GameManager.Instance.ResumeMusic(gameObject);
            if (restartOldMusicOnExit)
                GameManager.Instance.RestartMusic();
            GetComponent<AudioSource>().Stop();      
        }
    }
}
