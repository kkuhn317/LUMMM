using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    public enum Amount {one, ten, thirty, fifty}
    public Amount type;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player") {
            if (type == Amount.one) { 
                GameManager.Instance.AddCoin(1); //Add one coin to current coin counter value and a hundred points to the score counter
            } else if (type == Amount.ten) {
                GameManager.Instance.AddCoin(10); //Add ten coins to current coin counter value and thousand points to the score counter
            } else if (type == Amount.thirty) {
                GameManager.Instance.AddCoin(30); //Add thirty coins to current coin counter value and three thousand points to the score counterv
            } else if (type == Amount.fifty) {
                GameManager.Instance.AddCoin(50); //Add fifty coins to current coin counter value and five thousand points to the score counter
            }
            Destroy(this.gameObject); //Delete coin when touch gameObject tag "Player"
        }
    }
}
