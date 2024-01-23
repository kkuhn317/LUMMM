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

    protected virtual void OnCoinCollected()
    {
        AddCoinAmount();

        if (type != Amount.green)
        {
            Destroy(gameObject);
        }
        else
        {
            Collider2D collider = GetComponent<Collider2D>();
            collider.enabled = false;
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.enabled = false;
        }
    }

    void AddCoinAmount(bool playSound = true)
    {
        int coinValue = GetCoinValue();

        if (type != Amount.green)
        {
            GameManager.Instance.AddCoin(coinValue, playSound: playSound);
        }
        else
        {
            GameManager.Instance.CollectGreenCoin(gameObject);
        }
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

        GetComponent<Animator>().Play("PopUp"); // Play the pop-up animation
        GameManager.Instance.PlayCoinSound();

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
        AddCoinAmount(false);
        Destroy(gameObject);
    }


    // Default coin movement
    IEnumerator MoveCoin()
    {
        // Move in a parabola
        float t = 0;
        while (t < bounceTime)
        {
            t += Time.deltaTime;
            float y = Mathf.Sin(Mathf.PI * t / bounceTime);
            transform.position = new Vector2(transform.position.x, originalPosition.y + y * bounceHeight);
            yield return null;
        }

        AddCoinAmountAndDestroy();
    }

}
