using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LevelLoader : MonoBehaviour
{
    public Animator transition;
    public float transitionChangeDelay = 1.2f;
    private AudioSource audioSource;
    private bool isTransitioning = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void LoadNextLevel()
    {
        if (!isTransitioning)
            LoadLevel(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void LoadSceneByName(string sceneName)
    {
        if (!isTransitioning)
            StartCoroutine(LoadSceneByNameCoroutine(sceneName));
    }

    void LoadLevel(int levelIndex)
    {
        StartCoroutine(LoadLevelCoroutine(levelIndex));
    }

    IEnumerator LoadLevelCoroutine(int levelIndex)
    {
        isTransitioning = true;

        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = false;

        transition.SetTrigger("Start");
        audioSource.Play();

        // Start loading in background immediately, but don't activate yet
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(levelIndex);
        asyncLoad.allowSceneActivation = false;

        yield return new WaitForSeconds(transitionChangeDelay);

        // Scene is already loaded, just activate it now
        asyncLoad.allowSceneActivation = true;
    }

    IEnumerator LoadSceneByNameCoroutine(string sceneName)
    {
        isTransitioning = true;

        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = false;

        transition.SetTrigger("Start");

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        yield return new WaitForSeconds(transitionChangeDelay);

        asyncLoad.allowSceneActivation = true;
    }
}