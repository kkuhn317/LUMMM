using UnityEngine;

public class Trampoline : MonoBehaviour
{
    #region Variables

    public bool objectBounce = false;
    public float playerBouncePower = 10;
    public float objectBouncePower = 25;
    public bool sideways = false; // If the spring is sideways

    private bool _isVisible = false; // If the spring is visible to the camera
    private bool _bouncedThisContact = false;

    private Animator    _animator;
    private AudioSource _audioSource;

    #endregion

    #region Unity Methods

    private void Awake()
    {
        TryGetComponent(out _animator);
        TryGetComponent(out _audioSource);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        _bouncedThisContact = false;
        Vector2 impulse = Vector2.zero;

        int contactCount = other.contactCount;
        for (int i = 0; i < contactCount; i++)
        {
            var contact = other.GetContact(i);
            impulse += contact.normal * contact.normalImpulse;
#if !UNITY_ANDROID
            impulse.x += contact.tangentImpulse * contact.normal.y;
            impulse.y -= contact.tangentImpulse * contact.normal.x;
#endif
            // NOTE: The tiny spring could bounce mario up from the side on mobile if the above lines are uncommented
            // ALSO, on Web version, springs might have a chance to not work if those 2 lines are not present (at least that's my theory...)
            // So, for now we will only run those lines on Android and not on Web or Windows
        }

        if (other.gameObject.CompareTag("Player"))
        {
            MarioCore player = GetPlayer(other.gameObject);
            if (player != null)
            {
                Rigidbody2D rb = player.Rb;

                bool hittingFromAbove = impulse.y < 0 ||
                    (player.State.OnGround && !sideways && other.GetContact(0).normal.y > 0.5f);

                if (hittingFromAbove && !sideways && !_bouncedThisContact)
                {
                    _bouncedThisContact = true;
                    if (player.State.GroundPounding)
                        MarioEvents.FireGroundPoundCancelled(player.PlayerIndex);
                    player.State.GroundPounding = false;
                    player.State.IsBounced = true;
                    player.StateMachine.ForceTransition(MarioStateID.Fall);
                    player.StateMachine.ForceTransition(MarioStateID.Rise);
                    rb.velocity += new Vector2(0, playerBouncePower);
                    Bounce();
                }
                else if (impulse.x < 0 && sideways)
                {
                    rb.velocity += new Vector2(playerBouncePower, 0);
                    Bounce();
                }
                else if (impulse.x > 0 && sideways)
                {
                    rb.velocity += new Vector2(-playerBouncePower, 0);
                    Bounce();
                }
            }
        }

        TryBounceObject(other.transform, other.gameObject.GetComponent<ObjectPhysics>());
    }

    // Sometimes OnCollisionEnter doesnt detect Mario being on top of the spring. This makes sure that its detected
    private void OnCollisionStay2D(Collision2D other)
    {
        OnCollisionEnter2D(other);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        _bouncedThisContact = false;

        // Check if the triggering object is the player
        if (other.gameObject.CompareTag("Player"))
        {
            MarioCore player = GetPlayer(other.gameObject);
            if (player != null)
            {
                // Cancel ground pound when bouncing on trampoline (trigger version)
                if (player.State.GroundPounding)
                    MarioEvents.FireGroundPoundCancelled(player.PlayerIndex);
                player.State.GroundPounding = false;
                player.StateMachine.ForceTransition(MarioStateID.Fall);
            }
        }

        TryBounceObject(other.transform, other.GetComponent<ObjectPhysics>());
    }

    private void OnBecameVisible()   => _isVisible = true;
    private void OnBecameInvisible() => _isVisible = false;

    #endregion

    #region Methods

    public void Bounce()
    {
        if (_animator != null) // If there's an animation
            _animator.SetTrigger("Bounce");

        if (!_isVisible) // If it's not visible, don't play the sound
            return;

        if (_audioSource != null) // If it contains an audio
            _audioSource.Play();
    }

    private MarioCore GetPlayer(GameObject obj)
        => obj.GetComponent<MarioCore>() ?? obj.GetComponentInParent<MarioCore>();

    private void TryBounceObject(Transform other, ObjectPhysics obj)
    {
        if (!objectBounce || obj == null) return;

        if (other.position.y > transform.position.y && other.position.x > transform.position.x - 1 && other.position.x < transform.position.x + 1 && !sideways)
        {
            obj.velocity = new Vector2(obj.velocity.x, objectBouncePower);
            Bounce();
        }
        else if (other.position.x > transform.position.x && other.position.y > transform.position.y - 1 && other.position.y < transform.position.y + 1 && sideways)
        {
            obj.velocity = new Vector2(objectBouncePower, obj.velocity.y);
            Bounce();
        }
        else if (other.position.x < transform.position.x && other.position.y > transform.position.y - 1 && other.position.y < transform.position.y + 1 && sideways)
        {
            obj.velocity = new Vector2(-objectBouncePower, obj.velocity.y);
            Bounce();
        }
    }

    #endregion
}