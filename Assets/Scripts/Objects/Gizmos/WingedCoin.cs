using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WingedCoin : Coin
{
    public Wing[] wings;
    public float moveDistance = 3f;
    public float moveSpeed = 5f;

    protected override void OnCoinCollected()
    {
        int coinValue = GetCoinValue();

        if (type != Amount.green)
        {
            GameManager.Instance.AddCoin(coinValue);
        }
        else
        {
            throw new System.NotImplementedException("Green coin collection not implemented for WingedCoin");
        }

        foreach (Wing wing in wings)
        {
            // make it not a child of the block
            wing.transform.parent = null;
            wing.Fall();
        }
        Destroy(gameObject);
    }

    void Update()
    {
        // move up and down
        transform.position += Vector3.up * Mathf.Cos(Time.time * moveSpeed) * Time.deltaTime * moveDistance;
    }
}
