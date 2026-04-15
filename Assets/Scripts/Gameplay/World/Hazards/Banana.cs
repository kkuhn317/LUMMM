using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Banana : MonoBehaviour
{
    private bool isSlipping = false;

    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        MarioCore _bananaCore = other.GetComponentInParent<MarioCore>();
        GameObject _bananaRoot = _bananaCore != null ? _bananaCore.gameObject : other.gameObject;
        if (other.CompareTag("Player") && !isSlipping && !_bananaRoot.TryGetComponent<MarioSlip>(out _))
        {
            GetComponent<AudioSource>().Play();
            animator.SetTrigger("fall");
            // Add the slip component to the player root
            _bananaRoot.AddComponent<MarioSlip>();
        }
    }

    public void SadBanana()
    {
        animator.SetTrigger("sad");
    }
}