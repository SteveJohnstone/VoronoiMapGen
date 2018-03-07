using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

public class MapGeneratorPreview : MonoBehaviour
{
    public enum PreviewType
    {
        Map,
        HeightMap
    }
    public enum PointGeneration
    {
        Random, PoissonDisc,
        OffsetGrid,
        Grid
    }

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public HeightMapSettings heightMapSettings;

    public int seed = 0;

    public int meshSize = 200;
    public int textureSize = 512;

    public PreviewType previewType;

    [Header("Voronoi Generation")]
    public PointGeneration pointGeneration;
    public int pointSpacing = 10;
    public int relaxationIterations = 1;
    public float snapDistance = 0;

    public bool flatshading;

    public bool autoUpdate;

    public void Start()
    {
        StartCoroutine(GenerateMapAsync());
    }

    public IEnumerator GenerateMapAsync()
    {
        yield return new WaitForSeconds(.1f);
        GenerateMap();
    }

    public void GenerateMap()
    {
        var startTime = DateTime.Now;
        var points = GetPoints();

        var voronoi = new Delaunay.Voronoi(points, null, new Rect(0, 0, meshSize, meshSize), relaxationIterations);
        heightMapSettings.noiseSettings.seed = seed;
        var heightMap = HeightMapGenerator.GenerateHeightMap(meshSize, meshSize, heightMapSettings, Vector2.zero);

        var mapGraph = new MapGraph(voronoi, heightMap, snapDistance);
        MapGenerator.GenerateMap(mapGraph);

        if (previewType == PreviewType.HeightMap)
        {
            OnMeshDataReceived(MapRenderer.GenerateMesh(mapGraph, heightMap, meshSize));
            UpdateTexture(TextureGenerator.TextureFromHeightMap(heightMap));
        }
        if (previewType == PreviewType.Map)
        {
            OnMeshDataReceived(MapRenderer.GenerateMesh(mapGraph, heightMap, meshSize, flatshading));

            var time = DateTime.Now;
            var texture = MapRenderer.GenerateTexture(mapGraph, meshSize, textureSize);
            Debug.Log(string.Format("Texture Generated: {0:n0}ms", DateTime.Now.Subtract(time).TotalMilliseconds));

            OnTextureDataReceived(texture);
        }

        Debug.Log(string.Format("Finished Generating World: {0:n0}ms with {1} nodes", DateTime.Now.Subtract(startTime).TotalMilliseconds, mapGraph.nodesByCenterPosition.Count));
    }

    private List<Vector2> GetPoints()
    {
        List<Vector2> points = null;
        if (pointGeneration == PointGeneration.Random)
        {
            points = VoronoiGenerator.GetVector2Points(seed, (meshSize / pointSpacing) * (meshSize / pointSpacing), meshSize);
        }
        else if (pointGeneration == PointGeneration.PoissonDisc)
        {
            var poisson = new PoissonDiscSampler(meshSize, meshSize, pointSpacing, seed);
            points = poisson.Samples().ToList();
        }
        else if (pointGeneration == PointGeneration.Grid)
        {
            points = new List<Vector2>();
            for (int x = pointSpacing; x < meshSize; x += pointSpacing)
            {
                for (int y = pointSpacing; y < meshSize; y += pointSpacing)
                {
                    points.Add(new Vector2(x, y));
                }
            }
        }
        else if (pointGeneration == PointGeneration.OffsetGrid)
        {
            points = new List<Vector2>();
            for (int x = pointSpacing; x < meshSize; x += pointSpacing)
            {
                bool even = false;
                for (int y = pointSpacing; y < meshSize; y += pointSpacing)
                {
                    var newX = even ? x : x - (pointSpacing / 2f);
                    points.Add(new Vector2(newX, y));
                    even = !even;
                }
            }
        }

        return points;
    }

    private void OnTextureDataReceived(object result)
    {
        var textureData = result as TextureData;
        UpdateTexture(textureData);
    }

    private void OnMeshDataReceived(object result)
    {
        var meshData = result as MeshData;
        UpdateMesh(meshData);
    }

    private void UpdateTexture(TextureData data)
    {
        var texture = new Texture2D(textureSize, textureSize);
        texture.SetPixels(data.colours);
        texture.Apply();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }

    private void UpdateTexture(Texture2D texture)
    {
        meshRenderer.sharedMaterial.mainTexture = texture;
    }

    public void UpdateMesh(MeshData meshData)
    {
        var mesh = new Mesh
        {
            vertices = meshData.vertices.ToArray(),
            triangles = meshData.indices.ToArray(),
            uv = meshData.uvs
        };
        mesh.RecalculateNormals();
        meshFilter.sharedMesh = mesh;
    }

    void OnValidate()
    {
        if (heightMapSettings != null)
        {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }

    }

    private void OnValuesUpdated()
    {
        GenerateMap();
    }
}
