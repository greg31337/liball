using System;
using System.Collections.Generic;

namespace geo;

internal class TopologyRing
{
    internal Coordinate[] Coordinates = [];
    internal TopologyDirectedEdge[] DirectedEdges = [];
    internal bool IsMaximal;
    internal bool IsHole;
    internal double AbsoluteArea;
}

internal static class TopologyRingBuilder
{
    internal static TopologyRing[] BuildMaximalRings(PlanarGraph graph)
    {
        if (graph == null)
        {
            throw new InvalidGeometryException("Ring-building planar graph is null.");
        }
        graph.LinkResultDirectedEdges();
        int resetIndex = 0;
        while (resetIndex < graph.DirectedEdges.Count)
        {
            TopologyDirectedEdge edge = graph.DirectedEdges[resetIndex];
            edge.Visited = false;
            edge.Ring = null;
            edge.NextMinimal = null;
            resetIndex++;
        }
        List<TopologyRing> rings = new List<TopologyRing>();
        int index = 0;
        while (index < graph.DirectedEdges.Count)
        {
            TopologyDirectedEdge start = graph.DirectedEdges[index];
            if (start.InResult && !start.Visited)
            {
                rings.Add(TraceMaximalRing(start, graph.DirectedEdges.Count));
            }
            index++;
        }
        return rings.ToArray();
    }

    internal static TopologyRing TraceMaximalRing(
        TopologyDirectedEdge start, int maximumEdgeCount)
    {
        if (start == null || maximumEdgeCount < 1)
        {
            throw new TopologyException("Maximal-ring traversal input is invalid.");
        }
        List<TopologyDirectedEdge> edges =
            new List<TopologyDirectedEdge>();
        TopologyDirectedEdge current = start;
        int guard = 0;
        while (guard <= maximumEdgeCount)
        {
            if (current.Visited && current != start)
            {
                throw new TopologyException(
                    "Result-edge links merge before closing a maximal ring.");
            }
            edges.Add(current);
            current.Visited = true;
            if (current.Next == null)
            {
                throw new TopologyException("A maximal ring contains an unlinked result edge.");
            }
            current = current.Next;
            if (current == start) break;
            guard++;
        }
        if (current != start)
        {
            throw new TopologyException("Maximal-ring traversal exceeded the graph edge count.");
        }
        Coordinate[] coordinates = CreateRingCoordinates(edges);
        TopologyRing ring = CreateCoordinateRing(coordinates, true);
        ring.DirectedEdges = edges.ToArray();
        int index = 0;
        while (index < edges.Count)
        {
            edges[index].Ring = ring;
            index++;
        }
        return ring;
    }

    internal static Coordinate[] CreateRingCoordinates(
        List<TopologyDirectedEdge> edges)
    {
        List<Coordinate> coordinates = new List<Coordinate>();
        int edgeIndex = 0;
        while (edgeIndex < edges.Count)
        {
            Coordinate[] edgeCoordinates = edges[edgeIndex].GetDirectedCoordinates();
            int coordinateIndex = 0;
            while (coordinateIndex + 1 < edgeCoordinates.Length)
            {
                coordinates.Add(RobustLineIntersection.CopyCoordinate(
                    edgeCoordinates[coordinateIndex]));
                coordinateIndex++;
            }
            edgeIndex++;
        }
        if (coordinates.Count > 0)
        {
            coordinates.Add(RobustLineIntersection.CopyCoordinate(coordinates[0]));
        }
        return coordinates.ToArray();
    }

    internal static TopologyRing[] BuildMinimalRings(TopologyRing[] maximalRings)
    {
        if (maximalRings == null)
        {
            throw new InvalidGeometryException("Maximal-ring array is null.");
        }
        List<TopologyRing> minimal = new List<TopologyRing>();
        int index = 0;
        while (index < maximalRings.Length)
        {
            TopologyRing maximal = maximalRings[index];
            if (maximal == null)
            {
                throw new InvalidGeometryException("Maximal ring is null.");
            }
            SplitRepeatedNodeCycles(maximal.Coordinates, minimal);
            int edgeIndex = 0;
            while (edgeIndex < maximal.DirectedEdges.Length)
            {
                maximal.DirectedEdges[edgeIndex].NextMinimal =
                    maximal.DirectedEdges[edgeIndex].Next;
                edgeIndex++;
            }
            index++;
        }
        return minimal.ToArray();
    }

    internal static void SplitRepeatedNodeCycles(
        Coordinate[] coordinates, List<TopologyRing> output)
    {
        ValidateClosedRing(coordinates);
        List<Coordinate> path = new List<Coordinate>();
        int index = 0;
        while (index < coordinates.Length)
        {
            Coordinate coordinate = coordinates[index];
            int repeatedIndex = FindPlanarCoordinate(path, coordinate);
            if (repeatedIndex < 0)
            {
                path.Add(RobustLineIntersection.CopyCoordinate(coordinate));
            }
            else
            {
                List<Coordinate> cycle = new List<Coordinate>();
                int cycleIndex = repeatedIndex;
                while (cycleIndex < path.Count)
                {
                    cycle.Add(RobustLineIntersection.CopyCoordinate(path[cycleIndex]));
                    cycleIndex++;
                }
                cycle.Add(RobustLineIntersection.CopyCoordinate(coordinate));
                if (cycle.Count >= 4 &&
                    RingMath.GetRingOrientation(cycle.ToArray()) != RingOrientation.Undefined)
                {
                    output.Add(CreateCoordinateRing(cycle.ToArray(), false));
                }
                while (path.Count > repeatedIndex + 1)
                {
                    path.RemoveAt(path.Count - 1);
                }
            }
            index++;
        }
    }

    internal static int FindPlanarCoordinate(
        List<Coordinate> coordinates, Coordinate target)
    {
        int index = 0;
        while (index < coordinates.Count)
        {
            if (RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[index], target)) return index;
            index++;
        }
        return -1;
    }

    internal static TopologyRing CreateCoordinateRing(
        Coordinate[] coordinates, bool maximal)
    {
        ValidateClosedRing(coordinates);
        TopologyRing ring = new TopologyRing();
        ring.Coordinates = new Coordinate[coordinates.Length];
        int index = 0;
        while (index < coordinates.Length)
        {
            ring.Coordinates[index] = RobustLineIntersection.CopyCoordinate(
                coordinates[index]);
            index++;
        }
        ring.IsMaximal = maximal;
        double area = RingMath.GetSignedArea(ring.Coordinates);
        ring.AbsoluteArea = Math.Abs(area);
        ring.IsHole = area < 0.0;
        return ring;
    }

    internal static void ValidateClosedRing(Coordinate[] coordinates)
    {
        RingMath.ValidateRingCoordinates(coordinates);
        if (coordinates.Length < 4 ||
            !RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[0], coordinates[coordinates.Length - 1]))
        {
            throw new TopologyException(
                "A topology ring must be closed and contain at least four coordinates.");
        }
    }
}
