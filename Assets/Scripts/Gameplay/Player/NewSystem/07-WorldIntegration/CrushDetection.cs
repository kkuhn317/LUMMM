using UnityEngine;

/// <summary>
/// Detects when Mario is crushed between two "Crushing"-tagged objects.
///
/// Place this on a child object of the player with a small BoxCollider2D
/// set to NOT collide normally (trigger or on its own layer). When a
/// Crushing object touches it, it deals forced damage or triggers a
/// custom transformation (e.g. pig curse).
///
/// Follows the collider to account for crouch collider resize each frame.
/// </summary>
public class CrushDetection : MonoBehaviour
{
    [Tooltip("If set, Mario transforms into this object instead of dying when crushed.")]
    public GameObject customCrushDeath;

    private void FixedUpdate()
    {
        // Keep the crush detector centred on the parent's collider
        // so it doesn't stick out when Mario crouches
        var parentBox = transform.parent.GetComponent<BoxCollider2D>();
        if (parentBox != null)
        {
            transform.localPosition = new Vector3(
                parentBox.offset.x,
                parentBox.offset.y,
                transform.localPosition.z);
        }
    }

    private void OnCollisionEnter2D(Collision2D col) => CheckCrush(col.gameObject);
    private void OnCollisionStay2D(Collision2D col)  => CheckCrush(col.gameObject);
    private void OnTriggerEnter2D(Collider2D col)    => CheckCrush(col.gameObject);

    private void CheckCrush(GameObject other)
    {
        if (!other.CompareTag("Crushing")) return;

        var core = GetComponentInParent<MarioCore>();
        if (core == null) return;

        if (customCrushDeath != null)
            core.Combat.TransformIntoObject(customCrushDeath);
        else
            core.Combat.DamageMario(force: true);
    }
}
