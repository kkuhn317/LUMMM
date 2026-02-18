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

    bool playerInside = false; // True when at least ONE player is inside.

    public bool active = true;

    public float EnterDelay = 0f;   // Delay before enter action happens
    public float ExitDelay = 0f;    // Delay before exit action happens

    private PlayerRegistry playerRegistry;

    // this is used to prevent multiple coroutines from stacking in weird edge cases
    private Coroutine enterRoutine;
    private Coroutine exitRoutine;

    private void Start()
    {
        CacheRegistry();
    }

    private void CacheRegistry()
    {
        if (GameManager.Instance != null)
            playerRegistry = GameManager.Instance.GetSystem<PlayerRegistry>();

        if (playerRegistry == null)
            playerRegistry = FindObjectOfType<PlayerRegistry>(true);
    }

    void Update()
    {
        if (!active) return;
        if (onPlayerEnter == null) return;

        if (playerRegistry == null) CacheRegistry();
        if (playerRegistry == null) return;

        bool anyPlayerInTrigger = AnyPlayerIsInTrigger();

        if (anyPlayerInTrigger)
        {
            if (!playerInside)
            {
                playerInside = true;

                // Avoid stacking enter/exit routines if something toggles fast
                if (exitRoutine != null) { StopCoroutine(exitRoutine); exitRoutine = null; }
                if (enterRoutine != null) StopCoroutine(enterRoutine);

                enterRoutine = StartCoroutine(DoAction(onPlayerEnter, EnterDelay));

                if (autoDeactivate)
                {
                    // Deactivate the trigger (same behavior as before)
                    active = false;
                }
            }
        }
        else
        {
            if (playerInside)
            {
                playerInside = false;

                // Avoid stacking enter/exit routines if something toggles fast
                if (enterRoutine != null) { StopCoroutine(enterRoutine); enterRoutine = null; }
                if (exitRoutine != null) StopCoroutine(exitRoutine);

                exitRoutine = StartCoroutine(DoAction(onPlayerExit, ExitDelay));

                if (autoDeactivate)
                {
                    // Deactivate the trigger (same behavior as before)
                    active = false;
                }
            }
        }
    }

    IEnumerator DoAction(UnityEvent action, float delay)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        action.Invoke();
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

    bool AnyPlayerIsInTrigger()
    {
        GameObject[] players = playerRegistry.GetAllPlayerObjects();
        if (players == null || players.Length == 0) return false;

        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            if (p == null) continue;

            Vector2 pos = p.transform.position;

            if (pos.x > bottomLeft.x && pos.x < topRight.x &&
                pos.y > bottomLeft.y && pos.y < topRight.y)
            {
                return true;
            }
        }

        return false;
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