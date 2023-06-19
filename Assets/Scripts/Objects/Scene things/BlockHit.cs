using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockHit : MonoBehaviour
{
    //Thank you Zigurous! Check his YT https://www.youtube.com/@Zigurous
    public GameObject item;
    public int maxHits = -1;
    public Sprite emptyBlock;
    public AudioClip bumpSound;

    private bool animating;
    private int coinsLeft;
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>(); //Get gameObject's Audio Source component
        spriteRenderer = GetComponent<SpriteRenderer>(); //Get gameObject's Sprite Renderer component
        animator = GetComponent<Animator>(); //Get gameObject's Animator component
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!animating && maxHits != 0 && collision.gameObject.CompareTag("Player"))
        {
            if (collision.transform.DotTest(transform, Vector2.up))
            {
                Hit();
            }
        }
    }

    private void Hit()
    {
        spriteRenderer.enabled = true; //Show if hidden

        if (emptyBlock == null || bumpSound == null || item == null)
        {
            StartCoroutine(Animate());
            return;
        }

        if (maxHits != 0)
        {
            maxHits--;
            coinsLeft++;
        }

        if (maxHits == 0)
        {
            //Deactive Animator and change block image
            if (animator != null)
            {
                //It deactives the animator component, so the emptyblock sprite can be displayed on the screen
                animator.enabled = false;
            }
            spriteRenderer.sprite = emptyBlock; //Changes the gameObject sprite to the assigned sprite variable
        }
        else if (animator != null)
        {
            animator.enabled = true;
        }

        StartCoroutine(SpawnCoins());
        StartCoroutine(Animate());
        audioSource.PlayOneShot(bumpSound);
    }

    //Available for coins
    private IEnumerator SpawnCoins()
    {
        while (coinsLeft > 0)
        {
            GameObject coin = Instantiate(item, transform.position, Quaternion.identity);
            coinsLeft--;

            yield return new WaitForSeconds(0.5f); // delay between coin spawns
        }
    }

    private IEnumerator Animate()
    {
        if (maxHits != 0)
        {
            animating = true;

            Vector3 restingPosition = transform.localPosition;
            Vector3 animatedPosition = restingPosition + Vector3.up * 0.5f;

            yield return Move(restingPosition, animatedPosition);
            yield return Move(animatedPosition, restingPosition);

            animating = false;
        }
    }

    private IEnumerator Move(Vector3 from, Vector3 to)
    {
        float elapsed = 0f;
        float duration = 0.25f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            transform.localPosition = Vector3.Lerp(from, to, t);
            elapsed += Time.deltaTime;

            yield return null;
        }

        transform.localPosition = to;
    }

    public void SetMaxHits(int value)
    {
        maxHits = value;
    }
}
