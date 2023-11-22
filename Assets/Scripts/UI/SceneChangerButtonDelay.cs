using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneChangerButtonDelay : MonoBehaviour
{ 
    private int currentSceneBuildIndex;
    public float delayBeforeSceneChange = 10f; // Adjust this value to set the delay before scene change

    private FadeInOutScene fadeInOutScene;
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // Find the FadeInOutScene script in the scene
        fadeInOutScene = FindObjectOfType<FadeInOutScene>();

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
        audioSource.Play();

        // If the script is found, apply the fade effect and load the next scene
        if (fadeInOutScene != null)
        {
            fadeInOutScene.LoadNextSceneWithFade();
        }
    }

    private IEnumerator ChangeSceneAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeSceneChange);

        audioSource.Play();

        // If the script is found, apply the fade effect and load the next scene
        if (fadeInOutScene != null)
        {
            fadeInOutScene.LoadNextSceneWithFade();
        }
    }
}
