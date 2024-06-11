using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PositionTriggerEvent : MonoBehaviour
{
    public Vector2 size = new Vector2(1, 1);

    [SerializeField] UnityEvent onPlayerEnter;
    [SerializeField] UnityEvent onPlayerExit;
    [SerializeField] bool autoDeactivate = false;

    GameObject player;  // dont use this directly, use getPlayer() instead

    bool playerInside = false;

    public bool active = true;

    private GameObject getPlayer()
    {
        if (player == null)
        {
            // TODO: support multiple players
            MarioMovement playerscript = GameManager.Instance.GetPlayer(0);
            if (playerscript != null)
            {
                player = playerscript.gameObject;
            } else
            {
                return null;
            }
        }
        return player;
    }

    void Update()
    {
        if (!active) return;
        if (onPlayerEnter == null) return;

        if (PlayerIsInTrigger())
        {
            if (!playerInside)
            {
                onPlayerEnter.Invoke();
                playerInside = true;

                if (autoDeactivate)
                {
                    // Deactivate the trigger
                    active = false;
                }
            }
        }
        else
        {
            if (playerInside)
            {
                onPlayerExit.Invoke();
                playerInside = false;

                if (autoDeactivate)
                {
                    // Deactivate the trigger
                    active = false;
                }
            }
        }
    }

    // For use by unity events
    public void Activate()
    {
        active = true;
    }

    public void Deactivate()
    {
        active = false;
    }


    Vector2 topLeft => (Vector2)transform.position + new Vector2(-size.x / 2, size.y / 2);
    Vector2 topRight => (Vector2)transform.position + new Vector2(size.x / 2, size.y / 2);
    Vector2 bottomLeft => (Vector2)transform.position + new Vector2(-size.x / 2, -size.y / 2);
    Vector2 bottomRight => (Vector2)transform.position + new Vector2(size.x / 2, -size.y / 2);

    bool PlayerIsInTrigger()
    {
        GameObject player = getPlayer();
        if (player == null) return false;
        Vector2 playerPos = player.transform.position;
        Vector2 triggerPos = transform.position;
        return playerPos.x > bottomLeft.x && playerPos.x < topRight.x && playerPos.y > bottomLeft.y && playerPos.y < topRight.y;
    }

    // Draw the trigger area
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }

}
