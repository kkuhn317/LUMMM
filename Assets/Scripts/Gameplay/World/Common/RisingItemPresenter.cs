using System.Collections;
using UnityEngine;

/// <summary>
/// Reusable presenter to spawn an item and make it "rise out of a block" like SMB.
/// Handles: instantiation, temporary disabling scripts, tag/sorting overrides, collider delay,
/// and restoring original values at the end.
/// </summary>
public class RisingItemPresenter : MonoBehaviour
{
    [Header("Defaults (optional)")]
    [Tooltip("Default height used if caller passes <= 0.")]
    public float defaultMoveHeight = 1f;

    [Tooltip("Default speed used if caller passes <= 0.")]
    public float defaultMoveSpeed = 1f;

    [Tooltip("Delay before enabling the item's collider while rising.")]
    public float colliderEnableDelay = 0.25f;

    [Tooltip("Temporary tag applied while rising.")]
    public string risingTag = "RisingItem";

    [Tooltip("If true, temporarily force sortingLayer/sortingOrder while rising.")]
    public bool overrideSortingWhileRising = true;

    [Tooltip("Temporary sorting order while rising (only if overrideSortingWhileRising = true).")]
    public int risingSortingOrder = -1;

    // If your project relies on sortingLayerID = 0, keep it.
    // Otherwise, you can disable overrideSortingWhileRising or adjust this.
    [Tooltip("Temporary sorting layer id while rising (only if overrideSortingWhileRising = true).")]
    public int risingSortingLayerId = 0;

    private bool stopAllRising = false;

    /// <summary>
    /// Stops all rising animations managed by this presenter (current coroutines will exit).
    /// </summary>
    public void StopAllRising()
    {
        stopAllRising = true;
    }

    /// <summary>
    /// Allows rising again after StopAllRising() (useful on Reset).
    /// </summary>
    public void ResetStop()
    {
        stopAllRising = false;
    }

    /// <summary>
    /// Spawns prefab and performs rise-up.
    /// Returns the spawned instance (or null).
    /// </summary>
    public GameObject PresentRising(
        GameObject prefab,
        Transform parent,
        Vector2 origin,
        float moveHeight,
        float moveSpeed,
        AudioSource audioSource = null,
        AudioClip riseSound = null)
    {
        if (prefab == null) return null;

        if (moveHeight <= 0f) moveHeight = defaultMoveHeight;
        if (moveSpeed <= 0f) moveSpeed = defaultMoveSpeed;

        if (audioSource != null && riseSound != null)
            audioSource.PlayOneShot(riseSound);

        GameObject item = Instantiate(prefab, parent, true);
        item.transform.position = origin;

        // Disable scripts while rising
        MonoBehaviour[] scripts = item.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != null)
                script.enabled = false;
        }

        // Collider off at start
        BoxCollider2D itemCollider = item.GetComponent<BoxCollider2D>();
        if (itemCollider != null)
            itemCollider.enabled = false;

        // Tag + sorting overrides
        string oldTag = item.tag;

        SpriteRenderer sr = item.GetComponent<SpriteRenderer>();
        int oldSortingLayerId = sr != null ? sr.sortingLayerID : 0;
        int oldSortingOrder = sr != null ? sr.sortingOrder : 0;

        item.tag = risingTag;

        if (sr != null && overrideSortingWhileRising)
        {
            sr.sortingLayerID = risingSortingLayerId;
            sr.sortingOrder = risingSortingOrder;
        }

        StartCoroutine(RiseUpCoroutine(
            item,
            oldTag,
            sr,
            oldSortingLayerId,
            oldSortingOrder,
            scripts,
            itemCollider,
            moveHeight,
            moveSpeed
        ));

        return item;
    }

    private IEnumerator RiseUpCoroutine(
        GameObject item,
        string oldTag,
        SpriteRenderer sr,
        int oldSortingLayerId,
        int oldSortingOrder,
        MonoBehaviour[] scripts,
        BoxCollider2D itemCollider,
        float moveHeight,
        float moveSpeed)
    {
        if (item == null) yield break;

        float startTime = Time.time;
        bool colliderEnabled = false;

        float targetY = item.transform.position.y + moveHeight;

        while (item != null && !stopAllRising)
        {
            float newY = Mathf.MoveTowards(item.transform.position.y, targetY, moveSpeed * Time.deltaTime);
            item.transform.position = new Vector3(item.transform.position.x, newY, item.transform.position.z);

            if (!colliderEnabled && Time.time >= startTime + colliderEnableDelay)
            {
                if (itemCollider != null)
                    itemCollider.enabled = true;
                colliderEnabled = true;
            }

            if (item.transform.position.y >= targetY - 0.01f)
            {
                RestoreItem(item, oldTag, sr, oldSortingLayerId, oldSortingOrder, scripts);
                yield break;
            }

            yield return null;
        }
    }

    private void RestoreItem(
        GameObject item,
        string oldTag,
        SpriteRenderer sr,
        int oldSortingLayerId,
        int oldSortingOrder,
        MonoBehaviour[] scripts)
    {
        if (item == null) return;

        // Re-enable scripts
        foreach (MonoBehaviour script in scripts)
        {
            if (script != null)
                script.enabled = true;
        }

        // Restore tag
        item.tag = oldTag;

        // Restore sorting
        if (sr != null)
        {
            sr.sortingLayerID = oldSortingLayerId;
            sr.sortingOrder = oldSortingOrder;
        }
    }
}