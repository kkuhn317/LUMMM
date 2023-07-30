using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class QuestionBlock : MonoBehaviour
{
    [Header("Invisible Block Behavior")]
    public bool isInvisible;
    public float blockYposition = 1.1f; // apply for both small nd big Mario

    [Header("Block Behavior")]
    public float bounceHeight = 0.5f;
    public float bounceSpeed = 4f;

    public bool brickBlock;

    public bool noCoinIfNoItem = false;

    public GameObject spawnItem;

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

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        originalPosition = transform.localPosition;
    }
    void Update()
    {
        if (isInvisible)
        {
            GetComponent<SpriteRenderer>().enabled = false;

            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                // Check if the player object has a Collider2D component before accessing its bounds
                if (player.TryGetComponent<Collider2D>(out Collider2D playerCollider))
                {
                    Rigidbody2D playerRigidbody = player.GetComponent<Rigidbody2D>();
                    float playerY = player.transform.position.y;
                    float playerHeight = playerCollider.bounds.size.y;
                    float blockY = transform.position.y - blockYposition;

                    // If the player's y position is higher than the adjusted block's y position and moving upward, disable the collider
                    // Or if the player is falling and directly above the block, disable the collider
                    if ((playerY + playerHeight / 2f > blockY && playerRigidbody.velocity.y > 0) ||
                        (playerY + playerHeight / 2f > blockY && playerRigidbody.velocity.y < 0 && playerY > blockY))
                    {
                        GetComponent<Collider2D>().enabled = false; // The collider disables
                    }
                    else
                    {
                        GetComponent<Collider2D>().enabled = true; // The collider enables
                    }
                }
                else
                {
                    // The player doesn't have a Collider2D, so we disable the block's collider to be safe.
                    GetComponent<Collider2D>().enabled = false;
                }
            }
        }
        else
        {
            GetComponent<SpriteRenderer>().enabled = true;
            GetComponent<Collider2D>().enabled = true;

            // Change the layer back to the ground layer to enable player interaction
            gameObject.layer = originalLayer;
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
            if (!brickBlock)
            {
                QuestionBlockBounce();
            }
            else
            {
                MarioMovement playerScript = other.gameObject.GetComponent<MarioMovement>();
                if (playerScript.powerupState == MarioMovement.PowerupState.small)
                {
                    QuestionBlockBounce();
                }
                else
                {
                    BrickBlockBreak();
                }
            }
        }
    }

    // this is called when a koopa shell hits the block for example
    public void Activate()
    {
        if (canBounce)
        {
            // Change the layer back to the ground layer to enable player interaction
            gameObject.layer = originalLayer;

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
            isInvisible = false;

            if (spawnItem || !brickBlock)
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
        if (spawnItem)
        {
            isInvisible = false;

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

        if (spawnItem)
        {
            //print("custom item");
            audioSource.PlayOneShot(itemRiseSound);
            GameObject spawnedItem = (GameObject)Instantiate(spawnItem) as GameObject;
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
            spawnedItem.transform.localPosition = new Vector3(originalPosition.x, originalPosition.y, 0);
            StartCoroutine(RiseUp(spawnedItem, ogTag, ogLayer, scripts));

        }
        else if (!noCoinIfNoItem)
        {
            audioSource.Play();
            GameObject spinningCoin = (GameObject)Instantiate(Resources.Load("Prefabs/Spinning_Coin", typeof(GameObject)));

            spinningCoin.transform.SetParent(this.transform.parent);

            spinningCoin.transform.localPosition = new Vector2(originalPosition.x, originalPosition.y + 1);

            StartCoroutine(MoveCoin(spinningCoin));
            GameManager.Instance.AddCoin(1); // The coin counter iterates after the coroutine
        }
    }

    IEnumerator Bounce()
    {
        //print(spawnItem != null);

        if (spawnItem || !brickBlock)
        {
            ChangeSprite();
        }

        if (!spawnItem && !brickBlock)
        {
            PresentCoin();
        }
        else if (spawnItem)
        {
            Invoke("PresentCoin", 0.25f);
        }

        while (true)
        {

            transform.localPosition = new Vector2(transform.localPosition.x, transform.localPosition.y + bounceSpeed * Time.deltaTime);

            if (transform.localPosition.y >= originalPosition.y + bounceHeight)
                break;

            yield return null;
        }

        while (true)
        {

            transform.localPosition = new Vector2(transform.localPosition.x, transform.localPosition.y - bounceSpeed * Time.deltaTime);

            if (transform.localPosition.y <= originalPosition.y)
            {

                transform.localPosition = originalPosition;
                break;
            }

            yield return null;
        }
    }

    IEnumerator MoveCoin(GameObject coin)
    {

        while (true)
        {

            coin.transform.localPosition = new Vector2(coin.transform.localPosition.x, coin.transform.localPosition.y + coinMoveSpeed * Time.deltaTime);

            if (coin.transform.localPosition.y >= originalPosition.y + coinMoveHeight + 1)
                break;

            yield return null;
        }

        while (true)
        {

            coin.transform.localPosition = new Vector2(coin.transform.localPosition.x, coin.transform.localPosition.y - coinMoveSpeed * Time.deltaTime);

            if (coin.transform.localPosition.y <= originalPosition.y + coinFallDistance + 1)
            {

                Destroy(coin.gameObject);
                break;
            }

            yield return null;
        }
    }

    IEnumerator RiseUp(GameObject item, string ogTag, int ogLayer, MonoBehaviour[] scripts)
    {
        while (true)
        {
            

            //item.GetComponent<Rigidbody2D>().velocity = Vector3.zero;

            item.transform.localPosition = new Vector3(item.transform.localPosition.x, item.transform.localPosition.y + itemMoveSpeed * Time.deltaTime, 0);
            //print(item.transform.localPosition.y + "vs" + originalPosition.y);
            if (item.transform.localPosition.y >= originalPosition.y + itemMoveHeight)
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
    }

    void OnDrawGizmos()
    {
        // Only execute the Gizmos drawing when the game is running (not in edit mode)
        if (Application.isPlaying)
        {
            // Draw a red sphere at the block's position
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.1f);

            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                // Check if the player object has a Collider2D component before accessing its bounds
                if (player.TryGetComponent<Collider2D>(out Collider2D playerCollider))
                {
                    // Draw a green sphere at the player's position
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(player.transform.position, 0.1f);

                    float playerY = player.transform.position.y;
                    float playerHeight = playerCollider.bounds.size.y;
                    float blockY = transform.position.y - blockYposition; // Adjust the block's Y position

                    // Draw a blue sphere at the adjusted block's position
                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(new Vector3(transform.position.x, blockY, transform.position.z), 0.1f);
                }
                else
                {
                    // The player doesn't have a Collider2D, so we don't need to draw the Gizmos for this case.
                }
            }
        }
    }
}
