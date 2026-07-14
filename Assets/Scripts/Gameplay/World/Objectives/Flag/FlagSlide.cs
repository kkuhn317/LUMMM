using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

/// <summary>
/// Handles the flagpole slide — spawning cutscene Mario puppets, sliding them
/// down the pole, and animating climbSpeed. Notifies Flag when all players
/// have reached the bottom via OnAllPlayersAtBottom.
///
/// Multiplayer wait: when a player grabs the pole, they hold their position
/// for multiplayerWaitTime seconds before sliding. Any player that arrives
/// during the wait slides simultaneously when the wait expires. Players
/// arriving after the wait start sliding immediately on their own.
/// </summary>
public class FlagSlide : MonoBehaviour
{
    [Tooltip("Cutscene Mario prefab used when player is small.")]
    public GameObject cutsceneMario;

    [Tooltip("Optional. Used instead of cutsceneMario when player is big or higher.")]
    public GameObject optCutsceneBigMario;

    public float marioSlideSpeed = 50f;
    public float flagMoveSpeed   = 50f;
    [HideInInspector] public bool flagOnRight       = false; // player side — puppet spawn/slide/facing
    [HideInInspector] public bool flagVisualOnRight = false; // flag sprite side — only affects flag lowering position

    [Tooltip("How long to wait for other players before sliding starts. 0 = slide immediately.")]
    public float multiplayerWaitTime = 2f;

    public GameObject starParticlePrefab;

    // Mirrors the real player's palette row onto the slide puppet so the star/element
    // shows during the slide instead of a normal-coloured clone.
    private static readonly int PaletteRowID = Shader.PropertyToID("_PaletteRow");
    private MaterialPropertyBlock _puppetMpb;

    // ─── Per-Player State ────────────────────────────────────────────────────

    public class PlayerSlideState
    {
        public MarioCore  Mario;
        public GameObject CutsceneMarioInstance;
        public bool       WasStarred;         // was the player starred at grab?
        public float      StarTimeRemaining;   // counted down here since the player's Update is dead
        public bool       AtBottom;
        public bool       ArrivedAtTarget;
        public bool       SlideReleased;  // true when player is allowed to start sliding
        public Vector3    EndPos;
        public Vector3    TargetSlot;
        public int        Order;
        public Coroutine  ArrivalRoutine;
        public Vector3    LastPosition;
    }

    private readonly List<PlayerSlideState>  _slidingPlayers    = new();
    private readonly HashSet<MarioCore>      _registeredPlayers = new();
    private int  _bottomCount    = 0;
    private bool _slideReleased  = false; // true once the wait is over
    private bool _waitStarted    = false;

    // ─── Events ──────────────────────────────────────────────────────────────

    public System.Action<List<PlayerSlideState>> OnAllPlayersAtBottom;
    public System.Action<PlayerSlideState>       OnFirstPlayerAtBottom;

    /// <summary>Fired the moment the first player grabs the pole (before any multiplayer wait).</summary>
    public System.Action OnSlideStarted;

    /// <summary>
    /// Optional per-player gate. If set, it runs for each spawned puppet AFTER it
    /// appears at the grab position but BEFORE that puppet is released to slide
    /// (the flag also stays raised until it completes). Return an IEnumerator that
    /// finishes when the puppet is ready to slide. Null = no gate (default).
    ///
    /// Used by level-specific flags — e.g. the Coin Doors Maze tiny→small poof.
    /// Keep this component game-agnostic: put the actual behaviour in a separate
    /// component that assigns this delegate.
    /// </summary>
    public System.Func<PlayerSlideState, IEnumerator> PuppetPreSlideGate;

    // ─── References ──────────────────────────────────────────────────────────

    private GameObject  _flag;
    private FlagArrival _arrival;
    private CameraFollow _cameraFollow;

    // ─── Public API ──────────────────────────────────────────────────────────

    public List<PlayerSlideState> SlidingPlayers => _slidingPlayers;
    public bool IsEmpty => _slidingPlayers.Count == 0;

