using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMapGenerator {

	public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCentre) {
		float[,] values = Noise.GenerateNoiseMap (width, height, settings.noiseSettings, sampleCentre);

        float[,] falloff= new float[0,0];
        if (settings.useFalloff)
        {
            var curve = new AnimationCurve(settings.falloffCurve.keys);
            falloff = FalloffGenerator.GenerateFalloffMap(width, settings.falloffType, curve);
        }

		AnimationCurve threadSafeHeightCurve = new AnimationCurve (settings.heightCurve.keys);

		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		for (int i = 0; i < width; i++) {
			for (int j = 0; j < height; j++) {

                if (settings.useFalloff)
                {
                    values[i, j] -= falloff[i, j];
                }

                values[i, j] *= threadSafeHeightCurve.Evaluate(values[i, j]) * settings.heightMultiplier;

                if (values [i, j] > maxValue) {
					maxValue = values [i, j];
				}
				if (values [i, j] < minValue) {
					minValue = values [i, j];
				}
			}
		}

		return new HeightMap (values, minValue, maxValue);
	}

}

public struct HeightMap {
	public readonly float[,] values;
	public readonly float minValue;
	public readonly float maxValue;

	public HeightMap (float[,] values, float minValue, float maxValue)
	{
		this.values = values;
		this.minValue = minValue;
		this.maxValue = maxValue;
	}
}

