using UnityEngine;
using System.Collections;

public static class TextureHelper
{
    public static void DrawLine(Vector2 start, Vector2 end, Color[] colours, int textureSize, Color colour)
    {
        Vector2 t = start;
        float frac = 1 / Mathf.Sqrt(Mathf.Pow(end.x - start.x, 2) + Mathf.Pow(end.y - start.y, 2));
        float ctr = 0;

        while ((int)t.x != (int)end.x || (int)t.y != (int)end.y)
        {
            t = Vector2.Lerp(start, end, ctr);
            ctr += frac;
            if ((int)t.x > 0 && (int)t.x < textureSize && (int)t.y > 0 && (int)t.y < textureSize)
            {
                colours[(int)t.x + ((int)t.y * textureSize)] = colour;
            }
        }
    }

    public static void DrawLine(Vector2 start, Vector2 end, Texture2D texture, Color col)
    {
        Vector2 t = start;
        float frac = 1 / Mathf.Sqrt(Mathf.Pow(end.x - start.x, 2) + Mathf.Pow(end.y - start.y, 2));
        float ctr = 0;

        while ((int)t.x != (int)end.x || (int)t.y != (int)end.y)
        {
            t = Vector2.Lerp(start, end, ctr);
            ctr += frac;
            texture.SetPixel((int)t.x, (int)t.y, col);
        }
    }
}
