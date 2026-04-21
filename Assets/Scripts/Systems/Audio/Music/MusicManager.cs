using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    public enum MusicStartMode
    {
        Continue,   // do not restart; keep current playback position
        Restart     // restart intro -> loop
    }

    [Header("Debug")]
    [SerializeField] private GameObject mainMusic;
    [SerializeField] private GameObject currentlyPlayingMusic;

    private sealed class OverrideEntry
    {
        public string Key;
        public GameObject MusicObject;
        public int Priority;
        public HashSet<int> Owners = new();
        public long LastOrder;
    }

    private readonly Dictionary<string, OverrideEntry> overridesByKey = new();
    private long requestCounter;
    
    private const int LegacyGlobalOwner = int.MinValue;
    private const int LegacyDefaultPriority = 0;

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
        // Preserve old initialization behavior
        if (mainMusic == null)
            mainMusic = gameObject;

        if (currentlyPlayingMusic == null)
            currentlyPlayingMusic = mainMusic;

        var looper = currentlyPlayingMusic != null
            ? currentlyPlayingMusic.GetComponent<IntroLoopMusicPlayer>()
            : null;
        var src = currentlyPlayingMusic != null
            ? currentlyPlayingMusic.GetComponent<AudioSource>()
            : null;

        bool isPlaying =
            (looper != null && looper.IsPlaying) ||
            (src != null && src.isPlaying);

        if (looper != null)
        {
            if (!looper.IsPlaying)
                looper.EnsurePlaying();
        }
        else if (!isPlaying)
        {
            ClearMusicOverrides(MusicStartMode.Restart);
        }
    }

    public bool HasActiveOverride(string key)
    {
        return overridesByKey.TryGetValue(key, out var entry) &&
            entry != null &&
            entry.MusicObject != null &&
            entry.Owners.Count > 0;
    }

    public bool HasOverrideOwner(string key, int ownerId)
    {
        return overridesByKey.TryGetValue(key, out var entry) &&
            entry != null &&
            entry.Owners.Contains(ownerId);
    }

    public void RequestOverride(string key, GameObject musicOverride, int ownerId, int priority, MusicStartMode mode)
    {
        if (string.IsNullOrEmpty(key) || musicOverride == null)
            return;

        bool wasActive = HasActiveOverride(key);

        if (!overridesByKey.TryGetValue(key, out var entry))
        {
            entry = new OverrideEntry
            {
                Key = key,
                MusicObject = musicOverride,
                Priority = priority
            };
            overridesByKey.Add(key, entry);
        }
        else
        {
            if (entry.MusicObject == null)
                entry.MusicObject = musicOverride;

            entry.Priority = priority;
        }

        bool ownerAlreadyPresent = entry.Owners.Contains(ownerId);
        entry.Owners.Add(ownerId);
        entry.LastOrder = ++requestCounter;

        var effectiveMode = (wasActive || ownerAlreadyPresent)
            ? MusicStartMode.Continue
            : mode;

        RefreshActiveMusic(effectiveMode);
    }

    private static string MakeLegacyKey(GameObject musicOverride)
    {
        return musicOverride == null
            ? string.Empty
            : $"legacy:{musicOverride.GetInstanceID()}";
    }

    public void RegisterMainMusic(GameObject candidate)
    {
        if (!candidate) return;

        if (mainMusic == null)
        {
            SetInitialMainMusic(candidate, MusicStartMode.Restart);
            return;
        }

        if (IsMuted(mainMusic))
            SetMuted(candidate, true);

        Destroy(candidate);
    }

    public void SetNewMainMusic(GameObject newMain)
    {
        if (newMain == null) return;

        if (currentlyPlayingMusic == mainMusic)
            currentlyPlayingMusic = newMain;

        mainMusic = newMain;
    }

    public void PushMusicOverride(GameObject musicOverride, MusicStartMode mode)
    {
        if (musicOverride == null) return;

        RequestOverride(
            MakeLegacyKey(musicOverride),
            musicOverride,
            LegacyGlobalOwner,
            LegacyDefaultPriority,
            mode
        );
    }

    public void PopMusicOverride(GameObject musicOverride, MusicStartMode mode)
    {
        if (musicOverride == null) return;

        ReleaseOverride(
            MakeLegacyKey(musicOverride),
            LegacyGlobalOwner,
            mode
        );
    }

    public void ClearMusicOverrides(MusicStartMode mode)
    {
        ClearAllOverrides(mode);
    }

    public void MuteAllMusic()
    {
        if (currentlyPlayingMusic != null)
            SetMuted(currentlyPlayingMusic, true);

        if (mainMusic != null)
            SetMuted(mainMusic, true);

        foreach (var entry in overridesByKey.Values)
        {
            if (entry?.MusicObject != null)
                SetMuted(entry.MusicObject, true);
        }
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

    public void SetInitialMainMusic(GameObject music, MusicStartMode mode)
    {
        mainMusic = music;
        currentlyPlayingMusic = music;
        overridesByKey.Clear();

        if (currentlyPlayingMusic != null)
            ApplyCurrentMusic(mode);
    }

    public void ReleaseOverride(string key, int ownerId, MusicStartMode mode)
    {
        if (string.IsNullOrEmpty(key))
            return;

        if (!overridesByKey.TryGetValue(key, out var entry))
            return;

        entry.Owners.Remove(ownerId);

        if (entry.Owners.Count == 0)
        {
            StopTrack(entry.MusicObject);
            SetMuted(entry.MusicObject, true);
            overridesByKey.Remove(key);
        }

        RefreshActiveMusic(mode);
    }

    public void ReleaseAllOverridesFromOwner(int ownerId, MusicStartMode mode)
    {
        List<string> emptyKeys = null;

        foreach (var pair in overridesByKey)
        {
            var entry = pair.Value;
            if (!entry.Owners.Remove(ownerId))
                continue;

            if (entry.Owners.Count == 0)
            {
                emptyKeys ??= new List<string>();
                emptyKeys.Add(pair.Key);
            }
        }

        if (emptyKeys != null)
        {
            foreach (var key in emptyKeys)
            {
                var entry = overridesByKey[key];
                StopTrack(entry.MusicObject);
                SetMuted(entry.MusicObject, true);
                overridesByKey.Remove(key);
            }
        }

        RefreshActiveMusic(mode);
    }

    public void ClearAllOverrides(MusicStartMode mode)
    {
        foreach (var entry in overridesByKey.Values)
        {
            if (entry?.MusicObject == null) continue;
            StopTrack(entry.MusicObject);
            SetMuted(entry.MusicObject, true);
        }

        overridesByKey.Clear();
        RefreshActiveMusic(mode);
    }

    private void RefreshActiveMusic(MusicStartMode mode)
    {
        GameObject next = mainMusic;
        int bestPriority = int.MinValue;
        long bestOrder = long.MinValue;

        foreach (var pair in overridesByKey)
        {
            var entry = pair.Value;
            if (entry.Owners.Count == 0 || entry.MusicObject == null)
                continue;

            if (entry.Priority > bestPriority ||
                (entry.Priority == bestPriority && entry.LastOrder > bestOrder))
            {
                bestPriority = entry.Priority;
                bestOrder = entry.LastOrder;
                next = entry.MusicObject;
            }
        }

        if (currentlyPlayingMusic == next)
        {
            if (next != null)
            {
                SetMuted(next, false);

                // Preserve old PushMusicOverride(...Restart) behavior
                if (mode == MusicStartMode.Restart)
                    RestartTrack(next);
            }
            return;
        }

        if (currentlyPlayingMusic != null)
            SetMuted(currentlyPlayingMusic, true);

        currentlyPlayingMusic = next;

        if (currentlyPlayingMusic != null)
            ApplyCurrentMusic(mode);
    }

    private void ApplyCurrentMusic(MusicStartMode mode)
    {
        SetMuted(currentlyPlayingMusic, false);

        if (mode == MusicStartMode.Restart)
            RestartTrack(currentlyPlayingMusic);
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