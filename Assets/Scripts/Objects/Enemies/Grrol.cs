using UnityEngine;

public class Grrol : EnemyAI
{
    [SerializeField] private Transform rotationTarget; // The object to which rotation is applied
    [SerializeField] private float rotationSpeedMultiplier = 1f; // Multiplier for rotation speed

    private Animator animator;

    protected override void Start()
    {
        base.Start();
        animator = GetComponent<Animator>();

        enabled = true;
    }

    protected override void Update()
    {
        base.Update();

        // rotation speed v/r
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
}
