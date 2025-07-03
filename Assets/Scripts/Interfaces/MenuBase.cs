using UnityEngine;
using UnityEngine.EventSystems;

public abstract class MenuBase : MonoBehaviour, IMenu
{
    [SerializeField] private string menuId;
    [SerializeField] private string parentMenuId;

    public virtual string MenuId => menuId;
    public virtual string ParentMenuId => parentMenuId;

    public virtual void Open() => gameObject.SetActive(true);
    public virtual void Close() => gameObject.SetActive(false);
    public virtual void RestoreFocus() { }
}