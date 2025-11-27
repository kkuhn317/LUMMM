using UnityEngine;

[CreateAssetMenu(menuName = "Popups/Popup Style Set")]
public class PopupStyle : ScriptableObject
{
    public float moveSpeed = 1.5f;
    public Vector3 moveDirection = Vector3.up;

    public Vector2 scaleMultiplier = Vector2.one;

    public Color tint = Color.white;
}