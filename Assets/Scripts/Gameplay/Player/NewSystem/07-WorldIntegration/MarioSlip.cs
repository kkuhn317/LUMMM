using UnityEngine;

/// <summary>
/// Temporary slip behaviour when Mario hits a banana peel.
///
/// Takes over movement by calling Core.SetModulesEnabled(false),
/// applies a spinning arc, then restores control when Mario lands.
///
/// Ported from MarioSlip.cs — logic is identical, reference changed from
/// MarioMovement to MarioCore. Self-destructs on landing.
/// </summary>
[RequireComponent(typeof(MarioCore))]
public class MarioSlip : MonoBehaviour
{
    private Rigidbody2D  _rb;
    private Animator     _animator;
    private MarioCore    _core;

    private void Start()
    {
        _rb       = GetComponent<Rigidbody2D>();
        _animator = GetComponentInChildren<Animator>();
        _core     = GetComponent<MarioCore>();

        // Hand off movement to this script
        _core.SetModulesEnabled(false);

        bool movingRight = _rb.velocity.x >= 0f;

        _rb.constraints = RigidbodyConstraints2D.None;
        _rb.AddTorque(movingRight ? 100f : -100f);
        _rb.velocity    = new Vector2(movingRight ? 5f : -5f, 15f);
        _rb.gravityScale = _core.Physics.Config.FallGravity;

        _animator.SetBool("onGround", false);
        _animator.SetTrigger("slip");
    }

    private void FixedUpdate()
    {
        var hit = _core.GroundDetection.CheckGround();

        if (hit != null && _rb.velocity.y <= 0f)
        {
            // Landed — restore normal control
            _rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
            transform.rotation = Quaternion.identity;
            _rb.velocity     = Vector2.zero;

            _animator.SetBool("onGround", true);
            _core.SetModulesEnabled(true);

            Destroy(this);
        }
    }
}
