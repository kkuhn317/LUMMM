using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(PooledObject))]
public class ScorePopupSprite : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public Vector3 moveDirection = Vector3.up;

    [Header("Timing")]
    public float lifetime = 0.8f;
    public float fadeDuration = 0.3f;

    private SpriteRenderer spriteRenderer;
    private PooledObject pooledObject;
    private float timer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        pooledObject = GetComponent<PooledObject>();
    }

    private void OnEnable()
    {
        timer = 0f;
        if (spriteRenderer != null)
        {
            var c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }
    }

    public void Init(Sprite sprite)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
            var c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        timer = 0f;
    }

    private void Update()
    {
        // Move upwards
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        timer += Time.deltaTime;

        if (timer > lifetime)
        {
            float t = (timer - lifetime) / fadeDuration;
            t = Mathf.Clamp01(t);

            if (spriteRenderer != null)
            {
                var c = spriteRenderer.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                spriteRenderer.color = c;
            }

            if (t >= 1f)
                pooledObject.Release();
        }
    }
}