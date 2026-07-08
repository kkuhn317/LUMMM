using System.Collections;
using UnityEngine;
using UnityEngine.U2D.Animation;
using PowerupState = PowerStates.PowerupState;

/// <summary>
/// Coin Doors Maze flagpole special-case.
///
/// When a TINY Mario grabs the pole, he does NOT slide immediately. Instead the
/// puppet holds at the grab position, a poof effect plays, the puppet becomes a
/// SMALL Mario, and only then the normal slide continues. Non-tiny players are
/// unaffected and slide with no delay.
///
/// This drives the puppet only — the real Mario is already deactivated by
/// FlagSlide, and the level ends after the cutscene, so no real prefab-swap
/// transformation happens here. If you also need the persisted power state to
/// become "small", do that separately and deliberately (see notes in chat).
///
/// Setup: add this component to the FlagPole GameObject, alongside the existing
/// Flag / FlagSlide components. Assign the Poof reference. Nothing on the Flag
/// component needs to change.
/// </summary>
[RequireComponent(typeof(FlagSlide))]
public class FlagPoleTinyToSmall : MonoBehaviour
{
    [Header("Poof")]
    [Tooltip("Poof effect GameObject that already lives under the FlagPole. " +
             "It is moved to the puppet's position and activated when a tiny " +
             "Mario transforms, then deactivated after poofDuration.")]
    [SerializeField] private GameObject poof;

    [Tooltip("Optional: instantiate this prefab at the puppet position instead of " +
             "(or in addition to) toggling the scene 'poof' object.")]
    [SerializeField] private GameObject poofPrefab;

    [Tooltip("How long to hold on the poof before the slide begins.")]
    [Min(0f)]
    [SerializeField] private float poofDuration = 0.4f;

    [Tooltip("Optional sound played the moment the poof appears.")]
    [SerializeField] private AudioClip poofSound;

    [Tooltip("How far in front of the puppet the poof renders. The poof is placed " +
             "on the puppet's sorting layer with this added to its sorting order, so " +
             "a positive value draws it over the transforming puppet.")]
    [SerializeField] private int poofSortingOrderOffset = 10;

    [Header("Stars")]
    [Tooltip("Same star prefab the checkpoint uses (must have StarMoveOutward). " +
             "A ring of these bursts outward around the puppet when it turns small, " +
             "matching the checkpoint activation effect. Leave null to skip.")]
    [SerializeField] private GameObject starParticle;

    [Header("Small Appearance (fallback only)")]
    [Tooltip("Used ONLY if the sliding character has no 'small' prefab defined in " +
             "its CharacterData. Normally the small sprite library and scale are " +
             "read per-character from that character's own small variant, so this " +
             "stays empty. Leave the scale at (1,1,1) unless a specific fallback is needed.")]
    [SerializeField] private SpriteLibraryAsset fallbackSmallLibrary;

    [SerializeField] private Vector3 fallbackSmallScale = Vector3.one;

    private FlagSlide _slide;

    private void Awake()
    {
        _slide = GetComponent<FlagSlide>();
    }

    private void OnEnable()
    {
        if (_slide == null) _slide = GetComponent<FlagSlide>();
        if (_slide != null) _slide.PuppetPreSlideGate = TinyToSmallGate;
        if (poof != null) poof.SetActive(false);
    }

    private void OnDisable()
    {
        // Only clear the gate if it's still ours.
        if (_slide != null && _slide.PuppetPreSlideGate == TinyToSmallGate)
            _slide.PuppetPreSlideGate = null;
    }

