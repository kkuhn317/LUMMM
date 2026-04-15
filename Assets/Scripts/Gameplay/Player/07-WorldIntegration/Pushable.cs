using UnityEngine;

/// <summary>
/// Attached to pushable objects. Detects when Mario is in contact and
/// driving in the right direction, then applies push velocity to the object
/// and tells Mario's FSM to enter the Push state.
///
/// Ported from Pushable.cs — logic identical, reference changed from
/// MarioMovement to MarioCore.
/// </summary>
public class Pushable : MonoBehaviour
{
    public float     pushSpeed        = 10f;
    public float     stopWallDistance = 0f;
    public LayerMask wallLayer;

    private ObjectPhysics _physics;
    private MarioCore     _player;
    private bool _originalStopAfterLand;

    private void Start()
    {
        _physics = GetComponentInParent<ObjectPhysics>();
    }

    private void Update()
    {
        if (_player == null) return;

        var  rb      = _player.Rb;
        bool toRight = _player.transform.position.x < transform.position.x;
        float inputX = _player.State.MoveInput.x;

        // Block pushing when Mario is standing on top
        var objCol = GetComponent<Collider2D>();
        float objectTopY = objCol != null ? objCol.bounds.max.y : transform.position.y + 0.5f;
        float marioFeetY = _player.Rb.position.y - (_player.Collider != null ? _player.Collider.bounds.extents.y : 0.4f);
        bool marioIsOnTop = marioFeetY >= objectTopY - 0.1f;

        bool hitWall = !marioIsOnTop && WallDistanceCheck(toRight);

        bool canPushRight = toRight  && inputX > 0f && !marioIsOnTop && !hitWall;
        bool canPushLeft  = !toRight && inputX < 0f && !marioIsOnTop && !hitWall;

        Debug.Log($"[Pushable] toRight={toRight} input={inputX:F2} onTop={marioIsOnTop} marioFeet={marioFeetY:F2} objTop={objectTopY:F2} hitWall={hitWall} canR={canPushRight} canL={canPushLeft} Pushing={_player.State.Pushing}");

        if (canPushRight || canPushLeft)
        {
            _physics.movingLeft = !toRight;
            _physics.velocity.x = pushSpeed;

            if (!_player.State.Pushing)
            {
                _originalStopAfterLand   = _physics.stopAfterLand;
                _physics.stopAfterLand   = false;
                _player.Physics.FlipTo(toRight);
                _player.Carry.StartPushing(_physics, pushSpeed);
            }
        }
        else
        {
            if (_player.State.Pushing)
            {
                _physics.velocity.x    = 0f;
                _physics.stopAfterLand = _originalStopAfterLand;
                _player.Carry.StopPushing();
            }
        }
    }

    // checkRight=true → cast right, checkRight=false → cast left
    // Multiple rays at different heights, ignores slope surfaces
    private bool WallDistanceCheck(bool checkRight)
    {
        if (stopWallDistance <= 0f) return false;

        var col = GetComponent<Collider2D>();
        if (col == null) return false;

        float minY = col.bounds.min.y;
        float maxY = col.bounds.max.y;
        float originX = checkRight ? col.bounds.max.x : col.bounds.min.x;
        Vector2 dir = checkRight ? Vector2.right : Vector2.left;

        // Cast at 33%, 60%, 85% height — skips floor and top edges
        float[] heights = { 0.33f, 0.60f, 0.85f };
        foreach (float t in heights)
        {
            float y = Mathf.Lerp(minY, maxY, t);
            var hit = Physics2D.Raycast(new Vector2(originX, y), dir, stopWallDistance, wallLayer);
            // Only count true vertical walls — ignore sloped surfaces (normal.y > 0.3)
            if (hit.collider != null && Mathf.Abs(hit.normal.y) < 0.3f)
                return true;
        }
        return false;
    }

    public void StopPushing()
    {
        _physics.velocity.x    = 0f;
        _physics.stopAfterLand = _originalStopAfterLand;
        if (_player != null)
        {
            _player.Carry.StopPushing();
            _player = null;
        }
    }

    private void OnDrawGizmos()
    {
        if (stopWallDistance <= 0f) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left  * stopWallDistance);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * stopWallDistance);

        // Draw endpoint markers
        Gizmos.DrawWireSphere(transform.position + Vector3.left  * stopWallDistance, 0.08f);
        Gizmos.DrawWireSphere(transform.position + Vector3.right * stopWallDistance, 0.08f);
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        var core = col.GetComponent<MarioCore>() ?? col.GetComponentInParent<MarioCore>();
        if (core != null)
            _player = core;
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        var core = col.GetComponent<MarioCore>() ?? col.GetComponentInParent<MarioCore>();
        if (core != null && core == _player)
        {
            if (_player != null)
            {
                _player.Carry.StopPushing();
                _player = null;
            }
            _physics.velocity.x = 0f;
        }
    }
}