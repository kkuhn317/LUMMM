using UnityEngine;

public class Coin : MonoBehaviour
{
    public enum Amount { one, ten, thirty, fifty, green }
    public Amount type;

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
        if (other.gameObject.tag == "Player")
        {
            int coinValue = GetCoinValue();

            if (type != Amount.green)
            {
                GameManager.Instance.AddCoin(coinValue);
                Destroy(gameObject);
            }
            else
            {
                GameManager.Instance.CollectGreenCoin(gameObject);
                Collider2D collider = GetComponent<Collider2D>();
                collider.enabled = false;
                SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
                spriteRenderer.enabled = false;
            }
        }
    }
}
