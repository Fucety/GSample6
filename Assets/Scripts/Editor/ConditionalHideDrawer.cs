using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ConditionalHideAttribute))]
public class ConditionalHideDrawer : PropertyDrawer
{
    private bool ShouldShow(SerializedProperty property)
    {
        ConditionalHideAttribute condHAtt = (ConditionalHideAttribute)attribute;
        SerializedProperty sourcePropertyValue = property.serializedObject.FindProperty(condHAtt.ConditionalSourceField);

        if (sourcePropertyValue != null)
        {
            bool enabled = sourcePropertyValue.boolValue;
            return condHAtt.HiddenValue ? !enabled : enabled;
        }

        Debug.LogWarning("ConditionalHideAttribute: не найдено поле " + condHAtt.ConditionalSourceField);
        return true;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!ShouldShow(property)) return;

        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!ShouldShow(property))
        {
            return -EditorGUIUtility.standardVerticalSpacing; // Скрываем поле
        }

        return EditorGUI.GetPropertyHeight(property, label);
    }
}