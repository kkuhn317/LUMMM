using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicChangeArea : MonoBehaviour
{
    public bool restartOldMusicOnExit = false;
    public bool permanent = false;
    bool entered = false;

    public bool usePosition = false; // if true, use position instead of trigger (fixes issue when mario goes in a pipe)

    private Collider2D col;

    void Start() {
        col = GetComponent<Collider2D>();
    }

    void StartNewMusic()
    {
        GetComponent<AudioSource>().Play();
        GameManager.Instance.OverrideMusic(gameObject);
    }

    void ResumeOldMusic()
    {
        GameManager.Instance.ResumeMusic(gameObject);
        if (restartOldMusicOnExit)
            GameManager.Instance.RestartMusic();
        GetComponent<AudioSource>().Stop();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (entered) return;

        if (usePosition) return;

        if (other.gameObject.tag == "Player")
        {
            entered = true;
            StartNewMusic();     
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (permanent) return;
        if (usePosition) return;
        if (other.gameObject.tag == "Player")
        {
            entered = false;
            ResumeOldMusic();
        }
    }

    /* For usePosition = true */

    GameObject player;  // dont use this directly, use getPlayer() instead

    private GameObject getPlayer()
    {
        if (player == null)
        {
            // TODO: support multiple players
            MarioMovement playerscript = GameManager.Instance.GetPlayer(0);
            if (playerscript != null)
            {
                player = playerscript.gameObject;
            } else
            {
                return null;
            }
        }
        return player;
    }

    bool PlayerIsInArea()
    {
        GameObject player = getPlayer();
        if (player == null) return false;
        Vector2 playerPos = player.transform.position;
        Vector2 triggerPos = transform.position;
        return col.OverlapPoint(playerPos);
    }

    void Update()
    {
        if (usePosition)
        {
            if (PlayerIsInArea())
            {
                if (!entered)
                {
                    entered = true;
                    StartNewMusic();
                }
            }
            else
            {
                if (entered)
                {
                    entered = false;
                    ResumeOldMusic();
                }
            }
        }
    }
}
