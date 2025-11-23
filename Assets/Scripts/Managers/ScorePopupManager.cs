using UnityEngine;

public class ScorePopupManager : MonoBehaviour
{
    public static ScorePopupManager Instance { get; private set; }

    public GameObject popupPrefab;           // Prefab con ScorePopupSprite
    public PopupSpriteEntry[] popupSprites;  // Tabla id → sprite

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ShowPopup(string id, Vector3 worldPosition)
    {
        if (popupPrefab == null) return;

        Sprite sprite = GetSpriteById(id);
        if (sprite == null) return;

        GameObject popupGO = Instantiate(popupPrefab, worldPosition, Quaternion.identity);

        var popup = popupGO.GetComponent<ScorePopupSprite>();
        if (popup != null)
        {
            popup.Init(sprite);
        }
    }

    private Sprite GetSpriteById(string id)
    {
        for (int i = 0; i < popupSprites.Length; i++)
        {
            if (popupSprites[i].id == id)
                return popupSprites[i].sprite;
        }
        return null;
    }
}