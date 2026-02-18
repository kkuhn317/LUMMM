using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    public enum MusicStartMode
    {
        Continue, // do not restart; keep current playback position
        Restart   // restart intro -> loop
    }

    [Header("Debug")]
    [SerializeField] private GameObject mainMusic;
    [SerializeField] private GameObject currentlyPlayingMusic;

    private readonly List<GameObject> overrideStack = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // ensure main music is set
        if (mainMusic == null)
            mainMusic = gameObject;

        if (currentlyPlayingMusic == null)
            currentlyPlayingMusic = mainMusic;

        // If nothing is playing yet, start from intro.
        var looper = currentlyPlayingMusic.GetComponent<IntroLoopMusicPlayer>();
        var src = currentlyPlayingMusic.GetComponent<AudioSource>();

        bool isPlaying =
            (looper != null && looper.IsPlaying) ||
            (src != null && src.isPlaying);

        if (looper != null)
        {
            // If music is scheduled (intro->loop), don't restart
            if (!looper.IsPlaying)
                looper.EnsurePlaying();
        }
        else if (!isPlaying)
        {
            ClearMusicOverrides(MusicStartMode.Restart);
        }
    }


    public void RegisterMainMusic(GameObject candidate)
    {
        if (!candidate) return;

        // If no main yet, take it.
        if (mainMusic == null)
        {
            SetInitialMainMusic(candidate, MusicStartMode.Restart);
            return;
        }

        // If we already have a main, decide what to do with the new one:
        // - If main is muted (override active), mute the candidate so it doesn't blip
        if (IsMuted(mainMusic))
            SetMuted(candidate, true);

        // Always destroy the candidate to prevent duplicates
        Destroy(candidate);
    }

    private static bool IsMuted(GameObject obj)
    {
        if (!obj) return false;

        var looper = obj.GetComponent<IntroLoopMusicPlayer>();
        if (looper != null)
            return looper.source != null && looper.source.mute;

        var src = obj.GetComponent<AudioSource>();
        return src != null && src.mute;
    }

    /// <summary>
    /// Called when the "main music" object changes (for example when a new scene has a fresh main music object
    /// and MusicDontDestroy decides which one survives).
    /// </summary>
    public void SetNewMainMusic(GameObject newMain)
    {
        if (newMain == null) return;

        // If we were currently using the old main, update current pointer to the new one.
        if (currentlyPlayingMusic == mainMusic)
            currentlyPlayingMusic = newMain;

        mainMusic = newMain;
    }

    /// <summary>
    /// Set the initial main music. Use Restart if you want it to begin from the intro.
    /// </summary>
    public void SetInitialMainMusic(GameObject music, MusicStartMode mode)
    {
        mainMusic = music;
        currentlyPlayingMusic = music;
        overrideStack.Clear();

        if (currentlyPlayingMusic != null)
            ApplyCurrentMusic(mode);
    }

    public void PushMusicOverride(GameObject musicOverride, MusicStartMode mode)
    {
        if (musicOverride == null) return;

        // Mute current track (do NOT stop it; we want to resume position later)
        if (currentlyPlayingMusic != null)
            SetMuted(currentlyPlayingMusic, true);

        if (!overrideStack.Contains(musicOverride))
            overrideStack.Add(musicOverride);

        currentlyPlayingMusic = musicOverride;
        ApplyCurrentMusic(mode);
    }

    public void PopMusicOverride(GameObject musicOverride, MusicStartMode mode)
    {
        if (musicOverride == null) return;
        if (!overrideStack.Contains(musicOverride)) return;

        bool wasCurrent = (currentlyPlayingMusic == musicOverride);
        overrideStack.Remove(musicOverride);

        if (!wasCurrent) return;

        // STOP the override we are leaving (critical for IntroLoopMusicPlayer)
        StopTrack(musicOverride);

        if (overrideStack.Count > 0)
            currentlyPlayingMusic = overrideStack[^1];
        else
            currentlyPlayingMusic = mainMusic;

        if (currentlyPlayingMusic != null)
            ApplyCurrentMusic(mode);
    }

    private static void StopTrack(GameObject obj)
    {
        if (obj == null) return;

        var looper = obj.GetComponent<IntroLoopMusicPlayer>();
        if (looper != null)
        {
            looper.Stop();
            return;
        }

        var src = obj.GetComponent<AudioSource>();
        if (src != null) src.Stop();
    }

    public void ClearMusicOverrides(MusicStartMode mode)
    {
        overrideStack.Clear();
        currentlyPlayingMusic = mainMusic;

        if (currentlyPlayingMusic != null)
            ApplyCurrentMusic(mode);
    }

    public void MuteAllMusic()
    {
        // Mute current
        if (currentlyPlayingMusic != null)
            SetMuted(currentlyPlayingMusic, true);

        // Also mute main + any lingering overrides for safety
        if (mainMusic != null)
            SetMuted(mainMusic, true);

        foreach (var ov in overrideStack)
            if (ov != null) SetMuted(ov, true);
    }

    public void SetCurrentVolume(float volume01)
    {
        if (currentlyPlayingMusic == null) return;
        SetVolume(currentlyPlayingMusic, volume01);
    }

    public float GetCurrentVolume()
    {
        if (currentlyPlayingMusic == null) return 1f;
        return GetVolume(currentlyPlayingMusic);
    }

    private void ApplyCurrentMusic(MusicStartMode mode)
    {
        // Always unmute current
        SetMuted(currentlyPlayingMusic, false);

        if (mode == MusicStartMode.Restart)
            RestartTrack(currentlyPlayingMusic);
        // Continue: do nothing else (keeps playback position)
    }

    private static void RestartTrack(GameObject obj)
    {
        if (obj == null) return;

        var looper = obj.GetComponent<IntroLoopMusicPlayer>();
        if (looper != null)
        {
            looper.PlayFromStart();
            return;
        }

        var src = obj.GetComponent<AudioSource>();
        if (src != null)
        {
            src.Stop();
            src.Play();
        }
    }

    private static void SetMuted(GameObject obj, bool muted)
    {
        if (obj == null) return;

        var looper = obj.GetComponent<IntroLoopMusicPlayer>();
        if (looper != null)
        {
            looper.SetMuted(muted);
            return;
        }

        var src = obj.GetComponent<AudioSource>();
        if (src != null) src.mute = muted;
    }

    private static float GetVolume(GameObject obj)
    {
        if (obj == null) return 1f;

        var looper = obj.GetComponent<IntroLoopMusicPlayer>();
        if (looper != null) return looper.GetVolume();

        var src = obj.GetComponent<AudioSource>();
        return src != null ? src.volume : 1f;
    }

    private static void SetVolume(GameObject obj, float volume01)
    {
        if (obj == null) return;

        float v = Mathf.Clamp01(volume01);

        var looper = obj.GetComponent<IntroLoopMusicPlayer>();
        if (looper != null)
        {
            looper.SetVolume(v);
            return;
        }

        var src = obj.GetComponent<AudioSource>();
        if (src != null) src.volume = v;
    }
}