    /// <summary>Hides all puppet instances — called the moment the cutscene is about to play.</summary>
    public void HideAllPuppets()
    {
        Debug.Log($"[FlagSlide] HideAllPuppets called. Players: {_slidingPlayers.Count}");
        foreach (var ps in _slidingPlayers)
        {
            Debug.Log($"[FlagSlide] Hiding puppet: {(ps.CutsceneMarioInstance != null ? ps.CutsceneMarioInstance.name : "null")}");
            if (ps.CutsceneMarioInstance != null)
                ps.CutsceneMarioInstance.SetActive(false);
        }
    }

    public void Initialize(GameObject flagObject, FlagArrival arrival)
    {
        _flag         = flagObject;
        _arrival      = arrival;
        _cameraFollow = FindObjectOfType<CameraFollow>();
    }

    public bool IsRegistered(MarioCore mario) => _registeredPlayers.Contains(mario);

    /// <summary>
    /// Registers a player — spawns their puppet and holds them at grab position
    /// until the multiplayer wait expires.
    /// </summary>
    public void RegisterPlayer(Collider2D other, MarioCore mario)
    {
        if (_registeredPlayers.Contains(mario)) return;
        _registeredPlayers.Add(mario);

        var puppet = SpawnPuppet(other, mario);
        if (puppet == null) return;

        var ps = new PlayerSlideState
        {
            Mario                 = mario,
            CutsceneMarioInstance = puppet,
            AtBottom              = false,
            SlideReleased         = _slideReleased, // if wait already over, slide immediately
            EndPos                = PowerStates.IsBig(mario.State.PowerupState)
                                    ? new Vector3(flagOnRight ? 0.4f : -0.4f, 1.5f, 0)
                                    : new Vector3(flagOnRight ? 0.4f : -0.4f, 1.0f, 0)
        };

        // Capture the star state BEFORE deactivating — once the player GameObject is
        // inactive its Update stops, so MarioCombat's own TickStarPower can't run.
        ps.WasStarred        = mario.State.StarPower;
        ps.StarTimeRemaining = mario.State.StarPowerRemainingTime;

        _slidingPlayers.Add(ps);

        mario.gameObject.SetActive(false);

        // If the wait is already over (late-joining player), activate immediately
        if (_slideReleased)
        {
            puppet.SetActive(true);
            var animator = puppet.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetBool("isSideClimbing", true);
                animator.SetFloat("climbSpeed", 5f);
            }
        }

        if (starParticlePrefab != null)
            SpawnStarParticles(puppet);

        UpdateCameraTargets();

