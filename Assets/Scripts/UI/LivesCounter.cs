using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Timeline;

public class LivesCounter : MonoBehaviour
{
    public bool oldNumber = false;

    public PlayableDirector playableDirector;
    public TimelineAsset oneLifeCutscene;    // Cutscene to play when there is only one life left (optional)

    public float delay = 1f; // Additional delay after cutscene ends before going back to level

    // Start is called before the first frame update
    void Start()
    {
        int livesNum = GlobalVariables.lives;
        if (oldNumber) livesNum += 1;
        GetComponent<TextMeshProUGUI>().text = livesNum.ToString();
        if (oldNumber) {
            Invoke(nameof(PlaySound), 0.5f);

            PlayableDirector director = playableDirector;

            if (GlobalVariables.lives == 1 && oneLifeCutscene != null) {
                director.playableAsset = oneLifeCutscene;
            }

            director.Play();

            if (director != null) {
                double timelineDuration = director.duration + delay;

                // Use the timeline duration as the delay before loading the scene
                Invoke(nameof(GoBackToLevel), (float)timelineDuration);
            } else {
                Invoke(nameof(GoBackToLevel), 2f);
            }
        }
    }

    void GoBackToLevel() {
        SceneManager.LoadScene(GlobalVariables.levelInfo.levelScene);
    }

    void PlaySound() {
        GetComponent<AudioSource>().Play();
    }

    public void PauseGameMusic()
    {
        foreach (GameObject musicObject in GameObject.FindGameObjectsWithTag("GameMusic"))
        {
            AudioSource audioSource = musicObject.GetComponent<AudioSource>();
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Pause();
            }
        }
    }

    public void ResumeGameMusic()
    {
        foreach (GameObject musicObject in GameObject.FindGameObjectsWithTag("GameMusic"))
        {
            AudioSource audioSource = musicObject.GetComponent<AudioSource>();
            if (audioSource != null && !audioSource.isPlaying)
            {
                audioSource.UnPause();
            }
        }
    }

}
