using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SaveFileUI : MonoBehaviour, ISelectHandler
{
    [Header("Containers")]
    public GameObject newFileContainer;
    public GameObject infoContainer;

    [Header("Texts")]
    public TMP_Text letterText;
    public TMP_Text profileNameText;
    public TMP_Text levelsText;
    public TMP_Text coinsText;

    [Header("Stars")]
    public Image[] starImages;

    [Header("Visual state")]
    public Image backgroundImage;
    public Color normalColor = Color.white;
    public Color focusedColor = new Color(1f, 0.95f, 0.7f);

    [HideInInspector] public int slotIndex;

    private SaveSlotManager manager;

    private void Awake()
    {
        manager = FindObjectOfType<SaveSlotManager>();
    }

    #region Refresh & Visuals

    /// <summary>
    /// Refreshes this card UI based on the data of the given slot index.
    /// </summary>
    public void Refresh(int index)
    {
        slotIndex = index;

        if (letterText != null)
            letterText.text = ((char)('A' + index)).ToString();

        bool exists = SaveManager.SlotExists(index);

        if (newFileContainer != null) newFileContainer.SetActive(!exists);
        if (infoContainer != null) infoContainer.SetActive(exists);

        if (!exists)
        {
            if (profileNameText != null)
                profileNameText.text = "Empty";
        }
        else
        {
            SaveManager.Load(index);

            var playableLevels = SaveLoadSystem.Instance?.GetPlayableLevels();

            var summary = SaveLoadSystem.Instance != null
                ? SaveLoadSystem.Instance.BuildSummary()
                : SaveManager.BuildSummary(playableLevels);

            if (profileNameText != null)
                profileNameText.text = SaveManager.Current.profileName;

            if (levelsText != null)
                levelsText.text = $"{summary.completedLevels}/{summary.totalLevels}";

            if (coinsText != null)
                coinsText.text = $"{summary.collectedGreenCoins}/{summary.maxGreenCoins}";

            SetStars(summary.StarCount);
        }

        UpdateFocusVisual();
    }

    public void UpdateFocusVisual()
    {
        if (manager == null || backgroundImage == null) return;

        bool focused = manager.FocusedSlotIndex == slotIndex;
        backgroundImage.color = focused ? focusedColor : normalColor;
    }

    private void SetStars(int count)
    {
        if (starImages == null) return;

        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;

            bool isActive = i < count;
            starImages[i].gameObject.SetActive(isActive);
        }
    }

    #endregion

    #region EventSystem Hooks

    public void OnSelect(BaseEventData eventData)
    {
        if (manager == null) return;

        manager.FocusSlot(slotIndex);
        UpdateFocusVisual();
    }

    /// <summary>
    /// Called by the Button component's OnClick for this card.
    /// This should be the ONLY listener on the button.
    /// </summary>
    public void OnClickCard()
    {
        if (manager == null) return;

        manager.FocusSlot(slotIndex);
        manager.PlayFocusedSlot();
    }

    #endregion
}