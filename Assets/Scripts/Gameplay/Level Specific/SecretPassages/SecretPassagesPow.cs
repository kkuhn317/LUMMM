using UnityEngine;

public class SecretPassagesPow : MonoBehaviour
{
    [Header("Trigger")]
    public GameObject spiny; // the spiny that should activate it
    public POWEffect powEffect;

    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (other.gameObject != spiny) return;

        triggered = true;

        // Disable collider
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Disable this object's own SpriteRenderer
        var parentSr = GetComponent<SpriteRenderer>();
        if (parentSr != null) parentSr.enabled = false;

        // Disable ALL child SpriteRenderers
        var childSrs = GetComponentsInChildren<SpriteRenderer>();
        foreach (var childSr in childSrs)
        {
            childSr.enabled = false;
        }

        if (powEffect != null)
            powEffect.ActivatePOWEffect();
        else
            Debug.LogWarning("SecretPassagesPow: powEffect reference is missing.");

        Destroy(gameObject, 0.5f);
    }
}