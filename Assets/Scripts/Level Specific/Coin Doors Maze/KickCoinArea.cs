using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class KickCoinArea : UseableObject
{
    public GameObject coin;
    public Vector2 kickVelocity = new Vector2(5, 2);
    public GameObject kickEffectPrefab; 
    private GameObject currentPlayer;
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

        // Instantiate the kick effect at the collision point
        if (kickEffectPrefab != null)
        {
            // Get the closest point of collision between the player and the coin
            Collider2D playerCollider = currentPlayer.GetComponent<Collider2D>();
            Collider2D coinCollider = coin.GetComponent<Collider2D>();

            if (playerCollider != null && coinCollider != null)
            {
                Vector2 collisionPoint = coinCollider.ClosestPoint(playerCollider.bounds.center);
                Instantiate(kickEffectPrefab, collisionPoint, Quaternion.identity);
            }
        }
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        base.OnTriggerEnter2D(collision);
        if (collision.gameObject == coin)
        {
            coinInArea = true;
        }
        
        // Check if a player enters the area
        MarioMovement playerMovement = collision.GetComponent<MarioMovement>();
        if (playerMovement != null)
        {
            currentPlayer = collision.gameObject;
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

        // Check if the player exits the area
        if (collision.gameObject == currentPlayer)
        {
            currentPlayer = null;
        }

        if (!coinInArea || !playerInArea)
        {
            canKick = false;
            keyActivate.SetActive(false);
        }
    }
}