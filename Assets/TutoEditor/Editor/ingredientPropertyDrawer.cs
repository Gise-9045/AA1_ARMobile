using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(Ingredients))]
public class ingredientPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        //EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginProperty(position, label,property);
        Rect nameRect = new(position.x, position.y, 40, position.height);
        Rect quantityRect = new(position.x+45, position.y, 40, position.height);

        EditorGUI.PropertyField(nameRect, property.FindPropertyRelative("name"), GUIContent.none);        
        EditorGUI.PropertyField(quantityRect, property.FindPropertyRelative("quantity"), GUIContent.none);

        EditorGUI.EndProperty();
        //EditorGUILayout.EndHorizontal();
    }
}
