using System.Collections;
using UnityEngine;

/// <summary>
/// Cape attack ability — faithful port of the original CapeAttack.cs to MarioCore.
///
/// Behaviour (unchanged from original):
/// - onExtraActionPressed triggers the cape swing after a short delay
/// - During the delay, marioPrepareAttackSound plays and isBlockingJump = true
/// - When the delay fires, a raycast hits enemies in front and calls EnemyAI.OnCapeAttack()
/// - While isCapeActive in the air: reduce gravity and clamp fall speed
/// - While isCapeActive on the ground: zero horizontal velocity
/// - If groundPounding/spinning/wallSliding starts during cape: cancel immediately
/// - canCape goes false for the cooldown duration, then true again
/// - Landing during an air cape attack also cancels it
///
/// The only changes from the original:
///   marioMovement.* → State.* / Core.*
///   marioRb         → Core.Rb
///   animator        → Core's animator via MarioEvents (OnCapeAttackStarted/Ended)
/// </summary>
public class CapeAttack : MarioAbility
{
    [Header("Cape Settings")]
    public bool  canCape          = true;
    public float capeCooldown     = 0.75f;
    public float capeAttackDelay  = 0.3f;

    private int         _enemyLayerMask;

    private Coroutine _capeDelayRoutine;
    private Coroutine _capeCooldownRoutine;

    // ─── Lifecycle ───────────────────────────────────────────────────────────

    public override void Initialize(MarioCore core)
    {
        base.Initialize(core);
        _enemyLayerMask = LayerMask.GetMask("Enemy");
    }

    // ─── Ability Hook ────────────────────────────────────────────────────────

    public override void onExtraActionPressed()
    {
        Debug.Log($"[Cape] onExtraActionPressed called. canCape={canCape} IsCapeActive={State.IsCapeActive} IsMidairSpinning={State.IsMidairSpinning}");
        if (!canCape) return;

        // Block while already doing an incompatible action
        if (State.GroundPounding || State.Spinning ||
            State.WallSliding    || State.IsMidairSpinning)
            return;
        
        // This stops any leftover cooldown from the previous cape
        if (_capeCooldownRoutine != null)
        {
            _capeCooldownRoutine = StartCoroutine(CapeCooldown());
            Debug.Log($"[Cape] Started new cooldown coroutine: {_capeCooldownRoutine.GetHashCode()}");
            _capeCooldownRoutine = null;
        }

        State.IsCapeActive = true;
        isBlockingJump     = true;
        canCape            = false;

        // Flip to face move direction if airborne and input is opposite facing
        if (!State.OnGround &&
            State.MoveInput.x != 0f &&
            State.FacingRight != (State.MoveInput.x > 0f))
        {
            Core.Physics.FlipTo(State.MoveInput.x > 0f);
        }

        MarioEvents.FireCapeAttackStarted(PlayerIndex);

        _capeDelayRoutine    = StartCoroutine(CapeAttackDelay());
        _capeCooldownRoutine = StartCoroutine(CapeCooldown());
    }

    // ─── Fixed Update Hook ───────────────────────────────────────────────────

    public override void onFixedUpdate()
    {
        if (!State.IsCapeActive) return;

        // Cancel if an incompatible action started mid-cape
        if (State.GroundPounding || State.Spinning || State.WallSliding || State.IsMidairSpinning)
        {
            CancelCapeAttack();
            return;
        }

        if (!State.OnGround)
        {
            // Air cape: float slowly
            Core.Rb.gravityScale = Core.Physics.Config.FallGravity * 0.3f;
            Core.Rb.velocity     = new Vector2(0f, Mathf.Max(Core.Rb.velocity.y, -1f));
        }
        else if (!State.WasGrounded)
        {
            // Just landed during an air cape — cancel
            CancelCapeAttack();
        }
        else
        {
            // Ground cape: no horizontal movement
            Core.Rb.velocity = new Vector2(0f, Core.Rb.velocity.y);
        }

        // Ground unlock (original logic kept verbatim)
        if (State.OnGround && !isBlockingJump && canCape)
            isBlockingJump = false;
    }

    // ─── Coroutines ──────────────────────────────────────────────────────────

    private IEnumerator CapeAttackDelay()
    {
        yield return new WaitForSeconds(capeAttackDelay);

        MarioEvents.FireCapeAttackSwung(PlayerIndex);

        Vector2 dir = State.FacingRight ? Vector2.right : Vector2.left;

        RaycastHit2D[] hits = Physics2D.RaycastAll(
            transform.position, dir, 1.5f, _enemyLayerMask);

        foreach (var hit in hits)
        {
            var enemy = hit.collider.GetComponentInParent<EnemyAI>();
            enemy?.OnCapeAttack(State.FacingRight);
        }

        _capeDelayRoutine = null;
    }

    private IEnumerator CapeCooldown()
    {
        Debug.Log($"[Cape] CapeCooldown started");
        yield return new WaitForSeconds(capeCooldown);
        Debug.Log($"[Cape] CapeCooldown finished, calling EndCape");
        EndCape();
        canCape = true;
        isBlockingJump = false;
        _capeCooldownRoutine = null;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void EndCape()
    {
        if (!State.IsCapeActive) return;
        Debug.Log($"[Cape] EndCape called. Stack: {new System.Diagnostics.StackTrace()}");
        State.IsCapeActive   = false;
        Core.Rb.gravityScale = Core.Physics.Config.FallGravity;
        MarioEvents.FireCapeAttackEnded(PlayerIndex);
    }

    private void CancelCapeAttack()
    {
        EndCape();

        isBlockingJump = false;

        if (_capeDelayRoutine != null)
        {
            StopCoroutine(_capeDelayRoutine);
            _capeDelayRoutine = null;
        }

        if (_capeCooldownRoutine != null)
        {
            StopCoroutine(_capeCooldownRoutine);
            _capeCooldownRoutine = null;
        }

        canCape = true;
    }
}