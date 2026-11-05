using System.Collections.Generic;

namespace geo;

internal class LineMergeNode
{
    internal PlanarNodeKey Key;
    internal Coordinate Coordinate;
    internal List<LineMergeEdge> Edges = new List<LineMergeEdge>();
    internal List<LineMergeEdge> Incoming = new List<LineMergeEdge>();
    internal List<LineMergeEdge> Outgoing = new List<LineMergeEdge>();

    internal LineMergeNode(Coordinate coordinate)
    {
        Key = new PlanarNodeKey(coordinate);
        Coordinate = RobustLineIntersection.CopyCoordinate(coordinate);
        Coordinate.X = Key.X;
        Coordinate.Y = Key.Y;
    }
}

internal class LineMergeEdge
{
    internal Coordinate[] Coordinates;
    internal LineMergeNode StartNode;
    internal LineMergeNode EndNode;
    internal int SourceIndex;
    internal bool Used;

    internal LineMergeEdge(
        Coordinate[] coordinates, LineMergeNode startNode,
        LineMergeNode endNode, int sourceIndex)
    {
        Coordinates = CoordinateSequences.CopyCoordinateSequence(coordinates);
        StartNode = startNode;
        EndNode = endNode;
        SourceIndex = sourceIndex;
    }
}

internal static class LineMerger
{
    internal static Geometry MergeGeometry(
        Geometry geometry, bool directed)
    {
        GeometryInspection.EnsureGeometryGraph(geometry);
        List<Coordinate[]> lines = new List<Coordinate[]>();
        CollectLineCoordinates(geometry, lines);
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(geometry);
        if (lines.Count == 0)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.GeometryCollection, options);
        }
        Dictionary<string, LineMergeNode> nodeIndex =
            new Dictionary<string, LineMergeNode>();
        List<LineMergeEdge> edges = new List<LineMergeEdge>();
        int index = 0;
        while (index < lines.Count)
        {
            AddLineEdge(lines[index], nodeIndex, edges, index);
            index++;
        }
        LineMergeNode[] nodes = CreateOrderedNodes(nodeIndex);
        List<LineString> merged = directed ?
            MergeDirectedPaths(nodes, edges) :
            MergeUndirectedPaths(nodes, edges);
        return CreateResultGeometry(merged, options, directed);
    }

    internal static void CollectLineCoordinates(
        Geometry geometry, List<Coordinate[]> lines)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        int index = 0;
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
            case GeometryType.Polygon:
            case GeometryType.MultiPolygon:
                return;
            case GeometryType.LineString:
                LineString line = (LineString)payload;
                if (line.Coordinates.Length != 0)
                {
                    lines.Add(line.Coordinates);
                }
                return;
            case GeometryType.LinearRing:
                LinearRing ring = (LinearRing)payload;
                if (ring.Coordinates.Length != 0)
                {
                    lines.Add(ring.Coordinates);
                }
                return;
            case GeometryType.MultiLineString:
                MultiLineString multiLine = (MultiLineString)payload;
                while (index < multiLine.LineStrings.Length)
                {
                    if (multiLine.LineStrings[index].Coordinates.Length != 0)
                    {
                        lines.Add(
                            multiLine.LineStrings[index].Coordinates);
                    }
                    index++;
                }
                return;
            case GeometryType.GeometryCollection:
                GeometryCollection collection =
                    (GeometryCollection)payload;
                while (index < collection.Geometries.Length)
                {
                    CollectLineCoordinates(
                        collection.Geometries[index], lines);
                    index++;
                }
                return;
            default:
                throw new InvalidGeometryException(
                    "Line merge does not support extended geometry types.");
        }
    }

    internal static void AddLineEdge(
        Coordinate[] source,
        Dictionary<string, LineMergeNode> nodeIndex,
        List<LineMergeEdge> edges, int sourceIndex)
    {
        Coordinate[] coordinates =
            DouglasPeucker.RemoveAdjacentDuplicates(source, false);
        if (coordinates.Length == 1)
        {
            coordinates = [
                RobustLineIntersection.CopyCoordinate(source[0]),
                RobustLineIntersection.CopyCoordinate(
                    source[source.Length - 1])
            ];
        }
        LineMergeNode start = FindOrAddMergeNode(
            coordinates[0], nodeIndex);
        LineMergeNode end = FindOrAddMergeNode(
            coordinates[coordinates.Length - 1], nodeIndex);
        LineMergeEdge edge = new LineMergeEdge(
            coordinates, start, end, sourceIndex);
        edges.Add(edge);
        start.Edges.Add(edge);
        end.Edges.Add(edge);
        start.Outgoing.Add(edge);
        end.Incoming.Add(edge);
    }

    internal static LineMergeNode FindOrAddMergeNode(
        Coordinate coordinate,
        Dictionary<string, LineMergeNode> nodeIndex)
    {
        PlanarNodeKey key = new PlanarNodeKey(coordinate);
        if (nodeIndex.ContainsKey(key.Identity))
        {
            return nodeIndex[key.Identity];
        }
        LineMergeNode node = new LineMergeNode(coordinate);
        nodeIndex.Add(key.Identity, node);
        return node;
    }

    internal static LineMergeNode[] CreateOrderedNodes(
        Dictionary<string, LineMergeNode> nodeIndex)
    {
        LineMergeNode[] nodes = new LineMergeNode[nodeIndex.Count];
        int index = 0;
        foreach (LineMergeNode node in nodeIndex.Values)
        {
            node.Edges.Sort((first, second) =>
                first.SourceIndex.CompareTo(second.SourceIndex));
            node.Incoming.Sort((first, second) =>
                first.SourceIndex.CompareTo(second.SourceIndex));
            node.Outgoing.Sort((first, second) =>
                first.SourceIndex.CompareTo(second.SourceIndex));
            nodes[index] = node;
            index++;
        }
        System.Array.Sort(nodes, (first, second) =>
            PlanarNodeKey.CompareKeys(first.Key, second.Key));
        return nodes;
    }

    internal static List<LineString> MergeUndirectedPaths(
        LineMergeNode[] nodes, List<LineMergeEdge> edges)
    {
        List<LineString> result = new List<LineString>();
        int nodeIndex = 0;
        while (nodeIndex < nodes.Length)
        {
            LineMergeNode node = nodes[nodeIndex];
            if (node.Edges.Count != 2)
            {
                int edgeIndex = 0;
                while (edgeIndex < node.Edges.Count)
                {
                    LineMergeEdge edge = node.Edges[edgeIndex];
                    if (!edge.Used)
                    {
                        result.Add(TraceUndirectedPath(
                            node, edge, edges.Count));
                    }
                    edgeIndex++;
                }
            }
            nodeIndex++;
        }
        nodeIndex = 0;
        while (nodeIndex < nodes.Length)
        {
            int edgeIndex = 0;
            while (edgeIndex < nodes[nodeIndex].Edges.Count)
            {
                LineMergeEdge edge =
                    nodes[nodeIndex].Edges[edgeIndex];
                if (!edge.Used)
                {
                    result.Add(TraceUndirectedPath(
                        nodes[nodeIndex], edge, edges.Count));
                }
                edgeIndex++;
            }
            nodeIndex++;
        }
        return result;
    }

    internal static LineString TraceUndirectedPath(
        LineMergeNode start, LineMergeEdge first,
        int maximumEdges)
    {
        List<Coordinate> coordinates = new List<Coordinate>();
        LineMergeNode node = start;
        LineMergeEdge edge = first;
        int traversed = 0;
        while (true)
        {
            bool forward = edge.StartNode == node;
            AppendEdgeCoordinates(coordinates, edge, forward);
            edge.Used = true;
            traversed++;
            if (traversed > maximumEdges)
            {
                throw new TopologyException(
                    "Undirected line merge exceeded the edge count.");
            }
            LineMergeNode next = GetOtherMergeNode(edge, node);
            if (next.Edges.Count != 2) break;
            LineMergeEdge? continuation =
                FindUnusedIncident(next);
            if (continuation == null) break;
            node = next;
            edge = continuation;
        }
        LineString line = new LineString();
        line.Coordinates =
            GeometryNormalization.NormalizeOpenCoordinateSequence(
                coordinates.ToArray());
        return line;
    }

    internal static LineMergeEdge? FindUnusedIncident(
        LineMergeNode node)
    {
        int index = 0;
        while (index < node.Edges.Count)
        {
            if (!node.Edges[index].Used) return node.Edges[index];
            index++;
        }
        return null;
    }

    internal static LineMergeNode GetOtherMergeNode(
        LineMergeEdge edge, LineMergeNode node)
    {
        return edge.StartNode == node ? edge.EndNode : edge.StartNode;
    }

    internal static List<LineString> MergeDirectedPaths(
        LineMergeNode[] nodes, List<LineMergeEdge> edges)
    {
        List<LineString> result = new List<LineString>();
        int nodeIndex = 0;
        while (nodeIndex < nodes.Length)
        {
            LineMergeNode node = nodes[nodeIndex];
            if (!IsDirectedContinuationNode(node))
            {
                int edgeIndex = 0;
                while (edgeIndex < node.Outgoing.Count)
                {
                    LineMergeEdge edge = node.Outgoing[edgeIndex];
                    if (!edge.Used)
                    {
                        result.Add(TraceDirectedPath(
                            edge, edges.Count));
                    }
                    edgeIndex++;
                }
            }
            nodeIndex++;
        }
        nodeIndex = 0;
        while (nodeIndex < nodes.Length)
        {
            int edgeIndex = 0;
            while (edgeIndex < nodes[nodeIndex].Outgoing.Count)
            {
                LineMergeEdge edge =
                    nodes[nodeIndex].Outgoing[edgeIndex];
                if (!edge.Used)
                {
                    result.Add(TraceDirectedPath(edge, edges.Count));
                }
                edgeIndex++;
            }
            nodeIndex++;
        }
        return result;
    }

    internal static bool IsDirectedContinuationNode(
        LineMergeNode node)
    {
        return node.Edges.Count == 2 &&
            node.Incoming.Count == 1 && node.Outgoing.Count == 1;
    }

    internal static LineString TraceDirectedPath(
        LineMergeEdge first, int maximumEdges)
    {
        List<Coordinate> coordinates = new List<Coordinate>();
        LineMergeEdge edge = first;
        int traversed = 0;
        while (true)
        {
            AppendEdgeCoordinates(coordinates, edge, true);
            edge.Used = true;
            traversed++;
            if (traversed > maximumEdges)
            {
                throw new TopologyException(
                    "Directed line merge exceeded the edge count.");
            }
            LineMergeNode next = edge.EndNode;
            if (!IsDirectedContinuationNode(next)) break;
            LineMergeEdge? continuation = FindUnusedOutgoing(next);
            if (continuation == null) break;
            edge = continuation;
        }
        LineString line = new LineString();
        line.Coordinates = coordinates.ToArray();
        return line;
    }

    internal static LineMergeEdge? FindUnusedOutgoing(
        LineMergeNode node)
    {
        int index = 0;
        while (index < node.Outgoing.Count)
        {
            if (!node.Outgoing[index].Used)
            {
                return node.Outgoing[index];
            }
            index++;
        }
        return null;
    }

    internal static void AppendEdgeCoordinates(
        List<Coordinate> output, LineMergeEdge edge,
        bool forward)
    {
        int count = edge.Coordinates.Length;
        int index = output.Count == 0 ? 0 : 1;
        while (index < count)
        {
            if (output.Count >=
                GeometryConstants.DefaultMaximumCoordinates)
            {
                throw new InvalidGeometryException(
                    "Merged line exceeds the coordinate limit.");
            }
            int sourceIndex = forward ?
                index : count - 1 - index;
            output.Add(RobustLineIntersection.CopyCoordinate(
                edge.Coordinates[sourceIndex]));
            index++;
        }
    }

    internal static Geometry CreateResultGeometry(
        List<LineString> merged,
        GeometryFactoryOptions options, bool directed)
    {
        if (merged.Count == 0)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.GeometryCollection, options);
        }
        LineString[] lines = merged.ToArray();
        if (!directed)
        {
            int index = 0;
            while (index < lines.Length)
            {
                lines[index].Coordinates =
                    GeometryNormalization.NormalizeOpenCoordinateSequence(
                        lines[index].Coordinates);
                index++;
            }
        }
        GeometryNormalization.SortLineStringValues(lines);
        if (lines.Length == 1)
        {
            return GeometryBuilder.CreateLineStringValue(
                lines[0].Coordinates, options);
        }
        return GeometryBuilder.CreateMultiLineStringValue(lines, options);
    }
}
