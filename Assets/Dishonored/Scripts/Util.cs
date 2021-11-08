using UnityEngine;

public class Util : MonoBehaviour
{
    public static float EaseInSine(float x)
    {
        return 1 - Mathf.Cos((x * Mathf.PI) / 2);
    }

    public static float Remap(float f, float fromMin, float fromMax, float toMin, float toMax)
    {
        float t = (f - fromMin) / (fromMax - fromMin);
        return Mathf.LerpUnclamped(toMin, toMax, t);
    }
}
