using System.Collections.Generic;

namespace geo;

internal class PolygonizeNode
{
    internal Coordinate Coordinate;
    internal PlanarNodeKey Key;
    internal int Id;
    internal List<PolygonizeEdge> Edges = new List<PolygonizeEdge>();

    internal PolygonizeNode(Coordinate coordinate, int id)
    {
        Coordinate = RobustLineIntersection.CopyCoordinate(coordinate);
        Key = new PlanarNodeKey(coordinate);
        Coordinate.X = Key.X;
        Coordinate.Y = Key.Y;
        Id = id;
    }
}

internal class PolygonizeEdge
{
    internal Coordinate[] Coordinates;
    internal PolygonizeNode StartNode;
    internal PolygonizeNode EndNode;
    internal int Id;
    internal bool Removed;
    internal bool IsDangle;
    internal bool IsCutEdge;

    internal PolygonizeEdge(
        Coordinate[] coordinates, PolygonizeNode startNode,
        PolygonizeNode endNode, int id)
    {
        Coordinates = CoordinateSequences.CopyCoordinateSequence(coordinates);
        StartNode = startNode;
        EndNode = endNode;
        Id = id;
    }
}

internal class PolygonizeBridgeFrame
{
    internal int NodeId;
    internal int ParentEdgeId;
    internal int NextEdgeIndex;

    internal PolygonizeBridgeFrame(int nodeId, int parentEdgeId)
    {
        NodeId = nodeId;
        ParentEdgeId = parentEdgeId;
    }
}

internal class PolygonizationResult
{
    internal Geometry Polygons = new Geometry();
    internal Geometry Dangles = new Geometry();
    internal Geometry CutEdges = new Geometry();
    internal Geometry InvalidRings = new Geometry();
    internal TopologyRing[] AreaRings = [];
}

internal static class Polygonizer
{
    internal static Geometry PolygonizeGeometry(Geometry geometry)
    {
        return GetPolygonization(geometry).Polygons;
    }

    internal static Geometry PolygonizeFullGeometry(Geometry geometry)
    {
        PolygonizationResult result = GetPolygonization(geometry);
        return GeometryCollections.CreateRawCollection(
            [result.Polygons, result.Dangles,
                result.CutEdges, result.InvalidRings],
            geometry.CoordinateLayout, geometry.SpatialReferenceId);
    }

    internal static PolygonizationResult GetPolygonization(
        Geometry geometry)
    {
        GeometryInspection.EnsureGeometryGraph(geometry);
        List<Coordinate[]> lines = new List<Coordinate[]>();
        CollectLinework(geometry, lines);
        Dictionary<string, PolygonizeNode> nodeIndex =
            new Dictionary<string, PolygonizeNode>();
        List<PolygonizeNode> nodes = new List<PolygonizeNode>();
        List<PolygonizeEdge> edges = new List<PolygonizeEdge>();
        int index = 0;
        while (index < lines.Count)
        {
            AddPolygonizeEdge(
                lines[index], nodeIndex, nodes, edges);
            index++;
        }
        RemoveDangles(nodes);
        MarkCutEdges(nodes, edges);

        List<Coordinate[]> dangles = new List<Coordinate[]>();
        List<Coordinate[]> cutEdges = new List<Coordinate[]>();
        index = 0;
        while (index < edges.Count)
        {
            if (edges[index].IsDangle)
            {
                dangles.Add(edges[index].Coordinates);
            }
            else if (edges[index].IsCutEdge)
            {
                cutEdges.Add(edges[index].Coordinates);
            }
            index++;
        }

        List<TopologyRing> validRings = new List<TopologyRing>();
        List<Coordinate[]> invalidRings = new List<Coordinate[]>();
        TraceRings(edges, validRings, invalidRings);
        Polygon[] faces = BuildFacePolygons(validRings);
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(geometry);
        PolygonizationResult result = new PolygonizationResult();
        result.Polygons = OverlayResultExtractor.CreateResult(
            faces, [], [], geometry, GeometryType.Polygon);
        result.Dangles = CreateDiagnosticGeometry(dangles, options);
        result.CutEdges = CreateDiagnosticGeometry(cutEdges, options);
        result.InvalidRings = CreateDiagnosticGeometry(
            invalidRings, options);
        result.AreaRings = SelectAreaRings(validRings);
        return result;
    }

