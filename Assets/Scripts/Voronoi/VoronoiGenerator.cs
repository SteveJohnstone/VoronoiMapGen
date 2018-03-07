using System.Collections.Generic;
using UnityEngine;

public static class VoronoiGenerator
{
    public static List<Vector2> GetVector2Points(int seed, int number, int max)
    {
        UnityEngine.Random.InitState(seed);
        var points = new List<Vector2>();
        for (int i = 0; i < number; i++)
        {
            points.Add(new Vector2(UnityEngine.Random.Range(0, max), UnityEngine.Random.Range(0, max)));
        }
        return points;
    }
}
