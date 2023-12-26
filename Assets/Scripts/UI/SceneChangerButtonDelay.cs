using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneChangerButtonDelay : MonoBehaviour
{ 
    public float delayBeforeSceneChange = 10f; // Adjust this value to set the delay before scene change
    private int currentSceneBuildIndex;

    private AudioSource audioSource;
    private bool buttonPressed = false;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        StartCoroutine(ChangeSceneAfterDelay());
    }

    private void Update()
    {
        // Check for button press
        if (!buttonPressed && Input.GetButtonDown("Pause")) {
            ChangeScene();
        }
    }

    private void ChangeScene()
    {
        buttonPressed = true;
        audioSource.Play();
        FadeInOutScene.Instance.LoadNextSceneWithFade();
    }

    private IEnumerator ChangeSceneAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeSceneChange);

        if (!buttonPressed) {
            ChangeScene();
        }
    }
}
