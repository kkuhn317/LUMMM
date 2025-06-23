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
        {
            EventSystem.current.sendNavigationEvents = false;
            // EventSystem.current.SetSelectedGameObject(null); // Remove selected button, uncommeting this will disables the indicator as well
        }

        transition.SetTrigger("Start");
        audioSource.Play();

        yield return new WaitForSeconds(transitionChangeDelay);
        SceneManager.LoadScene(levelIndex);
    }

    IEnumerator LoadSceneByNameCoroutine(string sceneName)
    {
        isTransitioning = true;

        if (EventSystem.current != null)
        {
            EventSystem.current.sendNavigationEvents = false;
            // EventSystem.current.SetSelectedGameObject(null); // Remove selected button, uncommeting this will disables the indicator as well 
        }

        transition.SetTrigger("Start");

        yield return new WaitForSeconds(transitionChangeDelay);
        SceneManager.LoadScene(sceneName);
    }
}