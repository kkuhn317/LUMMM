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

    // ─── Per-Player State ────────────────────────────────────────────────────

    public class PlayerSlideState
    {
        public MarioCore  Mario;
        public GameObject CutsceneMarioInstance;
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

        // Read the sprite library from MarioPowerup.NormalSpriteLibrary — the
        // authoritative per-character asset — NOT from the SpriteLibrary component.
        var marioPowerup = mario.GetComponent<MarioPowerup>();
        var csLib = instance.GetComponentInChildren<SpriteLibrary>();
        if (marioPowerup != null && marioPowerup.NormalSpriteLibrary != null && csLib != null)
            csLib.spriteLibraryAsset = marioPowerup.NormalSpriteLibrary;

        instance.GetComponent<PuppetGroundDetection>()?.Initialize();

        var sr = instance.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.flipX = flagOnRight;

        return instance;
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