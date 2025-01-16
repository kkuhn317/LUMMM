using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class ObjectIndicator : MonoBehaviour
{
    [System.Serializable]
    public class ObjectIndicatorSettings
    {
        public Transform targetObject; // The object to track
        public Sprite indicatorSprite; // Custom sprite for the indicator
        public Vector3 indicatorPosition; // Direct position for the indicator
    }

    public ObjectIndicatorSettings[] objectSettings; // List of objects and their settings
    public GameObject existingIndicator; // Reference to the existing indicator in the scene

    private ObjectIndicatorSettings currentSettings;
    private Image indicatorImage; // UI Image component of the indicator

    void Start()
    {
        if (existingIndicator == null)
        {
            Debug.LogError("Existing indicator is not assigned in the Inspector!");
            return;
        }

        // Get the Image component from the existing indicator
        indicatorImage = existingIndicator.GetComponent<Image>();
        if (indicatorImage == null)
        {
            Debug.LogError("The existing indicator does not have an Image component!");
        }
    }

    void Update()
    {
        // Check the currently selected GameObject in the Event System
        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;

        if (selectedObject != null)
        {
            // Validate if the selected object is part of objectSettings
            currentSettings = GetObjectSettings(selectedObject.transform);

            if (currentSettings != null)
            {
                UpdateIndicator();
            }
            else
            {
                // If the selected object is not in objectSettings, disable the indicator
                existingIndicator.SetActive(false);
            }
        }
    }

    private ObjectIndicatorSettings GetObjectSettings(Transform selectedObject)
    {
        // Search for the selected object in the settings array
        foreach (var settings in objectSettings)
        {
            if (settings.targetObject == selectedObject)
            {
                return settings; // Found a match
            }
        }
        return null; // No match found
    }

    private void UpdateIndicator()
    {
        if (currentSettings != null && existingIndicator != null)
        {
            // Enable the indicator
            existingIndicator.SetActive(true);

            // Get the RectTransform of the indicator
            RectTransform indicatorRect = existingIndicator.GetComponent<RectTransform>();

            if (indicatorRect != null)
            {
                // Set the indicator's local position directly
                //indicatorRect.localPosition = currentSettings.indicatorPosition;

                // Set the indicator's local position directly, offset from the target object
                
                // First make the indicator a sibling of the target object
                existingIndicator.transform.SetParent(currentSettings.targetObject.parent);
                // Then set the indicator's position relative to the target object
                indicatorRect.localPosition = currentSettings.indicatorPosition + currentSettings.targetObject.localPosition;

                // Update the image's sprite if specified
                if (indicatorImage != null && currentSettings.indicatorSprite != null)
                {
                    indicatorImage.sprite = currentSettings.indicatorSprite;
                }
            }
            else
            {
                Debug.LogError("Indicator does not have a RectTransform!");
            }
        }
        else
        {
            Debug.LogWarning("Target object, settings, or existing indicator is missing!");
            if (existingIndicator != null)
            {
                existingIndicator.SetActive(false);
            }
        }
    }
}
