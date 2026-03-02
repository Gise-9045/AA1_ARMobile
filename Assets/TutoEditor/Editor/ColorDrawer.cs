using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ColorAttribute))]
public class ColorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ColorAttribute colorAttribute = (ColorAttribute)attribute; //alternativamente: (ColorAttribute)attribute

        Color antColor = GUI.color;

        GUI.color = colorAttribute.color;
        Debug.Log("entra");
        EditorGUI.PropertyField(position, property, label);

        GUI.color = antColor;
    }
}
