using System;
using System.Collections.Generic;

namespace geo;

internal static class SegmentNoding
{
    internal static SegmentString CreateSegmentString(
        Coordinate[] coordinates, object? sourceMetadata, int sourceIndex)
    {
        if (sourceIndex < 0)
        {
            throw new InvalidGeometryException("Segment string source index cannot be negative.");
        }
        return new SegmentString(coordinates, sourceMetadata, sourceIndex);
    }

    internal static Coordinate[] CopyAndCleanCoordinates(Coordinate[] coordinates)
    {
        Coordinate[] cleaned = RemoveRepeatedCoordinateValues(coordinates);
        if (cleaned.Length < 2)
        {
            throw new InvalidGeometryException(
                "A segment string must contain at least two distinct consecutive coordinates.");
        }
        return cleaned;
    }

    internal static Coordinate[] RemoveRepeatedCoordinateValues(Coordinate[] coordinates)
    {
        if (coordinates == null)
        {
            throw new InvalidGeometryException("Segment string coordinate sequence is null.");
        }
        List<Coordinate> cleaned = new List<Coordinate>();
        int index = 0;
        while (index < coordinates.Length)
        {
            Coordinate coordinate = coordinates[index];
            VectorMath.ValidateCoordinateValue(coordinate, "Segment string coordinate");
            if (cleaned.Count == 0 || !RobustLineIntersection.CoordinatesEqualPlanar(
                cleaned[cleaned.Count - 1], coordinate))
            {
                cleaned.Add(RobustLineIntersection.CopyCoordinate(coordinate));
            }
            index++;
        }
        return cleaned.ToArray();
    }

    internal static SegmentNode AddIntersectionNode(
        SegmentString segmentString, Coordinate intersection, int segmentIndex)
    {
        ValidateSegmentPosition(segmentString, segmentIndex);
        VectorMath.ValidateCoordinateValue(intersection, "Intersection coordinate");
        Coordinate start = segmentString.Coordinates[segmentIndex];
        Coordinate end = segmentString.Coordinates[segmentIndex + 1];
        if (!SegmentDistanceAlgorithms.IsPointInSegmentEnvelope(intersection, start, end))
        {
            throw new InvalidGeometryException("Intersection lies outside the source segment envelope.");
        }
        double factor = 0.0;
        if (RobustLineIntersection.CoordinatesEqualPlanar(intersection, end))
        {
            factor = 1.0;
        }
        else if (!RobustLineIntersection.CoordinatesEqualPlanar(intersection, start))
        {
            factor = SegmentDistanceAlgorithms.GetExactProjectionFactor(
                intersection, start, end);
            if (double.IsNaN(factor)) factor = 0.0;
        }
        factor = SegmentDistanceAlgorithms.ClampSegmentFactor(factor);
        Coordinate projected = SegmentDistanceAlgorithms.CreatePointAtSegmentFactor(
            start, end, factor);
        if (RobustOrientation.GetOrientationIndex(start, end, intersection) == 0)
        {
            projected.X = intersection.X;
            projected.Y = intersection.Y;
        }
        double residual = SegmentDistanceAlgorithms.GetPlanarDistance(intersection, projected);
        double scale = GetPlanarScale(intersection, start, end);
        double tolerance = GeometryConstants.DefaultAbsoluteTolerance +
            GeometryConstants.DefaultRelativeTolerance * scale;
        if (residual > tolerance)
        {
            throw new InvalidGeometryException("Intersection does not lie on the source segment.");
        }
        int canonicalIndex = segmentIndex;
        double canonicalFraction = factor;
        if (factor == 1.0 && segmentIndex + 1 < segmentString.Coordinates.Length - 1)
        {
            canonicalIndex++;
            canonicalFraction = 0.0;
            projected = RobustLineIntersection.CopyCoordinate(
                segmentString.Coordinates[canonicalIndex]);
        }
        SegmentNode node = SegmentNodeList.CreateNode(
            projected, canonicalIndex, canonicalFraction);
        segmentString.Nodes.AddNodeValue(node);
        return FindNodeAtPosition(
            segmentString.Nodes, canonicalIndex, canonicalFraction);
    }

    internal static SegmentNode AddSnappedNode(
        SegmentString segmentString, Coordinate snappedCoordinate, int segmentIndex)
    {
        ValidateSegmentPosition(segmentString, segmentIndex);
        VectorMath.ValidateCoordinateValue(snappedCoordinate, "Snapped coordinate");
        Coordinate start = segmentString.Coordinates[segmentIndex];
        Coordinate end = segmentString.Coordinates[segmentIndex + 1];
        double factor = SegmentDistanceAlgorithms.GetExactProjectionFactor(
            snappedCoordinate, start, end);
        if (double.IsNaN(factor)) factor = 0.0;
        factor = SegmentDistanceAlgorithms.ClampSegmentFactor(factor);
        Coordinate nodeCoordinate = SegmentDistanceAlgorithms.CreatePointAtSegmentFactor(
            start, end, factor);
        nodeCoordinate.X = snappedCoordinate.X;
        nodeCoordinate.Y = snappedCoordinate.Y;
        int canonicalIndex = segmentIndex;
        double canonicalFraction = factor;
        if (RobustLineIntersection.CoordinatesEqualPlanar(snappedCoordinate, start))
        {
            canonicalFraction = 0.0;
            nodeCoordinate = RobustLineIntersection.CopyCoordinate(start);
        }
        else if (RobustLineIntersection.CoordinatesEqualPlanar(snappedCoordinate, end))
        {
            canonicalFraction = 1.0;
            nodeCoordinate = RobustLineIntersection.CopyCoordinate(end);
            if (segmentIndex + 1 < segmentString.Coordinates.Length - 1)
            {
                canonicalIndex++;
                canonicalFraction = 0.0;
                nodeCoordinate = RobustLineIntersection.CopyCoordinate(
                    segmentString.Coordinates[canonicalIndex]);
            }
        }
        SegmentNode node = SegmentNodeList.CreateNode(
            nodeCoordinate, canonicalIndex, canonicalFraction);
        segmentString.Nodes.AddNodeValue(node);
        return FindNodeAtPosition(
            segmentString.Nodes, canonicalIndex, canonicalFraction);
    }

