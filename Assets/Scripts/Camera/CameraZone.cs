using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// A rectangular camera region with optional lock axes and entry/clamp policies.
/// </summary>
public class CameraZone : MonoBehaviour
{
    [Header("Bounds (World Space)")]
    public Vector2 topLeft;
    public Vector2 bottomRight;

    [Header("Lock Settings")] 
    [Tooltip("Lock camera vertically to the green guide. When true, camera Y is fixed at verticalMiddle + lockOffset.y")] 
    public bool lockToHorizontal = false;
    [Tooltip("Lock camera horizontally to the green guide. When true, camera X is fixed at horizontalMiddle + lockOffset.x")] 
    public bool lockToVertical = false;

    [Tooltip("Offset for stationary cameras or to bias where the camera centers within the zone. Ranges ~(-1,-1) bottom-left to (1,1) top-right.")]
    public Vector2 cameraPosOffset = Vector2.zero;

    [Tooltip("Additional offset applied to the green guide position")]
    public Vector2 lockOffset = Vector2.zero;

    [Header("Identity")] 
    [Tooltip("Unique ID for routing-based entry behavior")] 
    public string zoneId = "";

    [Tooltip("When multiple zones overlap, the one with the higher priority wins.")]
    public int priority = 0;

    public enum EntryRule
    {
        Always,           // Always snap when entering this zone
        Never,            // Never snap on entry
        OnlyFromListed,   // Snap only if previous zone is in "snapFromZoneIds"
        ExceptFromListed  // Snap unless previous zone is in "snapFromZoneIds"
    }

    [Tooltip("How this zone decides whether to snap ON ENTRY based on the previous zone.")]
    public EntryRule entryRule = EntryRule.Always;

    [Tooltip("Zone IDs used by OnlyFromListed / ExceptFromListed.")]
    public string[] snapFromZoneIds = System.Array.Empty<string>();

    public enum ClampPolicy
    {
        Always,            // Always clamp camera inside this zone while staying in it
        Never,             // Never clamp while in this zone
        FollowEntryRule    // Clamp only if the entry decision resulted in a snap
    }

    [Header("Stay Clamp Policy")]
    [Tooltip("How camera clamping behaves while the camera remains inside this zone.")]
    public ClampPolicy clampPolicy = ClampPolicy.FollowEntryRule;

    // ------------------------------------------------------------------------------------------
    private CameraFollow cameraFollow;
    public float horizontalMiddle => (topLeft.x + bottomRight.x) * 0.5f;
    public float verticalMiddle => (topLeft.y + bottomRight.y) * 0.5f;
    public float cameraMinX => lockToVertical ? (horizontalMiddle + lockOffset.x) : topLeft.x + (cameraFollow.camWidth / 2f);
    public float cameraMaxX => lockToVertical ? (horizontalMiddle + lockOffset.x) : bottomRight.x - (cameraFollow.camWidth / 2f);
    public float cameraMinY => lockToHorizontal ? (verticalMiddle + lockOffset.y) : bottomRight.y + (cameraFollow.camHeight / 2f);
    public float cameraMaxY => lockToHorizontal ? (verticalMiddle + lockOffset.y) : topLeft.y - (cameraFollow.camHeight / 2f);

    private void Start()
    {
        cameraFollow = GetComponent<CameraFollow>();
    }

    /// <summary>
    /// Should we snap on entering THIS zone, given the previous zone?
    /// </summary>
    public bool ShouldSnapOnEnterFrom(CameraZone previous)
    {
        switch (entryRule)
        {
            case EntryRule.Always:
                return true;
            case EntryRule.Never:
                return false;
            case EntryRule.OnlyFromListed:
                if (previous == null) return false;
                return snapFromZoneIds != null && snapFromZoneIds.Contains(previous.zoneId);
            case EntryRule.ExceptFromListed:
                if (previous == null) return true;
                return snapFromZoneIds == null || !snapFromZoneIds.Contains(previous.zoneId);
        }
        return false;
    }

    /// <summary>
    /// Decide clamping for the duration of staying inside this zone, based on policy and entry result.
    /// </summary>
    public bool ShouldClampForStay(bool snappedOnEntry)
    {
        switch (clampPolicy)
        {
            case ClampPolicy.Always: return true;
            case ClampPolicy.Never: return false;
            case ClampPolicy.FollowEntryRule: return snappedOnEntry;
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Bounds (red rectangle)
        Gizmos.color = Color.red;
        Vector2 tl = topLeft;          
        Vector2 br = bottomRight;     
        Vector2 tr = new Vector2(br.x, tl.y);
        Vector2 bl = new Vector2(tl.x, br.y);
        Gizmos.DrawLine(tl, tr);
        Gizmos.DrawLine(tl, bl);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(br, bl);

        // Guide(s) (green)
        float hMid = Mathf.Approximately(horizontalMiddle, 0f) ? (topLeft.x + bottomRight.x) * 0.5f : horizontalMiddle;
        float vMid = Mathf.Approximately(verticalMiddle, 0f) ? (topLeft.y + bottomRight.y) * 0.5f : verticalMiddle;

        if (lockToHorizontal && !lockToVertical)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector2(topLeft.x, vMid + lockOffset.y), new Vector2(bottomRight.x, vMid + lockOffset.y));
        }
        else if (lockToVertical && !lockToHorizontal)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector2(hMid + lockOffset.x, topLeft.y), new Vector2(hMid + lockOffset.x, bottomRight.y));
        }
        else if (lockToHorizontal && lockToVertical)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(new Vector2(hMid + lockOffset.x, vMid + lockOffset.y), 0.3f);
        }
    }
#endif
}