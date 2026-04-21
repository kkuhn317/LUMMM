using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Legacy temporary world/music override helper.
// Do NOT use auto-register for player-owned music like Star.
public class MusicOverride : MonoBehaviour
{
    public float timeToStopPlaying = 1f;

    [SerializeField] private bool autoRegisterOnStart = true;

    void Start()
    {
        if (!autoRegisterOnStart) return;
        if (MusicManager.Instance == null) return;

        MusicManager.Instance.PushMusicOverride(gameObject, MusicManager.MusicStartMode.Restart);
    }

    public void stopPlayingAfterTime(float time)
    {
        Invoke(nameof(stopPlaying), time);
    }

    public void stopPlaying()
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.PopMusicOverride(gameObject, MusicManager.MusicStartMode.Continue);

        Destroy(gameObject);
    }
}