using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;
using PowerupState = PowerStates.PowerupState;

public class QuestionBlock : MonoBehaviour, IGroundPoundable
{
    [Header("Invisible Block Behavior")]
    public bool isInvisible;

    [Header("Block Behavior")]
    public float bounceHeight = 0.5f;
    public float bounceSpeed = 4f;
    public bool brickBlock;

    public GameObject[] spawnableItems; // Array to hold multiple spawnable items

    public float itemMoveHeight = 1f;
    public float itemMoveSpeed = 1f;

    public UnityEvent onBlockActivated;

    private Vector2 originalPosition;

    public Sprite emptyBlockSprite;

    private bool canBounce = true;

    public AudioClip itemRiseSound;

    private AudioSource audioSource;

    public string popUpCoinAnimationName = "";

    private int originalLayer = 3; // Layer 3 = ground layer
    private bool shouldContinueRiseUp = true; // Add a flag to control coroutine continuation
    private BoxCollider2D boxCollider;


    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        boxCollider = GetComponent<BoxCollider2D>();
        originalPosition = transform.position;

        if (isInvisible) {
            GetComponent<SpriteRenderer>().enabled = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            Vector2 impulse = Vector2.zero;

            int contactCount = other.contactCount;
            for (int i = 0; i < contactCount; i++)
            {
                var contact = other.GetContact(i);
                impulse += contact.normal * contact.normalImpulse;
                impulse.x += contact.tangentImpulse * contact.normal.y;
                impulse.y -= contact.tangentImpulse * contact.normal.x;
            }

            //print(impulse);

            // position comparison is to stop a weird bug where the player can hit the top corner of the block and activate it
            if (impulse.y <= 0 || other.transform.position.y > transform.position.y)
            {
                return;
            }

            if (gameObject == null)
            {
                // The object has been destroyed
                return;
            }

            // Brick Block and Question Block
            if (!brickBlock) // When it's a question block
            {
                if (canBounce) // If the block bounces when the player collides
                {
                    DefeatEnemy(other.collider); // Defeat the enemy
                }
                QuestionBlockBounce();
            }
            else // When it's a brick block
            {
                MarioMovement playerScript = other.gameObject.GetComponent<MarioMovement>();
                DefeatEnemy(other.collider);
                if (PowerStates.IsSmall(playerScript.powerupState))
                {
                    QuestionBlockBounce();
                }
                else
                {
                    BrickBlockBreak();         
                }
            }

            onBlockActivated.Invoke();
        }
    }

    public void OnGroundPound(MarioMovement player)
    {
        if (brickBlock) 
        {
            // Brick block behavior
            if (PowerStates.IsSmall(player.powerupState))
            {
                QuestionBlockBounce(); // Bounce for small Mario
            }
            else
            {
                BrickBlockBreak(); // Break for big Mario
            }
        }
        else
        {
            // Question block always bounces
            QuestionBlockBounce();
        }
    }

    private void DefeatEnemy(Collider2D blockCollider)
    {
        // When there's an enemy on the block the player hits
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(boxCollider.bounds.center, boxCollider.bounds.size, 0f, LayerMask.GetMask("Enemy"));

        if (hitEnemies.Length > 0)
        {
            foreach (Collider2D enemyCollider in hitEnemies)
            {
                EnemyAI enemy = enemyCollider.GetComponent<EnemyAI>();
                if (enemy != null)
                {
                    // Use the KnockAway method to change the enemy's state
                    enemy.KnockAway(blockCollider.transform.position.x > enemy.transform.position.x);
                    GameManager.Instance.AddScorePoints(100);
                }
            }
        }
    }

    // this is called when a koopa shell hits the block for example
    public void Activate()
    {
        if (canBounce)
        {
            if (brickBlock)
            {
                BrickBlockBreak();
            }
            else
            {
                QuestionBlockBounce();
            }
        }
    }

    public void QuestionBlockBounce()
    {
        if (canBounce)
        {
            if (isInvisible) {
                isInvisible = false;
                GetComponent<SpriteRenderer>().enabled = true;
                GetComponent<PlatformEffector2D>().useOneWay = false;

                // change layer
                gameObject.layer = originalLayer;
            }

            if (spawnableItems.Length > 0 || !brickBlock)
            {
                canBounce = false;
            }

            // do whatever timeline stuff here
            if (GetComponent<PlayableDirector>() != null)
            {
                GetComponent<PlayableDirector>().Play();
            }

            StartCoroutine(Bounce());
        }
    }

    public void BrickBlockBreak()
    {
        if (spawnableItems.Length > 0)
        {
            QuestionBlockBounce();
        }
        else
        {
            GetComponent<BreakableBlocks>().Break();
        }

    }

    void ChangeSprite()
    {

        if (!brickBlock)
            if (GetComponent<Animator>() != null)
            {
                GetComponent<Animator>().enabled = false;
            }

        GetComponent<SpriteRenderer>().sprite = emptyBlockSprite;

    }

    void PresentItems(List<GameObject> items)
    {
        audioSource.PlayOneShot(itemRiseSound);

        foreach (GameObject itemPrefab in items)
        {
            GameObject spawnedItem = Instantiate(itemPrefab, transform.parent, true);
            spawnedItem.transform.position = new Vector3(originalPosition.x, originalPosition.y, 0);
            MonoBehaviour[] scripts = spawnedItem.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                script.enabled = false;
            }

            string ogTag = spawnedItem.tag;
            int ogLayer = spawnedItem.GetComponent<SpriteRenderer>().sortingLayerID;
            spawnedItem.tag = "RisingItem";
            spawnedItem.transform.SetParent(this.transform.parent);
            spawnedItem.GetComponent<SpriteRenderer>().sortingLayerID = 0;
            spawnedItem.GetComponent<SpriteRenderer>().sortingOrder = -1;
            spawnedItem.transform.position = new Vector3(originalPosition.x, originalPosition.y, 0);
            StartCoroutine(RiseUp(spawnedItem, ogTag, ogLayer, scripts));
        }

    }

    void PresentCoins(List<GameObject> coins) {

        // audioSource.Play();
        // Right now the Game Manager is playing the coin sound (called from Coin.cs)
        // However, the pitch/sound will not be changed for a small question block for example
        // If we need this functionality, we can rework some of the code to allow for it

        float startHeight = originalPosition.y + boxCollider.size.y;

        foreach (GameObject coinPrefab in coins)
        {
            GameObject spinningCoin = Instantiate(coinPrefab, transform.parent);
            spinningCoin.transform.position = new Vector2(originalPosition.x, startHeight);

            Coin coinScript = spinningCoin.GetComponent<Coin>();
            if (coinScript != null)
            {
                coinScript.popUpAnimationName = popUpCoinAnimationName;
                coinScript.PopUp(); // No delay required
            }
        }
    }

    IEnumerator Bounce()
    {
        //print(spawnItem != null);

        // get all the items that are coins and make them pop up immediately
        // The other items will rise up after the block bounces
        List<GameObject> coins = new List<GameObject>();
        List<GameObject> otherItems = new List<GameObject>();
        if (spawnableItems.Length > 0)
        {
            foreach (GameObject itemPrefab in spawnableItems)
            {
                if (itemPrefab.GetComponent<Coin>() != null)
                {
                    coins.Add(itemPrefab);
                } else
                {
                    otherItems.Add(itemPrefab);
                }
            }
        }

        if (coins.Count > 0)
        {
            PresentCoins(coins);
        }

        while (true)
        {
            transform.position = new Vector2(transform.position.x, transform.position.y + bounceSpeed * Time.deltaTime);

            if (transform.position.y >= originalPosition.y + bounceHeight)
                break;

            yield return null;
        }

        while (true)
        {
            transform.position = new Vector2(transform.position.x, transform.position.y - bounceSpeed * Time.deltaTime);

            if (transform.position.y <= originalPosition.y)
            {

                transform.position = originalPosition;
                break;
            }

            yield return null;
        }

        if (otherItems.Count > 0)
        {
            PresentItems(otherItems);
        }

        if (!brickBlock || spawnableItems.Length > 0)
        {
            ChangeSprite();
        }
        
    }

    public void StopRiseUp()
    {
        shouldContinueRiseUp = false; // Method to stop the RiseUp coroutine prematurely
    }

    IEnumerator RiseUp(GameObject item, string ogTag, int ogLayer, MonoBehaviour[] scripts)
    {
        BoxCollider2D itemCollider = item.GetComponent<BoxCollider2D>();
        if (itemCollider != null)
        {
            itemCollider.enabled = false;  // Initially disable the collider
        }

        float riseStartTime = Time.time;  // Record the time when rise starts
        bool colliderEnabled = false;     // Track if the collider has been enabled

        while (item != null && shouldContinueRiseUp)
        {
            // Move the item up
            item.transform.position = new Vector3(item.transform.position.x, item.transform.position.y + itemMoveSpeed * Time.deltaTime, 0);

            // Enable the collider after a small delay from the start of the rise
            if (!colliderEnabled && Time.time >= riseStartTime + 0.25f)
            {
                if (itemCollider != null)
                {
                    itemCollider.enabled = true;
                }
                colliderEnabled = true;  // Mark collider as enabled to avoid re-enabling
            }

            // Check if the item has reached the target height
            if (item.transform.position.y >= originalPosition.y + itemMoveHeight)
            {
                foreach (MonoBehaviour script in scripts)
                {
                    script.enabled = true;
                }
                item.tag = ogTag;
                item.GetComponent<SpriteRenderer>().sortingLayerID = ogLayer;
                item.GetComponent<SpriteRenderer>().sortingOrder = 0;
                break;
            }

            yield return null;
        }

        // If the item GameObject is null or shouldContinueRiseUp is false, the coroutine will exit here.
    }

    // Call this method when the player grabs the item
    public void OnPlayerGrabItem()
    {
        StopRiseUp(); // Stop the RiseUp coroutine when the player grabs the item
    }
}