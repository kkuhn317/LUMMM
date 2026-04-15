using UnityEngine;

/// <summary>
/// Attach this to the Mario GameObject temporarily to diagnose the landing
/// velocity loss bug. It logs velocity and position every FixedUpdate for
/// a short window around the moment of landing.
///
/// REMOVE before shipping — debug only.
/// </summary>
[DefaultExecutionOrder(100)] // Run LAST so all other systems have already run this frame
public class LandingDebugger : MonoBehaviour
{
    private Rigidbody2D _rb;
    private MarioCore   _core;

    private bool  _wasGrounded;
    private int   _logFramesRemaining;
    private int   _fixedFrame;

    private void Awake()
    {
        _rb   = GetComponent<Rigidbody2D>();
        _core = GetComponent<MarioCore>();
    }

    private void FixedUpdate()
    {
        _fixedFrame++;
        bool isGrounded = _core.StateMachine.IsGrounded;

        // Detect the landing frame (airborne → grounded transition)
        if (!_wasGrounded && isGrounded)
            _logFramesRemaining = 6; // log 6 frames around landing

        if (_logFramesRemaining > 0)
        {
            _logFramesRemaining--;
            Debug.Log(
                $"[Landing F{_fixedFrame}] " +
                $"state={_core.StateMachine.CurrentStateID,-10} " +
                $"vel=({_rb.velocity.x:F4}, {_rb.velocity.y:F4}) " +
                $"pos=({_rb.position.x:F4}, {_rb.position.y:F4}) " +
                $"drag={_rb.drag:F1} " +
                $"gravScale={_rb.gravityScale:F1} " +
                $"onGround={_core.State.OnGround}"
            );
        }

        _wasGrounded = isGrounded;
    }
}