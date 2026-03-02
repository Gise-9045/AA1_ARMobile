using UnityEngine;

public class ColorAttribute : PropertyAttribute
{
    public Color color;

    public ColorAttribute(float r, float g, float b, float a = 1f)
    {
        color = new Color(r, g, b, a);
    }
}
