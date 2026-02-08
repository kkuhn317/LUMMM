using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;

[System.Serializable]
public struct LevelImageData
{
    public GameObject itemParent;
    public Texture itemTexture;
    public RawImage levelImage; // The RawImage to change sprite
    public Color primaryColor; // Primary color for the primary GameObjects
    public Color secondaryColor; // Secondary color for secondary GameObjects
    public GameObject[] primaryObjects; // Array of primary GameObjects
    public GameObject[] secondaryObjects; // Array of secondary GameObjects

    // Method to update the RawImage texture and colors of GameObjects
    public void UpdateLevelImageAndColors(float transitionDuration, LevelButton levelButton)
    {
        // Change the texture of the RawImage
        if (levelImage != null)
        {
            levelImage.texture = itemTexture;
        }

        // Start color transition for primary objects
        foreach (GameObject primaryObject in primaryObjects)
        {
            if (primaryObject != null)
            {
                RawImage primaryRawImage = primaryObject.GetComponent<RawImage>();
                if (primaryRawImage != null)
                {
                    primaryRawImage.color = primaryRawImage.color; // Reset color if needed
                    levelButton.StartCoroutine(TransitionColor(primaryRawImage, primaryColor, transitionDuration));
                }
            }
        }

        // Start color transition for secondary objects
        foreach (GameObject secondaryObject in secondaryObjects)
        {
            if (secondaryObject != null)
            {
                RawImage secondaryRawImage = secondaryObject.GetComponent<RawImage>();
                if (secondaryRawImage != null)
                {
                    secondaryRawImage.color = secondaryRawImage.color; // Reset color if needed
                    levelButton.StartCoroutine(TransitionColor(secondaryRawImage, secondaryColor, transitionDuration));
                }
            }
        }

        // Apply the texture to each child of itemParent
        for (int i = 0; i < itemParent.transform.childCount; i++)
        {
            GameObject child = itemParent.transform.GetChild(i).gameObject;
            RawImage childRawImage = child.GetComponent<RawImage>();
            if (childRawImage != null)
            {
                childRawImage.texture = itemTexture; // Change child's texture
            }
        }
    }

    // Coroutine to smoothly transition the color
    private IEnumerator TransitionColor(RawImage rawImage, Color targetColor, float duration)
    {
        Color initialColor = rawImage.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            rawImage.color = Color.Lerp(initialColor, targetColor, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        rawImage.color = targetColor; // Ensure the final color is set
    }
}

public class LevelButton : MonoBehaviour
{
    public LevelInfo levelInfo;

    public GameObject selectionMark;
    public GameObject completeLevelMark;
    public GameObject perfectLevelMark;
    public GameObject checkpointFlag;
    public GameObject obtainedRank;
    public GameObject ComingSoonOverlay;
    public List<Image> greenCoinListImages;
    public LevelSelectionManager.MarioAnimator marioAnimator;

    public LevelImageData levelImageData;

    public Sprite[] GreenCoinsprite; // 0 - uncollected, 1 - collected, 2 - unavailable
    public Sprite[] minirankTypes; // 0 - poison, 1 - mushroom, 2 - flower, 3 - 1up, 4 - star

    private AudioSource audioSource;

    public bool Beaten
    {
        get
        {
            if (SaveLoadSystem.Instance == null) return false;
            return SaveLoadSystem.Instance.IsLevelCompleted(levelInfo.levelID);
        }
    }

    private bool IsLevelCompleted()
    {
        // Primary source: SaveManager
        if (SaveManager.Current != null && SaveManager.Current.levels != null)
        {
            foreach (var levelProgress in SaveManager.Current.levels)
            {
                if (levelProgress.levelID == levelInfo.levelID)
                {
                    // Assumes your LevelProgressData (or equivalent) has a bool "completed" field.
                    // If the field has a different name, update this line accordingly.
                    return levelProgress.completed;
                }
            }
        }

        // Legacy fallback: old PlayerPrefs key
        // return PlayerPrefs.GetInt("LevelCompleted_" + levelInfo.levelID, 0) == 1;
        return SaveLoadSystem.Instance != null && SaveLoadSystem.Instance.IsLevelCompleted(levelInfo.levelID);
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        LevelSelectionManager.Instance.levelButtons.Add(this);
        bool playable = LevelSelectionManager.IsLevelPlayable(this);

        if (!playable) // Unavailable level
        {
            foreach (Image coin in greenCoinListImages)
            {
                // coin.sprite = GreenCoinsprite[2];
                coin.gameObject.SetActive(false);
            }
            ComingSoonOverlay.SetActive(true);
            return;
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        string id = levelInfo.levelID;
        
        if (SaveLoadSystem.Instance == null) return;

        // Perfect/Complete Level Mark
        bool isPerfect = SaveLoadSystem.Instance != null && SaveLoadSystem.Instance.IsLevelPerfect(id);
        bool isCompleted = SaveLoadSystem.Instance != null && SaveLoadSystem.Instance.IsLevelCompleted(id);

        if (isPerfect)
        {
            perfectLevelMark.SetActive(true);
            completeLevelMark.SetActive(false);
        }
        else if (isCompleted)
        {
            perfectLevelMark.SetActive(false);
            completeLevelMark.SetActive(true);
        }
        else
        {
            perfectLevelMark.SetActive(false);
            completeLevelMark.SetActive(false);
        }

        // Green Coins
        var greenCoins = SaveLoadSystem.Instance.GetGreenCoins(id);
        int coinCount = Mathf.Min(greenCoinListImages.Count, greenCoins?.Length ?? 0);
        
        for (int i = 0; i < greenCoinListImages.Count; i++)
        {
            if (i < coinCount && greenCoins[i])
            {
                greenCoinListImages[i].sprite = GreenCoinsprite[1]; // collected
            }
            else
            {
                greenCoinListImages[i].sprite = GreenCoinsprite[0]; // uncollected
            }
        }

        // Checkpoint
        bool hasCheckpoint = SaveManager.HasCheckpointForLevel(id);
        checkpointFlag.SetActive(hasCheckpoint && SaveManager.Current.modifiers.checkpointMode != 0);

        // Rank
        int rank = SaveLoadSystem.Instance.GetHighestRank(id);
        if (rank > 0 && rank <= minirankTypes.Length)
        {
            obtainedRank.SetActive(true);
            obtainedRank.GetComponent<Image>().sprite = minirankTypes[rank - 1];
        }
        else
        {
            obtainedRank.SetActive(false);
        }
    }

    public void OnClick()
    {
        LevelSelectionManager.Instance.OnLevelButtonClick(this);
        levelImageData.UpdateLevelImageAndColors(0.5f, this);
    }

    public void OnDoubleClick()
    {
        if (!FadeInOutScene.Instance.isTransitioning)
        {
            LevelSelectionManager.Instance.OnPlayButtonClick();
            if (LevelSelectionManager.IsLevelPlayable(this)) {
                audioSource.Play();
            }
        }
    }

    public void UpdateCheckpointFlag()
    {
        string id = levelInfo.levelID;
        bool hasCheckpoint = SaveManager.HasCheckpointForLevel(id);
        checkpointFlag.SetActive(hasCheckpoint && SaveManager.Current.modifiers.checkpointMode != 0);
    }
}