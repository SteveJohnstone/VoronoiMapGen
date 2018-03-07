using System;
using System.Linq;
using UnityEngine;

public partial class MapGraph
{
    /// <summary>
    /// Represents a half-edge
    /// </summary>
    public class MapNodeHalfEdge
    {
        public MapPoint destination { get; set; }
        /// <summary>
        /// The next half-edge that shares the same map node (face)
        /// </summary>
        public MapNodeHalfEdge next { get; set; }
        /// <summary>
        /// The previous half-edge that shares the same map node (face)
        /// </summary>
        public MapNodeHalfEdge previous { get; set; }
        /// <summary>
        /// The other half of this half-edge, with a different map node (face)
        /// </summary>
        public MapNodeHalfEdge opposite { get; set; }
        /// <summary>
        /// The map node that this edge borders on
        /// </summary>
        public MapNode node;
        internal int water;

        public Vector3 GetStartPosition()
        {
            return previous.destination.position;
        }

        public Vector3 GetEndPosition()
        {
            return destination.position;
        }

        public override string ToString()
        {
            return "HalfEdge: " + previous.destination.position  + " -> " + destination.position;
        }

        public MapNodeHalfEdge GetDownSlopeEdge()
        {
            var corner = GetLowestCorner();

            return corner.GetEdges().Where(x => x.destination.position.y <= corner.position.y && x != this).OrderBy(x => x.destination.position.y).FirstOrDefault();
        }

        public MapPoint GetLowestCorner()
        {
            return destination.position.y < previous.destination.position.y ? destination : previous.destination;
        }

        public void AddWater()
        {
            water++;
        }

        public float GetSlopeAngle()
        {
            var vector = destination.position - previous.destination.position;
            var direction = new Vector3(vector.x, 0f, vector.z);
            var angle = Vector3.Angle(direction, vector);
            return angle;
        }
    }
}