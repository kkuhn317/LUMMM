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
    public List<Image> greenCoinListImages;
    public LevelSelectionManager.MarioAnimator marioAnimator;

    public LevelImageData levelImageData;

    public Sprite[] GreenCoinsprite; // 0 - uncollected, 1 - collected, 2 - unavailable
    public Sprite[] minirankTypes; // 0 - poison, 1 - mushroom, 2 - flower, 3 - 1up, 4 - star

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        LevelSelectionManager.Instance.levelButtons.Add(this);

        if (levelInfo.levelScene == "") // Unavailable level
        {
            foreach (Image coin in greenCoinListImages)
            {
                // coin.sprite = GreenCoinsprite[2];
                coin.gameObject.SetActive(false);
            }
            return;
        }

        string id = levelInfo.levelID;
        // Set the saved info
        
        // Perfect/Complete Level Mark
        // TODO: Set the LevelPerfect playerpref when you perfect a level
        if (PlayerPrefs.GetInt("LevelPerfect_" + id, 0) == 1)
        {
            perfectLevelMark.SetActive(true);
        }
        else if (PlayerPrefs.GetInt("LevelCompleted_" + id, 0) == 1)
        {
            completeLevelMark.SetActive(true);
        }

        // Coins
        for (int i = 0; i < 3; i++)
        {
            int sprite = PlayerPrefs.GetInt("CollectedCoin" + i + "_" + id, 0);
            greenCoinListImages[i].sprite = GreenCoinsprite[sprite];
        }

        // Checkpoint
        if (PlayerPrefs.GetString("SavedLevel") == id && PlayerPrefs.GetInt(SettingsKeys.CheckpointsKey, 0) == 1)
        {
            checkpointFlag.SetActive(true);
        }
        else{
            checkpointFlag.SetActive(false);
        }

        // Rank
        int rank = PlayerPrefs.GetInt("HighestPlayerRank_" + id, -1);
        if (rank > 0)
        {
            obtainedRank.SetActive(true);
            obtainedRank.GetComponent<Image>().sprite = minirankTypes[rank - 1];
        }
    }

    public void OnClick()
    {
        LevelSelectionManager.Instance.OnLevelButtonClick(this);
        levelImageData.UpdateLevelImageAndColors(0.5f, this);
    }

    public void OnDoubleClick()
    {
        if (!FadeInOutScene.Instance.transitioning)
        {
            LevelSelectionManager.Instance.OnPlayButtonClick();
            if (LevelSelectionManager.IsLevelPlayable(this)) {
                audioSource.Play();
            }
        }
    }

    public void UpdateCheckpointFlag()
    {
        bool hasCheckpoint = PlayerPrefs.HasKey("SavedCheckpoint") &&
                             PlayerPrefs.GetString("SavedLevel") == levelInfo.levelID;
        checkpointFlag.SetActive(hasCheckpoint);
    }
}