using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [System.Serializable]
    public class MenuSection
    {
        public GameObject sectionRoot; // The GameObject containing this section
        public Button defaultButton; // The button to select when this section is active
        public bool deactivateOnSwitch = false; // Whether to deactivate this section when switching away
    }

    [Header("Menu Sections")]
    [Tooltip("Define the different sections of your menu.")]
    public MenuSection[] sections;

    private int currentSection = 0;

    private void Start()
    {
        // Ensure the initial state of all sections
        for (int i = 0; i < sections.Length; i++)
        {
            sections[i].sectionRoot.SetActive(i == currentSection || !sections[i].deactivateOnSwitch);
        }

        HighlightDefaultButton(sections[currentSection].defaultButton);
    }

    public void GoToSection(int sectionIndex)
    {
        if (sectionIndex < 0 || sectionIndex >= sections.Length)
        {
            Debug.LogError($"Invalid section index: {sectionIndex}");
            return;
        }

        // Handle current section
        if (sections[currentSection].deactivateOnSwitch)
        {
            sections[currentSection].sectionRoot.SetActive(false);
        }

        // Update current section
        currentSection = sectionIndex;

        // Handle new section
        if (sections[currentSection].deactivateOnSwitch)
        {
            sections[currentSection].sectionRoot.SetActive(true);
        }

        HighlightDefaultButton(sections[currentSection].defaultButton);
    }

    public void GoToPreviousSection()
    {
        if (currentSection > 0)
        {
            GoToSection(currentSection - 1);
        }
    }

    private void HighlightDefaultButton(Button button)
    {
        if (button != null)
        {
            // Ensure there is an EventSystem in the scene
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
                button.Select();
            }
            else
            {
                Debug.LogWarning("No EventSystem detected in the scene! Ensure one is present for navigation.");
            }
        }
        else
        {
            Debug.LogWarning("No default button assigned for this section!");
        }
    }
}