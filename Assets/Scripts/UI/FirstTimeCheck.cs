using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FirstTimeCheck : MonoBehaviour
{
    [Header("Reference to the object to activate")]
    public GameObject objectToActivate;

    [Header("Animator Reference")]
    public Animator animator;

    [Header("Name of Animation State to Play")]
    public string animationStateName;

    [Header("Scene to Load After Animation")]
    public string nextSceneName = "AttentionPlease";

    private const string FirstTimeKey = "IsFirstTime";

    void Start()
    {
        if (animator == null)
        {
            Debug.LogError("Animator not assigned. Please assign an Animator in the Inspector.");
        }

        // Check if this is the first time the game is being opened
        if (PlayerPrefs.GetInt(FirstTimeKey, 1) == 1)
        {
            // First time opening the game
            Debug.Log("First Time Opening");
            // First time opening the game
            ActivateObject();
            // Set PlayerPrefs to indicate the game has been opened
            PlayerPrefs.SetInt(FirstTimeKey, 0);
            PlayerPrefs.Save();
        }
        else
        {
            // Not the first time; load the next scene
            SceneManager.LoadScene(nextSceneName);
        }
    }

    public void PlayAnimationAndChangeScene()
    {
        if (animator != null && !string.IsNullOrEmpty(animationStateName))
        {
            animator.Play(animationStateName);
            StartCoroutine(WaitForAnimationAndChangeScene());
        }
        else
        {
            Debug.LogError("Animator or animation state name not set.");
        }
    }

    private IEnumerator WaitForAnimationAndChangeScene()
    {
        if (animator != null)
        {
            // Wait for the animator to enter the desired state
            while (!animator.GetCurrentAnimatorStateInfo(0).IsName(animationStateName))
            {
                yield return null;
            }

            // Wait until the animation finishes
            while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            {
                yield return null;
            }
        }

        // Load the next scene
        SceneManager.LoadScene(nextSceneName);
    }

    private void ActivateObject()
    {
        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
        }
        else
        {
            Debug.LogWarning("No object assigned to activate.");
        }
    }
}