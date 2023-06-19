using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdeaBlock : MonoBehaviour
{
    public int totalCoins;
    private GameObject child;
    public Sprite disabled;

    // Start is called before the first frame update
    void Start()
    {
        child = transform.GetChild(0).gameObject;
    }

    // Update is called once per frame
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.collider.bounds.max.y < transform.position.y
            && col.collider.bounds.min.x < transform.position.x + 0.5f
            && col.collider.bounds.max.x > transform.position.x - 0.5f
            && col.collider.tag == "Player")
        {
            if (totalCoins > 0)
            {
                GameManager.Instance.AddCoin(1);
                totalCoins -= 1;

                if (totalCoins == 0)
                {
                    child.GetComponent<SpriteRenderer>().sprite = disabled;
                }
            }
        }
    }
}
