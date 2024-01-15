using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class QuestionBlock : MonoBehaviour
{
    [Header("Invisible Block Behavior")]
    public bool isInvisible;

    [Header("Block Behavior")]
    public float bounceHeight = 0.5f;
    public float bounceSpeed = 4f;

    public bool brickBlock;

    public bool noCoinIfNoItem = false;

    public GameObject[] spawnableItems; // Array to hold multiple spawnable items

    public float coinMoveSpeed = 8f;
    public float coinMoveHeight = 3f;
    public float coinFallDistance = 2f;

    public float itemMoveHeight = 1f;
    public float itemMoveSpeed = 1f;
    public bool activated = false;
    private Vector2 originalPosition;

    public Sprite emptyBlockSprite;

    private bool canBounce = true;

    public AudioClip itemRiseSound;

    private AudioSource audioSource;

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
                if (playerScript.powerupState == MarioMovement.PowerupState.small)
                {
                    DefeatEnemy(other.collider);
                    QuestionBlockBounce();
                }
                else
                {
                    DefeatEnemy(other.collider);
                    BrickBlockBreak();         
                }
            }
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
            GetComponent<Animator>().enabled = false;

        GetComponent<SpriteRenderer>().sprite = emptyBlockSprite;

    }

    void PresentCoin()
    {

        activated = true;

        if (spawnableItems.Length > 0)
        {
            //print("custom item");
            audioSource.PlayOneShot(itemRiseSound);

            foreach (GameObject itemPrefab in spawnableItems)
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
        else if (!noCoinIfNoItem)
        {
            audioSource.Play();
            GameObject spinningCoin = (GameObject)Instantiate(Resources.Load("Prefabs/Spinning_Coin", typeof(GameObject)));

            spinningCoin.transform.SetParent(this.transform.parent);

            spinningCoin.transform.position = new Vector2(originalPosition.x, originalPosition.y + 1);

            StartCoroutine(MoveCoin(spinningCoin));
            GameManager.Instance.AddCoin(1); // The coin counter iterates after the coroutine
        }
    }

    IEnumerator Bounce()
    {
        //print(spawnItem != null);

        if (spawnableItems.Length > 0 || !brickBlock)
        {
            ChangeSprite();
        }

        if (spawnableItems.Length == 0 && !brickBlock)
        {
            PresentCoin();
        }
        else if (spawnableItems.Length > 0)
        {
            Invoke("PresentCoin", 0.25f);
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
    }

    IEnumerator MoveCoin(GameObject coin)
    {
        while (true)
        {
            coin.transform.position = new Vector2(coin.transform.position.x, coin.transform.position.y + coinMoveSpeed * Time.deltaTime);

            if (coin.transform.position.y >= originalPosition.y + coinMoveHeight + 1)
                break;

            yield return null;
        }

        while (true)
        {

            coin.transform.position = new Vector2(coin.transform.position.x, coin.transform.position.y - coinMoveSpeed * Time.deltaTime);

            if (coin.transform.position.y <= originalPosition.y + coinFallDistance + 1)
            {

                Destroy(coin.gameObject);
                break;
            }

            yield return null;
        }
    }

    public void StopRiseUp()
    {
        shouldContinueRiseUp = false; // Method to stop the RiseUp coroutine prematurely
    }

    IEnumerator RiseUp(GameObject item, string ogTag, int ogLayer, MonoBehaviour[] scripts)
    {
        while (item != null && shouldContinueRiseUp) // Add a null check and the flag check here
        {
            // Rest of the code inside the coroutine

            //item.GetComponent<Rigidbody2D>().velocity = Vector3.zero;

            item.transform.position = new Vector3(item.transform.position.x, item.transform.position.y + itemMoveSpeed * Time.deltaTime, 0);
            //print(item.transform.position.y + "vs" + originalPosition.y);
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
