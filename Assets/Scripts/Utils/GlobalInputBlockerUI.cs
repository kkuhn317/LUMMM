using UnityEngine;

public class GlobalInputBlockerUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        SetBlocked(false);
    }

    public void SetBlocked(bool blocked)
    {
        if (canvasGroup == null) return;

        canvasGroup.gameObject.SetActive(blocked);
        canvasGroup.interactable = blocked;
        canvasGroup.blocksRaycasts = blocked;
        canvasGroup.alpha = 0f; // stay invisible, you only care about raycasts
    }
}