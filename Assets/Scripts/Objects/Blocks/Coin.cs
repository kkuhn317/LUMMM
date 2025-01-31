using System;
using System.Collections;
using UnityEngine;

public class Coin : MonoBehaviour
{
    public enum Amount { one, ten, thirty, fifty, green }
    public Amount type;

    [Header("Pop-Up Movement")]

    // The animation state "PopUp" will always be played. Set this to <=0 if the animation makes the coin move up and down
    public float bounceTime = 0.5f;
    public float bounceHeight = 2f;
    Vector2 originalPosition;   // Set when the coin is going to bounce

    public string popUpAnimationName = "PopUp";

    [Header("Audio")]
    public AudioClip coinSound;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider2D;
    private AudioSource audioSource;

    private void Awake() 
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        boxCollider2D = GetComponent<BoxCollider2D>();
        audioSource = GetComponent<AudioSource>();
    }
    
    // Method to get the coin value
    public int GetCoinValue()
    {
        switch (type)
        {
            case Amount.one:
                return 1;
            case Amount.ten:
                return 10;
            case Amount.thirty:
                return 30;
            case Amount.fifty:
                return 50;
            default:
                return 0; // Default case, in case new amounts are added and not handled
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            OnCoinCollected();
        }
    }

    public void PlayCoinSound()
    {
        // Lazily initialize AudioSource if it hasn't been assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogWarning("AudioSource component is missing on the coin prefab!");
                return;
            }
        }

        if (coinSound != null)
        {
            audioSource.PlayOneShot(coinSound);
        }
    }

    protected virtual void OnCoinCollected()
    {
        PlayCoinSound();
        AddCoinAmount();

        if (type != Amount.green)
        {
            DisableCoinVisuals();
            Destroy(gameObject, coinSound.length);
        }
        else
        {
            DisableCoinVisuals();
        }
    }

    void AddCoinAmount()
    {
        int coinValue = GetCoinValue();

        if (type != Amount.green)
        {
            GameManager.Instance.AddCoin(coinValue);
        }
        else
        {
            GameManager.Instance.CollectGreenCoin(gameObject);
        }
    }

    private void DisableCoinVisuals()
    {
        boxCollider2D.enabled = false;
        spriteRenderer.enabled = false;
    }

    public void PopUp()
    {
        if (type == Amount.green)
        {
            print("WARNING: Green coin pop-up not implemented yet!!");
            OnCoinCollected();
            return;
        }

        originalPosition = transform.position;

        // Not a big fan of this method but whatever
        if (!string.IsNullOrEmpty(popUpAnimationName))
        {
            GetComponent<Animator>().Play(popUpAnimationName); // Play the specified animation
            Debug.Log(popUpAnimationName);
        }
        else
        {
            GetComponent<Animator>().Play("PopUp"); // Default to "PopUp" if no name is provided
            Debug.Log("PopUp");
        }

        PlayCoinSound();

        if (bounceTime > 0)
        {
            StartCoroutine(MoveCoin());
        } else {
            float animTime = GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length;
            Invoke(nameof(AddCoinAmountAndDestroy), animTime);
        }
    }

    void AddCoinAmountAndDestroy()
    {
        AddCoinAmount();
        Destroy(gameObject, coinSound.length);
    }


    // Default coin movement
    IEnumerator MoveCoin()
    {
        if (boxCollider2D != null)
        boxCollider2D.enabled = false;

        // Move in a parabola
        float t = 0;
        while (t < bounceTime)
        {
            t += Time.deltaTime;
            float y = Mathf.Sin(Mathf.PI * t / bounceTime);
            transform.position = new Vector2(transform.position.x, originalPosition.y + y * bounceHeight);
            yield return null;
        }

        if (spriteRenderer != null)
        spriteRenderer.enabled = false;

        AddCoinAmountAndDestroy();
    }

}
