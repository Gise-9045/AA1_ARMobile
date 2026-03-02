using UnityEngine;
[CreateAssetMenu(fileName ="TutorialSO", menuName ="TutoSO/TutoSO, order = 1")]
public class TutoScriptableObject : ScriptableObject
{
    [SerializeField] public float testFloat;
    [SerializeField][Color(1f,0f,0f, 1f)] private string _str;

    [SerializeField] protected Recipes _recipe;
}
