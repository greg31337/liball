using System;
using System.Collections.Generic;

namespace geo;

internal class TopologyNode
{
    internal Coordinate Coordinate;
    internal PlanarNodeKey Key;
    internal int Id;
    internal DirectedEdgeStar Star;
    internal bool Visited;

    internal TopologyNode(Coordinate coordinate, int id)
    {
        VectorMath.ValidateCoordinateValue(coordinate, "Topology node coordinate");
        if (id < 0)
        {
            throw new InvalidGeometryException("Topology node identifier cannot be negative.");
        }
        Coordinate = RobustLineIntersection.CopyCoordinate(coordinate);
        Key = new PlanarNodeKey(coordinate);
        Coordinate.X = Key.X;
        Coordinate.Y = Key.Y;
        Id = id;
        Star = new DirectedEdgeStar(this);
    }
}

internal class TopologyEdge
{
    internal Coordinate[] Coordinates;
    internal TopologyLabel Label;
    internal int Id;
    internal TopologyDirectedEdge? Forward;
    internal TopologyDirectedEdge? Reverse;
    internal int MergeCount = 1;

    internal TopologyEdge(
        Coordinate[] coordinates, TopologyLabel label, int id)
    {
        if (coordinates == null || coordinates.Length < 2 || label == null || id < 0)
        {
            throw new InvalidGeometryException(
                "Topology edge coordinates, label, or identifier are invalid.");
        }
        Coordinates = new Coordinate[coordinates.Length];
        int index = 0;
        while (index < coordinates.Length)
        {
            VectorMath.ValidateCoordinateValue(coordinates[index], "Topology edge coordinate");
            Coordinates[index] = RobustLineIntersection.CopyCoordinate(coordinates[index]);
            if (index > 0 && RobustLineIntersection.CoordinatesEqualPlanar(
                Coordinates[index - 1], Coordinates[index]))
            {
                throw new InvalidGeometryException(
                    "Topology edge contains a collapsed consecutive segment.");
            }
            index++;
        }
        Label = label.CopyLabel();
        Id = id;
    }
}

internal class TopologyDirectedEdge
{
    internal TopologyEdge Edge;
    internal TopologyNode FromNode;
    internal TopologyNode ToNode;
    internal bool IsForward;
    internal int Id;
    internal double Angle;
    internal TopologyDirectedEdge? Symmetric;
    internal TopologyDirectedEdge? Next;
    internal TopologyDirectedEdge? NextMinimal;
    internal TopologyRing? Ring;
    internal bool InResult;
    internal bool Visited;
    internal int DepthLeft = -1;
    internal int DepthRight = -1;

    internal TopologyDirectedEdge(
        TopologyEdge edge, TopologyNode fromNode,
        TopologyNode toNode, bool isForward, int id)
    {
        if (edge == null || fromNode == null || toNode == null || id < 0)
        {
            throw new InvalidGeometryException("Directed topology edge is invalid.");
        }
        Edge = edge;
        FromNode = fromNode;
        ToNode = toNode;
        IsForward = isForward;
        Id = id;
        Coordinate directionStart = GetDirectionStart();
        Coordinate directionEnd = GetDirectionEnd();
        Angle = Math.Atan2(
            directionEnd.Y - directionStart.Y,
            directionEnd.X - directionStart.X);
    }

    internal Coordinate GetDirectionStart()
    {
        return IsForward ? Edge.Coordinates[0] :
            Edge.Coordinates[Edge.Coordinates.Length - 1];
    }

    internal Coordinate GetDirectionEnd()
    {
        return IsForward ? Edge.Coordinates[1] :
            Edge.Coordinates[Edge.Coordinates.Length - 2];
    }

    internal Coordinate[] GetDirectedCoordinates()
    {
        Coordinate[] coordinates = new Coordinate[Edge.Coordinates.Length];
        int index = 0;
        while (index < coordinates.Length)
        {
            int sourceIndex = IsForward ? index : coordinates.Length - 1 - index;
            coordinates[index] = RobustLineIntersection.CopyCoordinate(
                Edge.Coordinates[sourceIndex]);
            index++;
        }
        return coordinates;
    }

    internal int GetDirectedDepthDelta(int geometryIndex)
    {
        return Edge.Label.GetDepthDelta(geometryIndex, IsForward);
    }
}

internal class DirectedEdgeStar
{
    internal TopologyNode Owner;
    internal List<TopologyDirectedEdge> Edges = new List<TopologyDirectedEdge>();

    internal DirectedEdgeStar(TopologyNode owner)
    {
        if (owner == null)
        {
            throw new InvalidGeometryException("Directed-edge star owner is null.");
        }
        Owner = owner;
    }

    internal void AddDirectedEdge(TopologyDirectedEdge edge)
    {
        if (edge == null || edge.FromNode != Owner)
        {
            throw new InvalidGeometryException(
                "Directed edge does not originate at the star owner.");
        }
        Edges.Add(edge);
        SortEdges();
    }

    internal void SortEdges()
    {
        Edges.Sort((first, second) => CompareAngularOrder(first, second));
    }

    internal void LinkResultEdges()
    {
        SortEdges();
        int index = 0;
        while (index < Edges.Count)
        {
            TopologyDirectedEdge outgoingReverse = Edges[index];
            TopologyDirectedEdge? incoming = outgoingReverse.Symmetric;
            if (incoming != null && incoming.InResult)
            {
                TopologyDirectedEdge? next = FindClockwiseResultEdge(index);
                if (next == null)
                {
                    throw new TopologyException(
                        "A result edge entering a node has no linked outgoing edge.");
                }
                incoming.Next = next;
            }
            index++;
        }
    }

