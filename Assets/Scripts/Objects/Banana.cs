using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Banana : MonoBehaviour
{
    public Vector2 velocity = new Vector2(1, 4);
    public float slipRotationSpeed = 100f;
    public float slipForce = 20f;
    private bool isSlipping = false;

    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isSlipping)
        {
            isSlipping = true;

            Rigidbody2D playerRigidbody = other.GetComponent<Rigidbody2D>();
            Animator playerAnimator = other.GetComponent<Animator>();
            PlayerInput playerInput = other.GetComponent<PlayerInput>();
            MarioMovement playerMovement = other.GetComponent<MarioMovement>();

            if (playerRigidbody != null)
            {        
                if (playerInput != null)
                    playerInput.enabled = false;

                // Determine the direction the player is facing
                float playerDirection = Mathf.Sign(other.transform.localScale.x);

                // Apply slip force in the direction the player is facing
                Vector2 slipDirection = new Vector2(velocity.x * playerDirection, velocity.y).normalized;
                playerRigidbody.AddForce(slipDirection * slipForce, ForceMode2D.Impulse);

                // Rotate the player while slipping and reset rotation when grounded
                StartCoroutine(RotatePlayerWhileSlipping(other.transform, playerInput, playerMovement, playerAnimator));
            }
        }
    }

    private IEnumerator RotatePlayerWhileSlipping(Transform playerTransform, PlayerInput playerInput, MarioMovement playerMovement, Animator playerAnimator)
    {
        Vector2 currentVelocity = velocity; // To have the same velocity

        while (isSlipping)
        {
            if (playerAnimator != null)
                playerAnimator.SetTrigger("slip");

            // Apply slip gravity effect
            float slipGravity = 10f;
            Vector3 pos = playerTransform.localPosition;
            pos.x += currentVelocity.x * Time.deltaTime;
            pos.y += currentVelocity.y * Time.deltaTime;
            currentVelocity.y -= slipGravity * Time.deltaTime;
            playerTransform.localPosition = pos;

            // Rotate the player while slipping
            playerTransform.Rotate(Vector3.forward, slipRotationSpeed * Time.deltaTime);

            // Check if the player is grounded
            if (playerMovement.onGround)
            {
                // Wait for the player to reach the ground
                yield return new WaitForFixedUpdate();

                if (playerInput != null)
                    playerInput.enabled = true;

                // Reset rotation to zero
                playerTransform.rotation = Quaternion.identity;

                // Exit the slipping coroutine
                isSlipping = false;
            }

            yield return null;
        }
    }

    public void SadBanana()
    {
        animator.SetTrigger("sad");
    }
}
