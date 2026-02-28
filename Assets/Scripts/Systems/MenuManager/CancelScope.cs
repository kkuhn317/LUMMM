using UnityEngine;

public class CancelScope : MonoBehaviour, ICancelHandler
{
    [SerializeField] private int cancelPriority = 1000;
    public int CancelPriority => cancelPriority;

    public virtual bool OnCancel()
    {
        gameObject.SetActive(false);
        return true;
    }

    private void OnEnable() => CancelStack.Register(this);
    private void OnDisable() => CancelStack.Unregister(this);
}