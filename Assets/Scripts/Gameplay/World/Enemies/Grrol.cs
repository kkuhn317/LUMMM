using UnityEngine;

public class Grrol : EnemyAI
{
    [Header("Grrol")]
    [SerializeField] private Transform rotationTarget; // The object to which rotation is applied
    [SerializeField] private float rotationSpeedMultiplier = 1f; // Multiplier for rotation speed
    [SerializeField] private float groundMovementSpeed = 5f;
    [SerializeField] private bool startMovementWhenTouchGround = true;
    [SerializeField] private bool enableCameraShake = true; // whether to enable camera shake on touch wall
    
    [Header("Camera Shake Settings")]
    [SerializeField] private float shakeDuration = 0.25f; // Duration of camera shake
    [SerializeField] private float shakeIntensity = 2.0f; // Intensity of the shake
    [SerializeField] private float shakeFrequency = 1.0f; // Frequency of the shake
    [SerializeField] private Vector3 shakeDirection = new Vector3(0f, 1f, 0f); // Direction of the shake
    [SerializeField] private int shakeRepetitions = 3; // Number of shake repetitions
    [SerializeField] private float shakeInterval = 0.1f; // Interval between repetitions

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

        // THIS IS IF YOU WANT YOUR OBJECT ROTATES FASTER WHEN FALLING
        // rotation speed: velocity / radius
        // float linearVelocityMagnitude = Mathf.Sqrt(velocity.x * velocity.x + velocity.y * velocity.y);       // Calculate linear velocity magnitude
        // float rotationSpeed = linearVelocityMagnitude * rotationSpeedMultiplier;                             // Calculate rotation speed based on linear velocity magnitude and multiplier
        // float rotationDirection = movingLeft ? 1f : -1f;                                                     // Determine rotation direction based on whether the object is moving left

        // Calculate rotation speed based on the x component of velocity
        float rotationSpeed = Mathf.Abs(velocity.x) * rotationSpeedMultiplier;

        // Determine rotation direction based on whether the object is moving left
        float rotationDirection = movingLeft ? 1f : -1f;

        // Rotate the rotationTarget object around the z-axis
        if (rotationTarget != null)
        {
            rotationTarget.Rotate(0, 0, rotationDirection * rotationSpeed * Time.deltaTime);
        } 
        else 
        {
            Debug.Log("Rotation target missing. Please add which object you want to rotate");
        }

        // Check if the object is moving
        bool isMoving = Mathf.Abs(velocity.x) > 0;

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

        if (startMovementWhenTouchGround && objectState == ObjectState.grounded)
        {
            velocity.x = groundMovementSpeed;
        }
    }

    protected override void OnBounced() {
        if (startMovementWhenTouchGround) {
            velocity.x = groundMovementSpeed;
        }
    }

    protected override void onTouchWall(GameObject other)
    {
        base.onTouchWall(other);

        if (other == null) 
        {
            Debug.LogError("Collision detected, but 'other' collider is null.");
            return;
        }

        // Prevent a billion sounds playing when grrols get stuck due to randomizer
        if (audioSource != null && !GlobalVariables.cheatRandomizer)
        {
            audioSource.Play();
        }

        if (enableCameraShake) 
        {
            // Get the CameraFollow component from the camera
            CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
            // Trigger camera shake
            cameraFollow.ShakeCameraRepeatedly(
                shakeDuration, 
                shakeIntensity, 
                shakeFrequency, 
                shakeDirection, 
                shakeRepetitions, 
                shakeInterval
            );
        }
    }
}
