using UnityEngine;

/// <summary>
/// ScriptableObject that maps surface materials to footstep audio clips.
/// Create one asset per character and assign it in CharacterData.
///
/// Create via: Right-click → Create → Mario → Footstep Sound Data
/// </summary>
[CreateAssetMenu(fileName = "FootstepSoundData", menuName = "Mario/Footstep Sound Data")]
public class FootstepSoundData : ScriptableObject
{
    [System.Serializable]
    public class SurfaceEntry
    {
        public SurfaceMaterial Material;
        [Tooltip("One or more clips — a random one plays each step for variation.")]
        public AudioClip[] Clips;
        [Range(0f, 1f)]
        public float Volume = 1f;
    }

    [SerializeField] private SurfaceEntry[] _entries;

    [Header("Fallback")]
    [Tooltip("Plays when no entry matches the current surface.")]
    public AudioClip[] FallbackClips;
    [Range(0f, 1f)]
    public float FallbackVolume = 0.8f;

    /// <summary>
    /// Returns a random clip and volume for the given surface material.
    /// Falls back to FallbackClips if no matching entry exists.
    /// </summary>
    public bool TryGetClip(SurfaceMaterial material, out AudioClip clip, out float volume)
    {
        if (_entries != null)
        {
            foreach (var entry in _entries)
            {
                if (entry.Material == material && entry.Clips != null && entry.Clips.Length > 0)
                {
                    clip   = entry.Clips[Random.Range(0, entry.Clips.Length)];
                    volume = entry.Volume;
                    return clip != null;
                }
            }
        }

        // Fallback
        if (FallbackClips != null && FallbackClips.Length > 0)
        {
            clip   = FallbackClips[Random.Range(0, FallbackClips.Length)];
            volume = FallbackVolume;
            return clip != null;
        }

        clip   = null;
        volume = 0f;
        return false;
    }
}
