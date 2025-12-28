using UnityEngine;

public class MusicDontDestroy : MonoBehaviour
{
    private GameObject[] music;

    void Awake()
    {
        DontDestroyOnLoad(transform.gameObject);
    }

    void Start()
    {
        music = GameObject.FindGameObjectsWithTag(this.tag);
        if (music.Length <= 1) return;

        GameObject keep = null;
        GameObject kill = null;

        foreach (var m in music)
        {
            if (m.scene.name == "DontDestroyOnLoad")
                keep = m;
            else
                kill = m;
        }

        // Fallback if both are in same scene for some reason
        if (keep == null) keep = music[0];
        if (kill == null) kill = music[1];

        // If the kept one is muted (override active), mute the new one too so it doesn't blip
        if (IsMusicMuted(keep))
            SetMusicMuted(kill, true);

        if (MusicManager.Instance != null)
            MusicManager.Instance.SetNewMainMusic(keep);

        Destroy(kill);
    }

    private bool IsMusicMuted(GameObject musicObj)
    {
        if (!musicObj) return false;

        var looper = musicObj.GetComponent<IntroLoopMusicPlayer>();
        if (looper != null)
            return looper.source != null && looper.source.mute;

        var src = musicObj.GetComponent<AudioSource>();
        return src != null && src.mute;
    }

    private void SetMusicMuted(GameObject musicObj, bool muted)
    {
        if (!musicObj) return;

        var looper = musicObj.GetComponent<IntroLoopMusicPlayer>();
        if (looper != null)
        {
            looper.SetMuted(muted);
            return;
        }

        var src = musicObj.GetComponent<AudioSource>();
        if (src != null) src.mute = muted;
    }
}
