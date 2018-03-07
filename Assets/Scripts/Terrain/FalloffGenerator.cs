using UnityEngine;
using System.Collections;

public static class FalloffGenerator
{

    public static float[,] GenerateFalloffMap(int size, HeightMapSettings.FalloffType falloffType, AnimationCurve falloffCurve)
    {
        float[,] map = new float[size, size];

        if (falloffType == HeightMapSettings.FalloffType.Square)
        {

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    float x = i / (float)size * 2 - 1;
                    float y = j / (float)size * 2 - 1;

                    float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                    map[i, j] = Evaluate(value);
                }
            }

        }
        else if (falloffType == HeightMapSettings.FalloffType.Circle)
        {

            var center = size / 2f;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    var distance = Vector2.Distance(new Vector2(i, j), new Vector2(center, center)) / size / 0.5f;
                    map[i, j] = falloffCurve.Evaluate(Mathf.Clamp01(distance));
                }
            }

        }
        return map;
    }

    static float Evaluate(float value)
    {
        float a = 3;
        float b = 2.2f;

        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}
