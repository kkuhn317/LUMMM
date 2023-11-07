using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public Animator transition;
    public float transitionChangeDelay = 1.2f;

    public void LoadNextLevel() => LoadLevel(SceneManager.GetActiveScene().buildIndex + 1);

    public void LoadSceneByName(string sceneName)
    {
        StartCoroutine(LoadSceneByNameCoroutine(sceneName));
    }

    void LoadLevel(int levelIndex)
    {
        StartCoroutine(LoadLevelCoroutine(levelIndex));
    }

    IEnumerator LoadLevelCoroutine(int levelIndex)
    {
        // Play Animation
        transition.SetTrigger("Start");
        // Add a delay
        yield return new WaitForSeconds(transitionChangeDelay);
        // Load scene
        SceneManager.LoadScene(levelIndex);
    }

    IEnumerator LoadSceneByNameCoroutine(string scenename)
    {
        // Play Animation
        transition.SetTrigger("Start");
        // Add a delay
        yield return new WaitForSeconds(transitionChangeDelay);
        // Load scene
        SceneManager.LoadScene(scenename);
    }
}
