using UnityEngine;

/// <summary>
/// Central manager for all score combos related to enemy kills.
/// Handles:
/// - Stomp combo (jump / spin / ground pound kills)
/// - Shell combo (enemies killed by a moving shell)
/// 
/// The actual score application + popups are done by EnemyAI via:
/// - AwardStompComboReward(...)
/// - AwardShellComboReward(...)
/// This manager only decides which value/id comes next in each sequence.
/// </summary>
public class StompComboManager : MonoBehaviour
{
    public static StompComboManager Instance { get; private set; }

    [Header("Stomp combo sequence (jump / spin / ground pound)")]
    [Tooltip("Sequence of popup IDs for stomp-based kills (Goomba, Koopa, etc.). " +
             "Use numeric strings (e.g. \"100\") and \"1UP\" for extra lives.")]
    public string[] stompSequenceIds =
    {
        // SMB1-like stomp sequence:
        // 100, 200, 400, 500, 800, 1000, 2000, 4000, 5000, 8000, then 1UPs
        "100", "200", "400", "500", "800",
        "1000", "2000", "4000", "5000", "8000", "1UP"
    };

    [Header("Shell combo sequence (enemies killed by shell)")]
    [Tooltip("Sequence of popup IDs for enemies killed by a moving shell.")]
    public string[] shellSequenceIds =
    {
        // SMB1-like shell sequence:
        // 500, 800, 1000, 2000, 4000, 5000, 8000, then 1UPs
        "500", "800", "1000", "2000",
        "4000", "5000", "8000", "1UP"
    };

    [Header("State (debug)")]
    [Tooltip("Current index in the stomp combo sequence. -1 means no combo started.")]
    [SerializeField] private int stompIndex = -1;

    [Tooltip("Current index in the shell combo sequence. -1 means no combo started.")]
    [SerializeField] private int shellIndex = -1;

    /// <summary>
    /// Last numeric stomp score that was awarded (e.g. 100, 200, 400, 500, 800...).
    /// Used for context-dependent rewards like kicking a shell (400/500/800).
    /// Resets to 0 when the combo is reset or a 1UP is given.
    /// </summary>
    public int LastStompScore { get; private set; } = 0;

    /// <summary>
    /// Optional flag you can use to prevent combo reset while a shell chain is active.
    /// For example:
    /// - Set to true when a Koopa shell starts moving.
    /// - Set to false when the shell stops or despawns.
    /// Then in MarioMovement you can skip resetting combos on landing if this is true.
    /// </summary>
    [Header("Shell chain")]
    [Tooltip("True while a moving shell is actively chaining kills. " +
             "You can use this in MarioMovement to avoid resetting combos on landing.")]
    public bool shellChainActive = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    #region Public API

    /// <summary>
    /// Called when an enemy is killed by a stomp-like action:
    /// - Normal jump from above
    /// - Spin jump (if configured as stomp)
    /// - Ground pound (if configured as stomp)
    /// Returns:
    ///   popupId: string used by ScorePopupManager (e.g. "100", "5000", "1UP")
    ///   scorePoints: points to add to the score (0 if this is a 1UP)
    ///   grantsOneUp: true if this stomp should grant an extra life.
    /// </summary>
    public string RegisterStompKill(out int scorePoints, out bool grantsOneUp)
    {
        string id = GetNextFromSequence(stompSequenceIds, ref stompIndex, out scorePoints, out grantsOneUp);

        // Update LastStompScore only when we actually gave numeric points
        if (!grantsOneUp && scorePoints > 0)
        {
            LastStompScore = scorePoints;
        }
        else
        {
            LastStompScore = 0;
        }

        return id;
    }

    /// <summary>
    /// Called when an enemy is killed by a moving shell (Koopa shell, etc.).
    /// Returns:
    ///   popupId: string used by ScorePopupManager (e.g. "500", "8000", "1UP")
    ///   scorePoints: points to add to the score (0 if this is a 1UP)
    ///   grantsOneUp: true if this kill should grant an extra life.
    /// </summary>
    public string RegisterShellKill(out int scorePoints, out bool grantsOneUp)
    {
        return GetNextFromSequence(shellSequenceIds, ref shellIndex, out scorePoints, out grantsOneUp);
    }

    /// <summary>
    /// Resets the stomp combo back to the initial state (next stomp will be the first in the sequence).
    /// Also clears LastStompScore.
    /// </summary>
    public void ResetStompCombo()
    {
        stompIndex = -1;
        LastStompScore = 0;
    }

    /// <summary>
    /// Resets the shell combo back to the initial state (next shell kill will be the first in the sequence).
    /// Does not affect LastStompScore.
    /// </summary>
    public void ResetShellCombo()
    {
        shellIndex = -1;
    }

    /// <summary>
    /// Resets both stomp and shell combos.
    /// </summary>
    public void ResetAllCombos()
    {
        ResetStompCombo();
        ResetShellCombo();
    }

    #endregion

    #region Internal helpers

    /// <summary>
    /// Generic helper that advances a combo sequence and returns the next reward.
    /// This is used for both stomp and shell combos.
    /// </summary>
    /// <param name="seq">Array of popup ids for the sequence (e.g. "100", "200", ..., "1UP").</param>
    /// <param name="index">Reference to the current index for this sequence.</param>
    /// <param name="scorePoints">Numeric points to award (0 if this is a 1UP entry).</param>
    /// <param name="grantsOneUp">True if this entry is "1UP".</param>
    /// <returns>The popup id string for this step in the sequence.</returns>
    private string GetNextFromSequence(string[] seq, ref int index, out int scorePoints, out bool grantsOneUp)
    {
        scorePoints = 0;
        grantsOneUp = false;

        if (seq == null || seq.Length == 0)
        {
            return null;
        }

        // Advance to next step
        index++;

        // Clamp to last element so we stay on the last reward (usually "1UP") once we reach it
        if (index >= seq.Length)
        {
            index = seq.Length - 1;
        }

        string id = seq[index];

        // Handle "1UP" as a special case: gives a life instead of numeric score
        if (id == "1UP")
        {
            grantsOneUp = true;
            scorePoints = 0;
            return id;
        }

        // For numeric entries (100, 200, 400, 500, 800, 1000, etc.)
        if (!int.TryParse(id, out scorePoints))
        {
            scorePoints = 0;
        }

        grantsOneUp = false;
        return id;
    }

    #endregion
}