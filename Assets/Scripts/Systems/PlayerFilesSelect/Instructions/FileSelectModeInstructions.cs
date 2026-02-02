using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FileSelectModeInstructions : MonoBehaviour
{
    [Header("References")]
    public SaveSlotManager slotManager;
    [Tooltip("Parent that contains ONLY the instruction objects. We reorder within this container only.")]
    public Transform instructionsContainer;

    [Header("Instruction objects")]
    public GameObject normal;
    public GameObject delete;
    public GameObject copySelectSource;
    public GameObject copySelectDest;
    public GameObject importSelectDest;
    public GameObject exportSelectSource;

    [Header("Animator Triggers")]
    public string downTrigger = "Down";
    public string upTrigger = "Up";

    [Header("Animator State Names")]
    [Tooltip("Exact Up state name as shown in the Animator.")]
    public string upStateName = "FileSelect_SpikeColumn_Up"; // might change to a better to do it, but this works

    [Tooltip("Animator layer index (usually 0).")]
    public int animatorLayer = 0;

    // Track current visible instruction GO
    private GameObject current;

    // To prevent old coroutines disabling the wrong object
    private readonly Dictionary<GameObject, Coroutine> hideRoutines = new();

    private void OnEnable()
    {
        if (slotManager != null)
            slotManager.ModeChanged += OnModeChanged;

        SetModeInstant(slotManager != null ? slotManager.CurrentMode : SaveSlotManager.InteractionMode.Normal);
    }

    private void OnDisable()
    {
        if (slotManager != null)
            slotManager.ModeChanged -= OnModeChanged;

        foreach (var kv in hideRoutines)
        {
            if (kv.Value != null) StopCoroutine(kv.Value);
        }
        hideRoutines.Clear();
    }

    private void OnModeChanged(SaveSlotManager.InteractionMode mode)
    {
        TransitionTo(GetGOForMode(mode));
    }

    private void SetModeInstant(SaveSlotManager.InteractionMode mode)
    {
        var go = GetGOForMode(mode);

        SafeSetActive(normal, false);
        SafeSetActive(delete, false);
        SafeSetActive(copySelectSource, false);
        SafeSetActive(copySelectDest, false);
        SafeSetActive(importSelectDest, false);
        SafeSetActive(exportSelectSource, false);

        current = go;

        if (current != null)
        {
            CancelPendingHide(current);

            SafeSetActive(current, true);

            // Only reorder inside the instructions container
            BringToFront(current);

            PlayTrigger(current, downTrigger);
        }
    }

    private void TransitionTo(GameObject next)
    {
        if (next == current)
            return;

        if (current != null)
            PlayUpAndDisable(current);

        current = next;
        if (current != null)
        {
            CancelPendingHide(current);

            SafeSetActive(current, true);

            // Only reorder inside the instructions container
            BringToFront(current);

            PlayTrigger(current, downTrigger);
        }
    }

    private void PlayUpAndDisable(GameObject go)
    {
        CancelPendingHide(go);

        SafeSetActive(go, true);
        PlayTrigger(go, upTrigger);

        hideRoutines[go] = StartCoroutine(DisableAfterUp(go));
    }

    private IEnumerator DisableAfterUp(GameObject go)
    {
        if (go == null) yield break;

        var anim = go.GetComponent<Animator>();
        if (anim == null)
        {
            if (go != current) go.SetActive(false);
            hideRoutines.Remove(go);
            yield break;
        }

        int upShortHash = Animator.StringToHash(upStateName);
        int upFullPathHash = Animator.StringToHash("Base Layer." + upStateName);

        // Wait until it ENTERS Up
        while (go != null && go.activeInHierarchy)
        {
            var st = anim.GetCurrentAnimatorStateInfo(animatorLayer);
            bool isUp = st.shortNameHash == upShortHash || st.fullPathHash == upFullPathHash;
            if (isUp) break;
            yield return null;
        }

        // Wait until Up finishes
        while (go != null && go.activeInHierarchy)
        {
            var st = anim.GetCurrentAnimatorStateInfo(animatorLayer);
            bool isUp = st.shortNameHash == upShortHash || st.fullPathHash == upFullPathHash;

            if (!isUp) break;
            if (st.normalizedTime >= 1f) break;

            yield return null;
        }

        if (go != null && go != current)
            go.SetActive(false);

        hideRoutines.Remove(go);
    }

    private void CancelPendingHide(GameObject go)
    {
        if (go == null) return;

        if (hideRoutines.TryGetValue(go, out var running) && running != null)
            StopCoroutine(running);

        hideRoutines.Remove(go);
    }

    private void PlayTrigger(GameObject go, string trigger)
    {
        if (go == null) return;

        var anim = go.GetComponent<Animator>();
        if (anim == null) return;

        anim.ResetTrigger(upTrigger);
        anim.ResetTrigger(downTrigger);
        anim.SetTrigger(trigger);
    }

    private void BringToFront(GameObject go)
    {
        if (go == null) return;

        // If a container is assigned, we only reorder within it (recommended).
        if (instructionsContainer != null)
        {
            // Safety: ensure the object is actually inside the container
            if (go.transform.parent == instructionsContainer)
                go.transform.SetAsLastSibling();

            return;
        }

        // Fallback if container wasn't assigned
        go.transform.SetAsLastSibling();
    }

    private GameObject GetGOForMode(SaveSlotManager.InteractionMode mode)
    {
        return mode switch
        {
            SaveSlotManager.InteractionMode.Normal => normal,
            SaveSlotManager.InteractionMode.Delete => delete,
            SaveSlotManager.InteractionMode.CopySelectSource => copySelectSource,
            SaveSlotManager.InteractionMode.CopySelectDestination => copySelectDest,
            SaveSlotManager.InteractionMode.ImportSelectDestination => importSelectDest,
            SaveSlotManager.InteractionMode.ExportSelectSource => exportSelectSource,
            _ => normal
        };
    }

    private void SafeSetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }
}