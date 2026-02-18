using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameOverController : MonoBehaviour
{
    [Header("Game Over References")]
    [SerializeField] private GameObject gameOverScreenObject;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button quitButton;
    
    private bool isGameOverActive = false;
    
    private void Start()
    {
        if (gameOverScreenObject != null)
        {
            gameOverScreenObject.SetActive(false);
        }
        
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryPressed);
        }
        
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitPressed);
        }
    }
    
    private void OnEnable()
    {
        GameEvents.OnGameOver += ShowGameOverScreen;
    }
    
    private void OnDisable()
    {
        GameEvents.OnGameOver -= ShowGameOverScreen;
    }
    
    public void ShowGameOverScreen()
    {
        if (gameOverScreenObject != null)
        {
            gameOverScreenObject.SetActive(true);
            isGameOverActive = true;
            
            // Select retry button by default
            if (retryButton != null)
            {
                retryButton.Select();
            }
            
            GameEvents.TriggerUIUpdated();
        }
    }
    
    public void HideGameOverScreen()
    {
        if (gameOverScreenObject != null)
        {
            gameOverScreenObject.SetActive(false);
            isGameOverActive = false;
        }
    }
    
    private void OnRetryPressed()
    {
        GameManager.Instance?.RestartLevelFromBeginning();
    }
    
    private void OnQuitPressed()
    {
        // Return to level select
        FadeInOutScene.Instance.LoadSceneWithFade("SelectLevel");
    }
    
    public bool IsGameOverActive => isGameOverActive;
}