using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class CoinDoor : Door
{
    public int coinsRequired = 1;

    protected bool playerInRange = false;
    public bool placePlayerOnDoorCenter = false;

    public bool mustBeStanding = true; // If true, player must be standing to open the door

    public GameObject coinEffectObject; // This object should just have a sprite renderer on it
    public Vector2 spawnOffset = new Vector2(0, 0.5f);
    public float moveHeight = 1.0f;
    public float moveDuration = 1.0f;
    public float timeBetweenCoins = 0.25f;
    public int coinsAtOnce = 1; // How many coins to spend at once
    public float coinSeparation = 0.5f; // How far apart to spawn coins
    public AudioClip useCoinSound;
    private IEnumerator coinSpendCoroutine;

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

    protected override void FreezePlayer()
    {
        player.GetComponent<Rigidbody2D>().simulated = false;
        player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        Animator playerAnimator = player.GetComponent<Animator>();
        if (!mustBeStanding)
        {
            playerAnimator.SetTrigger("hold");
        }
        else
        {
            playerAnimator.SetBool("onGround", true);
        }
        
        playerAnimator.SetBool("isRunning", false);
        playerAnimator.SetBool("isSkidding", false);

        // disable all scripts
        foreach (MonoBehaviour script in player.GetComponents<MonoBehaviour>())
        {
            script.enabled = false;
        }
    }

    protected override void Unlock()
    {
        // Place the player at the center of the door if the flag is enabled
        CenterPlayerAtDoor();
        // we want to have the door spit out coins until the player uses enough coins to open the door
        FreezePlayer();
        coinSpendCoroutine = SpawnCoinsUntilOpen();
        StartCoroutine(coinSpendCoroutine);
    }

    protected override void CenterPlayerAtDoor()
    {
        if (placePlayerOnDoorCenter && player != null)
        {
            Vector3 doorCenter = new Vector3(transform.position.x, player.transform.position.y, player.transform.position.z);
            player.transform.position = doorCenter;
        }
    }


    protected virtual IEnumerator SpawnCoinsUntilOpen()
    {
        int coinsRemaining = coinsRequired;
        while (coinsRemaining > 0)
        {
            // Spawn multiple coins at once
            for (int i = 0; i < coinsAtOnce; i++)
            {
                if (coinsRemaining <= 0)
                    break;

                // Spawn coin effect
                GameObject coinEffect = Instantiate(coinEffectObject, transform.position + (Vector3)spawnOffset + new Vector3((i * coinSeparation) - (coinSeparation/2f * (coinsAtOnce - 1)), 0, 0), Quaternion.identity);
                // Start coroutine to move up and fade out
                
                StartCoroutine(MoveUpAndFadeOut(coinEffect));
                coinsRemaining--;
                SubtractOneCoin();
            }

            audioSource.PlayOneShot(useCoinSound);
            yield return new WaitForSeconds(timeBetweenCoins);
        }
        // now that the coins are done, we can open the door by doing base.Unlock()
        base.Unlock();
        coinSpendCoroutine = null;
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

    // Used in big crusher section when the door is crushed while you are spending coins
    public void StopSpendingCoins() {
        if (coinSpendCoroutine != null) {
            StopCoroutine(coinSpendCoroutine);
            UnfreezePlayer();
        }
    }
}
