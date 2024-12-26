using UnityEngine;
using UnityEngine.UI;

public class DPadHighlighter : MonoBehaviour
{
    [System.Serializable]
    public class DirectionConfig
    {
        public Image directionImage;  // The Image for this direction (Up, Down, Left, Right)
        public Sprite idleSprite;    // Sprite when the direction is idle
        public Sprite activeSprite;  // Sprite when the direction is active
    }

    public DirectionConfig up;
    public DirectionConfig down;
    public DirectionConfig left;
    public DirectionConfig right;

    public void UpdateDPad(Vector2 input)
    {
        // Reset all directions to idle
        SetDirectionSprite(up, false);
        SetDirectionSprite(down, false);
        SetDirectionSprite(left, false);
        SetDirectionSprite(right, false);

        // Highlight the active direction based on input
        if (input.y > 0) SetDirectionSprite(up, true);
        if (input.y < 0) SetDirectionSprite(down, true);
        if (input.x > 0) SetDirectionSprite(right, true);
        if (input.x < 0) SetDirectionSprite(left, true);
    }

    private void SetDirectionSprite(DirectionConfig config, bool isActive)
    {
        if (config != null && config.directionImage != null)
        {
            config.directionImage.sprite = isActive ? config.activeSprite : config.idleSprite;
        }
    }
}