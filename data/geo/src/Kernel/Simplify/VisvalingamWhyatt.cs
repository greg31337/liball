using System;
using System.Collections.Generic;

namespace geo;

internal class VisvalingamNode
{
    internal Coordinate Coordinate = new Coordinate();
    internal VisvalingamNode? Previous;
    internal VisvalingamNode? Next;
    internal bool Removed;
    internal bool Fixed;
    internal double Area;
    internal int Generation;
}

internal class VisvalingamQueueEntry
{
    internal VisvalingamNode Node = new VisvalingamNode();
    internal double Area;
    internal int Generation;
}

internal class VisvalingamQueue
{
    internal SortedDictionary<double, List<VisvalingamQueueEntry>> Buckets =
        new SortedDictionary<double, List<VisvalingamQueueEntry>>();

    internal void AddNode(VisvalingamNode node)
    {
        double key = node.Area;
        if (!Buckets.ContainsKey(key))
        {
            Buckets.Add(key, new List<VisvalingamQueueEntry>());
        }
        VisvalingamQueueEntry entry = new VisvalingamQueueEntry();
        entry.Node = node;
        entry.Area = node.Area;
        entry.Generation = node.Generation;
        Buckets[key].Add(entry);
    }

    internal VisvalingamQueueEntry? RemoveMinimum()
    {
        double selectedKey = 0.0;
        List<VisvalingamQueueEntry>? selected = null;
        foreach (KeyValuePair<double, List<VisvalingamQueueEntry>>
            bucket in Buckets)
        {
            selectedKey = bucket.Key;
            selected = bucket.Value;
            break;
        }
        if (selected == null) return null;
        VisvalingamQueueEntry result = selected[0];
        selected.RemoveAt(0);
        if (selected.Count == 0) Buckets.Remove(selectedKey);
        return result;
    }
}

internal static class VisvalingamWhyatt
{
    internal static Geometry SimplifyGeometry(
        Geometry geometry, SimplifyParameters parameters)
    {
        ValidateVisvalingamParameters(parameters);
        GeometryInspection.EnsureGeometryGraph(geometry);
        Geometry result = GeometryCopy.CopyGeometry(geometry);
        SimplifyCopiedGeometry(result, parameters);
        GeometryInspection.EnsureGeometryGraph(result);
        return result;
    }

    internal static void ValidateVisvalingamParameters(
        SimplifyParameters parameters)
    {
        if (parameters == null)
        {
            throw new InvalidGeometryException(
                "Simplification parameters are null.");
        }
        if (parameters.Algorithm !=
            SimplificationAlgorithm.VisvalingamWhyatt)
        {
            throw new InvalidGeometryException(
                "Visvalingam-Whyatt requires its matching algorithm selection.");
        }
        if (!double.IsFinite(parameters.Tolerance) ||
            parameters.Tolerance < 0.0)
        {
            throw new InvalidGeometryException(
                "Visvalingam-Whyatt tolerance must be finite and non-negative.");
        }
    }

