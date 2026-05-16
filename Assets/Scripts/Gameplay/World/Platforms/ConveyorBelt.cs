using UnityEngine;

public enum ConveyorDirection { Left, Right }

public class ConveyorBelt : MonoBehaviour
{
    public GameObject left;
    public GameObject middle;
    public GameObject right;

    [HideInInspector] public int length;

    // [Tooltip("The amount of space on the edges of the conveyor where the speed is slower")]
    // [field: SerializeField, Min(0f)] public float HelperEdgeLength { get; private set; }
    // [Tooltip("The speed of the conveyor on the edges")]
    // [field: SerializeField, Min(0f)] public float HelperEdgeSpeed { get; private set; }

    [field: SerializeField, Min(0f)] public float Speed { get; private set; }

    [field: SerializeField] public ConveyorDirection Direction { get; private set; }

    [Tooltip("If enabled, animation speed scales proportionally with Speed.")]
    [SerializeField] private bool _scaleAnimationWithSpeed = false;

    private Animator _animator;
    private BoxCollider2D _collider;

    public bool IsActive => Speed > 0f;

    public Vector2 Velocity => new Vector2(Speed * DirectionMultiplier, 0f);

    private int DirectionMultiplier => Direction == ConveyorDirection.Right ? 1 : -1;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _collider = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        RefreshAnimator();
    }

    private void OnValidate()
    {
        RefreshAnimator();
    }

    private void RefreshAnimator()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();
        if (_animator == null) return;

        if (!IsActive)
            _animator.speed = 0f;
        else
            _animator.speed = _scaleAnimationWithSpeed ? Speed : 1f;

        _animator.SetBool("direction", Direction == ConveyorDirection.Right);
    }

    public void ChangeLength(int newLength)
    {
        if (left == null || middle == null || right == null) return;

        length = newLength;

        float halfOffset = (length - 1) / 2f;
        left.transform.localPosition  = new Vector3(-halfOffset, 0f, 0f);
        right.transform.localPosition = new Vector3( halfOffset, 0f, 0f);

        SpriteRenderer middleRenderer = middle.GetComponent<SpriteRenderer>();
        if (middleRenderer != null)
            middleRenderer.size = new Vector2(length - 2, 1f);

        if (_collider == null)
            _collider = GetComponent<BoxCollider2D>();
        if (_collider != null)
            _collider.size = new Vector2(length, 1f);
    }
}