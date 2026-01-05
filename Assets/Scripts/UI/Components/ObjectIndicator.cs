using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ObjectIndicator : MonoBehaviour
{
    [System.Serializable]
    public class ObjectIndicatorSettings
    {
        public Transform targetObject;
        public Sprite indicatorSprite;
        public Vector3 indicatorPosition;
    }

    public ObjectIndicatorSettings[] objectSettings;
    public GameObject existingIndicator;

    private ObjectIndicatorSettings currentSettings;
    private Image indicatorImage;

    public AudioClip onIndicatorMove;
    public AudioClip onIndicatorHide;

    private GameObject lastSelectedObject;
    private bool audioArmed;

    void OnDisable()
    {
        if (objectSettings.Length > 0 && objectSettings[0].targetObject != null)
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(objectSettings[0].targetObject.gameObject);
            }
        }
    }

    void Start()
    {
        if (existingIndicator == null)
        {
            Debug.LogError("Existing indicator is not assigned in the Inspector!");
            enabled = false;
            return;
        }

        indicatorImage = existingIndicator.GetComponent<Image>();
        if (indicatorImage == null)
        {
            Debug.LogError("The existing indicator does not have an Image component!");
        }

        if (EventSystem.current != null)
            lastSelectedObject = EventSystem.current.currentSelectedGameObject;

        audioArmed = false;
    }

    void Update()
    {
        if (EventSystem.current == null)
            return;

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;

        if (selectedObject == null)
        {
            if (existingIndicator.activeSelf)
                HideIndicator(false);
            return;
        }

        bool selectionChanged = selectedObject != lastSelectedObject;

        currentSettings = GetObjectSettings(selectedObject.transform);

        if (currentSettings != null)
        {
            UpdateIndicator();

            if (selectionChanged)
            {
                if (!audioArmed)
                {
                    audioArmed = true;
                }
                else if (onIndicatorMove != null)
                {
                    PlayOneShot(onIndicatorMove);
                }
            }
        }
        else
        {
            if (existingIndicator.activeSelf)
                HideIndicator(audioArmed);

            if (selectionChanged && !audioArmed)
                audioArmed = true;
        }

        if (selectionChanged)
            lastSelectedObject = selectedObject;
    }

    private ObjectIndicatorSettings GetObjectSettings(Transform selectedObject)
    {
        foreach (var settings in objectSettings)
        {
            if (settings != null && settings.targetObject == selectedObject)
                return settings;
        }
        return null;
    }

    private void UpdateIndicator()
    {
        if (currentSettings == null || existingIndicator == null)
        {
            if (existingIndicator != null)
                existingIndicator.SetActive(false);
            return;
        }

        existingIndicator.SetActive(true);

        RectTransform indicatorRect = existingIndicator.GetComponent<RectTransform>();
        if (indicatorRect == null)
        {
            Debug.LogError("Indicator does not have a RectTransform!");
            return;
        }

        if (currentSettings.targetObject == null)
        {
            existingIndicator.SetActive(false);
            return;
        }

        existingIndicator.transform.SetParent(currentSettings.targetObject.parent);
        indicatorRect.localPosition = currentSettings.indicatorPosition + currentSettings.targetObject.localPosition;

        if (indicatorImage != null && currentSettings.indicatorSprite != null)
            indicatorImage.sprite = currentSettings.indicatorSprite;
    }

    private void HideIndicator(bool playSfx)
    {
        existingIndicator.SetActive(false);

        if (playSfx && onIndicatorHide != null)
            PlayOneShot(onIndicatorHide);
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || AudioManager.Instance == null)
            return;

        AudioManager.Instance.Play(clip, SoundCategory.SFX);
    }

    void OnDrawGizmos()
    {
        if (objectSettings == null)
            return;

        for (int i = 0; i < objectSettings.Length; i++)
        {
            var s = objectSettings[i];
            if (s == null || s.targetObject == null)
                continue;

            Transform parent = s.targetObject.parent;
            if (parent == null)
                continue;

            Vector3 targetWorld = s.targetObject.position;

            // Matches your runtime positioning rule:
            // indicator local = target.local + indicatorPosition (in parent space)
            Vector3 indicatorWorld = parent.TransformPoint(s.targetObject.localPosition + s.indicatorPosition);

            Gizmos.DrawSphere(targetWorld, 0.03f);
            Gizmos.DrawLine(targetWorld, indicatorWorld);
            Gizmos.DrawWireSphere(indicatorWorld, 0.05f);
        }
    }
}