    internal static void CollectLinework(
        Geometry geometry, List<Coordinate[]> output)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        int index = 0;
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                return;
            case GeometryType.LineString:
                AddUsableLine(((LineString)payload).Coordinates, output);
                return;
            case GeometryType.LinearRing:
                AddUsableLine(((LinearRing)payload).Coordinates, output);
                return;
            case GeometryType.MultiLineString:
                MultiLineString multiLine = (MultiLineString)payload;
                while (index < multiLine.LineStrings.Length)
                {
                    AddUsableLine(
                        multiLine.LineStrings[index].Coordinates, output);
                    index++;
                }
                return;
            case GeometryType.Polygon:
                AddPolygonLinework((Polygon)payload, output);
                return;
            case GeometryType.MultiPolygon:
                MultiPolygon multiPolygon = (MultiPolygon)payload;
                while (index < multiPolygon.Polygons.Length)
                {
                    AddPolygonLinework(
                        multiPolygon.Polygons[index], output);
                    index++;
                }
                return;
            case GeometryType.GeometryCollection:
                GeometryCollection collection = (GeometryCollection)payload;
                while (index < collection.Geometries.Length)
                {
                    CollectLinework(collection.Geometries[index], output);
                    index++;
                }
                return;
            default:
                throw new InvalidGeometryException(
                    "Polygonization does not support extended geometry types.");
        }
    }

    internal static void AddPolygonLinework(
        Polygon polygon, List<Coordinate[]> output)
    {
        AddUsableLine(polygon.Shell.Coordinates, output);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            AddUsableLine(polygon.Holes[index].Coordinates, output);
            index++;
        }
    }

    internal static void AddUsableLine(
        Coordinate[] coordinates, List<Coordinate[]> output)
    {
        if (!GeometrySplitter.HasUsableSegment(coordinates)) return;
        output.Add(SegmentNoding.RemoveRepeatedCoordinateValues(
            coordinates));
    }

    internal static void AddPolygonizeEdge(
        Coordinate[] coordinates,
        Dictionary<string, PolygonizeNode> nodeIndex,
        List<PolygonizeNode> nodes, List<PolygonizeEdge> edges)
    {
        PolygonizeNode start = FindOrAddPolygonizeNode(
            coordinates[0], nodeIndex, nodes);
        PolygonizeNode end = FindOrAddPolygonizeNode(
            coordinates[coordinates.Length - 1], nodeIndex, nodes);
        PolygonizeEdge edge = new PolygonizeEdge(
            coordinates, start, end, edges.Count);
        edges.Add(edge);
        start.Edges.Add(edge);
        end.Edges.Add(edge);
    }

    internal static PolygonizeNode FindOrAddPolygonizeNode(
        Coordinate coordinate,
        Dictionary<string, PolygonizeNode> nodeIndex,
        List<PolygonizeNode> nodes)
    {
        PlanarNodeKey key = new PlanarNodeKey(coordinate);
        if (nodeIndex.ContainsKey(key.Identity))
        {
            PolygonizeNode existing = nodeIndex[key.Identity];
            OrdinateReconciler.ReconcileCoordinateInto(
                existing.Coordinate, coordinate);
            return existing;
        }
        PolygonizeNode node = new PolygonizeNode(
            coordinate, nodes.Count);
        nodes.Add(node);
        nodeIndex.Add(key.Identity, node);
        return node;
    }

    internal static void RemoveDangles(List<PolygonizeNode> nodes)
    {
        Queue<PolygonizeNode> queue = new Queue<PolygonizeNode>();
        int index = 0;
        while (index < nodes.Count)
        {
            if (CountActiveDegree(nodes[index]) == 1)
            {
                queue.Enqueue(nodes[index]);
            }
            index++;
        }
        while (queue.Count != 0)
        {
            PolygonizeNode node = queue.Dequeue();
            if (CountActiveDegree(node) != 1) continue;
            PolygonizeEdge? edge = FindActiveEdge(node);
            if (edge == null) continue;
            edge.Removed = true;
            edge.IsDangle = true;
            PolygonizeNode other = GetOtherNode(edge, node);
            if (CountActiveDegree(other) == 1) queue.Enqueue(other);
        }
    }

    internal static int CountActiveDegree(PolygonizeNode node)
    {
        int count = 0;
        int index = 0;
        while (index < node.Edges.Count)
        {
            if (!node.Edges[index].Removed) count++;
            index++;
        }
        return count;
    }

    internal static PolygonizeEdge? FindActiveEdge(
        PolygonizeNode node)
    {
        int index = 0;
        while (index < node.Edges.Count)
        {
            if (!node.Edges[index].Removed) return node.Edges[index];
            index++;
        }
        return null;
    }

    internal static PolygonizeNode GetOtherNode(
        PolygonizeEdge edge, PolygonizeNode node)
    {
        return edge.StartNode == node ? edge.EndNode : edge.StartNode;
    }

    internal static void MarkCutEdges(
        List<PolygonizeNode> nodes, List<PolygonizeEdge> edges)
    {
        int[] discovery = new int[nodes.Count];
        int[] low = new int[nodes.Count];
        int[] parentEdges = new int[nodes.Count];
        int index = 0;
        while (index < nodes.Count)
        {
            discovery[index] = -1;
            low[index] = -1;
            parentEdges[index] = -1;
            index++;
        }
        int clock = 0;
        index = 0;
        while (index < nodes.Count)
        {
            if (discovery[index] < 0 && CountActiveDegree(nodes[index]) != 0)
            {
                clock = TraverseForCutEdges(
                    nodes[index], nodes, edges, discovery,
                    low, parentEdges, clock);
            }
            index++;
        }
        index = 0;
        while (index < edges.Count)
        {
            if (edges[index].IsCutEdge) edges[index].Removed = true;
            index++;
        }
    }

    internal static int TraverseForCutEdges(
        PolygonizeNode root, List<PolygonizeNode> nodes,
        List<PolygonizeEdge> edges,
        int[] discovery, int[] low, int[] parentEdges, int clock)
    {
        Stack<PolygonizeBridgeFrame> stack =
            new Stack<PolygonizeBridgeFrame>();
        discovery[root.Id] = clock;
        low[root.Id] = clock;
        clock++;
        stack.Push(new PolygonizeBridgeFrame(root.Id, -1));
        while (stack.Count != 0)
        {
            PolygonizeBridgeFrame frame = stack.Peek();
            PolygonizeNode node = nodes[frame.NodeId];
            if (frame.NextEdgeIndex < node.Edges.Count)
            {
                PolygonizeEdge edge = node.Edges[frame.NextEdgeIndex];
                frame.NextEdgeIndex++;
                if (edge.Removed || edge.Id == frame.ParentEdgeId) continue;
                PolygonizeNode other = GetOtherNode(edge, node);
                if (discovery[other.Id] < 0)
                {
                    parentEdges[other.Id] = edge.Id;
                    discovery[other.Id] = clock;
                    low[other.Id] = clock;
                    clock++;
                    stack.Push(new PolygonizeBridgeFrame(
                        other.Id, edge.Id));
                }
                else if (discovery[other.Id] < low[node.Id])
                {
                    low[node.Id] = discovery[other.Id];
                }
            }
            else
            {
                stack.Pop();
                int parentEdgeId = parentEdges[node.Id];
                if (parentEdgeId >= 0)
                {
                    PolygonizeEdge parentEdge = edges[parentEdgeId];
                    PolygonizeNode parent = GetOtherNode(parentEdge, node);
                    if (low[node.Id] > discovery[parent.Id])
                    {
                        parentEdge.IsCutEdge = true;
                    }
                    if (low[node.Id] < low[parent.Id])
                    {
                        low[parent.Id] = low[node.Id];
                    }
                }
            }
        }
        return clock;
    }

    internal static void TraceRings(
        List<PolygonizeEdge> edges, List<TopologyRing> valid,
        List<Coordinate[]> invalid)
    {
        PlanarGraph graph = new PlanarGraph();
        TopologyLabel label = BufferPolygonizer.CreateBoundaryLabel();
        int index = 0;
        while (index < edges.Count)
        {
            if (!edges[index].Removed)
            {
                TopologyEdge graphEdge = graph.AddEdge(
                    edges[index].Coordinates, label);
                graph.SetResultDirection(graphEdge, true, true);
                graph.SetResultDirection(graphEdge, false, true);
            }
            index++;
        }
        if (graph.Edges.Count == 0) return;
        TopologyRing[] rings =
            TopologyRingBuilder.BuildMaximalRings(graph);
        index = 0;
        while (index < rings.Length)
        {
            Coordinate[] coordinates = rings[index].Coordinates;
            if (IsValidRing(coordinates))
            {
                valid.Add(rings[index]);
            }
            else if (!ContainsEquivalentRing(invalid, coordinates))
            {
                invalid.Add(coordinates);
            }
            index++;
        }
    }

    internal static bool IsValidRing(Coordinate[] coordinates)
    {
        if (coordinates.Length < 4) return false;
        if (!RobustLineIntersection.CoordinatesEqualPlanar(
            coordinates[0], coordinates[coordinates.Length - 1]))
        {
            return false;
        }
        if (RingMath.GetExactSignedAreaSign(coordinates) == 0) return false;
        return VisvalingamWhyatt.ClosedRingIsSimple(coordinates);
    }

    internal static bool ContainsEquivalentRing(
        List<Coordinate[]> rings, Coordinate[] target)
    {
        int index = 0;
        while (index < rings.Count)
        {
            if (RingsAreEquivalent(rings[index], target)) return true;
            index++;
        }
        return false;
    }

    internal static bool RingsAreEquivalent(
        Coordinate[] first, Coordinate[] second)
    {
        if (first.Length != second.Length || first.Length < 2) return false;
        int edgeCount = first.Length - 1;
        int start = 0;
        while (start < edgeCount)
        {
            if (RobustLineIntersection.CoordinatesEqualPlanar(
                first[0], second[start]))
            {
                bool forward = true;
                bool reverse = true;
                int offset = 0;
                while (offset < edgeCount)
                {
                    int forwardIndex = start + offset;
                    if (forwardIndex >= edgeCount) forwardIndex -= edgeCount;
                    int reverseIndex = start - offset;
                    if (reverseIndex < 0) reverseIndex += edgeCount;
                    if (!RobustLineIntersection.CoordinatesEqualPlanar(
                        first[offset], second[forwardIndex])) forward = false;
                    if (!RobustLineIntersection.CoordinatesEqualPlanar(
                        first[offset], second[reverseIndex])) reverse = false;
                    offset++;
                }
                if (forward || reverse) return true;
            }
            start++;
        }
        return false;
    }

    internal static Polygon[] BuildFacePolygons(
        List<TopologyRing> validRings)
    {
        List<PolygonShellAssignment> shells =
            new List<PolygonShellAssignment>();
        List<TopologyRing> holes = new List<TopologyRing>();
        int index = 0;
        while (index < validRings.Count)
        {
            TopologyRing ring = validRings[index];
            int sign = RingMath.GetExactSignedAreaSign(ring.Coordinates);
            if (sign > 0)
            {
                PolygonShellAssignment shell =
                    new PolygonShellAssignment();
                shell.Shell = ring;
                shells.Add(shell);
            }
            else
            {
                holes.Add(ring);
            }
            index++;
        }
        index = 0;
        while (index < holes.Count)
        {
            PolygonShellAssignment? shell =
                FindFaceShell(holes[index], shells);
            if (shell != null) shell.Holes.Add(holes[index]);
            index++;
        }
        Polygon[] polygons = new Polygon[shells.Count];
        index = 0;
        while (index < shells.Count)
        {
            polygons[index] =
                TopologyPolygonBuilder.CreatePolygon(shells[index]);
            index++;
        }
        GeometryNormalization.SortPolygonValues(polygons);
        return polygons;
    }

    internal static PolygonShellAssignment? FindFaceShell(
        TopologyRing hole, List<PolygonShellAssignment> shells)
    {
        PolygonShellAssignment? best = null;
        int index = 0;
        while (index < shells.Count)
        {
            PolygonShellAssignment candidate = shells[index];
            if (candidate.Shell.AbsoluteArea > hole.AbsoluteArea &&
                TopologyPolygonBuilder.ContainsRing(candidate.Shell, hole) &&
                (best == null || candidate.Shell.AbsoluteArea <
                    best.Shell.AbsoluteArea))
            {
                best = candidate;
            }
            index++;
        }
        return best;
    }

    internal static TopologyRing[] SelectAreaRings(
        List<TopologyRing> validRings)
    {
        List<TopologyRing> result = new List<TopologyRing>();
        int index = 0;
        while (index < validRings.Count)
        {
            if (RingMath.GetExactSignedAreaSign(
                validRings[index].Coordinates) > 0)
            {
                result.Add(validRings[index]);
            }
            index++;
        }
        return result.ToArray();
    }

    internal static Geometry CreateDiagnosticGeometry(
        List<Coordinate[]> coordinates,
        GeometryFactoryOptions options)
    {
        if (coordinates.Count == 0)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.MultiLineString, options);
        }
        LineString[] lines = new LineString[coordinates.Count];
        int index = 0;
        while (index < coordinates.Count)
        {
            LineString line = new LineString();
            line.Coordinates = CoordinateSequences.CopyCoordinateSequence(
                coordinates[index]);
            lines[index] = line;
            index++;
        }
        GeometryNormalization.SortLineStringValues(lines);
        return GeometryBuilder.CreateMultiLineStringValue(lines, options);
    }
}
