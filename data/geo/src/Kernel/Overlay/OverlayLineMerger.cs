using System.Collections.Generic;

namespace geo;

internal class OverlayLineNode
{
    internal PlanarNodeKey Key;
    internal Coordinate Coordinate;
    internal List<TopologyEdge> Edges = new List<TopologyEdge>();

    internal OverlayLineNode(Coordinate coordinate)
    {
        Key = new PlanarNodeKey(coordinate);
        Coordinate = RobustLineIntersection.CopyCoordinate(coordinate);
        Coordinate.X = Key.X;
        Coordinate.Y = Key.Y;
    }
}

internal static class OverlayLineMerger
{
    internal static LineString[] MergeEdges(TopologyEdge[] edges)
    {
        if (edges == null)
        {
            throw new InvalidGeometryException(
                "Overlay line edge array is null.");
        }
        if (edges.Length == 0) return [];
        Dictionary<string, OverlayLineNode> nodes =
            new Dictionary<string, OverlayLineNode>();
        int maximumEdgeId = -1;
        int edgeIndex = 0;
        while (edgeIndex < edges.Length)
        {
            TopologyEdge edge = edges[edgeIndex];
            if (edge == null || edge.Coordinates.Length < 2)
            {
                throw new TopologyException(
                    "An overlay result line edge is invalid.");
            }
            AddEdgeEndpoint(nodes, edge, edge.Coordinates[0]);
            AddEdgeEndpoint(
                nodes, edge,
                edge.Coordinates[edge.Coordinates.Length - 1]);
            if (edge.Id > maximumEdgeId) maximumEdgeId = edge.Id;
            edgeIndex++;
        }
        OverlayLineNode[] orderedNodes = new OverlayLineNode[nodes.Count];
        int nodeIndex = 0;
        foreach (OverlayLineNode node in nodes.Values)
        {
            node.Edges.Sort((first, second) =>
                TopologyEdgeMerger.CompareEdgeSequences(first, second));
            orderedNodes[nodeIndex] = node;
            nodeIndex++;
        }
        System.Array.Sort(orderedNodes, (first, second) =>
            PlanarNodeKey.CompareKeys(first.Key, second.Key));
        bool[] used = new bool[maximumEdgeId + 1];
        List<LineString> merged = new List<LineString>();
        nodeIndex = 0;
        while (nodeIndex < orderedNodes.Length)
        {
            OverlayLineNode node = orderedNodes[nodeIndex];
            if (node.Edges.Count != 2)
            {
                AddNodePaths(node, nodes, used, merged, edges.Length);
            }
            nodeIndex++;
        }
        nodeIndex = 0;
        while (nodeIndex < orderedNodes.Length)
        {
            AddNodePaths(
                orderedNodes[nodeIndex], nodes,
                used, merged, edges.Length);
            nodeIndex++;
        }
        LineString[] result = merged.ToArray();
        GeometryNormalization.SortLineStringValues(result);
        return result;
    }

    internal static void AddEdgeEndpoint(
        Dictionary<string, OverlayLineNode> nodes,
        TopologyEdge edge, Coordinate coordinate)
    {
        PlanarNodeKey key = new PlanarNodeKey(coordinate);
        OverlayLineNode node;
        if (nodes.ContainsKey(key.Identity))
        {
            node = nodes[key.Identity];
            OrdinateReconciler.ReconcileCoordinateInto(
                node.Coordinate, coordinate);
        }
        else
        {
            node = new OverlayLineNode(coordinate);
            nodes.Add(key.Identity, node);
        }
        node.Edges.Add(edge);
    }

    internal static void AddNodePaths(
        OverlayLineNode start,
        Dictionary<string, OverlayLineNode> nodes,
        bool[] used, List<LineString> merged,
        int maximumPathEdges)
    {
        int edgeIndex = 0;
        while (edgeIndex < start.Edges.Count)
        {
            TopologyEdge edge = start.Edges[edgeIndex];
            if (!used[edge.Id])
            {
                merged.Add(TracePath(
                    start, edge, nodes, used, maximumPathEdges));
            }
            edgeIndex++;
        }
    }

    internal static LineString TracePath(
        OverlayLineNode start, TopologyEdge firstEdge,
        Dictionary<string, OverlayLineNode> nodes,
        bool[] used, int maximumPathEdges)
    {
        List<Coordinate> coordinates = new List<Coordinate>();
        OverlayLineNode currentNode = start;
        TopologyEdge currentEdge = firstEdge;
        int traversed = 0;
        while (traversed < maximumPathEdges)
        {
            AppendDirectedEdge(coordinates, currentEdge, currentNode);
            used[currentEdge.Id] = true;
            OverlayLineNode nextNode = GetOtherNode(
                currentEdge, currentNode, nodes);
            if (nextNode.Edges.Count != 2) break;
            TopologyEdge? nextEdge = FindUnusedEdge(nextNode, used);
            if (nextEdge == null) break;
            currentNode = nextNode;
            currentEdge = nextEdge;
            traversed++;
        }
        if (traversed >= maximumPathEdges)
        {
            throw new TopologyException(
                "Overlay line merging exceeded the selected edge count.");
        }
        LineString line = new LineString();
        line.Coordinates = GeometryNormalization.NormalizeOpenCoordinateSequence(
            coordinates.ToArray());
        return line;
    }

    internal static void AppendDirectedEdge(
        List<Coordinate> output, TopologyEdge edge,
        OverlayLineNode fromNode)
    {
        bool forward = RobustLineIntersection.CoordinatesEqualPlanar(
            fromNode.Coordinate, edge.Coordinates[0]);
        int count = edge.Coordinates.Length;
        int index = output.Count == 0 ? 0 : 1;
        while (index < count)
        {
            int sourceIndex = forward ? index : count - 1 - index;
            output.Add(RobustLineIntersection.CopyCoordinate(
                edge.Coordinates[sourceIndex]));
            index++;
        }
    }

    internal static OverlayLineNode GetOtherNode(
        TopologyEdge edge, OverlayLineNode current,
        Dictionary<string, OverlayLineNode> nodes)
    {
        Coordinate other = RobustLineIntersection.CoordinatesEqualPlanar(
            current.Coordinate, edge.Coordinates[0]) ?
            edge.Coordinates[edge.Coordinates.Length - 1] :
            edge.Coordinates[0];
        PlanarNodeKey key = new PlanarNodeKey(other);
        if (!nodes.ContainsKey(key.Identity))
        {
            throw new TopologyException(
                "An overlay line endpoint is absent from its node map.");
        }
        return nodes[key.Identity];
    }

    internal static TopologyEdge? FindUnusedEdge(
        OverlayLineNode node, bool[] used)
    {
        int index = 0;
        while (index < node.Edges.Count)
        {
            TopologyEdge edge = node.Edges[index];
            if (!used[edge.Id]) return edge;
            index++;
        }
        return null;
    }
}
