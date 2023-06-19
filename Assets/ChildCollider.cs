using UnityEngine;
using UnityEngine.Events;

public class ChildCollider : MonoBehaviour
{
    [SerializeField]
    private UnityEvent hit;

    public void Hit()
    {
        hit?.Invoke();
    }
}