    internal TopologyDirectedEdge? FindClockwiseResultEdge(int startIndex)
    {
        int step = 1;
        while (step <= Edges.Count)
        {
            int candidateIndex = startIndex - step;
            if (candidateIndex < 0) candidateIndex += Edges.Count;
            TopologyDirectedEdge candidate = Edges[candidateIndex];
            if (candidate.InResult) return candidate;
            step++;
        }
        return null;
    }

    internal static int CompareAngularOrder(
        TopologyDirectedEdge first, TopologyDirectedEdge second)
    {
        if (first.Angle < second.Angle) return -1;
        if (first.Angle > second.Angle) return 1;
        Coordinate firstEnd = first.GetDirectionEnd();
        Coordinate secondEnd = second.GetDirectionEnd();
        int coordinateOrder = RobustLineIntersection.ComparePlanarCoordinates(
            firstEnd, secondEnd);
        if (coordinateOrder != 0) return coordinateOrder;
        return first.Id.CompareTo(second.Id);
    }
}

internal class PlanarGraph
{
    internal List<TopologyNode> Nodes = new List<TopologyNode>();
    internal List<TopologyEdge> Edges = new List<TopologyEdge>();
    internal List<TopologyDirectedEdge> DirectedEdges =
        new List<TopologyDirectedEdge>();
    internal Dictionary<string, TopologyNode> NodeIndex =
        new Dictionary<string, TopologyNode>();

    internal TopologyEdge AddEdge(
        Coordinate[] coordinates, TopologyLabel label)
    {
        TopologyEdge edge = new TopologyEdge(coordinates, label, Edges.Count);
        TopologyNode fromNode = FindOrAddNode(edge.Coordinates[0]);
        TopologyNode toNode = FindOrAddNode(
            edge.Coordinates[edge.Coordinates.Length - 1]);
        TopologyDirectedEdge forward = new TopologyDirectedEdge(
            edge, fromNode, toNode, true, DirectedEdges.Count);
        TopologyDirectedEdge reverse = new TopologyDirectedEdge(
            edge, toNode, fromNode, false, DirectedEdges.Count + 1);
        forward.Symmetric = reverse;
        reverse.Symmetric = forward;
        edge.Forward = forward;
        edge.Reverse = reverse;
        Edges.Add(edge);
        DirectedEdges.Add(forward);
        DirectedEdges.Add(reverse);
        fromNode.Star.AddDirectedEdge(forward);
        toNode.Star.AddDirectedEdge(reverse);
        PropagateNodeOrdinates(fromNode);
        PropagateNodeOrdinates(toNode);
        return edge;
    }

    internal TopologyEdge AddMergedEdge(
        Coordinate[] coordinates, TopologyLabel label)
    {
        return TopologyEdgeMerger.AddMergedEdge(this, coordinates, label);
    }

    internal TopologyNode FindOrAddNode(Coordinate coordinate)
    {
        PlanarNodeKey key = new PlanarNodeKey(coordinate);
        if (NodeIndex.ContainsKey(key.Identity))
        {
            TopologyNode existing = NodeIndex[key.Identity];
            OrdinateReconciler.ReconcileCoordinateInto(
                existing.Coordinate, coordinate);
            PropagateNodeOrdinates(existing);
            return existing;
        }
        TopologyNode node = new TopologyNode(coordinate, Nodes.Count);
        Nodes.Add(node);
        NodeIndex.Add(key.Identity, node);
        return node;
    }

    internal void PropagateNodeOrdinates(TopologyNode node)
    {
        int index = 0;
        while (index < node.Star.Edges.Count)
        {
            TopologyDirectedEdge directed = node.Star.Edges[index];
            int coordinateIndex = directed.IsForward ? 0 :
                directed.Edge.Coordinates.Length - 1;
            Coordinate endpoint = directed.Edge.Coordinates[coordinateIndex];
            endpoint.X = node.Coordinate.X;
            endpoint.Y = node.Coordinate.Y;
            endpoint.Z = node.Coordinate.Z;
            endpoint.M = node.Coordinate.M;
            index++;
        }
    }

    internal TopologyNode[] GetNodesInPlanarOrder()
    {
        TopologyNode[] ordered = Nodes.ToArray();
        System.Array.Sort(ordered, (first, second) =>
            PlanarNodeKey.CompareKeys(first.Key, second.Key));
        return ordered;
    }

    internal TopologyEdge[] GetEdgesInPlanarOrder()
    {
        TopologyEdge[] ordered = Edges.ToArray();
        System.Array.Sort(ordered, (first, second) =>
            TopologyEdgeMerger.CompareEdgeSequences(first, second));
        return ordered;
    }

    internal void SetResultDirection(
        TopologyEdge edge, bool forward, bool inResult)
    {
        if (edge == null || edge.Forward == null || edge.Reverse == null)
        {
            throw new InvalidGeometryException("Result topology edge is invalid.");
        }
        TopologyDirectedEdge directed = forward ? edge.Forward : edge.Reverse;
        directed.InResult = inResult;
    }

    internal void LinkResultDirectedEdges()
    {
        int index = 0;
        while (index < Nodes.Count)
        {
            Nodes[index].Star.LinkResultEdges();
            index++;
        }
    }

    internal void ClearTraversalState()
    {
        int nodeIndex = 0;
        while (nodeIndex < Nodes.Count)
        {
            Nodes[nodeIndex].Visited = false;
            nodeIndex++;
        }
        int edgeIndex = 0;
        while (edgeIndex < DirectedEdges.Count)
        {
            DirectedEdges[edgeIndex].Visited = false;
            edgeIndex++;
        }
    }
}
