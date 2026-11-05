using System;
using System.Collections.Generic;

namespace geo;

internal class DensifierState
{
    internal int CoordinateCount;
}

internal static class Densifier
{
    internal static Geometry DensifyGeometry(
        Geometry geometry, double maximumSegmentLength,
        PrecisionModel precisionModel)
    {
        ValidateMaximumSegmentLength(maximumSegmentLength);
        GeometryInspection.EnsureGeometryGraph(geometry);
        GeometryFactoryOptions precisionOptions =
            GeometryTransforms.CreatePrecisionOptions(
                geometry, precisionModel);
        Geometry result = GeometryCopy.CopyGeometry(geometry);
        DensifierState state = new DensifierState();
        DensifyCopiedGeometry(
            result, maximumSegmentLength, precisionOptions, state);
        GeometryInspection.EnsureGeometryGraph(result);
        return result;
    }

    internal static void ValidateMaximumSegmentLength(
        double maximumSegmentLength)
    {
        if (!double.IsFinite(maximumSegmentLength) ||
            maximumSegmentLength <= 0.0)
        {
            throw new InvalidGeometryException(
                "Maximum segment length must be finite and positive.");
        }
    }

    internal static void DensifyCopiedGeometry(
        Geometry geometry, double maximumSegmentLength,
        GeometryFactoryOptions precisionOptions,
        DensifierState state)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        int index = 0;
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                Point point = (Point)payload;
                if (point.Coordinate != null)
                {
                    point.Coordinate = ApplyCoordinatePrecision(
                        point.Coordinate, precisionOptions);
                    CountCoordinate(state);
                }
                return;
            case GeometryType.LineString:
                LineString line = (LineString)payload;
                line.Coordinates = DensifyCoordinateSequence(
                    line.Coordinates, maximumSegmentLength,
                    precisionOptions, state, false);
                return;
            case GeometryType.LinearRing:
                LinearRing ring = (LinearRing)payload;
                ring.Coordinates = DensifyCoordinateSequence(
                    ring.Coordinates, maximumSegmentLength,
                    precisionOptions, state, true);
                return;
            case GeometryType.Polygon:
                DensifyPolygonPayload(
                    (Polygon)payload, maximumSegmentLength,
                    precisionOptions, state);
                return;
            case GeometryType.MultiPoint:
                MultiPoint multiPoint = (MultiPoint)payload;
                while (index < multiPoint.Points.Length)
                {
                    Point component = multiPoint.Points[index];
                    if (component.Coordinate != null)
                    {
                        component.Coordinate = ApplyCoordinatePrecision(
                            component.Coordinate, precisionOptions);
                        CountCoordinate(state);
                    }
                    index++;
                }
                return;
            case GeometryType.MultiLineString:
                MultiLineString multiLine = (MultiLineString)payload;
                while (index < multiLine.LineStrings.Length)
                {
                    LineString component = multiLine.LineStrings[index];
                    component.Coordinates = DensifyCoordinateSequence(
                        component.Coordinates, maximumSegmentLength,
                        precisionOptions, state, false);
                    index++;
                }
                return;
            case GeometryType.MultiPolygon:
                MultiPolygon multiPolygon = (MultiPolygon)payload;
                while (index < multiPolygon.Polygons.Length)
                {
                    DensifyPolygonPayload(
                        multiPolygon.Polygons[index],
                        maximumSegmentLength, precisionOptions, state);
                    index++;
                }
                return;
            case GeometryType.GeometryCollection:
                GeometryCollection collection =
                    (GeometryCollection)payload;
                while (index < collection.Geometries.Length)
                {
                    DensifyCopiedGeometry(
                        collection.Geometries[index],
                        maximumSegmentLength, precisionOptions, state);
                    index++;
                }
                return;
            default:
                throw new InvalidGeometryException(
                    "Densification supports core planar geometry.");
        }
    }

    internal static void DensifyPolygonPayload(
        Polygon polygon, double maximumSegmentLength,
        GeometryFactoryOptions precisionOptions,
        DensifierState state)
    {
        polygon.Shell.Coordinates = DensifyCoordinateSequence(
            polygon.Shell.Coordinates, maximumSegmentLength,
            precisionOptions, state, true);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            polygon.Holes[index].Coordinates =
                DensifyCoordinateSequence(
                    polygon.Holes[index].Coordinates,
                    maximumSegmentLength, precisionOptions,
                    state, true);
            index++;
        }
    }

    internal static Coordinate[] DensifyCoordinateSequence(
        Coordinate[] source, double maximumSegmentLength,
        GeometryFactoryOptions precisionOptions,
        DensifierState state, bool closed)
    {
        if (source.Length == 0) return [];
        List<Coordinate> result = new List<Coordinate>();
        AddCoordinate(
            result, source[0], precisionOptions, state, false);
        int segmentIndex = 0;
        while (segmentIndex + 1 < source.Length)
        {
            Coordinate start = source[segmentIndex];
            Coordinate end = source[segmentIndex + 1];
            int subdivisions = GetSubdivisionCount(
                start, end, maximumSegmentLength);
            int subdivisionIndex = 1;
            while (subdivisionIndex <= subdivisions)
            {
                double factor =
                    (double)subdivisionIndex / subdivisions;
                Coordinate coordinate =
                    SegmentDistanceAlgorithms.CreatePointAtSegmentFactor(
                        start, end, factor);
                AddCoordinate(
                    result, coordinate, precisionOptions, state,
                    subdivisionIndex == subdivisions);
                subdivisionIndex++;
            }
            segmentIndex++;
        }
        if (closed)
        {
            result[0] = ApplyCoordinatePrecision(
                source[0], precisionOptions);
            EnsureClosedRing(result, precisionOptions, state);
            ValidateDensifiedRing(result);
        }
        else if (result.Count == 1 && source.Length >= 2)
        {
            result[0] = ApplyCoordinatePrecision(
                source[0], precisionOptions);
            Coordinate endpoint = ApplyCoordinatePrecision(
                source[source.Length - 1], precisionOptions);
            result.Add(endpoint);
            CountCoordinate(state);
        }
        else
        {
            result[0] = ApplyCoordinatePrecision(
                source[0], precisionOptions);
            result[result.Count - 1] = ApplyCoordinatePrecision(
                source[source.Length - 1], precisionOptions);
        }
        return result.ToArray();
    }

    internal static int GetSubdivisionCount(
        Coordinate start, Coordinate end,
        double maximumSegmentLength)
    {
        double distance =
            SegmentDistanceAlgorithms.GetPlanarDistance(start, end);
        if (distance <= maximumSegmentLength) return 1;
        double required = Math.Ceiling(
            distance / maximumSegmentLength);
        if (!double.IsFinite(required) ||
            required > GeometryConstants.DefaultMaximumCoordinates)
        {
            throw new InvalidGeometryException(
                "Densified segment exceeds the coordinate limit.");
        }
        return (int)required;
    }

    internal static void AddCoordinate(
        List<Coordinate> coordinates, Coordinate source,
        GeometryFactoryOptions precisionOptions,
        DensifierState state, bool preferSourceEndpoint)
    {
        Coordinate candidate = ApplyCoordinatePrecision(
            source, precisionOptions);
        if (coordinates.Count != 0 &&
            RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[coordinates.Count - 1], candidate))
        {
            if (preferSourceEndpoint)
            {
                coordinates[coordinates.Count - 1] = candidate;
            }
            return;
        }
        coordinates.Add(candidate);
        CountCoordinate(state);
    }

    internal static Coordinate ApplyCoordinatePrecision(
        Coordinate source, GeometryFactoryOptions precisionOptions)
    {
        VectorMath.ValidateCoordinateValue(
            source, "Densified coordinate");
        Coordinate result = RobustLineIntersection.CopyCoordinate(source);
        result.X = GeometryBuilder.ApplyPrecision(
            source.X, precisionOptions.PrecisionOffsetX,
            precisionOptions);
        result.Y = GeometryBuilder.ApplyPrecision(
            source.Y, precisionOptions.PrecisionOffsetY,
            precisionOptions);
        return result;
    }

    internal static void EnsureClosedRing(
        List<Coordinate> coordinates,
        GeometryFactoryOptions precisionOptions,
        DensifierState state)
    {
        if (coordinates.Count == 0) return;
        Coordinate first = coordinates[0];
        Coordinate last = coordinates[coordinates.Count - 1];
        if (RobustLineIntersection.CoordinatesEqualPlanar(first, last))
        {
            coordinates[coordinates.Count - 1] =
                RobustLineIntersection.CopyCoordinate(first);
            return;
        }
        AddCoordinate(
            coordinates, first, precisionOptions, state, true);
    }

    internal static void ValidateDensifiedRing(
        List<Coordinate> coordinates)
    {
        if (coordinates.Count == 0) return;
        if (coordinates.Count < 4)
        {
            throw new PrecisionException(
                "Precision application collapsed a densified ring.");
        }
        int distinct = 0;
        int index = 0;
        while (index + 1 < coordinates.Count)
        {
            bool found = false;
            int previous = 0;
            while (previous < index)
            {
                if (RobustLineIntersection.CoordinatesEqualPlanar(
                    coordinates[previous], coordinates[index]))
                {
                    found = true;
                    break;
                }
                previous++;
            }
            if (!found) distinct++;
            index++;
        }
        if (distinct < 3)
        {
            throw new PrecisionException(
                "Precision application collapsed a densified ring.");
        }
    }

    internal static void CountCoordinate(DensifierState state)
    {
        if (state.CoordinateCount >=
            GeometryConstants.DefaultMaximumCoordinates)
        {
            throw new InvalidGeometryException(
                "Densified geometry exceeds the coordinate limit.");
        }
        state.CoordinateCount++;
    }
}
