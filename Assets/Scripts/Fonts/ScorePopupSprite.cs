using UnityEngine;

public class ScorePopupSprite : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public float lifetime = 0.8f;
    public float fadeTime = 0.3f;

    public SpriteRenderer spriteRenderer;

    float timer;

    public void Init(Sprite scoreSprite)
    {
        spriteRenderer.sprite = scoreSprite;
    }

    void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        timer += Time.deltaTime;

        if (timer > lifetime)
        {
            float t = (timer - lifetime) / fadeTime;

            if (spriteRenderer != null)
            {
                var c = spriteRenderer.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = c;
            }

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}