using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneChangerButtonDelay : MonoBehaviour
{ 
    private int currentSceneBuildIndex;
    public float delayBeforeSceneChange = 10f; // Adjust this value to set the delay before scene change

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        // Get the current scene build index
        currentSceneBuildIndex = SceneManager.GetActiveScene().buildIndex;

        // Uncomment the line below if you want to change scene after a certain period of time
        StartCoroutine(ChangeSceneAfterDelay());
    }

    private void Update()
    {
        // Check for button press
        if (Input.GetButtonDown("Pause"))
        {
            ChangeScene();
        }
    }

    private void ChangeScene()
    {
        // Increment the current scene build index by 1
        int nextSceneIndex = currentSceneBuildIndex + 1;

        // If the nextSceneIndex exceeds the number of scenes in the build, wrap around to the first scene
        if (nextSceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            nextSceneIndex = 0;
        }

        audioSource.Play();

        // Load the next scene
        SceneManager.LoadScene(nextSceneIndex);
    }

    private IEnumerator ChangeSceneAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeSceneChange);

        audioSource.Play();

        // Increment the current scene build index by 1
        int nextSceneIndex = currentSceneBuildIndex + 1;

        // If the nextSceneIndex exceeds the number of scenes in the build, wrap around to the first scene
        if (nextSceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            nextSceneIndex = 0;
        }

        // Load the next scene
        SceneManager.LoadScene(nextSceneIndex);
    }
}
