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
    private PlayerRegistry playerRegistry;
    private readonly HashSet<Collider2D> playersInside = new HashSet<Collider2D>();

    void Start() {
        col = GetComponent<Collider2D>();
        CacheRegistry();
    }

    private void CacheRegistry()
    {
        if (GameManagerRefactored.Instance != null)
            playerRegistry = GameManagerRefactored.Instance.GetSystem<PlayerRegistry>();

        if (playerRegistry == null)
            playerRegistry = FindObjectOfType<PlayerRegistry>(true);
    }

    void StartNewMusic()
    {
        GetComponent<AudioSource>().Play();
        MusicManager.Instance.PushMusicOverride(gameObject, MusicManager.MusicStartMode.Restart);
    }

    void ResumeOldMusic()
    {
        // If you want the old music to restart when exiting the area, use Restart.
        var mode = restartOldMusicOnExit
            ? MusicManager.MusicStartMode.Restart
            : MusicManager.MusicStartMode.Continue;

        MusicManager.Instance.PopMusicOverride(gameObject, mode);
        GetComponent<AudioSource>().Stop();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // if (entered) return;

        if (!other.CompareTag("Player"))
            return;

        if (usePosition) return;

        // Track that THIS player is inside
        playersInside.Add(other);

        /*if (other.gameObject.tag == "Player")
        {
            entered = true;
            StartNewMusic();     
        }*/

        // Only start when the FIRST player enters
        if (!entered)
        {
            entered = true;
            StartNewMusic();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (permanent) return;
        if (usePosition) return;

        /* (other.gameObject.tag == "Player")
        {
            entered = false;
            ResumeOldMusic();
        }*/

        // Remove THIS player
        playersInside.Remove(other);

        // Only stop when the LAST player leaves
        if (entered && playersInside.Count == 0)
        {
            entered = false;
            ResumeOldMusic();
        }
    }

    /* For usePosition = true */

    GameObject player;  // dont use this directly, use getPlayer() instead

    private GameObject getPlayer()
    {
        /*if (player == null)
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
        return player;*/

        if (playerRegistry == null) CacheRegistry();
        if (playerRegistry == null) return null;

        // Keep the original intent: "player 0" (usually Mario / P1)
        var p0 = playerRegistry.GetPlayer(0);
        if (p0 == null) return null;

        // Cache it like before, but sourced from registry
        if (player == null || player.gameObject == null)
            player = p0.gameObject;

        return player;
    }

    bool PlayerIsInArea()
    {
        /*GameObject player = getPlayer();
        if (player == null) return false;
        Vector2 playerPos = player.transform.position;
        Vector2 triggerPos = transform.position;
        return col.OverlapPoint(playerPos);*/

        if (col == null) return false;

        if (playerRegistry == null) CacheRegistry();
        if (playerRegistry == null) return false;

        // if ANY PLAYER is inside, the area is considered entered
        var players = playerRegistry.GetAllPlayerObjects();
        if (players == null || players.Length == 0) return false;

        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p == null) continue;

            Vector2 playerPos = p.transform.position;
            if (col.OverlapPoint(playerPos))
                return true;
        }

        return false;
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