    internal static void SimplifyCopiedGeometry(
        Geometry geometry, SimplifyParameters parameters)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        int index = 0;
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                return;
            case GeometryType.LineString:
                LineString line = (LineString)payload;
                line.Coordinates = SimplifyCoordinates(
                    line.Coordinates, parameters.Tolerance,
                    false, parameters.PreserveEndpoints);
                return;
            case GeometryType.LinearRing:
                LinearRing ring = (LinearRing)payload;
                ring.Coordinates = SimplifyCoordinates(
                    ring.Coordinates, parameters.Tolerance, true, true);
                return;
            case GeometryType.Polygon:
                SimplifyPolygonPayload((Polygon)payload, parameters);
                return;
            case GeometryType.MultiLineString:
                MultiLineString multiLine = (MultiLineString)payload;
                while (index < multiLine.LineStrings.Length)
                {
                    LineString component = multiLine.LineStrings[index];
                    component.Coordinates = SimplifyCoordinates(
                        component.Coordinates, parameters.Tolerance,
                        false, parameters.PreserveEndpoints);
                    index++;
                }
                return;
            case GeometryType.MultiPolygon:
                MultiPolygon multiPolygon = (MultiPolygon)payload;
                while (index < multiPolygon.Polygons.Length)
                {
                    SimplifyPolygonPayload(
                        multiPolygon.Polygons[index], parameters);
                    index++;
                }
                return;
            case GeometryType.GeometryCollection:
                GeometryCollection collection =
                    (GeometryCollection)payload;
                while (index < collection.Geometries.Length)
                {
                    SimplifyCopiedGeometry(
                        collection.Geometries[index], parameters);
                    index++;
                }
                return;
            default:
                throw new InvalidGeometryException(
                    "Visvalingam-Whyatt simplification supports core planar geometry.");
        }
    }

    internal static void SimplifyPolygonPayload(
        Polygon polygon, SimplifyParameters parameters)
    {
        Coordinate[] originalShell = polygon.Shell.Coordinates;
        Coordinate[] shell = SimplifyCoordinates(
            originalShell, parameters.Tolerance, true, true);
        LinearRing[] holes = new LinearRing[polygon.Holes.Length];
        int index = 0;
        while (index < holes.Length)
        {
            LinearRing hole = new LinearRing();
            hole.Coordinates = SimplifyCoordinates(
                polygon.Holes[index].Coordinates,
                parameters.Tolerance, true, true);
            holes[index] = hole;
            index++;
        }
        if (!PolygonRingsAreValid(shell, holes)) return;
        polygon.Shell.Coordinates = shell;
        polygon.Holes = holes;
    }

    internal static Coordinate[] SimplifyCoordinates(
        Coordinate[] source, double tolerance,
        bool closed, bool preserveEndpoints)
    {
        Coordinate[] coordinates =
            DouglasPeucker.RemoveAdjacentDuplicates(source, closed);
        int count = coordinates.Length;
        if (closed && count > 1 &&
            RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[0], coordinates[count - 1]))
        {
            count--;
        }
        int minimum = closed ? 3 : 2;
        if (count <= minimum)
        {
            return CreateCoordinateResult(coordinates, count, closed);
        }
        VisvalingamNode[] nodes = new VisvalingamNode[count];
        int index = 0;
        while (index < count)
        {
            VisvalingamNode node = new VisvalingamNode();
            node.Coordinate = coordinates[index];
            node.Fixed = !closed && preserveEndpoints &&
                (index == 0 || index == count - 1);
            nodes[index] = node;
            index++;
        }
        LinkNodes(nodes, closed);
        VisvalingamQueue queue = new VisvalingamQueue();
        index = 0;
        while (index < nodes.Length)
        {
            UpdateNode(nodes[index], 0.0, queue);
            index++;
        }
        int remaining = count;
        while (remaining > minimum)
        {
            VisvalingamQueueEntry? entry = queue.RemoveMinimum();
            if (entry == null) break;
            VisvalingamNode node = entry.Node;
            if (node.Removed || node.Fixed ||
                entry.Generation != node.Generation ||
                entry.Area != node.Area)
            {
                continue;
            }
            if (entry.Area > tolerance) break;
            if (closed && RemovalCreatesIntersection(node, nodes))
            {
                node.Generation++;
                continue;
            }
            VisvalingamNode? previous = node.Previous;
            VisvalingamNode? next = node.Next;
            node.Removed = true;
            if (previous != null) previous.Next = next;
            if (next != null) next.Previous = previous;
            remaining--;
            if (previous != null)
            {
                UpdateNode(previous, entry.Area, queue);
            }
            if (next != null)
            {
                UpdateNode(next, entry.Area, queue);
            }
        }
        return CollectCoordinates(nodes, remaining, closed);
    }

    internal static bool RemovalCreatesIntersection(
        VisvalingamNode node, VisvalingamNode[] nodes)
    {
        if (node.Previous == null || node.Next == null) return true;
        Coordinate start = node.Previous.Coordinate;
        Coordinate end = node.Next.Coordinate;
        int index = 0;
        while (index < nodes.Length)
        {
            VisvalingamNode edgeStart = nodes[index];
            if (!edgeStart.Removed && edgeStart.Next != null &&
                edgeStart != node.Previous && edgeStart != node &&
                SegmentHasInteriorIntersection(
                    start, end, edgeStart.Coordinate,
                    edgeStart.Next.Coordinate))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool SegmentHasInteriorIntersection(
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd)
    {
        SegmentIntersectionResult intersection =
            RobustLineIntersection.GetSegmentIntersection(
                firstStart, firstEnd, secondStart, secondEnd);
        if (intersection.Type == SegmentIntersectionType.None) return false;
        if (intersection.Type == SegmentIntersectionType.ProperPoint ||
            intersection.Type == SegmentIntersectionType.CollinearOverlap)
        {
            return true;
        }
        if (IntersectionWitnessIsInterior(
            intersection.FirstIntersection,
            firstStart, firstEnd, secondStart, secondEnd))
        {
            return true;
        }
        return IntersectionWitnessIsInterior(
            intersection.SecondIntersection,
            firstStart, firstEnd, secondStart, secondEnd);
    }

    internal static bool IntersectionWitnessIsInterior(
        Coordinate? witness,
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd)
    {
        if (witness == null) return false;
        bool firstEndpoint =
            RobustLineIntersection.CoordinatesEqualPlanar(
                witness, firstStart) ||
            RobustLineIntersection.CoordinatesEqualPlanar(
                witness, firstEnd);
        bool secondEndpoint =
            RobustLineIntersection.CoordinatesEqualPlanar(
                witness, secondStart) ||
            RobustLineIntersection.CoordinatesEqualPlanar(
                witness, secondEnd);
        return !firstEndpoint || !secondEndpoint;
    }

    internal static void LinkNodes(
        VisvalingamNode[] nodes, bool closed)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            if (index > 0) nodes[index].Previous = nodes[index - 1];
            if (index + 1 < nodes.Length) nodes[index].Next = nodes[index + 1];
            index++;
        }
        if (closed)
        {
            nodes[0].Previous = nodes[nodes.Length - 1];
            nodes[nodes.Length - 1].Next = nodes[0];
        }
    }

    internal static void UpdateNode(
        VisvalingamNode node, double minimumArea,
        VisvalingamQueue queue)
    {
        node.Generation++;
        if (node.Removed || node.Fixed) return;
        double area = GetNodeArea(node);
        if (area < minimumArea) area = minimumArea;
        node.Area = area;
        queue.AddNode(node);
    }

    internal static double GetNodeArea(VisvalingamNode node)
    {
        if (node.Previous != null && node.Next != null)
        {
            return GetTriangleArea(
                node.Previous.Coordinate, node.Coordinate,
                node.Next.Coordinate);
        }
        if (node.Previous == null && node.Next != null &&
            node.Next.Next != null)
        {
            return GetTriangleArea(
                node.Coordinate, node.Next.Coordinate,
                node.Next.Next.Coordinate);
        }
        if (node.Next == null && node.Previous != null &&
            node.Previous.Previous != null)
        {
            return GetTriangleArea(
                node.Previous.Previous.Coordinate,
                node.Previous.Coordinate, node.Coordinate);
        }
        return double.PositiveInfinity;
    }

    internal static double GetTriangleArea(
        Coordinate first, Coordinate middle, Coordinate last)
    {
        BinaryProduct[] products =
            RobustOrientation.CreateOrientationProducts(
                first.X, first.Y, middle.X, middle.Y,
                last.X, last.Y);
        int[] coefficients = [1, -1, -1, 1, 1, -1];
        ExactDyadicValue determinant =
            ExactDyadicArithmetic.CreateProductSum(
                products, coefficients);
        BinaryProduct[] divisorProducts = [
            RobustDeterminant.CreateBinaryProduct(2.0, 1.0)
        ];
        ExactDyadicValue divisor =
            ExactDyadicArithmetic.CreateProductSum(
                divisorProducts, [1]);
        return Math.Abs(ExactDyadicArithmetic.DivideValues(
            determinant, divisor));
    }

    internal static Coordinate[] CollectCoordinates(
        VisvalingamNode[] nodes, int remaining, bool closed)
    {
        int length = closed ? remaining + 1 : remaining;
        Coordinate[] result = new Coordinate[length];
        int output = 0;
        int index = 0;
        while (index < nodes.Length)
        {
            if (!nodes[index].Removed)
            {
                result[output] = RobustLineIntersection.CopyCoordinate(
                    nodes[index].Coordinate);
                output++;
            }
            index++;
        }
        if (closed)
        {
            result[result.Length - 1] =
                RobustLineIntersection.CopyCoordinate(result[0]);
        }
        return result;
    }

    internal static Coordinate[] CreateCoordinateResult(
        Coordinate[] coordinates, int count, bool closed)
    {
        if (!closed)
        {
            if (coordinates.Length == 1)
            {
                return [
                    RobustLineIntersection.CopyCoordinate(coordinates[0]),
                    RobustLineIntersection.CopyCoordinate(coordinates[0])
                ];
            }
            return CoordinateSequences.CopyCoordinateSequence(coordinates);
        }
        if (count == 0) return [];
        Coordinate[] result = new Coordinate[count + 1];
        int index = 0;
        while (index < count)
        {
            result[index] = RobustLineIntersection.CopyCoordinate(
                coordinates[index]);
            index++;
        }
        result[count] = RobustLineIntersection.CopyCoordinate(result[0]);
        return result;
    }

    internal static bool PolygonRingsAreValid(
        Coordinate[] shell, LinearRing[] holes)
    {
        if (!ClosedRingIsSimple(shell)) return false;
        int index = 0;
        while (index < holes.Length)
        {
            Coordinate[] hole = holes[index].Coordinates;
            if (!ClosedRingIsSimple(hole)) return false;
            if (hole.Length != 0 &&
                RingPointLocation.LocatePointByWinding(
                    hole[0], shell) != TopologicalLocation.Interior)
            {
                return false;
            }
            if (RingsIntersect(shell, hole)) return false;
            int other = 0;
            while (other < index)
            {
                Coordinate[] prior = holes[other].Coordinates;
                if (RingsIntersect(prior, hole)) return false;
                if (prior.Length != 0 && hole.Length != 0 &&
                    (RingPointLocation.LocatePointByWinding(
                        prior[0], hole) != TopologicalLocation.Exterior ||
                    RingPointLocation.LocatePointByWinding(
                        hole[0], prior) != TopologicalLocation.Exterior))
                {
                    return false;
                }
                other++;
            }
            index++;
        }
        return true;
    }

    internal static bool ClosedRingIsSimple(Coordinate[] ring)
    {
        if (ring.Length == 0) return true;
        if (ring.Length < 4) return false;
        int edgeCount = ring.Length - 1;
        int first = 0;
        while (first < edgeCount)
        {
            int second = first + 1;
            while (second < edgeCount)
            {
                bool adjacent = second == first + 1 ||
                    first == 0 && second == edgeCount - 1;
                if (!adjacent)
                {
                    SegmentIntersectionResult intersection =
                        RobustLineIntersection.GetSegmentIntersection(
                            ring[first], ring[first + 1],
                            ring[second], ring[second + 1]);
                    if (intersection.Type != SegmentIntersectionType.None)
                    {
                        return false;
                    }
                }
                second++;
            }
            first++;
        }
        return true;
    }

    internal static bool RingsIntersect(
        Coordinate[] first, Coordinate[] second)
    {
        int firstIndex = 0;
        while (firstIndex + 1 < first.Length)
        {
            int secondIndex = 0;
            while (secondIndex + 1 < second.Length)
            {
                SegmentIntersectionResult intersection =
                    RobustLineIntersection.GetSegmentIntersection(
                        first[firstIndex], first[firstIndex + 1],
                        second[secondIndex], second[secondIndex + 1]);
                if (intersection.Type != SegmentIntersectionType.None)
                {
                    return true;
                }
                secondIndex++;
            }
            firstIndex++;
        }
        return false;
    }
}
