using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Owns all carry, drop, throw, useable-object, and push interactions.
///
/// Responsibilities:
/// - CheckForCarry() raycast and pickup
/// - DropCarry() with wall-aware placement
/// - ThrowCarry() with wall-aware placement
/// - Useable object stack (levers, switches)
/// - Push API (called by pushable objects, drives PushState)
///
/// Writes to: State.Carrying, State.Pushing, State.PushingObject, State.PushingSpeed
/// Reads from: State.FacingRight, State.PowerupState
/// </summary>
[RequireComponent(typeof(MarioCore))]
public class MarioCarry : MonoBehaviour
{
    [Header("Grab Settings")]
    public bool PressRunToGrab = true;
    public bool CrouchToGrab  = false;
    public CarryMethod CarryMethod = CarryMethod.Normal;

    [Header("Carry Position")]
    public GameObject HeldObjectPosition;

    [Header("Grab Raycast")]
    [Tooltip("Empty GameObject positioned at grab height in the prefab. " +
             "Move it visually per Mario size — no code changes needed.")]
    public Transform grabOrigin;
    [SerializeField] private float _grabRaycastDistance = 0.6f;

    private MarioCore  _core;
    private MarioState State       => _core.State;
    private int        PlayerIndex => _core.PlayerIndex;

    private readonly List<UseableObject> _useableObjects = new();

    private void Awake() => _core = GetComponent<MarioCore>();

    // ─── Carry ───────────────────────────────────────────────────────────────

    public void CheckForCarry()
    {
        if (State.IsDead) return;

        Vector2 dir    = State.FacingRight ? Vector2.right : Vector2.left;
        // grabOrigin is an empty child GameObject positioned at the correct
        // grab height per prefab — Small Mario and Big Mario each have it
        // placed visually, so no code calculation is needed.
        Vector3 origin = grabOrigin != null ? grabOrigin.position : transform.position;

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, _grabRaycastDistance);

        foreach (var h in hits)
        {
            if (!h.collider.TryGetComponent(out ObjectPhysics obj)) continue;
            if (obj.carried || !obj.carryable)                       continue;
            Carry(obj);
            return;
        }
    }

    private void Carry(ObjectPhysics obj)
    {
        State.Carrying = true;
        obj.transform.SetParent(HeldObjectPosition.transform);
        obj.getCarried();
        MarioEvents.FirePickedUp(PlayerIndex, obj);
    }

    public void DropCarry()
    {
        if (!State.Carrying || HeldObjectPosition.transform.childCount == 0) return;

        State.Carrying = false;

        var obj = HeldObjectPosition.transform.GetChild(0).GetComponent<ObjectPhysics>();
        if (obj == null) return;

        obj.transform.SetParent(null);
        obj.transform.rotation = Quaternion.identity;

        float halfWidth = obj.width / 2f;
        float yOffset   = State.PowerupState == PowerStates.PowerupState.small ? 0f : -0.5f;

        Vector2? wall = ThrowRaycast(yOffset, 1f + halfWidth, obj.wallMask);
        if (wall.HasValue)
        {
            obj.transform.position = (Vector2)wall.Value
                + new Vector2(State.FacingRight ? -halfWidth : halfWidth, 0f);
            float backX = State.FacingRight
                ? wall.Value.x - obj.width - 0.5f
                : wall.Value.x + obj.width + 0.5f;
            transform.position = new Vector3(backX, transform.position.y, transform.position.z);
        }
        else
        {
            obj.transform.position = transform.position
                + new Vector3(State.FacingRight ? 1 : -1, yOffset, 0f);
        }

        obj.getDropped(State.FacingRight);
        MarioEvents.FireDropped(PlayerIndex, obj);
    }

    public void ThrowCarry()
    {
        if (!State.Carrying || HeldObjectPosition.transform.childCount == 0) return;

        State.Carrying = false;

        var obj = HeldObjectPosition.transform.GetChild(0).GetComponent<ObjectPhysics>();
        if (obj == null) return;

        obj.transform.SetParent(null);
        obj.transform.rotation = Quaternion.identity;

        float halfWidth = obj.width / 2f;
        float yOffset   = State.PowerupState == PowerStates.PowerupState.small ? 0.1f : -0.1f;

        Vector2? wall = ThrowRaycast(yOffset, 1f + halfWidth, obj.wallMask);
        if (wall.HasValue)
        {
            obj.transform.position = (Vector2)wall.Value
                + new Vector2(State.FacingRight ? -halfWidth : halfWidth, 0f);
            float backX = State.FacingRight
                ? wall.Value.x - obj.width - 0.5f
                : wall.Value.x + obj.width + 0.5f;
            transform.position = new Vector3(backX, transform.position.y, transform.position.z);
        }
        else
        {
            obj.transform.position = transform.position
                + new Vector3(State.FacingRight ? 1 : -1, yOffset, 0f);
        }

        obj.GetThrown(State.FacingRight);
        MarioEvents.FireThrown(PlayerIndex, obj);
    }

    private Vector2? ThrowRaycast(float yOffset, float distance, int layerMask)
    {
        layerMask &= ~(1 << gameObject.layer);
        Vector3      origin = transform.position + new Vector3(0f, yOffset, 0f);
        Vector2      dir    = State.FacingRight ? Vector2.right : Vector2.left;
        RaycastHit2D hit    = Physics2D.Raycast(origin, dir, distance, layerMask);
        return hit.collider != null ? (Vector2?)hit.point : null;
    }

    // ─── Useable Objects ─────────────────────────────────────────────────────

    public void AddUseableObject(UseableObject obj)
    {
        if (!_useableObjects.Contains(obj)) _useableObjects.Add(obj);
    }

    public void RemoveUseableObject(UseableObject obj) => _useableObjects.Remove(obj);

    public void TryUseObject()
    {
        if (_useableObjects.Count > 0)
            _useableObjects[^1].Use(_core);
    }

    // ─── Push API (called by pushable ObjectPhysics) ─────────────────────────

    public void StartPushing(ObjectPhysics pushObject, float speed)
    {
        State.Pushing       = true;
        State.PushingObject = pushObject;
        State.PushingSpeed  = speed;
        _core.StateMachine.ForceTransition(MarioStateID.Push);
    }

    public void StopPushing()
    {
        State.Pushing       = false;
        State.PushingObject = null;
    }

    // ─── Transfer to New Mario ───────────────────────────────────────────────

    public void TransferCarryTo(MarioCarry target)
    {
        if (!State.Carrying || HeldObjectPosition.transform.childCount == 0) return;

        var obj = HeldObjectPosition.transform.GetChild(0).gameObject;
        obj.transform.SetParent(target.HeldObjectPosition.transform);
        obj.transform.localPosition = Vector3.zero;
        target.State.Carrying = true;
    }
}