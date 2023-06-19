using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class doorAudio_play : MonoBehaviour
{
    private AudioSource audioSource;
    public AudioClip openDoor;
    public AudioClip closeDoor;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void OpenDoorSound()
    {
        audioSource.PlayOneShot(openDoor);
    }

    public void CloseDoorSound()
    {
        audioSource.PlayOneShot(closeDoor);
    }
}
