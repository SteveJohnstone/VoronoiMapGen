using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public partial class MapGraph
{
    /// <summary>
    /// Represents a half-edge vertex
    /// </summary>
    public class MapPoint
    {
        public Vector3 position { get; set; }
        /// <summary>
        /// The half-edge that starts at this point
        /// </summary>
        public MapNodeHalfEdge leavingEdge { get; set; }

        public MapNodeHalfEdge GetDownSlopeEdge()
        {
            return GetEdges().Where(x => x.destination.position.y <= position.y).OrderBy(x => x.destination.position.y).FirstOrDefault();
        }

        public IEnumerable<MapNodeHalfEdge> GetEdges()
        {
            var firstEdge = leavingEdge;
            var nextEdge = firstEdge;

            var maxIterations = 20;
            var iterations = 0;

            do
            {
                yield return nextEdge;
                nextEdge = nextEdge.opposite == null ? null : nextEdge.opposite.next;
                iterations++;
            }
            while (nextEdge != firstEdge && nextEdge != null && iterations < maxIterations);
        }

        public List<MapNode> GetNodes()
        {
            return GetEdges().Select(x => x.node).ToList();
        }

        public MapNode GetLowestNode()
        {
            MapNode lowestNode = null;
            foreach (var node in GetNodes())
            {
                if (lowestNode == null || node.centerPoint.y < lowestNode.centerPoint.y) lowestNode = node;
            }
            return lowestNode;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", base.ToString(), position);
        }
    }
}