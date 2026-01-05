// SettingTypeFilterDrawer.cs (Editor folder)
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(SettingTypeFilterAttribute))]
public class SettingTypeFilterDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var filter = (SettingTypeFilterAttribute)attribute;
        if (property.propertyType == SerializedPropertyType.Enum)
        {
            SettingType current = (SettingType)property.enumValueIndex;
            SettingType[] allowed = filter.AllowedValues;

            int currentIndex = System.Array.IndexOf(allowed, current);
            if (currentIndex == -1) currentIndex = 0;

            string[] displayed = System.Array.ConvertAll(allowed, e => e.ToString());

            EditorGUI.BeginProperty(position, label, property);
            int selected = EditorGUI.Popup(position, label.text, currentIndex, displayed);
            property.enumValueIndex = (int)allowed[selected];
            EditorGUI.EndProperty();
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Use SettingTypeFilter with enums only.");
        }
    }
}
#endif