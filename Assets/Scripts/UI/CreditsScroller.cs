using UnityEngine;
using TMPro;

public class CreditsScroller : MonoBehaviour
{
    public GameObject creditsText; // Reference to the TMP Text component for credits
    public float scrollSpeed = 50f; // Speed at which the credits scroll
    public float scrollUpSpeed = 50f; // Speed at which the credits scroll upwards when up arrow is pressed
    public float fastScrollSpeed = 100f; // Speed at which the credits scroll when down arrow is pressed
    private float currentScrollSpeed = 0f;
    private float startPositionY; // The starting position of the credits
    public float stopPositionY; // The position at which the credits should stop scrolling

    public GameObject middleOfScreen;

    void Start()
    {
        currentScrollSpeed = scrollSpeed;
        startPositionY = creditsText.transform.position.y;
    }

    void Update()
    {
        // Move the credits upwards based on the scroll speed
        Vector3 newPos = creditsText.transform.position + Vector3.up * currentScrollSpeed * Time.deltaTime * Screen.height / 1080;

        // Adjust the stop position based on the screen resolution
        float adjustedStopPositionY = stopPositionY * Screen.height / 1080;

        // Snap the credits between startPositionY and adjustedStopPositionY
        newPos.y = Mathf.Clamp(newPos.y, startPositionY, adjustedStopPositionY);

        creditsText.transform.position = newPos;
    }

    public void OnUpButtonPress()
    {
        currentScrollSpeed = -scrollUpSpeed;
    }
    public void OnDownButtonPress()
    {
        currentScrollSpeed = fastScrollSpeed;
    }
    public void OnButtonRelease()
    {
        currentScrollSpeed = scrollSpeed;
    }

    // Show the stop position in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        if (middleOfScreen == null)
        {
            return;
        }

        float adjustedStopPositionY = stopPositionY * Camera.main.pixelHeight / 1080;
        float drawPosY = creditsText.transform.position.y - adjustedStopPositionY + middleOfScreen.transform.position.y;
        

        Vector3 drawPos = new Vector3(creditsText.transform.position.x, drawPosY, creditsText.transform.position.z);

        Gizmos.DrawSphere(drawPos, 50f);
    }
}
