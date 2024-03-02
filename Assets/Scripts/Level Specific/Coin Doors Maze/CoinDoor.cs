using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class CoinDoor : Door
{
    public int coinsRequired = 1;

    protected bool playerInRange = false;

    public bool mustBeStanding = true; // If true, player must be standing to open the door

    public GameObject coinEffectObject; // This object should just have a sprite renderer on it
    public Vector2 spawnOffset = new Vector2(0, 0.5f);
    public float moveHeight = 1.0f;
    public float moveDuration = 1.0f;
    public float timeBetweenCoins = 0.25f;

    public AudioClip useCoinSound;

    protected override void Start()
    {
        base.Start();
        if (destination)
        {
            animator.SetBool("3DOpen", false);
        }
    }

    protected override bool CheckForKey() {
        return GlobalVariables.coinCount >= coinsRequired;
    }

    protected override void Unlock()
    {
        // we want to have the door spit out coins until the player uses enough coins to open the door
        FreezePlayer();
        StartCoroutine(SpawnCoinsUntilOpen());
    }

    protected virtual IEnumerator SpawnCoinsUntilOpen()
    {
        int coinsRemaining = coinsRequired;
        while (coinsRemaining > 0)
        {
            GameObject coinEffect = Instantiate(coinEffectObject, transform.position + (Vector3)spawnOffset, Quaternion.identity);
            // for each coin, we want it to move up while fading out
            // start a coroutine to do that
            StartCoroutine(MoveUpAndFadeOut(coinEffect));
            coinsRemaining--;
            SubtractOneCoin();
            audioSource.PlayOneShot(useCoinSound);
            yield return new WaitForSeconds(timeBetweenCoins);
        }

        // now that the coins are done, we can open the door by doing base.Unlock()
        base.Unlock();
    }

    protected virtual void SubtractOneCoin()
    {
        GameManager.Instance.RemoveCoins(1);
    }

    protected IEnumerator MoveUpAndFadeOut(GameObject gameObject)
    {
        float time = 0;
        SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        Vector3 startPos = gameObject.transform.position;
        Vector3 endPos = startPos + new Vector3(0, moveHeight, 0);
        while (time < moveDuration)
        {
            time += Time.deltaTime;
            gameObject.transform.position = Vector3.Lerp(startPos, endPos, time / moveDuration);
            spriteRenderer.color = new Color(1, 1, 1, 1 - time / moveDuration);
            yield return null;
        }
        Destroy(gameObject);

    }

    protected override void SpendKey()
    {
        //GameManager.Instance.RemoveCoins(coinsRequired);
        // we don't need to spend coins here, because we're doing it in SpawnCoinsUntilOpen
    }

    protected override bool PlayerAtDoor(MarioMovement playerScript)
    {
        return playerInRange && (!mustBeStanding || playerScript.onGround);
    }

    protected override void Close()
    {
        if (destination)
        {
            base.Close();
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.CompareTag("Player")) {
            playerInRange = true;
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other) {
        if (other.gameObject.CompareTag("Player")) {
            playerInRange = false;
        }
    }
}
