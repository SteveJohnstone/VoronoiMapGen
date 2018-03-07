using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

public static class MapRenderer
{
    public static MeshData GenerateMesh(MapGraph mapGraph, HeightMap heightmap, int meshSize, bool flatshading = false)
    {
        var meshData = new MeshData();
        meshData.vertices = new List<Vector3>();
        meshData.indices = new List<int>();

        foreach(var node in mapGraph.nodesByCenterPosition.Values)
        {
            meshData.vertices.Add(node.centerPoint);
            var centerIndex = meshData.vertices.Count - 1;
            var edges = node.GetEdges().ToList();

            int lastIndex = 0;
            int firstIndex = 0;
            for (var i = 0; i < edges.Count(); i++)
            {
                if (i == 0)
                {
                    meshData.vertices.Add(edges[i].previous.destination.position);
                    var i2 = meshData.vertices.Count - 1;
                    meshData.vertices.Add(edges[i].destination.position);
                    var i3 = meshData.vertices.Count - 1;
                    AddTriangle(meshData, centerIndex, i2, i3);
                    firstIndex = i2;
                    lastIndex = i3;
                }
                else if (i < edges.Count() -1)
                {
                    meshData.vertices.Add(edges[i].destination.position);
                    var currentIndex = meshData.vertices.Count - 1;
                    AddTriangle(meshData, centerIndex, lastIndex, currentIndex);
                    lastIndex = currentIndex;
                } 
                else
                {
                    AddTriangle(meshData, centerIndex, lastIndex, firstIndex);
                }
            }
        }

        meshData.uvs = new Vector2[meshData.vertices.Count];
        for (int i = 0; i < meshData.uvs.Length; i++)
        {
            meshData.uvs[i] = new Vector2(meshData.vertices[i].x / meshSize, meshData.vertices[i].z / meshSize);
        }

        return meshData;
    }

    private static void AddTriangle(MeshData meshData, int v1, int v2, int v3)
    {
        meshData.indices.Add(v1);
        meshData.indices.Add(v2);
        meshData.indices.Add(v3);
    }

    public static TextureData GenerateTexture(MapGraph map, int meshSize, int textureSize)
    {
        var textureData = new TextureData();
        textureData.colours = new Color[textureSize * textureSize];

        DrawTileTypes(map, meshSize, textureSize, textureData);
        //DrawCenterPoints(map, meshSize, textureSize, textureData);
        //DrawEdges(map, meshSize, textureSize, textureData, Color.black);
        DrawRivers(map, meshSize, textureSize, textureData, 2, new Color(48f / 255f, 104f / 255f, 153f / 255f));
        //DrawDelauneyEdges(map, meshSize, textureSize, textureData, Color.red);

        return textureData;
    }

    private static void DrawEdges(MapGraph map, int meshSize, int textureSize, TextureData textureData, Color color)
    {
        var opposites = new List<MapGraph.MapNodeHalfEdge>();
        foreach (var edge in map.edges)
        {
            if (opposites.Contains(edge)) continue; // Make sure we don't draw both half edges.
            if (edge.opposite != null) opposites.Add(edge.opposite);

            var start = edge.GetStartPosition() / meshSize * textureSize; ;
            var end = edge.GetEndPosition() / meshSize * textureSize; ;

            TextureHelper.DrawLine(new Vector2(start.x, start.z), new Vector2(end.x, end.z), textureData.colours, textureSize, color);
        }
    }

    private static void DrawRivers(MapGraph map, int meshSize, int textureSize, TextureData textureData, int minRiverSize, Color color)
    {
        var opposites = new List<MapGraph.MapNodeHalfEdge>();
        foreach (var edge in map.edges)
        {
            if (edge.water < minRiverSize) continue;

            var start = edge.GetStartPosition() / meshSize * textureSize; ;
            var end = edge.GetEndPosition() / meshSize * textureSize; ;

            TextureHelper.DrawLine(new Vector2(start.x, start.z), new Vector2(end.x, end.z), textureData.colours, textureSize, color);
        }
    }

