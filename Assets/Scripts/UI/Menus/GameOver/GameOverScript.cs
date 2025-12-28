using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

public class GameOverScript : MonoBehaviour
{
    public float transitionChangeDelay = 6f;
    public UnityEvent onButtonsReady;

    [SerializeField] private CanvasGroup gameOverCanvasGroup;
    [SerializeField] private GameObject defaultSelectedOnReady;

    private bool canPressButtons = false;
    private bool optionChosen = false;

    private void OnEnable()
    {
        canPressButtons = false;
        optionChosen = false;

        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.interactable = false;
            gameOverCanvasGroup.blocksRaycasts = false;
        }

        if (transitionChangeDelay > 0f)
            Invoke(nameof(TriggerButtonsReady), transitionChangeDelay);
        else
            TriggerButtonsReady();
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void TriggerButtonsReady()
    {
        CursorHelper.ShowCursor();

        canPressButtons = true;

        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.interactable = true;
            gameOverCanvasGroup.blocksRaycasts = true;
        }

        onButtonsReady?.Invoke();
        
        if (defaultSelectedOnReady != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(defaultSelectedOnReady);
        }
    }

    private IEnumerator LockUIAfterFrame()
    {
        yield return null;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.blocksRaycasts = false;
        }
    }

    public void OnRetryButtonPressed()
    {
        if (!canPressButtons || optionChosen) return;
        optionChosen = true;
        canPressButtons = false;

        StartCoroutine(LockUIAfterFrame());

        if (GameManager.Instance != null)
            GameManager.Instance.RestartLevelFromBeginningWithFadeOut();
    }

    public void OnQuitButtonPressed()
    {
        if (!canPressButtons || optionChosen) return;
        optionChosen = true;
        canPressButtons = false;

        StartCoroutine(LockUIAfterFrame());

        if (FadeInOutScene.Instance != null)
            FadeInOutScene.Instance.LoadSceneWithFade("SelectLevel");
    }
}