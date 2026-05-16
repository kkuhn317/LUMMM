using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

/// <summary>
/// Trigger zone that applies a named emote to any player that enters.
/// Each character automatically gets their own sprite library for that
/// emote via their CharacterEmoteSpriteData asset — no per-zone
/// character mapping needed.
///
/// Also fires MarioEvents.OnEmoteStarted/OnEmoteEnded so MarioAnimatorController
/// can drive the correct animator bools in sync with the sprite swap.
/// </summary>
public class SpriteSwapArea : MonoBehaviour
{
    [Header("Emote")]
    [Tooltip("Which emote to apply when a player enters this zone.")]
    public MarioEmote emote = MarioEmote.Worried;

    [Header("Settings")]
    public bool allowChangeOnEnter = true;
    public bool allowResetOnExit = true;
    public bool disableColliderOnApply = false;

    [Header("Audio (Optional)")]
    [Tooltip("If true, plays each character's own emote audio from CharacterData.")]
    public bool playCharacterAudio = true;
    [Tooltip("If true, suppresses the character audio for this zone regardless of CharacterData.")]
    public bool suppressAudio = false;
    [Tooltip("If true, the sound only plays once even if multiple players enter.")]
    public bool playAudioOnce = false;

    private readonly HashSet<int> _soundPlayedFor = new();
    private readonly Dictionary<int, SpriteLibraryAsset> _originalLibraries = new();

    // ── Trigger ───────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!allowChangeOnEnter || !other.CompareTag("Player")) return;

        var core = other.GetComponent<MarioCore>() ?? other.GetComponentInParent<MarioCore>();
        if (core == null) return;

        ApplyEmote(core);
        PlayEmoteAudio(core);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!allowResetOnExit || !other.CompareTag("Player")) return;

        var core = other.GetComponent<MarioCore>() ?? other.GetComponentInParent<MarioCore>();
        if (core == null) return;

        RestoreEmote(core);
    }

    // ── Public API ───────────────────

    public void ChangeSpriteLibrary()
    {
        foreach (var core in FindObjectsOfType<MarioCore>())
            ApplyEmote(core);
    }

    public void ResetSpriteLibrary()
    {
        foreach (var core in FindObjectsOfType<MarioCore>())
            RestoreEmote(core);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void PlayEmoteAudio(MarioCore core)
    {
        if (!playCharacterAudio || suppressAudio) return;
        if (playAudioOnce && _soundPlayedFor.Count > 0) return;
        if (!_soundPlayedFor.Add(core.PlayerIndex)) return;

        var clip = core.Powerup?.Character?.GetEmoteAudio(emote);
        Debug.Log("playing audio:" + clip);
        if (clip != null)
            AudioManager.Instance?.PlayAtPosition(clip, core.transform.position, SoundCategory.SFX);
    }

    private void ApplyEmote(MarioCore core)
    {
        var spriteLib = core.GetComponentInChildren<SpriteLibrary>();
        if (spriteLib == null) return;

        // Save original so we can restore it later
        if (!_originalLibraries.ContainsKey(core.PlayerIndex))
            _originalLibraries[core.PlayerIndex] = spriteLib.spriteLibraryAsset;

        // Look up this character's library for the emote via CharacterData
        var targetLibrary = core.Powerup?.Character?.GetEmoteLibrary(emote);
        if (targetLibrary != null)
            spriteLib.spriteLibraryAsset = targetLibrary;

        // Fire event so MarioAnimatorController drives the animator in sync
        MarioEvents.FireEmoteStarted(core.PlayerIndex, emote);

        // Optionally disable this trigger collider after firing
        if (disableColliderOnApply)
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }
    }

    private void RestoreEmote(MarioCore core)
    {
        var spriteLib = core.GetComponentInChildren<SpriteLibrary>();
        if (spriteLib == null) return;

        if (_originalLibraries.TryGetValue(core.PlayerIndex, out var original) && original != null)
        {
            spriteLib.spriteLibraryAsset = original;
            _originalLibraries.Remove(core.PlayerIndex);
        }
        else
        {
            // Fallback: ask MarioPowerup for the normal library
            core.Powerup?.ResetSpriteLibrary();
        }

        // Fire event so MarioAnimatorController restores normal animation
        MarioEvents.FireEmoteEnded(core.PlayerIndex);
    }
}