using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "terrain/heightmap settings", fileName = "HeightmapSettings")]
public class HeightMapSettings : UpdatableData
{
    public enum FalloffType
    {
        Square, Circle
    }

    public NoiseSettings noiseSettings;

    public bool useFalloff;

    public float heightMultiplier;
    public AnimationCurve heightCurve;
    public AnimationCurve falloffCurve;
    public FalloffType falloffType;

    public float minHeight
    {
        get
        {
            return heightMultiplier * heightCurve.Evaluate(0);
        }
    }

    public float maxHeight
    {
        get
        {
            return heightMultiplier * heightCurve.Evaluate(1);
        }
    }

#if UNITY_EDITOR

    protected override void OnValidate()
    {
        noiseSettings.ValidateValues();
        base.OnValidate();
    }
#endif

}
