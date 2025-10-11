using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class SceneChangerButtonDelay : MonoBehaviour
{ 
    public float delayBeforeSceneChange = 10f; // Adjust this value to set the delay before scene change

    private AudioSource audioSource;
    private bool buttonPressed = false;

    // We want to remove the event listener we install through InputSystem.onAnyButtonPress
    // after we're done so remember it here.
    private IDisposable m_EventListener;

    private void OnEnable()
    {
        // Subscribe to global button presses
        m_EventListener = InputSystem.onAnyButtonPress
            .Call(ctrl =>
            {
                if (!buttonPressed)
                {
                    ChangeScene();
                }
            });
    }
    
    private void OnDisable()
    {
        // Unsubscribe from global button presses
        if (m_EventListener != null)
        {
            m_EventListener.Dispose();
            m_EventListener = null;
        }
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        StartCoroutine(ChangeSceneAfterDelay());
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

        if (!buttonPressed) 
        {
            ChangeScene();
        }
    }
}