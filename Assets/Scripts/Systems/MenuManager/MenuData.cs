using UnityEngine;
using UnityEngine.UI;
using System;

[Serializable]
public class MenuData
{
    public string menuName;
    public GameObject menuPanel;

    [Tooltip("Name of the parent menu. Leave empty for the root menu.")]
    public string parentMenuName;

    [Tooltip("The button that gets selected by default when this menu opens. Used when no return target is available (e.g. opening from code, or the root menu).")]
    public Selectable defaultSelected;
}