using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGeneratorPreview))]
public class MapGeneratorPreviewEditor : Editor
{

    public override void OnInspectorGUI()
    {
        MapGeneratorPreview generator = (MapGeneratorPreview)target;

        if (DrawDefaultInspector())
        {
            if (generator.autoUpdate)
            {
                generator.GenerateMap();
            }
        }

        if (GUILayout.Button("Generate"))
        {
            generator.GenerateMap();
        }
    }
}