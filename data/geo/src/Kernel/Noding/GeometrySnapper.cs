using System;
using System.Collections.Generic;

namespace geo;

internal static class GeometrySnapper
{
    internal static Geometry SnapGeometry(
        Geometry source, Geometry target, double tolerance)
    {
        ValidateTolerance(tolerance);
        GeometryRelations.ValidateSpatialReferencePair(source, target);
        RelationGeometryParts sourceParts =
            new RelationGeometryParts(source);
        RelationGeometryParts targetParts =
            new RelationGeometryParts(target);
        Coordinate[] targetCoordinates =
            GeometryInspection.ExtractValidatedCoordinates(
                targetParts.Root);
        Geometry result = GeometryCopy.CopyGeometry(sourceParts.Root);
        SnapCopiedGeometry(result, targetCoordinates, tolerance);
        GeometryTransforms.ValidateEditedGeometryStructure(result);
        return result;
    }

    internal static void SnapCopiedGeometry(
        Geometry geometry, Coordinate[] targetCoordinates,
        double tolerance)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                Point point = (Point)payload;
                if (point.Coordinate != null)
                {
                    SnapCoordinateToTargets(
                        point.Coordinate, targetCoordinates, tolerance);
                }
                break;
            case GeometryType.LineString:
                LineString line = (LineString)payload;
                line.Coordinates = SnapCoordinateSequence(
                    line.Coordinates, targetCoordinates,
                    tolerance, false);
                break;
            case GeometryType.LinearRing:
                LinearRing ring = (LinearRing)payload;
                ring.Coordinates = SnapCoordinateSequence(
                    ring.Coordinates, targetCoordinates,
                    tolerance, true);
                break;
            case GeometryType.Polygon:
                SnapPolygon(
                    (Polygon)payload, targetCoordinates, tolerance);
                break;
            case GeometryType.MultiPoint:
                MultiPoint multiPoint = (MultiPoint)payload;
                int pointIndex = 0;
                while (pointIndex < multiPoint.Points.Length)
                {
                    Point component = multiPoint.Points[pointIndex];
                    if (component.Coordinate != null)
                    {
                        SnapCoordinateToTargets(
                            component.Coordinate,
                            targetCoordinates, tolerance);
                    }
                    pointIndex++;
                }
                break;
            case GeometryType.MultiLineString:
                MultiLineString multiLine = (MultiLineString)payload;
                int lineIndex = 0;
                while (lineIndex < multiLine.LineStrings.Length)
                {
                    LineString component =
                        multiLine.LineStrings[lineIndex];
                    component.Coordinates = SnapCoordinateSequence(
                        component.Coordinates, targetCoordinates,
                        tolerance, false);
                    lineIndex++;
                }
                break;
            case GeometryType.MultiPolygon:
                MultiPolygon multiPolygon = (MultiPolygon)payload;
                int polygonIndex = 0;
                while (polygonIndex < multiPolygon.Polygons.Length)
                {
                    SnapPolygon(
                        multiPolygon.Polygons[polygonIndex],
                        targetCoordinates, tolerance);
                    polygonIndex++;
                }
                break;
            case GeometryType.GeometryCollection:
                GeometryCollection collection =
                    (GeometryCollection)payload;
                int geometryIndex = 0;
                while (geometryIndex < collection.Geometries.Length)
                {
                    SnapCopiedGeometry(
                        collection.Geometries[geometryIndex],
                        targetCoordinates, tolerance);
                    geometryIndex++;
                }
                break;
            default:
                throw new InvalidGeometryException(
                    "Snapping currently supports core linear geometry types.");
        }
    }

    internal static void SnapPolygon(
        Polygon polygon, Coordinate[] targetCoordinates,
        double tolerance)
    {
        polygon.Shell.Coordinates = SnapCoordinateSequence(
            polygon.Shell.Coordinates, targetCoordinates,
            tolerance, true);
        if (polygon.Shell.Coordinates.Length == 0)
        {
            polygon.Holes = [];
            return;
        }
        List<LinearRing> holes = new List<LinearRing>();
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            LinearRing hole = polygon.Holes[index];
            hole.Coordinates = SnapCoordinateSequence(
                hole.Coordinates, targetCoordinates,
                tolerance, true);
            if (hole.Coordinates.Length != 0) holes.Add(hole);
            index++;
        }
        polygon.Holes = holes.ToArray();
    }

    internal static Coordinate[] SnapCoordinateSequence(
        Coordinate[] source, Coordinate[] targetCoordinates,
        double tolerance, bool closed)
    {
        if (source.Length == 0) return [];
        Coordinate[] snapped =
            CoordinateSequences.CopyCoordinateSequence(source);
        int index = 0;
        while (index < snapped.Length)
        {
            SnapCoordinateToTargets(
                snapped[index], targetCoordinates, tolerance);
            index++;
        }
        Coordinate[] cleaned =
            CollapsedEdgeCleaner.RemoveCollapsedCoordinates(snapped);
        if (closed)
        {
            if (cleaned.Length < 4 ||
                !GeometryInspection.HasThreeDistinctPlanarCoordinates(
                    cleaned))
            {
                return [];
            }
        }
        else if (cleaned.Length < 2)
        {
            return [];
        }

        SegmentString edge = new SegmentString(cleaned, null, 0);
        index = 0;
        while (index < targetCoordinates.Length)
        {
            SnapTargetVertexToSegments(
                edge, targetCoordinates[index], tolerance);
            index++;
        }
        return MergeSplitCoordinates(SegmentNoding.SplitEdges(edge));
    }

    internal static void SnapCoordinateToTargets(
        Coordinate source, Coordinate[] targets, double tolerance)
    {
        Coordinate? nearest = null;
        double nearestDistance = tolerance;
        int index = 0;
        while (index < targets.Length)
        {
            Coordinate candidate = targets[index];
            double distance = SegmentDistanceAlgorithms.GetPlanarDistance(
                source, candidate);
            if (distance <= nearestDistance && (nearest == null ||
                distance < nearestDistance ||
                RobustLineIntersection.ComparePlanarCoordinates(
                    candidate, nearest) < 0))
            {
                nearest = candidate;
                nearestDistance = distance;
            }
            index++;
        }
        if (nearest != null)
        {
            source.X = nearest.X;
            source.Y = nearest.Y;
        }
    }

    internal static Coordinate[] MergeSplitCoordinates(
        SegmentString[] fragments)
    {
        List<Coordinate> coordinates = new List<Coordinate>();
        int fragmentIndex = 0;
        while (fragmentIndex < fragments.Length)
        {
            Coordinate[] fragment = fragments[fragmentIndex].Coordinates;
            int coordinateIndex = 0;
            while (coordinateIndex < fragment.Length)
            {
                SegmentNoding.AddDistinctCoordinate(
                    coordinates, fragment[coordinateIndex]);
                coordinateIndex++;
            }
            fragmentIndex++;
        }
        return coordinates.ToArray();
    }

    internal static SegmentString[] SnapToTarget(
        SegmentString[] source, SegmentString[] target, double tolerance)
    {
        ValidateTolerance(tolerance);
        SegmentString[] sourceCopy = IndexedNoder.CloneSegmentStrings(source);
        SegmentString[] targetCopy = IndexedNoder.CloneSegmentStrings(target);
        SegmentString[] vertexSnapped = SnapSourceVertices(
            sourceCopy, targetCopy, tolerance);
        int sourceIndex = 0;
        while (sourceIndex < vertexSnapped.Length)
        {
            SegmentString sourceString = vertexSnapped[sourceIndex];
            int targetIndex = 0;
            while (targetIndex < targetCopy.Length)
            {
                Coordinate[] targetCoordinates = targetCopy[targetIndex].Coordinates;
                int vertexIndex = 0;
                while (vertexIndex < targetCoordinates.Length)
                {
                    SnapTargetVertexToSegments(
                        sourceString, targetCoordinates[vertexIndex], tolerance);
                    vertexIndex++;
                }
                targetIndex++;
            }
            sourceIndex++;
        }
        SegmentString[] split = IndexedNoder.SplitGroup(
            vertexSnapped, 0, vertexSnapped.Length);
        return CollapsedEdgeCleaner.CleanEdges(split);
    }

    internal static NodingResult SnapMutual(
        SegmentString[] first, SegmentString[] second, double tolerance)
    {
        SegmentString[] snappedFirst = SnapToTarget(first, second, tolerance);
        SegmentString[] snappedSecond = SnapToTarget(second, snappedFirst, tolerance);
        return IndexedNoder.NodeMutual(snappedFirst, snappedSecond, false);
    }

    internal static SegmentString[] SnapSourceVertices(
        SegmentString[] source, SegmentString[] target, double tolerance)
    {
        List<SegmentString> snapped = new List<SegmentString>();
        int sourceIndex = 0;
        while (sourceIndex < source.Length)
        {
            SegmentString sourceString = source[sourceIndex];
            Coordinate[] coordinates = new Coordinate[sourceString.Coordinates.Length];
            int coordinateIndex = 0;
            while (coordinateIndex < coordinates.Length)
            {
                Coordinate sourceCoordinate = sourceString.Coordinates[coordinateIndex];
                Coordinate? targetCoordinate = FindNearestTargetVertex(
                    sourceCoordinate, target, tolerance);
                coordinates[coordinateIndex] = RobustLineIntersection.CopyCoordinate(
                    sourceCoordinate);
                if (targetCoordinate != null)
                {
                    coordinates[coordinateIndex].X = targetCoordinate.X;
                    coordinates[coordinateIndex].Y = targetCoordinate.Y;
                }
                coordinateIndex++;
            }
            Coordinate[] cleaned = CollapsedEdgeCleaner.RemoveCollapsedCoordinates(
                coordinates);
            if (cleaned.Length >= 2)
            {
                int firstRetainedSegment = FindFirstRetainedSegment(coordinates);
                int lastRetainedSegment = FindLastRetainedSegment(coordinates);
                SegmentString result = new SegmentString(
                    cleaned, sourceString.SourceMetadata, sourceString.SourceIndex);
                result.SourceStartSegment = sourceString.SourceStartSegment +
                    firstRetainedSegment;
                result.SourceEndSegment = sourceString.SourceStartSegment +
                    lastRetainedSegment;
                snapped.Add(result);
            }
            sourceIndex++;
        }
        return snapped.ToArray();
    }

    internal static Coordinate? FindNearestTargetVertex(
        Coordinate source, SegmentString[] target, double tolerance)
    {
        Coordinate? nearest = null;
        double nearestDistance = tolerance;
        int targetIndex = 0;
        while (targetIndex < target.Length)
        {
            Coordinate[] coordinates = target[targetIndex].Coordinates;
            int coordinateIndex = 0;
            while (coordinateIndex < coordinates.Length)
            {
                Coordinate candidate = coordinates[coordinateIndex];
                double distance = SegmentDistanceAlgorithms.GetPlanarDistance(
                    source, candidate);
                if (distance <= nearestDistance && (nearest == null ||
                    distance < nearestDistance ||
                    RobustLineIntersection.ComparePlanarCoordinates(
                        candidate, nearest) < 0))
                {
                    nearest = candidate;
                    nearestDistance = distance;
                }
                coordinateIndex++;
            }
            targetIndex++;
        }
        return nearest;
    }

    internal static int FindFirstRetainedSegment(Coordinate[] coordinates)
    {
        int index = 1;
        while (index < coordinates.Length)
        {
            if (!RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[index - 1], coordinates[index])) return index - 1;
            index++;
        }
        return 0;
    }

    internal static int FindLastRetainedSegment(Coordinate[] coordinates)
    {
        int index = coordinates.Length - 1;
        while (index > 0)
        {
            if (!RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[index - 1], coordinates[index])) return index - 1;
            index--;
        }
        return 0;
    }

    internal static void SnapTargetVertexToSegments(
        SegmentString source, Coordinate targetVertex, double tolerance)
    {
        int segmentIndex = 0;
        while (segmentIndex + 1 < source.Coordinates.Length)
        {
            Coordinate start = source.Coordinates[segmentIndex];
            Coordinate end = source.Coordinates[segmentIndex + 1];
            if (!RobustLineIntersection.CoordinatesEqualPlanar(targetVertex, start) &&
                !RobustLineIntersection.CoordinatesEqualPlanar(targetVertex, end))
            {
                DistanceResult distance = SegmentDistanceAlgorithms.GetNearestPointSegment(
                    targetVertex, start, end);
                if (distance.Distance <= tolerance)
                {
                    SegmentNoding.AddSnappedNode(source, targetVertex, segmentIndex);
                }
            }
            segmentIndex++;
        }
    }

    internal static void ValidateTolerance(double tolerance)
    {
        if (!double.IsFinite(tolerance) || tolerance < 0.0)
        {
            throw new PrecisionException("Snap tolerance must be finite and non-negative.");
        }
    }
}
