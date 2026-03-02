using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TutoScriptableObject))]

public class TutoScriptableObjectEditor : Editor
{
    SerializedProperty strProp;
    private void OnEnable()
    {
        //propiedad entera de _str pasado a strProp
        strProp = serializedObject.FindProperty("_str");
    }
    public override void OnInspectorGUI()
    {
        //Hay que llamar esto para poder hacer endHorizontal luego
        EditorGUILayout.BeginHorizontal();
        //No acabo de entender exactamente que hace esto. Creo que es para dejar que se vea por inspector con un nombre custom?
        TutoScriptableObject tutoSO = (TutoScriptableObject)target;
        tutoSO.testFloat = EditorGUILayout.FloatField("Float of Tuto", tutoSO.testFloat);

        //Hay que poner update para que se guarden los cambios cuando copias las propiedades
        serializedObject.Update();
        EditorGUILayout.PropertyField(strProp);
        //Te guarda los cambios que cambies por inspector
        serializedObject.ApplyModifiedProperties();

        //Para recolocar mierdas en la interfaz o algo asi
        //mira si expandes la pestañita de inspector los campos estan uno al lado
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(50);
        DrawDefaultInspector();
    }
}