    /// <summary>
    /// Runs after the puppet appears but before it is released to slide.
    /// Only tiny Mario is affected; everyone else returns immediately.
    /// </summary>
    private IEnumerator TinyToSmallGate(FlagSlide.PlayerSlideState ps)
    {
        if (ps?.Mario == null || ps.Mario.State.PowerupState != PowerupState.tiny)
            yield break;

        var puppet = ps.CutsceneMarioInstance;
        Vector3 pos = puppet != null ? puppet.transform.position : transform.position;

        // ── Poof (appears the same frame the puppet turns small) ────────────────
        if (poofSound != null)
            AudioManager.Instance?.Play(poofSound, SoundCategory.SFX);

        GameObject spawnedPoof = null;
        if (poofPrefab != null)
        {
            spawnedPoof = Instantiate(poofPrefab, pos, Quaternion.identity);
            BringToFrontOf(spawnedPoof, puppet);
        }

        if (poof != null)
        {
            poof.transform.position = pos;
            BringToFrontOf(poof, puppet);
            poof.SetActive(true);
        }

        // Star burst around the puppet — same effect as reaching a checkpoint.
        SpawnStars(pos);

        // ── Tiny → Small on the puppet ──────────────────────────────────────────
        // Derive the SMALL library and scale from THIS character's own data, so
        // it works for every playable character sharing the pole — not a single
        // shared asset. Falls back to the inspector values only if the character
        // has no small variant defined.
        if (puppet != null)
        {
            ResolveSmallAppearance(ps.Mario, out var smallLibrary, out var smallScale);

            puppet.transform.localScale = smallScale;

            if (smallLibrary != null)
            {
                var lib = puppet.GetComponentInChildren<SpriteLibrary>();
                if (lib != null) lib.spriteLibraryAsset = smallLibrary;
            }
        }

        // ── Hold before sliding ─────────────────────────────────────────────────
        if (poofDuration > 0f)
            yield return new WaitForSeconds(poofDuration);

        // ── Cleanup ─────────────────────────────────────────────────────────────
        if (poof != null) poof.SetActive(false);
        if (spawnedPoof != null) Destroy(spawnedPoof);

        // Gate returns → FlagSlide releases this puppet and the slide continues.
    }

    /// <summary>
    /// Looks up the small-variant prefab for the sliding player's character and
    /// reads its sprite library and scale. Both fall back to the inspector values
    /// if the character or its small prefab can't be resolved.
    /// </summary>
    private void ResolveSmallAppearance(MarioCore mario, out SpriteLibraryAsset library, out Vector3 scale)
    {
        library = fallbackSmallLibrary;
        scale   = fallbackSmallScale;

        var powerup = mario != null ? mario.GetComponent<MarioPowerup>() : null;
        var character = powerup != null ? powerup.Character : null;
        if (character == null) return;

        var smallPrefab = character.GetPrefabForState(PowerupState.small);
        if (smallPrefab == null) return;

        var smallPowerup = smallPrefab.GetComponent<MarioPowerup>();
        if (smallPowerup != null && smallPowerup.NormalSpriteLibrary != null)
            library = smallPowerup.NormalSpriteLibrary;

        scale = smallPrefab.transform.localScale;
    }

    /// <summary>
    /// Places every renderer on the poof onto the puppet's sorting layer and a
    /// higher sorting order, so the poof draws in front of the transforming puppet.
    /// Falls back to a plain positive order if no puppet reference is available.
    /// Handles both sprite and particle renderers.
    /// </summary>
    private void BringToFrontOf(GameObject poofObject, GameObject puppet)
    {
        if (poofObject == null) return;

        int targetOrder = poofSortingOrderOffset;
        int? targetLayerId = null;

        if (puppet != null)
        {
            SpriteRenderer reference = null;
            foreach (var sr in puppet.GetComponentsInChildren<SpriteRenderer>(true))
                if (reference == null || sr.sortingOrder > reference.sortingOrder)
                    reference = sr;

            if (reference != null)
            {
                targetOrder   = reference.sortingOrder + poofSortingOrderOffset;
                targetLayerId = reference.sortingLayerID;
            }
        }

        foreach (var sr in poofObject.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (targetLayerId.HasValue) sr.sortingLayerID = targetLayerId.Value;
            sr.sortingOrder = targetOrder;
        }

        foreach (var pr in poofObject.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            if (targetLayerId.HasValue) pr.sortingLayerID = targetLayerId.Value;
            pr.sortingOrder = targetOrder;
        }
    }

    /// <summary>
    /// Spawns a ring of star particles around <paramref name="center"/> that move
    /// outward — the same 8-direction burst the checkpoint plays on activation.
    /// </summary>
    private void SpawnStars(Vector3 center)
    {
        if (starParticle == null) return;

        int[] vertical   = { -1, 0, 1 };
        int[] horizontal = { -1, 0, 1 };

        for (int i = 0; i < vertical.Length; i++)
        {
            for (int j = 0; j < horizontal.Length; j++)
            {
                if (vertical[i] == 0 && horizontal[j] == 0)
                    continue;

                float distance = (vertical[i] != 0 && horizontal[j] != 0) ? 0.7f : 1f;
                Vector3 startOffset = new Vector3(horizontal[j] * distance, vertical[i] * distance, 0f);

                GameObject star = Instantiate(starParticle, center + startOffset, Quaternion.identity);

                var moveOut = star.GetComponent<StarMoveOutward>();
                if (moveOut != null)
                {
                    moveOut.direction = new Vector2(horizontal[j], vertical[i]);
                    moveOut.speed = 2f;
                }
            }
        }
    }
}