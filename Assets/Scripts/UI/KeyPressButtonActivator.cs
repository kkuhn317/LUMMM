using UnityEngine;
using UnityEngine.UI;

public class KeyPressButtonActivator : MonoBehaviour
{
    public Button backButton;

    void Update()
    {
        // Check for key press, e.g., 'Backspace'
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            // Simulate button click
            backButton.onClick.Invoke();
        }
    }
}
