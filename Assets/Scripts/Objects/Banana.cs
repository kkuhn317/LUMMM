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
        if (other.CompareTag("Player") && !isSlipping && !other.TryGetComponent<MarioSlip>(out _))
        {
            GetComponent<AudioSource>().Play();
            animator.SetTrigger("fall");
            // Add the slip component to the player
            other.gameObject.AddComponent<MarioSlip>();
        }
    }

    public void SadBanana()
    {
        animator.SetTrigger("sad");
    }
}
