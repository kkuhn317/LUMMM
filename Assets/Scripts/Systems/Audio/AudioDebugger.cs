using UnityEngine;

public class AudioDebugger : MonoBehaviour
{
    void OnEnable()
    {
        Debug.Log("[AudioDebugger] Scene loading started");
    }
    
    void Start()
    {
        Debug.Log("[AudioDebugger] Start() called");
        
        // Check all AudioSources in scene
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>(true);
        Debug.Log($"[AudioDebugger] Found {allAudioSources.Length} AudioSources in scene");
        
        foreach (AudioSource source in allAudioSources)
        {
            Debug.Log($"[AudioDebugger] AudioSource: {source.name} on {source.gameObject.name}, PlayOnAwake: {source.playOnAwake}, IsPlaying: {source.isPlaying}");
        }
    }
    
    void Update()
    {
        // Optional: Log when any AudioSource starts playing
        if (Time.frameCount < 10) // Only check first 10 frames
        {
            AudioSource[] sources = FindObjectsOfType<AudioSource>();
            foreach (AudioSource source in sources)
            {
                if (source.isPlaying)
                {
                    Debug.Log($"[AudioDebugger] Frame {Time.frameCount}: {source.name} is playing clip: {source.clip?.name}");
                }
            }
        }
    }
}