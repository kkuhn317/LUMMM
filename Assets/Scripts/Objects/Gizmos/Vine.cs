using UnityEngine;

public class Vine : MonoBehaviour
{
    public LayerMask obstacleLayer; // LayerMask for obstacles
    public float increaseRate = 2f; // Rate at which to increase the height per second
    public float rayObstacleDetection = 0.1f;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        GrowVine();
        DetectObstacles();
    }

    #region VineLogic
    private void GrowVine()
    {
        Vector2 tiling = spriteRenderer.size;
        tiling.y += increaseRate * Time.deltaTime;
        spriteRenderer.size = tiling;
        boxCollider.size = tiling;
        boxCollider.offset = new Vector2(0f, tiling.y / 2f);
    }

    private void DetectObstacles()
    {
        Vector2 raycastOrigin = transform.position + new Vector3(0f, spriteRenderer.size.y, 0f);
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, transform.up, increaseRate * Time.deltaTime, obstacleLayer);

        if (hit.collider != null)
        {
            Debug.Log("Obstacle hit: " + hit.collider.gameObject.name);
            increaseRate = 0f; // Stop growth if obstacle detected
        }
    }
    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 raycastOrigin = transform.position + new Vector3(0f, GetComponent<SpriteRenderer>().size.y, 0f);
        Gizmos.DrawLine(raycastOrigin, raycastOrigin + transform.up * rayObstacleDetection);
    }
}
