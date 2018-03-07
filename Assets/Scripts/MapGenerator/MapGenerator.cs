using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class MapGenerator
{
    public static void GenerateMap(MapGraph graph)
    {
        SetNodesToGrass(graph);
        SetLowNodesToWater(graph, 0.2f);
        SetEdgesToWater(graph);

        FillOcean(graph);
        SetBeaches(graph);
        FindRivers(graph, 12f);
        CreateLakes(graph);

        AddMountains(graph);

        AverageCenterPoints(graph);

        FindCities(graph, 0.5f, 8f, 3);
    }

    private static void SetEdgesToWater(MapGraph graph)
    {
        foreach (var node in graph.nodesByCenterPosition.Values)
        {
            if (node.IsEdge()) node.nodeType = MapGraph.MapNodeType.FreshWater;
        }
    }

    private static void AverageCenterPoints(MapGraph graph)
    {
        foreach (var node in graph.nodesByCenterPosition.Values)
        {
            node.centerPoint = new Vector3(node.centerPoint.x, node.GetCorners().Average(x => x.position.y), node.centerPoint.z);
        }
    }

    private static void AddMountains(MapGraph graph)
    {
        foreach (var node in graph.nodesByCenterPosition.Values)
        {
            if (node.GetElevation() > 15f || node.GetHeightDifference() > 7f)
            {
                node.nodeType = MapGraph.MapNodeType.Mountain;
            }
            if (node.GetElevation() > 17f)
            {
                node.nodeType = MapGraph.MapNodeType.Snow;
            }
        }
    }

    private static void CreateLakes(MapGraph graph)
    {
        foreach (var node in graph.nodesByCenterPosition.Values)
        {
            var edges = node.GetEdges();
            if (!edges.Any(x => x.water == 0))
            {
                CreateLake(node);
            }
        }
    }

    private static void FindRivers(MapGraph graph, float minElevation)
    {
        var riverCount = 0;
        foreach (var node in graph.nodesByCenterPosition.Values)
        {
            var elevation = node.GetElevation();
            if (elevation > minElevation)
            {
                var waterSource = node.GetLowestCorner();
                var lowestEdge = waterSource.GetDownSlopeEdge();
                if (lowestEdge == null) continue;
                CreateRiver(graph, lowestEdge);

                riverCount++;
            }
        }
        //Debug.Log(string.Format("{0} rivers drawn", riverCount));
    }

    private static void CreateRiver(MapGraph graph, MapGraph.MapNodeHalfEdge startEdge)
    {
        bool heightUpdated = false;

        // Once a river has been generated, it tries again to see if a quicker route has been created.
        // This sets how many times we should go over the same river.
        var maxIterations = 1;
        var iterationCount = 0;

        // Make sure that the river generation code doesn't get stuck in a loop.
        var maxChecks = 100;
        var checkCount = 0;

        var previousRiverEdges = new List<MapGraph.MapNodeHalfEdge>();
        do
        {
            heightUpdated = false;

            var riverEdges = new List<MapGraph.MapNodeHalfEdge>();
            var previousEdge = startEdge;
            var nextEdge = startEdge;

            while (nextEdge != null)
            {
                if (checkCount >= maxChecks)
                {
                    Debug.LogError("Unable to find route for river. Maximum number of checks reached");
                    return;
                }
                checkCount++;

                var currentEdge = nextEdge;

                // We've already seen this edge and it's flowing back up itself.
                if (riverEdges.Contains(currentEdge) || riverEdges.Contains(currentEdge.opposite)) break;
                riverEdges.Add(currentEdge);
                currentEdge.AddWater();

                // Check that we haven't reached the sea
                if (currentEdge.destination.GetNodes().Any(x => x.nodeType == MapGraph.MapNodeType.SaltWater)) break;

                nextEdge = GetDownSlopeEdge(currentEdge, riverEdges);

                if (nextEdge == null && previousEdge != null)
                {
                    // We need to start carving a path for the river.
                    nextEdge = GetNewCandidateEdge(graph.GetCenter(), currentEdge, riverEdges, previousRiverEdges);

                    // If we can't get a candidate edge, then backtrack and try again
                    var previousEdgeIndex = riverEdges.Count - 1;
                    while (nextEdge == null || previousEdgeIndex == 0)
                    {
                        previousEdge = riverEdges[previousEdgeIndex];
                        previousEdge.water--;
                        nextEdge = GetNewCandidateEdge(graph.GetCenter(), previousEdge, riverEdges, previousRiverEdges);
                        riverEdges.Remove(previousEdge);
                        previousEdgeIndex--;
                    }
                    if (nextEdge != null)
                    {
                        if (nextEdge.previous.destination.position.y != nextEdge.destination.position.y)
                        {
                            LevelEdge(nextEdge);
                            heightUpdated = true;
                        }
                    }
                    else
                    {
                        // We've tried tunneling, backtracking, and we're still lost.
                        Debug.LogError("Unable to find route for river");
                    }
                }
                previousEdge = currentEdge;
            }
            if (maxIterations <= iterationCount) break;
            iterationCount++;

            // If the height was updated, we need to recheck the river again.
            if (heightUpdated)
            {
                foreach (var edge in riverEdges)
                {
                    if (edge.water > 0) edge.water--;
                }
                previousRiverEdges = riverEdges;
            }
        } while (heightUpdated);
    }

    private static void CreateLake(MapGraph.MapNode node)
    {
        var lowestCorner = node.GetLowestCorner();
        node.nodeType = MapGraph.MapNodeType.FreshWater;

        // Set all of the heights equal to where the water came in.
        SetNodeHeightToCornerHeight(node, lowestCorner);
    }

    private static void LevelEdge(MapGraph.MapNodeHalfEdge currentEdge)
    {
        currentEdge.destination.position = new Vector3(currentEdge.destination.position.x, currentEdge.previous.destination.position.y, currentEdge.destination.position.z);
    }

    private static MapGraph.MapNodeHalfEdge GetDownSlopeEdge(MapGraph.MapNodeHalfEdge source, List<MapGraph.MapNodeHalfEdge> seenEdges)
    {
        var corner = source.destination;

        var candidates = corner.GetEdges().Where(x =>
            x.destination.position.y < corner.position.y &&
            !seenEdges.Contains(x) &&
            x.opposite != null && !seenEdges.Contains(x.opposite) &&
            x.node.nodeType != MapGraph.MapNodeType.FreshWater &&
            x.opposite.node.nodeType != MapGraph.MapNodeType.FreshWater);

        // Make sure the river prefers to follow existing rivers
        var existingRiverEdge = candidates.FirstOrDefault(x => x.water > 0);
        if (existingRiverEdge != null) return existingRiverEdge;

        return candidates.OrderByDescending(x => x.GetSlopeAngle()).FirstOrDefault();
    }

    private static MapGraph.MapNodeHalfEdge GetNewCandidateEdge(Vector3 center, MapGraph.MapNodeHalfEdge source, List<MapGraph.MapNodeHalfEdge> seenEdges, List<MapGraph.MapNodeHalfEdge> previousEdges)
    {
        var corner = source.destination;

        var edges = corner.GetEdges().Where(x =>
            !seenEdges.Contains(x) &&
            x.opposite != null &&
            !seenEdges.Contains(x.opposite)).ToList();

        // Make sure the river prefers to follow existing rivers
        var existingRiverEdge = edges.FirstOrDefault(x => x.water > 0);
        if (existingRiverEdge != null) return existingRiverEdge;

        // Make the river prefer to follow previous iterations
        existingRiverEdge = edges.FirstOrDefault(x => previousEdges.Contains(x));
        if (existingRiverEdge != null) return existingRiverEdge;

        var awayFromCenterEdges = edges.Where(x => Vector3.Dot(x.destination.position - x.previous.destination.position, x.destination.position - center) >= 0);
        if (awayFromCenterEdges.Any()) edges = awayFromCenterEdges.ToList();
        return edges.OrderBy(x => x.destination.position.y).FirstOrDefault();
    }


    private static void SetNodeHeightToCornerHeight(MapGraph.MapNode node, MapGraph.MapPoint targetCorner)
    {
        foreach (var corner in node.GetCorners())
        {
            corner.position = new Vector3(corner.position.x, targetCorner.position.y, corner.position.z);
        }
        node.centerPoint = new Vector3(node.centerPoint.x, targetCorner.position.y, node.centerPoint.z);
    }

    private static void FillOcean(MapGraph graph)
    {
        var startNode = graph.nodesByCenterPosition.FirstOrDefault(x => x.Value.IsEdge() && x.Value.nodeType == MapGraph.MapNodeType.FreshWater).Value;
        FloodFill(startNode, MapGraph.MapNodeType.FreshWater, MapGraph.MapNodeType.SaltWater);
    }

    private static void FloodFill(MapGraph.MapNode node, MapGraph.MapNodeType targetType, MapGraph.MapNodeType replacementType)
    {
        if (targetType == replacementType) return;
        if (node.nodeType != targetType) return;
        node.nodeType = replacementType;
        foreach (var neighbor in node.GetNeighborNodes())
        {
            FloodFill(neighbor, targetType, replacementType);
        }
    }

    private static void SetBeaches(MapGraph graph)
    {
        foreach (var node in graph.nodesByCenterPosition.Values)
        {
            if (node.nodeType == MapGraph.MapNodeType.Grass)
            {
                foreach (var neighbor in node.GetNeighborNodes())
                {
                    if (neighbor.nodeType == MapGraph.MapNodeType.SaltWater)
                    {
                        if (node.GetHeightDifference() < 0.8f)
                        {
                            node.nodeType = MapGraph.MapNodeType.Beach;
                        }
                        break;
                    }
                }
            }
        }
    }

    private static void FindCities(MapGraph graph, float minElevation, float maxElevation, int maxCities)
    {
        var preferredElevation = 6f;
        var heightDifferenceWeighting = 4f;

        int cityCount = 0;
        while (cityCount < maxCities)
        {
            var candidate = GetCityCandidate(graph, preferredElevation, heightDifferenceWeighting).FirstOrDefault();
            if (candidate == null) break;
            candidate.nodeType = MapGraph.MapNodeType.City;
            cityCount++;
        }
    }

    private static IOrderedEnumerable<MapGraph.MapNode> GetCityCandidate(MapGraph graph, float preferredElevation, float heightDifferenceWeighting)
    {
        var candidates = graph.nodesByCenterPosition.Values.Where((node) =>
        {
            return node.nodeType == MapGraph.MapNodeType.Grass &&
            node.GetEdges().Any(x => x.water > 0) && // Has a river
            !node.GetNeighborNodes().Any(x => x.nodeType == MapGraph.MapNodeType.City); // Not next to another city
        }).OrderBy((node) =>
        {
            var heightDifference = node.GetHeightDifference();
            var elevation = node.GetElevation();
            return Mathf.Abs(elevation - preferredElevation) + heightDifference * heightDifferenceWeighting;
        });
        return candidates;
    }

    private static void SetNodesToGrass(MapGraph graph)
    {
        foreach (var node in graph.nodesByCenterPosition.Values)
        {
            if (node.nodeType != MapGraph.MapNodeType.Error) node.nodeType = MapGraph.MapNodeType.Grass;
        }
    }

    private static void SetLowNodesToWater(MapGraph graph, float cutoff)
    {
        foreach (var node in graph.nodesByCenterPosition.Values)
        {
            if (node.centerPoint.y <= cutoff)
            {
                var allZero = true;
                foreach (var edge in node.GetEdges())
                {
                    if (edge.destination.position.y > cutoff)
                    {
                        allZero = false;
                        break;
                    }
                }
                if (allZero && node.nodeType != MapGraph.MapNodeType.Error) node.nodeType = MapGraph.MapNodeType.FreshWater;
            }
        }
    }
}
