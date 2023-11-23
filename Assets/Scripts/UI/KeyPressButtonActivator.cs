using UnityEngine;
using UnityEngine.UI;

public class KeyPressButtonActivator : MonoBehaviour
{
    public Button targetButton;
    public string[] inputButtonNames = { "Select" }; // Add the desired input button names

    void Update()
    {
        // Check for button press using the specified input buttons
        foreach (string inputButtonName in inputButtonNames)
        {
            if (Input.GetButtonDown(inputButtonName))
            {
                // Simulate button click
                targetButton.onClick.Invoke();
            }
        }
    }
}
