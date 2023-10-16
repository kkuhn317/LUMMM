using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicChangeArea : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            GetComponent<AudioSource>().Play();
            GameManager.Instance.OverrideMusic(gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            GameManager.Instance.ResumeMusic(gameObject);
            GetComponent<AudioSource>().Stop();
        }
    }
}