    internal static SegmentIntersectionResult InsertSegmentIntersections(
        SegmentString first, int firstSegmentIndex,
        SegmentString second, int secondSegmentIndex)
    {
        ValidateSegmentPosition(first, firstSegmentIndex);
        ValidateSegmentPosition(second, secondSegmentIndex);
        SegmentIntersectionResult result = RobustLineIntersection.GetSegmentIntersection(
            first.Coordinates[firstSegmentIndex], first.Coordinates[firstSegmentIndex + 1],
            second.Coordinates[secondSegmentIndex], second.Coordinates[secondSegmentIndex + 1]);
        if (result.FirstIntersection != null)
        {
            AddIntersectionNode(first, result.FirstIntersection, firstSegmentIndex);
            AddIntersectionNode(second, result.FirstIntersection, secondSegmentIndex);
        }
        if (result.SecondIntersection != null)
        {
            AddIntersectionNode(first, result.SecondIntersection, firstSegmentIndex);
            AddIntersectionNode(second, result.SecondIntersection, secondSegmentIndex);
        }
        return result;
    }

    internal static SegmentString[] SplitEdges(SegmentString segmentString)
    {
        if (segmentString == null || segmentString.Nodes == null)
        {
            throw new InvalidGeometryException("Segment string or node list is null.");
        }
        SegmentNode[] nodes = segmentString.Nodes.GetSortedNodes();
        List<SegmentString> splits = new List<SegmentString>();
        int index = 0;
        while (index + 1 < nodes.Length)
        {
            SegmentNode start = nodes[index];
            SegmentNode end = nodes[index + 1];
            Coordinate[] coordinates = CreateSplitCoordinates(segmentString, start, end);
            if (coordinates.Length >= 2 && (coordinates.Length > 2 ||
                !RobustLineIntersection.CoordinatesEqualPlanar(
                    coordinates[0], coordinates[coordinates.Length - 1])))
            {
                SegmentString split = new SegmentString(
                    coordinates, segmentString.SourceMetadata, segmentString.SourceIndex);
                split.SourceStartSegment = segmentString.SourceStartSegment +
                    start.SegmentIndex;
                split.SourceEndSegment = segmentString.SourceStartSegment +
                    end.SegmentIndex;
                splits.Add(split);
            }
            index++;
        }
        return splits.ToArray();
    }

    internal static Coordinate[] CreateSplitCoordinates(
        SegmentString segmentString, SegmentNode start, SegmentNode end)
    {
        List<Coordinate> coordinates = new List<Coordinate>();
        AddDistinctCoordinate(coordinates, start.Coordinate);
        int vertexIndex = start.SegmentIndex + 1;
        while (vertexIndex <= end.SegmentIndex)
        {
            if (vertexIndex == end.SegmentIndex && end.SegmentFraction == 0.0) break;
            AddDistinctCoordinate(coordinates, segmentString.Coordinates[vertexIndex]);
            vertexIndex++;
        }
        AddDistinctCoordinate(coordinates, end.Coordinate);
        return coordinates.ToArray();
    }

    internal static void AddDistinctCoordinate(
        List<Coordinate> coordinates, Coordinate coordinate)
    {
        if (coordinates.Count == 0 || !RobustLineIntersection.CoordinatesEqualPlanar(
            coordinates[coordinates.Count - 1], coordinate))
        {
            coordinates.Add(RobustLineIntersection.CopyCoordinate(coordinate));
        }
    }

    internal static SegmentNode FindNodeAtPosition(
        SegmentNodeList nodeList, int segmentIndex, double segmentFraction)
    {
        int index = 0;
        while (index < nodeList.Nodes.Count)
        {
            SegmentNode node = nodeList.Nodes[index];
            if (node.SegmentIndex == segmentIndex &&
                node.SegmentFraction == segmentFraction) return node;
            index++;
        }
        throw new InvalidGeometryException("Inserted segment node could not be found.");
    }

    internal static double GetPlanarScale(
        Coordinate first, Coordinate second, Coordinate third)
    {
        double scale = VectorMath.MaximumAbsolutePlanarOrdinate(first, second, third);
        return scale < 1.0 ? 1.0 : scale;
    }

    internal static void ValidateSegmentPosition(
        SegmentString segmentString, int segmentIndex)
    {
        if (segmentString == null || segmentString.Coordinates == null ||
            segmentString.Nodes == null)
        {
            throw new InvalidGeometryException("Segment string or node list is null.");
        }
        if (segmentIndex < 0 || segmentIndex + 1 >= segmentString.Coordinates.Length)
        {
            throw new InvalidGeometryException("Segment index is outside the segment string.");
        }
    }
}
