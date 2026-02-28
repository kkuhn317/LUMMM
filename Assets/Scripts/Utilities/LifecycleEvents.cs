using UnityEngine;
using UnityEngine.Events;

public class LifecycleEvents : MonoBehaviour
{
    [Header("Called when this GameObject becomes enabled/active")]
    [SerializeField] private UnityEvent onEnabled;

    [Header("Called when this GameObject becomes disabled/inactive")]
    [SerializeField] private UnityEvent onDisabled;

    private void OnEnable()  => onEnabled?.Invoke();
    private void OnDisable() => onDisabled?.Invoke();
}