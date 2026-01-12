using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveFileUI : MonoBehaviour
{
    [Header("Texts")]
    public TMP_Text letterText; // "A", "B", "C"
    public TMP_Text profileNameText;
    public TMP_Text levelsText; // "0/24"
    public TMP_Text coinsText; // "0/24"

    [Header("Stars")]
    public Image[] starImages; // size 3
    public Sprite starOnSprite;
    public Sprite starOffSprite;

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

    public void Refresh(int index)
    {
        slotIndex = index;

        if (letterText != null)
            letterText.text = ((char)('A' + index)).ToString();

        // Load save data for this slot
        SaveManager.Load(index);

        LevelDefinition[] defs = null;
        if (SaveFileConfig.Instance != null)
            defs = SaveFileConfig.Instance.allLevels;

        var summary = SaveManager.BuildSummary(defs);

        bool exists = SaveManager.SlotExists(index);
        bool empty  = !exists || SaveManager.Current.levels.Count == 0 || summary.totalLevels == 0;

        if (empty)
        {
            profileNameText.text = "Empty";
            levelsText.text = "0/0";
            coinsText.text  = "0/0";
            SetStars(0);
        }
        else
        {
            profileNameText.text = string.IsNullOrEmpty(SaveManager.Current.profileName)
                ? $"File {(char)('A' + index)}"
                : SaveManager.Current.profileName;

            levelsText.text = $"{summary.completedLevels}/{summary.totalLevels}";
            coinsText.text  = $"{summary.collectedCoins}/{summary.maxCoins}";
            SetStars(summary.StarCount);
        }

        UpdateFocusVisual();
    }

    public void UpdateFocusVisual()
    {
        if (manager == null) return;

        bool focused = manager.FocusedSlotIndex == slotIndex;
        if (backgroundImage != null)
            backgroundImage.color = focused ? focusedColor : normalColor;
    }

    private void SetStars(int count)
    {
        if (starImages == null) return;

        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;
            starImages[i].sprite = i < count ? starOnSprite : starOffSprite;
        }
    }

    // Called by clicking this card (Button OnClick)
    public void OnClickCard()
    {
        if (manager != null)
            manager.FocusSlot(slotIndex);
    }

    // Optional: if you add a tiny Play button on the card
    public void OnClickPlay()
    {
        if (manager != null)
            manager.PlaySlot(slotIndex);
    }
}