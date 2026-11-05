using System;
using System.Collections.Generic;

namespace geo;

internal class DouglasPeuckerRange
{
    internal int First;
    internal int Last;
}

internal static class DouglasPeucker
{
    internal static Geometry SimplifyGeometry(
        Geometry geometry, SimplifyParameters parameters)
    {
        ValidateParameters(parameters);
        GeometryInspection.EnsureGeometryGraph(geometry);
        return SimplifyGeometryValue(geometry, parameters);
    }

    internal static Geometry SimplifyGeometryValue(
        Geometry geometry, SimplifyParameters parameters)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                return GeometryNormalization.CloneGeometryValue(geometry);
            case GeometryType.LineString:
                return SimplifyLineString(
                    (LineString)payload, parameters, options);
            case GeometryType.LinearRing:
                return SimplifyLinearRing(
                    (LinearRing)payload, parameters, options);
            case GeometryType.Polygon:
                return SimplifyPolygon(
                    (Polygon)payload, parameters, options);
            case GeometryType.MultiLineString:
                return SimplifyMultiLineString(
                    (MultiLineString)payload, parameters, options);
            case GeometryType.MultiPolygon:
                return SimplifyMultiPolygon(
                    (MultiPolygon)payload, parameters, options);
            case GeometryType.GeometryCollection:
                return SimplifyGeometryCollection(
                    (GeometryCollection)payload, parameters, options);
            default:
                throw new InvalidGeometryException(
                    "Douglas-Peucker simplification supports core planar geometry.");
        }
    }

    internal static Geometry SimplifyLineString(
        LineString line, SimplifyParameters parameters,
        GeometryFactoryOptions options)
    {
        if (line == null || line.Coordinates == null)
        {
            throw new InvalidGeometryException(
                "Douglas-Peucker line payload is null.");
        }
        Coordinate[] simplified = SimplifyOpenCoordinates(
            line.Coordinates, parameters.Tolerance);
        return GeometryBuilder.CreateLineStringValue(
            simplified, options);
    }

    internal static Geometry SimplifyLinearRing(
        LinearRing ring, SimplifyParameters parameters,
        GeometryFactoryOptions options)
    {
        if (ring == null || ring.Coordinates == null)
        {
            throw new InvalidGeometryException(
                "Douglas-Peucker ring payload is null.");
        }
        if (ring.Coordinates.Length == 0)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.LinearRing, options);
        }
        Coordinate[] simplified = SimplifyRingCoordinates(
            ring.Coordinates, parameters.Tolerance);
        if (simplified.Length < 4)
        {
            if (parameters.KeepCollapsedComponents)
            {
                return CreateCollapsedRingGeometry(
                    simplified, options);
            }
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.LinearRing, options);
        }
        Geometry result = GeometryBuilder.CreateGeometryShell(
            GeometryType.LinearRing, options);
        LinearRing payload = new LinearRing();
        payload.Coordinates =
            CoordinateSequences.CopyCoordinateSequence(simplified);
        result.LinearRing = payload;
        return result;
    }

    internal static Geometry SimplifyPolygon(
        Polygon polygon, SimplifyParameters parameters,
        GeometryFactoryOptions options)
    {
        if (polygon == null || polygon.Shell == null ||
            polygon.Holes == null)
        {
            throw new InvalidGeometryException(
                "Douglas-Peucker polygon payload is incomplete.");
        }
        if (polygon.Shell.Coordinates.Length == 0)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.Polygon, options);
        }
        Coordinate[] shell = SimplifyRingCoordinates(
            polygon.Shell.Coordinates, parameters.Tolerance);
        if (shell.Length < 4)
        {
            if (parameters.KeepCollapsedComponents)
            {
                return CreateCollapsedRingGeometry(shell, options);
            }
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.Polygon, options);
        }
        List<LinearRing> holes = new List<LinearRing>();
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            LinearRing sourceHole = polygon.Holes[index];
            if (sourceHole == null)
            {
                throw new InvalidGeometryException(
                    "Douglas-Peucker polygon hole is null.");
            }
            Coordinate[] hole = SimplifyRingCoordinates(
                sourceHole.Coordinates, parameters.Tolerance);
            if (hole.Length >= 4)
            {
                LinearRing holePayload = new LinearRing();
                holePayload.Coordinates = hole;
                holes.Add(holePayload);
            }
            index++;
        }
        LinearRing shellPayload = new LinearRing();
        shellPayload.Coordinates = shell;
        Geometry result = GeometryBuilder.CreatePolygonValue(
            shellPayload, holes.ToArray(), options);
        return GeometryNormalization.NormalizeGeometryValue(result);
    }

    internal static Geometry SimplifyMultiLineString(
        MultiLineString multiLine, SimplifyParameters parameters,
        GeometryFactoryOptions options)
    {
        if (multiLine == null || multiLine.LineStrings == null)
        {
            throw new InvalidGeometryException(
                "Douglas-Peucker multi-line payload is null.");
        }
        LineString[] lines = new LineString[multiLine.LineStrings.Length];
        int index = 0;
        while (index < lines.Length)
        {
            LineString source = multiLine.LineStrings[index];
            if (source == null)
            {
                throw new InvalidGeometryException(
                    "Douglas-Peucker line component is null.");
            }
            LineString line = new LineString();
            line.Coordinates = SimplifyOpenCoordinates(
                source.Coordinates, parameters.Tolerance);
            lines[index] = line;
            index++;
        }
        return GeometryBuilder.CreateMultiLineStringValue(lines, options);
    }

    internal static Geometry SimplifyMultiPolygon(
        MultiPolygon multiPolygon, SimplifyParameters parameters,
        GeometryFactoryOptions options)
    {
        if (multiPolygon == null || multiPolygon.Polygons == null)
        {
            throw new InvalidGeometryException(
                "Douglas-Peucker multi-polygon payload is null.");
        }
        List<Geometry> results = new List<Geometry>();
        bool polygonsOnly = true;
        int index = 0;
        while (index < multiPolygon.Polygons.Length)
        {
            Polygon source = multiPolygon.Polygons[index];
            Geometry simplified = SimplifyPolygon(
                source, parameters, options);
            if (!GeometryInspection.IsEmptyValue(simplified) ||
                parameters.KeepCollapsedComponents)
            {
                results.Add(simplified);
                if (simplified.GeometryType != GeometryType.Polygon)
                {
                    polygonsOnly = false;
                }
            }
            index++;
        }
        if (polygonsOnly)
        {
            Polygon[] polygons = new Polygon[results.Count];
            index = 0;
            while (index < results.Count)
            {
                Geometry selected = results[index];
                if (selected.Polygon == null)
                {
                    throw new InvalidGeometryException(
                        "Simplified polygon payload is missing.");
                }
                polygons[index] = selected.Polygon;
                index++;
            }
            return GeometryBuilder.CreateMultiPolygonValue(
                polygons, options);
        }
        return GeometryBuilder.CreateGeometryCollectionValue(
            results.ToArray(), options);
    }

    internal static Geometry SimplifyGeometryCollection(
        GeometryCollection collection, SimplifyParameters parameters,
        GeometryFactoryOptions options)
    {
        if (collection == null || collection.Geometries == null)
        {
            throw new InvalidGeometryException(
                "Douglas-Peucker collection payload is null.");
        }
        Geometry[] results = new Geometry[collection.Geometries.Length];
        int index = 0;
        while (index < results.Length)
        {
            results[index] = SimplifyGeometryValue(
                collection.Geometries[index], parameters);
            index++;
        }
        return GeometryBuilder.CreateGeometryCollectionValue(
            results, options);
    }

    internal static Coordinate[] SimplifyOpenCoordinates(
        Coordinate[] source, double tolerance)
    {
        Coordinate[] coordinates = RemoveAdjacentDuplicates(source, false);
        if (coordinates.Length <= 2)
        {
            return CoordinateSequences.CopyCoordinateSequence(coordinates);
        }
        bool[] retained = new bool[coordinates.Length];
        retained[0] = true;
        retained[coordinates.Length - 1] = true;
        List<DouglasPeuckerRange> ranges =
            new List<DouglasPeuckerRange>();
        AddRange(ranges, 0, coordinates.Length - 1);
        while (ranges.Count != 0)
        {
            DouglasPeuckerRange range = ranges[ranges.Count - 1];
            ranges.RemoveAt(ranges.Count - 1);
            int farthest = FindFarthestCoordinate(
                coordinates, range.First, range.Last, tolerance);
            if (farthest >= 0)
            {
                retained[farthest] = true;
                AddRange(ranges, range.First, farthest);
                AddRange(ranges, farthest, range.Last);
            }
        }
        List<Coordinate> result = new List<Coordinate>();
        int index = 0;
        while (index < coordinates.Length)
        {
            if (retained[index])
            {
                result.Add(RobustLineIntersection.CopyCoordinate(
                    coordinates[index]));
            }
            index++;
        }
        return result.ToArray();
    }

    internal static Coordinate[] SimplifyRingCoordinates(
        Coordinate[] source, double tolerance)
    {
        Coordinate[] coordinates = RemoveAdjacentDuplicates(source, true);
        int count = coordinates.Length;
        if (count > 1 && RobustLineIntersection.CoordinatesEqualPlanar(
            coordinates[0], coordinates[count - 1])) count--;
        if (count < 3) return [];
        int[] anchors = FindFarthestPair(coordinates, count);
        Coordinate[] firstChain = CreateRingChain(
            coordinates, count, anchors[0], anchors[1]);
        Coordinate[] secondChain = CreateRingChain(
            coordinates, count, anchors[1], anchors[0]);
        Coordinate[] first = SimplifyOpenCoordinates(
            firstChain, tolerance);
        Coordinate[] second = SimplifyOpenCoordinates(
            secondChain, tolerance);
        List<Coordinate> combined = new List<Coordinate>();
        int index = 0;
        while (index < first.Length)
        {
            combined.Add(first[index]);
            index++;
        }
        index = 1;
        while (index + 1 < second.Length)
        {
            combined.Add(second[index]);
            index++;
        }
        if (combined.Count < 3)
        {
            Coordinate[] collapsed = new Coordinate[combined.Count];
            index = 0;
            while (index < combined.Count)
            {
                collapsed[index] = RobustLineIntersection.CopyCoordinate(
                    combined[index]);
                index++;
            }
            return collapsed;
        }
        combined.Add(RobustLineIntersection.CopyCoordinate(combined[0]));
        return combined.ToArray();
    }

    internal static int[] FindFarthestPair(
        Coordinate[] coordinates, int count)
    {
        int first = 0;
        int index = 1;
        while (index < count)
        {
            if (RobustLineIntersection.ComparePlanarCoordinates(
                coordinates[index], coordinates[first]) < 0)
            {
                first = index;
            }
            index++;
        }
        int second = first == 0 ? 1 : 0;
        ExactDyadicValue best =
            SegmentDistanceAlgorithms.CreateSquaredSegmentLength(
                coordinates[first], coordinates[second]);
        index = 0;
        while (index < count)
        {
            if (index != first)
            {
                ExactDyadicValue candidate =
                    SegmentDistanceAlgorithms.CreateSquaredSegmentLength(
                        coordinates[first], coordinates[index]);
                if (ExactDyadicArithmetic.CompareAbsoluteValues(
                    candidate, best) > 0)
                {
                    second = index;
                    best = candidate;
                }
            }
            index++;
        }
        return [first, second];
    }

    internal static Coordinate[] CreateRingChain(
        Coordinate[] coordinates, int count, int first, int last)
    {
        List<Coordinate> chain = new List<Coordinate>();
        int index = first;
        while (true)
        {
            chain.Add(coordinates[index]);
            if (index == last) break;
            index = (index + 1) % count;
        }
        return chain.ToArray();
    }

    internal static int FindFarthestCoordinate(
        Coordinate[] coordinates, int first, int last,
        double tolerance)
    {
        int farthest = -1;
        double maximum = tolerance;
        int index = first + 1;
        while (index < last)
        {
            DistanceResult distance =
                SegmentDistanceAlgorithms.GetNearestPointSegment(
                    coordinates[index], coordinates[first],
                    coordinates[last]);
            if (distance.Distance > maximum)
            {
                maximum = distance.Distance;
                farthest = index;
            }
            index++;
        }
        return farthest;
    }

    internal static void AddRange(
        List<DouglasPeuckerRange> ranges, int first, int last)
    {
        if (last <= first + 1) return;
        DouglasPeuckerRange range = new DouglasPeuckerRange();
        range.First = first;
        range.Last = last;
        ranges.Add(range);
    }

    internal static Coordinate[] RemoveAdjacentDuplicates(
        Coordinate[] source, bool closed)
    {
        if (source == null)
        {
            throw new InvalidGeometryException(
                "Douglas-Peucker coordinate sequence is null.");
        }
        List<Coordinate> result = new List<Coordinate>();
        int index = 0;
        while (index < source.Length)
        {
            VectorMath.ValidateCoordinateValue(
                source[index], "Douglas-Peucker coordinate");
            if (result.Count == 0 ||
                !RobustLineIntersection.CoordinatesEqualPlanar(
                    result[result.Count - 1], source[index]))
            {
                result.Add(source[index]);
            }
            index++;
        }
        if (closed && result.Count > 1 &&
            !RobustLineIntersection.CoordinatesEqualPlanar(
                result[0], result[result.Count - 1]))
        {
            result.Add(result[0]);
        }
        return result.ToArray();
    }

    internal static Geometry CreateCollapsedRingGeometry(
        Coordinate[] coordinates, GeometryFactoryOptions options)
    {
        if (coordinates.Length == 0)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.GeometryCollection, options);
        }
        if (coordinates.Length == 1)
        {
            return GeometryBuilder.CreatePointValue(
                coordinates[0], options);
        }
        return GeometryBuilder.CreateLineStringValue(
            [coordinates[0], coordinates[coordinates.Length - 1]],
            options);
    }

    internal static void ValidateParameters(SimplifyParameters parameters)
    {
        if (parameters == null)
        {
            throw new InvalidGeometryException(
                "Simplification parameters are null.");
        }
        if (parameters.Algorithm != SimplificationAlgorithm.DouglasPeucker)
        {
            throw new InvalidGeometryException(
                "Douglas-Peucker requires its matching algorithm selection.");
        }
        if (!double.IsFinite(parameters.Tolerance) ||
            parameters.Tolerance < 0.0)
        {
            throw new InvalidGeometryException(
                "Douglas-Peucker tolerance must be finite and non-negative.");
        }
    }
}
