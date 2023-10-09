using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultipleAudioRange : MonoBehaviour
{
    public List<Collider> soundColliders;
    public List<AudioSource> audioSources;
    public float minDistance = 5f;
    public float maxDistance = 20f;

    void Update()
    {
        for (int i = 0; i < soundColliders.Count; i++)
        {
            float distance = Vector3.Distance(transform.position, soundColliders[i].transform.position);
            float volume = Mathf.Lerp(0f, 1f, (maxDistance - distance) / (maxDistance - minDistance));
            audioSources[i].volume = volume;
        }
    }
}
