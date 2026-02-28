using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[CustomPropertyDrawer(typeof(MenuData))]
public class MenuDataDrawer : PropertyDrawer
{
    private const float Padding     = 2f;
    private const float FieldHeight = 20f;
    private const float TotalHeight = FieldHeight * 4 + Padding * 5; // 4 fields now

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return TotalHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var menuNameProp      = property.FindPropertyRelative("menuName");
        var menuPanelProp     = property.FindPropertyRelative("menuPanel");
        var parentNameProp    = property.FindPropertyRelative("parentMenuName");
        var defaultSelectProp = property.FindPropertyRelative("defaultSelected");

        var allMenuNames = GetAllMenuNames(property);

        float y          = position.y + Padding;
        float labelWidth = EditorGUIUtility.labelWidth;
        float fieldWidth = position.width - labelWidth;

        // --- Menu Name ---
        Rect menuNameRect = new Rect(position.x, y, position.width, FieldHeight);
        EditorGUI.PropertyField(menuNameRect, menuNameProp, new GUIContent("Menu Name"));
        y += FieldHeight + Padding;

        // --- Menu Panel ---
        Rect menuPanelRect = new Rect(position.x, y, position.width, FieldHeight);
        EditorGUI.PropertyField(menuPanelRect, menuPanelProp, new GUIContent("Menu Panel"));
        y += FieldHeight + Padding;

        // --- Default Selected ---
        Rect defaultSelectRect = new Rect(position.x, y, position.width, FieldHeight);
        EditorGUI.PropertyField(defaultSelectRect, defaultSelectProp, new GUIContent("Default Selected", "The Selectable (button, etc.) that gets focused when this menu opens with no return target."));
        y += FieldHeight + Padding;

        // --- Parent Menu (dropdown) ---
        Rect parentLabelRect = new Rect(position.x, y, labelWidth, FieldHeight);
        Rect parentFieldRect = new Rect(position.x + labelWidth, y, fieldWidth, FieldHeight);

        EditorGUI.LabelField(parentLabelRect, new GUIContent("Parent Menu", "The menu that owns this one. Choose (Root) if this is the top-level menu."));

        string thisMenuName = menuNameProp.stringValue;
        var options = new List<string> { "(Root)" };
        options.AddRange(allMenuNames.Where(n => n != thisMenuName));

        string currentParent = parentNameProp.stringValue;
        bool isRoot          = string.IsNullOrEmpty(currentParent);
        int currentIndex     = isRoot ? 0 : options.IndexOf(currentParent);
        bool isInvalid       = !isRoot && currentIndex < 0;

        if (isInvalid)
        {
            var prevColor = GUI.color;
            GUI.color = new Color(1f, 0.4f, 0.4f);
            EditorGUI.Popup(parentFieldRect, 0, new[] { $"! Missing: '{currentParent}'" });
            GUI.color = prevColor;

            if (GUI.Button(parentFieldRect, GUIContent.none, GUIStyle.none))
                parentNameProp.stringValue = "";
        }
        else
        {
            int chosen = EditorGUI.Popup(parentFieldRect, currentIndex, options.ToArray());
            parentNameProp.stringValue = chosen == 0 ? "" : options[chosen];
        }

        EditorGUI.EndProperty();
    }

    private List<string> GetAllMenuNames(SerializedProperty property)
    {
        var names = new List<string>();

        SerializedProperty menusArray = property.serializedObject.FindProperty("menus");
        if (menusArray == null || !menusArray.isArray)
            return names;

        for (int i = 0; i < menusArray.arraySize; i++)
        {
            var element  = menusArray.GetArrayElementAtIndex(i);
            var nameProp = element.FindPropertyRelative("menuName");
            if (nameProp != null && !string.IsNullOrEmpty(nameProp.stringValue))
                names.Add(nameProp.stringValue);
        }

        return names;
    }
}