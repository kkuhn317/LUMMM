using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.U2D.Animation;
using PowerupState = PowerStates.PowerupState;

public class Flag : MonoBehaviour
{
    [HideInInspector]
    public float height;

    public GameObject starParticlePrefab;
    public AudioClip finishFlag;

    public GameObject flag;
    public GameObject pole;
    public GameObject cutsceneMario;

    // if null, it always uses cutsceneMario
    // if not null, use cutsceneMario if mario is small, and use optCutsceneBigMario if mario is big or higher
    // for big cutscene mario, its sprite library will be set to the same as the mario that touched the flag
    // so all powerups will be preserved
    public GameObject optCutsceneBigMario;

    GameObject csMario; // the mario that will be used in the cutscene
    Vector3 endPos = new(-0.4f, 1.0f, 0); // Where mario will stop sliding down the pole (changes when he's big)

    public float marioSlideSpeed = 50f;
    public float flagMoveSpeed = 50f;
    public float slideTime = 2f; // how long from when mario first touches the flag to when he gets off the pole
    public float cutsceneTime = 10f; // how long the cutscene lasts before the level ends
    bool marioAtBottom = false;
    public bool flagOnRight = false;    // if the flag is on the right side of the pole

    [Header("Cutscene System")]
    [SerializeField] private CutsceneSelector cutsceneSelector;

    // Level flow (refactor) - owns end-level sequences
    private LevelFlowController levelFlowController;

    // flagpole scoring
    [System.Serializable]
    public struct FlagScoreBand
    {
        [Range(0f, 1f)] public float minT;
        [Range(0f, 1f)] public float maxT;
        public int points;
    }

    [Header("Flagpole Scoring")]
    [Tooltip("Optional overrides for pole bottom/top. If not set, collider bounds are used.")]
    [SerializeField] private Transform poleBottom;

    [Tooltip("Optional overrides for pole bottom/top. If not set, collider bounds are used.")]
    [SerializeField] private Transform poleTop;

    [SerializeField] private bool useColliderIfNoTransforms = true;

    [Tooltip("Normalized score bands from bottom (0) to top (1). Works for any pole height.")]
    [SerializeField] private FlagScoreBand[] scoreBands = new FlagScoreBand[]
    {
        // Default SMB-ish mapping (normalized). Adjust per pole/level if you want.
        new FlagScoreBand { minT = 0.00f, maxT = 0.11f, points = 100  },
        new FlagScoreBand { minT = 0.11f, maxT = 0.37f, points = 400  },
        new FlagScoreBand { minT = 0.37f, maxT = 0.53f, points = 800  },
        new FlagScoreBand { minT = 0.53f, maxT = 0.83f, points = 2000 },
        new FlagScoreBand { minT = 0.83f, maxT = 0.99f, points = 4000 },
        new FlagScoreBand { minT = 0.99f, maxT = 1.00f, points = 5000 },
    };

    private void CacheLevelFlow()
    {
        // We don't want any fallback to the legacy GameManager.
        // LevelFlowController is part of the refactor pipeline.
        if (levelFlowController == null)
            levelFlowController = FindObjectOfType<LevelFlowController>(true);
    }

