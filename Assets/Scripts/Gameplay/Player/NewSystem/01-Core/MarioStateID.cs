/// <summary>
/// State identifier constants for Mario's FSM.
///
/// WHY STRING CONSTANTS INSTEAD OF AN ENUM:
/// Enums are sealed — you cannot add values from external assemblies,
/// powerup scripts, or runtime-loaded content without editing this file.
/// String constants are structurally identical at call sites (MarioStateID.Idle
/// still works everywhere) but any script can define new IDs without touching
/// this class. New states just register themselves with the StateMachine.
///
/// SUPER-STATE TAGS (defined in MarioStateMachine.SuperStateTags):
/// Instead of exhaustive switch patterns, states opt into groups via tags.
/// New states declare which tags they belong to in their constructor.
/// Query with: Core.StateMachine.HasTag(stateId, MarioStateTags.Airborne)
///
/// CONVENTION:
/// Use PascalCase. Keep IDs unique across the whole project.
/// Third-party states should prefix with their system name (e.g. "Yoshi_Mounted").
/// </summary>
public static class MarioStateID
{
    // ─── Grounded ────────────────────────────────────────────────────────────
    public const string Idle   = "Idle";
    public const string Walk   = "Walk";
    public const string Run    = "Run";
    public const string Skid   = "Skid";
    public const string Crouch = "Crouch";
    public const string Crawl  = "Crawl";
    public const string Push   = "Push";

    // ─── Airborne ────────────────────────────────────────────────────────────
    public const string Rise       = "Rise";
    public const string Fall       = "Fall";
    public const string WallSlide  = "WallSlide";
    public const string MidairSpin = "MidairSpin";
    public const string SpinJump   = "SpinJump";
    public const string WallJump   = "WallJump";

    // ─── Ground Pound ────────────────────────────────────────────────────────
    public const string GroundPoundSpin = "GroundPoundSpin";
    public const string GroundPoundFall = "GroundPoundFall";
    public const string GroundPoundLand = "GroundPoundLand";

    // ─── Swimming ────────────────────────────────────────────────────────────
    public const string SwimIdle = "SwimIdle";
    public const string Swim     = "Swim";

    // ─── Climbing ────────────────────────────────────────────────────────────
    public const string ClimbFront = "ClimbFront";
    public const string ClimbSide  = "ClimbSide";

    // ─── Special ─────────────────────────────────────────────────────────────
    public const string Locked = "Locked";
    public const string Dead   = "Dead";
}

/// <summary>
/// Super-state tag constants. States declare which tags they belong to.
/// The StateMachine indexes these so IsGrounded / IsAirborne / etc.
/// are O(1) HashSet lookups rather than exhaustive switch patterns.
///
/// Add new tags here when you add new movement super-states
/// (e.g. "Mounted" for a Yoshi system, "Sliding" for an ice mechanic).
/// </summary>
public static class MarioStateTags
{
    public const string Grounded = "Grounded";
    public const string Airborne = "Airborne";
    public const string Swimming = "Swimming";
    public const string Climbing = "Climbing";
    public const string Locked   = "Locked";
    public const string Dead     = "Dead";
}