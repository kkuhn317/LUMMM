using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KickCoinArea : UseableObject
{
    public GameObject coin;
    public Vector2 kickVelocity = new Vector2(5, 2);
    private AudioSource audioSource;
    bool canKick = false;
    bool coinInArea = false;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    protected override bool CanUseObject()
    {
        return canKick;
    }

    protected override void UseObject()
    {
        KickCoin();
    }

    void KickCoin()
    {
        ObjectPhysics physics = coin.GetComponent<ObjectPhysics>();
        Pushable pushable = coin.GetComponentInChildren<Pushable>();
        pushable.StopPushing();
        physics.Fall();
        physics.movingLeft = false;
        physics.velocity = kickVelocity;
        audioSource.Play();
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        if (collision.gameObject == coin)
        {
            coinInArea = true;
        }

        if (coinInArea && playerInArea)
        {
            canKick = true;
            keyActivate.SetActive(true);
        }
    }

    protected override void OnTriggerExit2D(Collider2D collision)
    {
        base.OnTriggerExit2D(collision);

        if (collision.gameObject == coin)
        {
            coinInArea = false;
        }

        if (!coinInArea || !playerInArea)
        {
            canKick = false;
            keyActivate.SetActive(false);
        }
    }



}
