using UnityEngine;

public class Grrol : EnemyAI
{
    [SerializeField] private Transform rotationTarget; // The object to which rotation is applied
    [SerializeField] private float rotationSpeedMultiplier = 1f; // Multiplier for rotation speed

    private Animator animator;
    private AudioSource audioSource;

    protected override void Start()
    {
        base.Start();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        enabled = true;
    }

    protected override void Update()
    {
        base.Update();

        // rotation speed: velocity / radius
        
        // Calculate linear velocity magnitude
        float linearVelocityMagnitude = Mathf.Sqrt(velocity.x * velocity.x + velocity.y * velocity.y);

        // Calculate rotation speed based on linear velocity magnitude and multiplier
        float rotationSpeed = linearVelocityMagnitude * rotationSpeedMultiplier;

        // Determine rotation direction based on whether the object is moving left
        float rotationDirection = movingLeft ? 1f : -1f;

        // Rotate the rotationTarget object around the z-axis
        rotationTarget.Rotate(0, 0, rotationDirection * rotationSpeed * Time.deltaTime);

        // Check if the object is moving
        bool isMoving = linearVelocityMagnitude > 0;

        // Set animator speed based on whether the object is moving
        if (animator != null)
        {
            if (isMoving)
            {
                animator.speed = 1f; // Unpause the animator
            }
            else
            {
                animator.speed = 0f; // Pause the animator
            }
        }
    }

    protected override void onTouchWall(GameObject other)
    {
        base.onTouchWall(other);

        // Get the CameraFollow component from the camera
        CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();

        // Trigger camera shake
        cameraFollow.ShakeCameraRepeatedly(0.25f, 2.0f, 1.0f, new Vector3(0f, 1f, 0f), 3, 0.1f);

        if (audioSource != null)
        {
            audioSource.Play();
        }
    }
}
