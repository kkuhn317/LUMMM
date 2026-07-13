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

    public Animator livesParentAnimator;
    public PlayableDirector playableDirector;
    public TimelineAsset oneLifeCutscene;    // Cutscene to play when there is only one life left (optional)

    public float delay = 1f; // Additional delay after cutscene ends before going back to level

    void Start()
    {
        Time.timeScale = 1f;
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
                Invoke(nameof(GoBackToLevel), (float)timelineDuration);
            } else {
                Invoke(nameof(GoBackToLevel), 2f);
            }
        }
    }

    private void GoBackToLevel()
    {
        if (GlobalVariables.levelInfo == null)
        {
            Debug.LogWarning("[LivesCounter] levelInfo is null — reloading current scene as fallback.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(GlobalVariables.levelInfo.levelScene);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (GlobalVariables.levelInfo == null) return;

        if (scene.name == GlobalVariables.levelInfo.levelScene)
        {
            Debug.Log($"Scene {scene.name} loaded. Resuming music.");
            ResumeGameMusic();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    public void PlayLivesRise() => livesParentAnimator.SetTrigger("Play");

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