        // Start the wait timer on the first player to arrive
        if (!_waitStarted)
        {
            _waitStarted = true;
            OnSlideStarted?.Invoke(); // fires immediately when first player grabs the pole
            if (multiplayerWaitTime > 0f)
                StartCoroutine(MultiplayerWaitRoutine());
            else
                ReleaseAllPlayers();
        }
    }

    // ─── Multiplayer Wait ────────────────────────────────────────────────────

    private IEnumerator MultiplayerWaitRoutine()
    {
        yield return new WaitForSeconds(multiplayerWaitTime);
        ReleaseAllPlayers();
    }

    /// <summary>Releases all currently waiting players to start sliding.</summary>
    private void ReleaseAllPlayers()
    {
        // If a pre-slide gate is installed, run it per puppet first, then release.
        if (PuppetPreSlideGate != null)
        {
            StartCoroutine(GatedReleaseRoutine());
            return;
        }

        DoReleaseAllPlayers();
    }

    /// <summary>
    /// Shows each waiting puppet in an idle (non-climbing) pose, runs the
    /// pre-slide gate for it, and only then releases everyone to slide. Because
    /// the flag-lowering in Tick() is gated on _slideReleased, the flag also
    /// stays raised until the gate(s) complete.
    /// </summary>
    private IEnumerator GatedReleaseRoutine()
    {
        foreach (var ps in _slidingPlayers)
        {
            if (ps.SlideReleased || ps.CutsceneMarioInstance == null) continue;

            ps.CutsceneMarioInstance.SetActive(true);

            // Hold the frozen pole-grip pose (gripping, not moving) while the gate
            // runs — NOT ground idle. isSideClimbing stays true; only climbSpeed is 0.
            var idleAnim = ps.CutsceneMarioInstance.GetComponent<Animator>();
            if (idleAnim != null)
            {
                idleAnim.SetBool("isSideClimbing", true);
                idleAnim.SetFloat("climbSpeed", 0f);
            }

            yield return StartCoroutine(PuppetPreSlideGate(ps));
        }

        DoReleaseAllPlayers();
    }

    private void DoReleaseAllPlayers()
    {
        _slideReleased = true;
        foreach (var ps in _slidingPlayers)
        {
            ps.SlideReleased = true;
            if (ps.CutsceneMarioInstance == null) continue;

            ps.CutsceneMarioInstance.SetActive(true);

            var animator = ps.CutsceneMarioInstance.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetBool("isSideClimbing", true);
                animator.SetFloat("climbSpeed", 5f);
            }
        }
    }

    // ─── Update ──────────────────────────────────────────────────────────────

    public void Tick()
    {
        if (_flag == null) return;

        // Move flag down only after slide is released
        if (_slideReleased)
        {
            Vector3 flagTarget = new Vector3(flagVisualOnRight ? 0.5f : -0.5f, 1.1f, 0);
            _flag.transform.localPosition = Vector3.MoveTowards(
                _flag.transform.localPosition, flagTarget, flagMoveSpeed * Time.deltaTime);
        }

        bool allAtBottom = true;

        foreach (var ps in _slidingPlayers)
        {
            TickPuppetStar(ps);        // expire the star on time even though the player is inactive
            MirrorPuppetPalette(ps);   // keep star/element flashing on the puppet

            if (ps.AtBottom) continue;

            // Hold at grab position until released
            if (!ps.SlideReleased)
            {
                allAtBottom = false;
                continue;
            }

            allAtBottom = false;

            Vector3 flagTarget  = new Vector3(flagVisualOnRight ? 0.5f : -0.5f, 1.1f, 0);
            Vector3 poleTarget  = transform.position + ps.EndPos;
            Vector3 prevPos     = ps.CutsceneMarioInstance.transform.position;

            ps.CutsceneMarioInstance.transform.position = Vector3.MoveTowards(
                prevPos, poleTarget, marioSlideSpeed * Time.deltaTime);

            var anim = ps.CutsceneMarioInstance.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetBool("isSideClimbing", true);
                anim.SetFloat("climbSpeed", 5f);
            }

            ps.LastPosition = ps.CutsceneMarioInstance.transform.position;

            if (Mathf.Abs(ps.CutsceneMarioInstance.transform.position.y - poleTarget.y) <= 0.001f)
            {
                ps.AtBottom = true;
                ps.Order    = _bottomCount++;

                if (_arrival != null)
                    ps.ArrivalRoutine = StartCoroutine(
                        _arrival.WaitForFlagThenMove(ps, _flag, flagTarget, flagOnRight));

                if (_bottomCount == 1)
                    OnFirstPlayerAtBottom?.Invoke(ps);
            }
        }

        if (allAtBottom && _slidingPlayers.Count > 0)
            OnAllPlayersAtBottom?.Invoke(_slidingPlayers);
    }

    // ─── Camera ──────────────────────────────────────────────────────────────

    private void UpdateCameraTargets()
    {
        if (_cameraFollow == null) return;
        var targets = new List<GameObject>();
        foreach (var ps in _slidingPlayers)
            if (ps.CutsceneMarioInstance != null)
                targets.Add(ps.CutsceneMarioInstance);
        _cameraFollow.SetOverrideTargets(targets);
    }

    // ─── Puppet Helpers ───────────────────────────────────────────────────── 

    private static IEnumerable<SpriteRenderer>GetPuppetBodyRenderers(GameObject puppet)
    {
        if (puppet == null)
            yield break;

        foreach (SpriteRenderer renderer in
                puppet.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer == null)
                continue;

            // Includes the regular puppet renderer and the special
            // SpriteResolver used by ending animations.
            if (renderer.GetComponent<SpriteResolver>() == null)
                continue;

            yield return renderer;
        }
    }

    private void ApplyPuppetPalette(
        GameObject puppet,
        float paletteRow)
    {
        _puppetMpb ??= new MaterialPropertyBlock();

        foreach (SpriteRenderer renderer in
                GetPuppetBodyRenderers(puppet))
        {
            renderer.GetPropertyBlock(_puppetMpb);
            _puppetMpb.SetFloat(PaletteRowID, paletteRow);
            renderer.SetPropertyBlock(_puppetMpb);
        }
    }

    private void ApplyTimelineActorPalette(
        GameObject actor,
        MarioCore mario)
    {
        if (actor == null ||
            mario == null ||
            mario.Palette == null)
        {
            return;
        }

        Material paletteMaterial =
            mario.Palette.PaletteMaterial;

        float paletteRow =
            mario.Palette.CurrentRow;

        _puppetMpb ??= new MaterialPropertyBlock();

        foreach (SpriteRenderer renderer in
                actor.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer == null)
                continue;

            // Only modify actual Mario body renderers.
            if (renderer.GetComponent<SpriteResolver>() == null)
                continue;

            if (paletteMaterial != null)
                renderer.sharedMaterial = paletteMaterial;

            renderer.GetPropertyBlock(_puppetMpb);

            _puppetMpb.SetFloat(
                PaletteRowID,
                paletteRow
            );

            renderer.SetPropertyBlock(_puppetMpb);
        }
    }

    /// <summary>
    /// Copies the sprite library currently used by the real player to the
    /// Timeline-bound Cutscene Mario, then refreshes all of its SpriteResolvers.
    /// </summary>
    private void ApplyTimelineActorLibrary(
        GameObject actor,
        MarioCore mario)
    {
        if (actor == null || mario == null)
            return;

        SpriteLibrary sourceLibrary =
            mario.GetComponentInChildren<SpriteLibrary>(true);

        if (sourceLibrary == null ||
            sourceLibrary.spriteLibraryAsset == null)
        {
            return;
        }

        SpriteLibraryAsset sourceAsset =
            sourceLibrary.spriteLibraryAsset;

        // Apply the active library to every SpriteLibrary used by the actor.
        foreach (SpriteLibrary targetLibrary in
                actor.GetComponentsInChildren<SpriteLibrary>(true))
        {
            if (targetLibrary != null)
                targetLibrary.spriteLibraryAsset = sourceAsset;
        }

        // Force every resolver to update after changing the library.
        foreach (SpriteResolver resolver in
                actor.GetComponentsInChildren<SpriteResolver>(true))
        {
            if (resolver != null)
                resolver.ResolveSpriteToSpriteRenderer();
        }
    }

    /// <summary>
    /// Copies the first sliding player's current appearance to the inactive
    /// Cutscene Mario objects bound to Timeline.
    ///
    /// This is separate from the instantiated slide puppet. Timeline activates
    /// the original child objects stored in cutsceneMario/optCutsceneBigMario.
    /// </summary>
    public void PrepareTimelineActors()
    {
        if (_slidingPlayers.Count == 0)
            return;

        PlayerSlideState ps = _slidingPlayers[0];

        if (ps == null || ps.Mario == null)
            return;

        MarioCore mario = ps.Mario;

        bool isBig = PowerStates.IsBig(
            mario.State.PowerupState
        );

        GameObject selectedActor =
            isBig && optCutsceneBigMario != null
                ? optCutsceneBigMario
                : cutsceneMario;

        // Apply the palette to both Timeline actors so either one is ready.
        ApplyTimelineActorPalette(
            cutsceneMario,
            mario
        );

        ApplyTimelineActorPalette(
            optCutsceneBigMario,
            mario
        );

        // Copy the active sprite library only to the actor matching Mario's size.
        ApplyTimelineActorLibrary(
            selectedActor,
            mario
        );
    }

    // ─── Puppet Spawning ─────────────────────────────────────────────────────

    private GameObject SpawnPuppet(Collider2D other, MarioCore mario)
    {
        bool isBig = PowerStates.IsBig(mario.State.PowerupState);
        GameObject prefab = (optCutsceneBigMario != null && isBig)
            ? optCutsceneBigMario
            : cutsceneMario;

        if (prefab == null) return null;

        var instance = Instantiate(prefab);

        instance.transform.localScale = mario.transform.lossyScale;

        instance.SetActive(false);
        instance.transform.position = new Vector2(
            transform.position.x + (flagOnRight ? 0.4f : -0.4f),
            other.transform.position.y);

        // Copy the sprite library Mario is currently using, including the active
        // skin or power-up override. Fall back to NormalSpriteLibrary if necessary.
        var marioPowerup = mario.GetComponent<MarioPowerup>();
        var sourceLibrary =
            mario.GetComponentInChildren<SpriteLibrary>(true);
        var puppetLibrary =
            instance.GetComponentInChildren<SpriteLibrary>(true);

        if (puppetLibrary != null)
        {
            if (sourceLibrary != null &&
                sourceLibrary.spriteLibraryAsset != null)
            {
                // Copies the library Mario is actually using:
                // skin, power-up override, or normal library.
                puppetLibrary.spriteLibraryAsset =
                    sourceLibrary.spriteLibraryAsset;
            }
            else if (marioPowerup != null &&
                    marioPowerup.NormalSpriteLibrary != null)
            {
                puppetLibrary.spriteLibraryAsset =
                    marioPowerup.NormalSpriteLibrary;
            }

            foreach (SpriteResolver resolver in
                    instance.GetComponentsInChildren<SpriteResolver>(true))
            {
                resolver.ResolveSpriteToSpriteRenderer();
            }
        }

        instance.GetComponent<PuppetGroundDetection>()?.Initialize();

        Material paletteMaterial =
            mario.Palette != null
                ? mario.Palette.PaletteMaterial
                : null;

        if (paletteMaterial == null)
        {
            var simple = FindSimpleRenderer(mario);

            if (simple != null)
                paletteMaterial = simple.sharedMaterial;
        }

        foreach (SpriteRenderer renderer in
                GetPuppetBodyRenderers(instance))
        {
            renderer.flipX = flagOnRight;

            if (paletteMaterial != null)
                renderer.sharedMaterial = paletteMaterial;
        }

        // Apply the palette immediately, before the first Tick().
        if (mario.Palette != null)
        {
            ApplyPuppetPalette(
                instance,
                mario.Palette.CurrentRow
            );
        }

        return instance;
    }

    // ─── Palette mirroring ───────────────────────────────────────────────────

    private static SpriteRenderer FindSimpleRenderer(MarioCore mario)
    {
        foreach (var r in mario.GetComponentsInChildren<SpriteRenderer>(true))
            if (r.gameObject.name == "SpriteSimple") return r;
        return null;
    }

    /// <summary>
    /// Counts the captured star time down during the slide (the player's own Update is dead
    /// while it's deactivated) and ends the star cleanly when it runs out. StopStarPower works
    /// on the inactive player — it clears the palette (so MirrorPuppetPalette then reads the
    /// rest row and the puppet stops flashing) and releases the star music override; its
    /// IsEndingLevel guard keeps that release from restoring the overworld over the fanfare.
    /// </summary>
    private void TickPuppetStar(PlayerSlideState ps)
    {
        if (!ps.WasStarred) return;

        ps.StarTimeRemaining -= Time.deltaTime;
        if (ps.StarTimeRemaining > 0f) return;

        ps.WasStarred = false;
        ps.Mario?.Combat?.StopStarPower();
    }

    private void MirrorPuppetPalette(PlayerSlideState ps)
    {
        if (ps.Mario == null ||
            ps.Mario.Palette == null ||
            ps.CutsceneMarioInstance == null)
        {
            return;
        }

        ApplyPuppetPalette(
            ps.CutsceneMarioInstance,
            ps.Mario.Palette.CurrentRow
        );
    }

    // ─── Particles ───────────────────────────────────────────────────────────

    private void SpawnStarParticles(GameObject origin)
    {
        int[] v = { -1, 0, 1 };
        int[] h = { -1, 0, 1 };
        foreach (int vi in v)
        {
            foreach (int hi in h)
            {
                if (vi == 0 && hi == 0) continue;
                float dist   = (vi != 0 && hi != 0) ? 0.7f : 1f;
                Vector3 offs = new Vector3(hi * dist, vi * dist, 0);
                var star     = Instantiate(starParticlePrefab, origin.transform.position + offs, Quaternion.identity);
                var mover    = star.GetComponent<StarMoveOutward>();
                if (mover != null) { mover.direction = new Vector2(vi, hi); mover.speed = 2f; }
            }
        }
    }
}