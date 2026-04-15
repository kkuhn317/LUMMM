using UnityEngine;

/// <summary>
/// Handles flagpole scoring — normalized hit position mapped to score bands.
/// Fully self-contained and reusable on any pole type.
/// Override CanGrantReward() to gate scoring (e.g. boss must be defeated first).
/// </summary>
public class FlagPoleScoring : MonoBehaviour
{
    [System.Serializable]
    public struct ScoreBand
    {
        [Range(0f, 1f)] public float minT;
        public int points;
    }

    [Tooltip("Bottom of the pole. If null, falls back to collider bounds.")]
    [SerializeField] private Transform poleBottom;

    [Tooltip("Top of the pole. If null, falls back to collider bounds.")]
    [SerializeField] private Transform poleTop;

    [SerializeField] private bool useColliderIfNoTransforms = true;

    [Tooltip("Each band's From (minT) is where it starts. The max is derived from the next band's minT (or 1 for the last).")]
    [SerializeField] private ScoreBand[] scoreBands = new ScoreBand[]
    {
        new ScoreBand { minT = 0.00f, points = 100  },
        new ScoreBand { minT = 0.11f, points = 400  },
        new ScoreBand { minT = 0.37f, points = 800  },
        new ScoreBand { minT = 0.53f, points = 2000 },
        new ScoreBand { minT = 0.83f, points = 4000 },
        new ScoreBand { minT = 0.99f, points = 5000 },
    };

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Override to gate scoring (e.g. require boss defeated).
    /// Returns true by default.
    /// </summary>
    public virtual bool CanGrantReward() => true;

    /// <summary>
    /// Calculates and grants the flagpole reward for the given player collider.
    /// Returns the points granted, or 0 if reward was blocked or no points found.
    /// </summary>
    public int GrantReward(Collider2D playerCol, MarioCore mario)
    {
        if (!CanGrantReward()) return 0;

        int points = GetPoints(playerCol);
        if (points <= 0) return 0;

        GameManager.Instance?.GetSystem<ScoreSystem>().AddScore(points);

        if (ScorePopupManager.Instance != null && mario != null)
        {
            var popupId = PointsToPopupID(points);
            if (popupId != PopupID.None)
            {
                var result = new ComboResult(RewardType.Score, popupId, points);
                ScorePopupManager.Instance.ShowPopup(
                    result,
                    playerCol.transform.position + Vector3.up * 0.5f,
                    mario.State.PowerupState);
            }
        }

        return points;
    }

    /// <summary>Returns normalized hit position (0 = bottom, 1 = top).</summary>
    public float GetNormalizedHit(Collider2D playerCol)
    {
        if (!TryGetPoleRange(out float bottomY, out float topY)) return 0f;
        return Mathf.Clamp01(Mathf.InverseLerp(bottomY, topY, playerCol.bounds.max.y));
    }

    /// <summary>Returns points for a given normalized hit position.</summary>
    public int GetPoints(Collider2D playerCol)
    {
        float t = GetNormalizedHit(playerCol);
        return GetPointsFromT(t);
    }

    public bool TryGetPoleRange(out float bottomY, out float topY)
    {
        bottomY = 0f; topY = 0f;

        if (poleBottom != null && poleTop != null)
        {
            bottomY = poleBottom.position.y;
            topY    = poleTop.position.y;
            return !Mathf.Approximately(bottomY, topY);
        }

        if (useColliderIfNoTransforms)
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                bottomY = col.bounds.min.y;
                topY    = col.bounds.max.y;
                return !Mathf.Approximately(bottomY, topY);
            }
        }

        return false;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private int GetPointsFromT(float t)
    {
        if (scoreBands == null || scoreBands.Length == 0) return 0;

        for (int i = 0; i < scoreBands.Length; i++)
        {
            float minT = scoreBands[i].minT;
            float maxT = i < scoreBands.Length - 1 ? scoreBands[i + 1].minT : 1f;

            if (t >= minT && t <= maxT)
                return scoreBands[i].points;
        }

        return 0;
    }

    private PopupID PointsToPopupID(int points) => points switch
    {
        100  => PopupID.Score100,
        400  => PopupID.Score400,
        800  => PopupID.Score800,
        2000 => PopupID.Score2000,
        4000 => PopupID.Score4000,
        5000 => PopupID.Score5000,
        _    => PopupID.None
    };

    // ─── Gizmos ──────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (!TryGetPoleRange(out float bottomY, out float topY)) return;

        float x = transform.position.x;
        Gizmos.color = Color.white;
        Gizmos.DrawLine(new Vector3(x, bottomY, 0f), new Vector3(x, topY, 0f));

        if (scoreBands == null) return;

        for (int i = 0; i < scoreBands.Length; i++)
        {
            float bandMin = scoreBands[i].minT;
            float bandMax = i < scoreBands.Length - 1 ? scoreBands[i + 1].minT : 1f;
            float yMin    = Mathf.Lerp(bottomY, topY, bandMin);
            float yMax    = Mathf.Lerp(bottomY, topY, bandMax);
            float c       = Mathf.Lerp(0.3f, 1f, (bandMin + bandMax) * 0.5f);
            Gizmos.color  = new Color(c, c, c, 1f);
            Gizmos.DrawLine(new Vector3(x - 0.25f, yMin, 0f), new Vector3(x + 0.25f, yMin, 0f));
            Gizmos.DrawLine(new Vector3(x - 0.25f, yMax, 0f), new Vector3(x + 0.25f, yMax, 0f));
            Gizmos.DrawLine(new Vector3(x + 0.35f, yMin, 0f), new Vector3(x + 0.35f, yMax, 0f));
        }

        Gizmos.color = Color.white;
    }
}