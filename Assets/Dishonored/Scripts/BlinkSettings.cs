using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "BlinkSettings", menuName = "Scriptables/BlinkSettings", order = 1)]
public class BlinkSettings : ScriptableObject
{
    public float MinBlinkDistance;
    public float MaxBlinkDistance;

    public float MinBlinkTime;
    public float MaxBlinkTime;

    public AnimationCurve FovCurve;
}