    private static void DrawDelauneyEdges(MapGraph map, int meshSize, int textureSize, TextureData textureData, Color color)
    {
        var opposites = new List<MapGraph.MapNodeHalfEdge>();
        foreach (var edge in map.edges)
        {
            if (opposites.Contains(edge)) continue; // Make sure we don't draw both half edges.
            if (edge.opposite != null)
            {
                opposites.Add(edge.opposite);

                var start = edge.node.centerPoint / meshSize * textureSize; ;
                var end = edge.opposite.node.centerPoint / meshSize * textureSize; ;
                TextureHelper.DrawLine(new Vector2(start.x, start.z), new Vector2(end.x, end.z), textureData.colours, textureSize, color);
            }
        }
    }

    private static void DrawCenterPoints(MapGraph map, int meshSize, int textureSize, TextureData textureData)
    {
        foreach (var point in map.nodesByCenterPosition.Values)
        {
            var x = point.centerPoint.x / meshSize * textureSize;
            var y = point.centerPoint.z / meshSize * textureSize;
            textureData.colours[(int)x + ((int)y * textureSize)] = Color.red;
        }
    }

    private static void DrawTileTypes(MapGraph map, int meshSize, int textureSize, TextureData textureData)
    {
        const int drawingBuffer = 2;
        var ratio = (float)textureSize / meshSize;

        var grass = new Color(109f / 255f, 154f / 255f, 102f / 255f);
        var freshWater = new Color(48f / 255f, 104f / 255f, 153f / 255f);
        var saltWater = new Color(68f / 255f, 68f / 255f, 120f / 255f);
        var earth = new Color(162f / 255f, 99f / 255f, 68f / 255f);
        var sand = new Color(210f / 255f, 180f / 255f, 124f / 255f);
        var snow = new Color(248f / 255f, 248f / 255f, 248f / 255f);
        var city = Color.gray;

        foreach (var node in map.nodesByCenterPosition.Values)
        {
            var boundingRectangle = node.GetBoundingRectangle();
            var startX = (int)(boundingRectangle.x * ratio) - drawingBuffer;
            var startY = (int)(boundingRectangle.y * ratio) - drawingBuffer;
            var endX = (int)((boundingRectangle.x + boundingRectangle.width) * ratio) + drawingBuffer;
            var endY = (int)((boundingRectangle.y + boundingRectangle.height) * ratio) + drawingBuffer;

            var corners = node.GetCorners().Select(x => new Vector2(x.position.x, x.position.z) * ratio).ToArray();

            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    if (PolygonContainsPoint(corners, new Vector2(x, y)))
                    {
                        switch (node.nodeType)
                        {
                            case MapGraph.MapNodeType.City:
                                textureData.colours[x + y * textureSize] = city;
                                break;
                            case MapGraph.MapNodeType.FreshWater:
                                textureData.colours[x + y * textureSize] = freshWater;
                                break;
                            case MapGraph.MapNodeType.SaltWater:
                                textureData.colours[x + y * textureSize] = saltWater;
                                break;
                            case MapGraph.MapNodeType.Grass:
                                textureData.colours[x + y * textureSize] = grass;
                                break;
                            case MapGraph.MapNodeType.Mountain:
                                textureData.colours[x + y * textureSize] = earth;
                                break;
                            case MapGraph.MapNodeType.Beach:
                                textureData.colours[x + y * textureSize] = sand;
                                break;
                            case MapGraph.MapNodeType.Snow:
                                textureData.colours[x + y * textureSize] = snow;
                                break;
                            case MapGraph.MapNodeType.Error:
                                textureData.colours[x + y * textureSize] = Color.red;
                                break;
                        }
                    }
                }
            }
        }
    }

    private static bool PolygonContainsPoint(Vector2[] polyPoints, Vector2 p)
    {
        var j = polyPoints.Length - 1;
        var inside = false;
        for (var i = 0; i < polyPoints.Length; j = i++)
        {
            if (((polyPoints[i].y <= p.y && p.y < polyPoints[j].y) || (polyPoints[j].y <= p.y && p.y < polyPoints[i].y)) &&
               (p.x < (polyPoints[j].x - polyPoints[i].x) * (p.y - polyPoints[i].y) / (polyPoints[j].y - polyPoints[i].y) + polyPoints[i].x))
                inside = !inside;
        }
        return inside;
    }
}