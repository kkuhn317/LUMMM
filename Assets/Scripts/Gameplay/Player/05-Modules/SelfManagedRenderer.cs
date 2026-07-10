using UnityEngine;

/// <summary>
/// Marker component. Put this on any SpriteRenderer under SpriteBaseRoot or
/// SpriteSwapContainer whose visibility is owned by another system (e.g. the
/// cape, driven by the cape ability) and must NOT be toggled by the look-up
/// rig swap in MarioAnimatorController.
///
/// MarioAnimatorController skips these when caching swappable renderers, so
/// ShowBaseRig()/ShowSwapRig() leave them alone.
///
/// No fields, no logic — its presence is the whole signal.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SelfManagedRenderer : MonoBehaviour
{
}