    private bool TryGetPoleRange(out float bottomY, out float topY)
    {
        bottomY = 0f;
        topY = 0f;

        if (poleBottom != null && poleTop != null)
        {
            bottomY = poleBottom.position.y;
            topY = poleTop.position.y;
            return !Mathf.Approximately(bottomY, topY);
        }

        if (useColliderIfNoTransforms)
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                var b = col.bounds;
                bottomY = b.min.y;
                topY = b.max.y;
                return !Mathf.Approximately(bottomY, topY);
            }
        }

        return false;
    }

    private float GetNormalizedHitT(Collider2D playerCol)
    {
        if (!TryGetPoleRange(out float bottomY, out float topY))
            return 0f;

        // Using top of player collider = “how high you touched”.
        float touchY = playerCol.bounds.max.y;
        float t = Mathf.InverseLerp(bottomY, topY, touchY);
        return Mathf.Clamp01(t);
    }

    private int GetPointsFromT(float t)
    {
        if (scoreBands == null || scoreBands.Length == 0)
            return 0;

        int best = 0;

        for (int i = 0; i < scoreBands.Length; i++)
        {
            var band = scoreBands[i];
            float min = Mathf.Clamp01(Mathf.Min(band.minT, band.maxT));
            float max = Mathf.Clamp01(Mathf.Max(band.minT, band.maxT));

            if (t >= min && t <= max)
                best = Mathf.Max(best, band.points);
        }

        return best;
    }

    protected virtual void TryGrantFlagpoleReward(Collider2D other, MarioMovement mario)
    {
        int flagPoints = GetFlagpolePoints(other);
        if (flagPoints <= 0) return;

        // Refactor: score lives in ScoreSystem now
        GameManager.Instance.GetSystem<ScoreSystem>().AddScore(flagPoints);

        if (ScorePopupManager.Instance != null && mario != null)
        {
            var popupId = PointsToPopupID(flagPoints);
            if (popupId != PopupID.None)
            {
                Vector3 popupPos = other.transform.position + Vector3.up * 0.5f;

                // Same pattern as PowerUp.cs
                ComboResult result = new ComboResult(RewardType.Score, popupId, flagPoints);
                var popupPowerState = mario.powerupState;
                ScorePopupManager.Instance.ShowPopup(result, popupPos, popupPowerState);
            }
        }
    }

    protected int GetFlagpolePoints(Collider2D playerCol)
    {
        float t = GetNormalizedHitT(playerCol);
        return GetPointsFromT(t);
    }

    protected PopupID PointsToPopupID(int points)
    {
        return points switch
        {
            100 => PopupID.Score100,
            400 => PopupID.Score400,
            800 => PopupID.Score800,
            2000 => PopupID.Score2000,
            4000 => PopupID.Score4000,
            5000 => PopupID.Score5000,
            _ => PopupID.None
        };
    }

    enum FlagState
    {
        Idle,
        Sliding,
        Cutscene
    }

    FlagState state = FlagState.Idle;

    protected virtual void Start()
    {
        CacheLevelFlow();
    }

    protected virtual void Update()
    {
        if (state == FlagState.Sliding)
        {
            // flag moves down
            // final position is -0.5, 1.1 relative to the pole
            flag.transform.localPosition = Vector3.MoveTowards(
                flag.transform.localPosition,
                new Vector3(flagOnRight ? 0.5f : -0.5f, 1.1f, 0),
                flagMoveSpeed * Time.deltaTime
            );

            Vector3 nonLocalEndPos = transform.position + endPos;

            // cutsceneMario moves down
            if (!marioAtBottom)
            {
                csMario.transform.position = Vector3.MoveTowards(
                    csMario.transform.position,
                    nonLocalEndPos,
                    marioSlideSpeed * Time.deltaTime
                );

                // NOTE: comparing floats directly is risky; keep your logic but make it robust:
                if (Mathf.Abs(csMario.transform.position.y - nonLocalEndPos.y) <= 0.001f)
                {
                    marioAtBottom = true;
                    csMario.GetComponent<Animator>().SetFloat("climbSpeed", 0f);
                }
            }
        }
    }

    // might be of use later but for now it's useless (I was testing so this is why this is here)
    private CutsceneContext BuildContext()
    {
        // Refactor-friendly version:
        // If CutsceneContext expects a type different from GameManagerRefactored, keep only what you need.
        var mario = FindObjectOfType<MarioMovement>();

        return new CutsceneContext
        {
            scene = SceneManager.GetActiveScene(),
            mainPlayer = mario,
            playerPosition = mario != null ? mario.transform.position : Vector3.zero,
            powerupState = mario != null ? mario.powerupState : PowerupState.small,
            hasStarPower = mario != null && mario.starPower,
            isDead = mario != null && mario.Dead,
        };
    }

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player") && state == FlagState.Idle)
        {
            CacheLevelFlow();

            var mario = other.GetComponent<MarioMovement>();

            TryGrantFlagpoleReward(other, mario);
            
            var timerManager = GameManager.Instance.GetSystem<TimerManager>();
            LevelFlowController.MarkEndingLevel();
            timerManager?.StopAllTimers();
            timerManager?.StopTimeWarningMusic();

            csMario = cutsceneMario;

            if (optCutsceneBigMario != null && mario != null && PowerStates.IsBig(mario.powerupState))
            {
                csMario = optCutsceneBigMario;
                csMario.GetComponent<SpriteLibrary>().spriteLibraryAsset = other.gameObject.GetComponent<SpriteLibrary>().spriteLibraryAsset;
                endPos = new(-0.4f, 1.5f, 0);
            }

            csMario.SetActive(true);
            csMario.transform.position = new Vector2(transform.position.x - 0.4f, other.transform.position.y);

            if (starParticlePrefab != null)
            {
                SpawnStarParticles();
            }

            // hide the player
            other.gameObject.SetActive(false);

            var animator = csMario.GetComponent<Animator>();
            animator.SetBool("isSideClimbing", true);
            animator.SetFloat("climbSpeed", 5f);

            state = FlagState.Sliding;

            // play the flagpole sound
            GetComponents<AudioSource>()[0].Play(); // first audio source is the flagpole sound, second is for music

            // stop the music
            MusicManager.Instance.MuteAllMusic();

            // change to cutscene state after a certain amount of time
            StartCoroutine(ToCutsceneState());
        }
    }

    private CutsceneContext BuildCutsceneContext()
    {
        // If your cutscene system needs a "gameManager" reference, you should adapt CutsceneContext
        // to accept IGameManager instead of the legacy class.
        return new CutsceneContext
        {
            scene = SceneManager.GetActiveScene()
        };
    }

    private IEnumerator ToCutsceneState()
    {
        yield return new WaitForSeconds(slideTime);

        state = FlagState.Cutscene; // Transition to cutscene state
        print("Transitioning to cutscene state.");

        var ctx = BuildCutsceneContext();

        if (cutsceneSelector != null)
        {
            StartCoroutine(cutsceneSelector.PlaySelectedCutscene(ctx));
        }
        else
        {
            // Refactor: delegate end-level cutscene flow to LevelFlowController (NO legacy GameManager)
            var director = GetComponent<PlayableDirector>();
            if (levelFlowController != null && director != null)
            {
                // Parameters:
                // - cutsceneLength: how long the cutscene lasts before ending level
                // - destroyPlayersImmediately: we already hid the player, so false
                // - stopMusicImmediately: we already muted music, so false
                levelFlowController.TriggerCutsceneEnding(director, cutsceneTime, false, false);
            }
            else
            {
                Debug.LogWarning("Flag: No CutsceneSelector and no LevelFlowController/PlayableDirector found. Unable to finish level cutscene properly.");
            }
        }
    }

    // used in the editor to change the height of the entire flagpole easily
    public void ChangeHeight(float height)
    {
        this.height = height;

        // flag
        flag.transform.localPosition = new Vector3(flagOnRight ? 0.5f : -0.5f, height - 0.55f, 0);

        // pole
        pole.GetComponent<SpriteRenderer>().size = new Vector2(0.5f, height);
        pole.transform.localPosition = new Vector3(0, (height + 1) / 2, 0);

        // collider
        GetComponent<BoxCollider2D>().size = new Vector2(0.25f, height);
        GetComponent<BoxCollider2D>().offset = new Vector2(0, (height + 1) / 2);
    }

    private void OnDrawGizmos()
    {
        if (!TryGetPoleRange(out float bottomY, out float topY))
            return;

        float x = transform.position.x;

        // Main pole line
        Gizmos.color = Color.white;
        Gizmos.DrawLine(new Vector3(x, bottomY, 0f), new Vector3(x, topY, 0f));

        if (scoreBands == null) return;

        for (int i = 0; i < scoreBands.Length; i++)
        {
            var band = scoreBands[i];
            float min = Mathf.Clamp01(Mathf.Min(band.minT, band.maxT));
            float max = Mathf.Clamp01(Mathf.Max(band.minT, band.maxT));

            float yMin = Mathf.Lerp(bottomY, topY, min);
            float yMax = Mathf.Lerp(bottomY, topY, max);

            // grayscale ramp: higher bands brighter
            float mid = (min + max) * 0.5f;
            float c = Mathf.Lerp(0.3f, 1f, mid);
            Gizmos.color = new Color(c, c, c, 1f);

            // ticks
            Gizmos.DrawLine(new Vector3(x - 0.25f, yMin, 0f), new Vector3(x + 0.25f, yMin, 0f));
            Gizmos.DrawLine(new Vector3(x - 0.25f, yMax, 0f), new Vector3(x + 0.25f, yMax, 0f));

            // band segment on the side
            Gizmos.DrawLine(new Vector3(x + 0.35f, yMin, 0f), new Vector3(x + 0.35f, yMax, 0f));
        }

        Gizmos.color = Color.white;
    }

    #region particles
    void SpawnStarParticles()
    {
        // Spawn 8 stars around the flag
        int[] vertdirections = new int[] { -1, 0, 1 };
        int[] horizdirections = new int[] { -1, 0, 1 };
        for (int i = 0; i < vertdirections.Length; i++)
        {
            for (int j = 0; j < horizdirections.Length; j++)
            {
                if (vertdirections[i] == 0 && horizdirections[j] == 0)
                {
                    continue;
                }
                float distance = (vertdirections[i] != 0 && horizdirections[j] != 0) ? 0.7f : 1f;
                Vector3 startOffset = new Vector3(horizdirections[i] * distance, vertdirections[j] * distance, 0);

                // Instantiate the star particle
                GameObject starParticle = Instantiate(starParticlePrefab, csMario.transform.position + startOffset, Quaternion.identity);

                starParticle.GetComponent<StarMoveOutward>().direction = new Vector2(vertdirections[i], horizdirections[j]);
                starParticle.GetComponent<StarMoveOutward>().speed = 2f;
            }
        }
    }
    #endregion
}