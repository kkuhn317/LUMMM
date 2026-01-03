using UnityEngine;

public class ScorePopupManager : MonoBehaviour
{
    public static ScorePopupManager Instance { get; private set; }

    [Header("Pool")]
    public ObjectPool popupPool;

    [Header("Sprite Database")]
    public PopupDataSet popupDatabase;

    [Header("Scale per Power State")]
    public Vector3 tinyScale = Vector3.one * 0.6f;
    public Vector3 smallScale = Vector3.one * 0.8f;
    public Vector3 bigScale = Vector3.one * 1.0f;
    public Vector3 powerScale = Vector3.one * 1.0f;

    [Header("Offset per Power State")]
    public Vector3 tinyOffset = new Vector3(0f, 0.5f, 0f);
    public Vector3 smallOffset = new Vector3(0f, 0.5f, 0f);
    public Vector3 bigOffset = new Vector3(0f, 1.0f, 0f);
    public Vector3 powerOffset = new Vector3(0f, 1.0f, 0f);

    [Header("Motion per Power State")]
    public float tinyMotionMultiplier = 0.25f;
    public float smallMotionMultiplier = 1.0f;
    public float bigMotionMultiplier = 1.0f;
    public float powerMotionMultiplier = 1.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ShowPopup(ComboResult result, Vector3 worldPosition)
    {
        ShowPopup(result, worldPosition, PowerStates.PowerupState.big);
    }

    public void ShowPopup(ComboResult result, Vector3 worldPosition, PowerStates.PowerupState powerState)
    {
        if (popupPool == null || popupDatabase == null)
        {
            Debug.LogWarning("ScorePopupManager: popupPool or popupDatabase is not assigned.");
            return;
        }

        Sprite sprite = popupDatabase.GetSprite(result.popupID, powerState);
        if (sprite == null)
            return;

        Vector3 finalPos = worldPosition + GetOffset(powerState);

        GameObject go = popupPool.Get();
        if (go == null)
            return;

        go.transform.position = finalPos;
        go.transform.localScale = GetScale(powerState);

        var popup = go.GetComponent<ScorePopupSprite>();
        if (popup == null)
        {
            Debug.LogError("ScorePopupManager: popupPool prefab is missing ScorePopupSprite.");
            return;
        }

        popup.Init(sprite, GetMotionMultiplier(powerState));
    }

    private Vector3 GetScale(PowerStates.PowerupState state)
    {
        return state switch
        {
            PowerStates.PowerupState.tiny => tinyScale,
            PowerStates.PowerupState.small => smallScale,
            PowerStates.PowerupState.big => bigScale,
            PowerStates.PowerupState.power => powerScale,
            _ => Vector3.one
        };
    }

    private Vector3 GetOffset(PowerStates.PowerupState state)
    {
        return state switch
        {
            PowerStates.PowerupState.tiny => tinyOffset,
            PowerStates.PowerupState.small => smallOffset,
            PowerStates.PowerupState.big => bigOffset,
            PowerStates.PowerupState.power => powerOffset,
            _ => Vector3.zero
        };
    }

    private float GetMotionMultiplier(PowerStates.PowerupState state) => state switch
    {
        PowerStates.PowerupState.tiny  => tinyMotionMultiplier,
        PowerStates.PowerupState.small => smallMotionMultiplier,
        PowerStates.PowerupState.big   => bigMotionMultiplier,
        PowerStates.PowerupState.power => powerMotionMultiplier,
        _ => 1f
    };
}