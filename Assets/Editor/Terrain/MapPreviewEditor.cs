using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(TerrainGeneratorPreview))]
public class MapPreviewEditor : Editor
{

    public override void OnInspectorGUI()
    {
        TerrainGeneratorPreview mapPreview = (TerrainGeneratorPreview)target;

        if (DrawDefaultInspector())
        {
            if (mapPreview.autoUpdate)
            {
                mapPreview.DrawMapInEditor();
            }
        }

        if (GUILayout.Button("Generate"))
        {
            mapPreview.DrawMapInEditor();
        }
    }
}