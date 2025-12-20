using UnityEngine;

public class IntroLoopMusicPlayer : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip introClip;
    public AudioClip loopClip;

    [Header("Settings")]
    [Min(0)] public double scheduleSafetySeconds = 0.05;

    [Header("Audio (Intro Source)")]
    public AudioSource source; // keep this for compatibility (intro source)

    [Header("Audio (Loop Source)")]
    public AudioSource loopSource;

    void Awake()
    {
        if (!source) source = GetComponent<AudioSource>();
        if (!source) source = gameObject.AddComponent<AudioSource>();

        if (!loopSource)
        {
            loopSource = gameObject.AddComponent<AudioSource>();
        }

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

        Stop();

        double dspNow = AudioSettings.dspTime;
        double introStart = dspNow + scheduleSafetySeconds;

        // Schedule intro
        source.clip = introClip;
        source.loop = false;
        source.PlayScheduled(introStart);

        // Schedule loop exactly at intro end
        double introDuration = (double)introClip.samples / introClip.frequency;
        double loopStart = introStart + introDuration;

        loopSource.clip = loopClip;
        loopSource.loop = true;
        loopSource.PlayScheduled(loopStart);
    }

    public void Stop()
    {
        if (source) source.Stop();
        if (loopSource) loopSource.Stop();
    }

    public bool IsPlaying =>
    (source != null && source.isPlaying) || (loopSource != null && loopSource.isPlaying);

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
        // If neither intro nor loop is currently playing, start the music.
        // If one is already playing, do nothing (this preserves progress on death/respawn).
        bool introPlaying = source != null && source.isPlaying;
        bool loopPlaying  = loopSource != null && loopSource.isPlaying;

        if (!introPlaying && !loopPlaying)
        {
            PlayFromStart();
        }
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
        // Keep routing & 2D/3D behavior consistent
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

        // Keep the same initial loudness/mute state
        to.volume = from.volume;
        to.mute = from.mute;
    }
}