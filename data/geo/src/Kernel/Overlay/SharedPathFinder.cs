using System.Collections.Generic;

namespace geo;

internal static class SharedPathFinder
{
    internal static Geometry GetSharedPaths(
        Geometry first, Geometry second)
    {
        GeometryRelations.ValidateSpatialReferencePair(first, second);
        GeometrySplitter.ValidateLinealSource(first);
        GeometrySplitter.ValidateLinealSource(second);
        RelationGeometryParts firstParts =
            new RelationGeometryParts(first);
        RelationGeometryParts secondParts =
            new RelationGeometryParts(second);
        SegmentString[] firstEdges =
            GeometrySplitter.CreateSourceEdges(firstParts);
        SegmentString[] secondEdges =
            GeometrySplitter.CreateSourceEdges(secondParts);
        NodingResult noded = IndexedNoder.NodeMutual(
            firstEdges, secondEdges, false);
        List<LineString> forward = new List<LineString>();
        List<LineString> reverse = new List<LineString>();
        ExtractSharedPaths(
            noded.First, noded.Second, forward, reverse);
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(first);
        Geometry forwardGeometry =
            GeometryBuilder.CreateMultiLineStringValue(
                forward.ToArray(), options);
        Geometry reverseGeometry =
            GeometryBuilder.CreateMultiLineStringValue(
                reverse.ToArray(), options);
        return GeometryCollections.CreateRawCollection(
            [forwardGeometry, reverseGeometry],
            first.CoordinateLayout, first.SpatialReferenceId);
    }

    internal static void ExtractSharedPaths(
        SegmentString[] first, SegmentString[] second,
        List<LineString> forward, List<LineString> reverse)
    {
        List<Coordinate> current = new List<Coordinate>();
        int currentDirection = 0;
        int currentSource = -1;
        int index = 0;
        while (index < first.Length)
        {
            SegmentString fragment = first[index];
            int direction = ClassifyFragment(fragment, second);
            bool continues = direction != 0 &&
                direction == currentDirection &&
                fragment.SourceIndex == currentSource &&
                current.Count != 0 &&
                RobustLineIntersection.CoordinatesEqualPlanar(
                    current[current.Count - 1],
                    fragment.Coordinates[0]);
            if (!continues)
            {
                AddCurrentPath(
                    current, currentDirection, forward, reverse);
                current = new List<Coordinate>();
            }
            if (direction != 0)
            {
                AppendCoordinates(current, fragment.Coordinates);
                currentDirection = direction;
                currentSource = fragment.SourceIndex;
            }
            else
            {
                currentDirection = 0;
                currentSource = -1;
            }
            index++;
        }
        AddCurrentPath(current, currentDirection, forward, reverse);
    }

    internal static int ClassifyFragment(
        SegmentString first, SegmentString[] second)
    {
        int direction = 0;
        int segmentIndex = 0;
        while (segmentIndex + 1 < first.Coordinates.Length)
        {
            int segmentDirection = ClassifySegment(
                first.Coordinates[segmentIndex],
                first.Coordinates[segmentIndex + 1], second);
            if (segmentDirection == 0) return 0;
            if (direction == 0)
            {
                direction = segmentDirection;
            }
            else if (direction != segmentDirection)
            {
                return 0;
            }
            segmentIndex++;
        }
        return direction;
    }

    internal static int ClassifySegment(
        Coordinate firstStart, Coordinate firstEnd,
        SegmentString[] second)
    {
        bool reverse = false;
        int edgeIndex = 0;
        while (edgeIndex < second.Length)
        {
            Coordinate[] coordinates = second[edgeIndex].Coordinates;
            int segmentIndex = 0;
            while (segmentIndex + 1 < coordinates.Length)
            {
                Coordinate secondStart = coordinates[segmentIndex];
                Coordinate secondEnd = coordinates[segmentIndex + 1];
                if (SegmentDistanceAlgorithms.IsPointOnSegment(
                    firstStart, secondStart, secondEnd) &&
                    SegmentDistanceAlgorithms.IsPointOnSegment(
                    firstEnd, secondStart, secondEnd))
                {
                    if (HasSameDirection(
                        firstStart, firstEnd,
                        secondStart, secondEnd))
                    {
                        return 1;
                    }
                    reverse = true;
                }
                segmentIndex++;
            }
            edgeIndex++;
        }
        return reverse ? -1 : 0;
    }

    internal static bool HasSameDirection(
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd)
    {
        if (firstStart.X != firstEnd.X)
        {
            return (firstEnd.X > firstStart.X) ==
                (secondEnd.X > secondStart.X);
        }
        return (firstEnd.Y > firstStart.Y) ==
            (secondEnd.Y > secondStart.Y);
    }

    internal static void AppendCoordinates(
        List<Coordinate> output, Coordinate[] coordinates)
    {
        int index = 0;
        while (index < coordinates.Length)
        {
            SegmentNoding.AddDistinctCoordinate(
                output, coordinates[index]);
            index++;
        }
    }

    internal static void AddCurrentPath(
        List<Coordinate> coordinates, int direction,
        List<LineString> forward, List<LineString> reverse)
    {
        if (coordinates.Count < 2 || direction == 0) return;
        LineString line = new LineString();
        line.Coordinates = CoordinateSequences.CopyCoordinateSequence(
            coordinates.ToArray());
        if (direction > 0)
        {
            forward.Add(line);
        }
        else
        {
            reverse.Add(line);
        }
    }
}
