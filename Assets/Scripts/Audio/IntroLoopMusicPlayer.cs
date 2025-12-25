using UnityEngine;

public class IntroLoopMusicPlayer : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip introClip;
    public AudioClip loopClip;

    [Header("Scheduling")]
    [Min(0)] public double scheduleSafetySeconds = 0.05;

    // If MP3 introduces a tiny overlap, push loop a few ms later (try 0.005).
    [Min(0)] public double handoffPaddingSeconds = 0.0;

    [Header("Audio (Intro Source)")]
    public AudioSource source; // keep this for compatibility (intro source)

    [Header("Audio (Loop Source)")]
    public AudioSource loopSource;

    // Guard against re-scheduling (and helps MusicManager "gap" detection).
    private bool _isScheduled;

    // Expose for other systems if needed (optional but useful).
    public bool IsScheduled => _isScheduled;

    void Awake()
    {
        if (!source) source = GetComponent<AudioSource>();
        if (!source) source = gameObject.AddComponent<AudioSource>();

        if (!loopSource)
            loopSource = gameObject.AddComponent<AudioSource>();

        // Basic setup
        source.playOnAwake = false;
        source.loop = false;

        loopSource.playOnAwake = false;
        loopSource.loop = true;

        // Match important settings from the intro source
        CopyAudioSettings(source, loopSource);
    }

    public void PlayFromStart()
    {
        if (!introClip || !loopClip || !source || !loopSource) return;

        // Prevent duplicate scheduling (key to avoid overlaps from re-entrant calls)
        if (_isScheduled) return;

        StopInternal(cancelScheduled: true);

        double dspNow = AudioSettings.dspTime;
        double introStart = dspNow + scheduleSafetySeconds;

        // Schedule intro
        source.clip = introClip;
        source.loop = false;
        source.PlayScheduled(introStart);

        // Prefer AudioClip.length here (more reliable for compressed clips than samples/frequency)
        double introDuration = (double)introClip.length;

        // Schedule loop at intro end (+ optional padding)
        double loopStart = introStart + introDuration + handoffPaddingSeconds;

        // CRITICAL: hard cut intro at loopStart to avoid any bleed/overlap
        source.SetScheduledEndTime(loopStart);

        loopSource.clip = loopClip;
        loopSource.loop = true;
        loopSource.PlayScheduled(loopStart);

        _isScheduled = true;
    }

    public void Stop()
    {
        StopInternal(cancelScheduled: true);
    }

    private void StopInternal(bool cancelScheduled)
    {
        if (cancelScheduled)
        {
            // Cancel any queued PlayScheduled on the DSP timeline
            double now = AudioSettings.dspTime;
            if (source) source.SetScheduledEndTime(now);
            if (loopSource) loopSource.SetScheduledEndTime(now);
        }

        if (source) source.Stop();
        if (loopSource) loopSource.Stop();

        _isScheduled = false;
    }

    public bool IsPlaying =>
        _isScheduled || // treat "scheduled but not yet audible" as playing for manager logic
        (source != null && source.isPlaying) ||
        (loopSource != null && loopSource.isPlaying);

    public void UnPause()
    {
        if (source) source.UnPause();
        if (loopSource) loopSource.UnPause();
    }

    public void Pause()
    {
        if (source) source.Pause();
        if (loopSource) loopSource.Pause();
    }

    public void EnsurePlaying()
    {
        // If already scheduled or currently playing, do nothing.
        if (IsPlaying) return;

        PlayFromStart();
    }

    public void SetPaused(bool paused)
    {
        if (paused)
        {
            if (source && source.isPlaying) source.Pause();
            if (loopSource && loopSource.isPlaying) loopSource.Pause();
        }
        else
        {
            if (source) source.UnPause();
            if (loopSource) loopSource.UnPause();
        }
    }

    public void SetMuted(bool muted)
    {
        if (source) source.mute = muted;
        if (loopSource) loopSource.mute = muted;
    }

    public void SetVolume(float volume)
    {
        float v = Mathf.Clamp01(volume);
        if (source) source.volume = v;
        if (loopSource) loopSource.volume = v;
    }

    public float GetVolume()
    {
        return source ? source.volume : 1f;
    }

    private void CopyAudioSettings(AudioSource from, AudioSource to)
    {
        to.outputAudioMixerGroup = from.outputAudioMixerGroup;
        to.spatialBlend = from.spatialBlend;
        to.priority = from.priority;
        to.pitch = from.pitch;
        to.panStereo = from.panStereo;

        to.reverbZoneMix = from.reverbZoneMix;
        to.dopplerLevel = from.dopplerLevel;
        to.spread = from.spread;
        to.rolloffMode = from.rolloffMode;
        to.minDistance = from.minDistance;
        to.maxDistance = from.maxDistance;

        to.volume = from.volume;
        to.mute = from.mute;
    }
}