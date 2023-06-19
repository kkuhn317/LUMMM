using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockCoin : MonoBehaviour
{
    public int maxCoins = 10;
    public SpriteRenderer spriteRenderer;
    public Sprite emptyBlock;

    private int remainingCoins;
    private bool animating = false;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        remainingCoins = maxCoins;

        GameManager.Instance.AddCoin(1);

        StartCoroutine(Animate());
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!animating && remainingCoins > 0 && collision.gameObject.CompareTag("Player"))
        {
            if (collision.transform.DotTest(transform, Vector2.up))
            {
                remainingCoins--;
                GameManager.Instance.AddCoin(1);

                if (remainingCoins == 0)
                {
                    spriteRenderer.sprite = emptyBlock;
                }
            }
        }
    }

    private IEnumerator Animate()
    {
        Vector3 restingPosition = transform.localPosition;
        Vector3 animatedPosition = restingPosition + Vector3.up * 2f;

        yield return Move(restingPosition, animatedPosition);
        yield return Move(animatedPosition, restingPosition);

        Destroy(gameObject);
    }

    private IEnumerator Move(Vector3 from, Vector3 to)
    {
        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            transform.localPosition = Vector3.Lerp(from, to, t);
            elapsed += Time.deltaTime;

            yield return null;
        }

        transform.localPosition = to;
    }
}