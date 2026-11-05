using System;
using System.Collections.Generic;

namespace geo;

internal class RepresentativePointState
{
    internal Coordinate? SurfacePoint;
    internal double SurfaceWidth = -1.0;
    internal Coordinate? LinePoint;
    internal double LineLength = -1.0;
    internal Coordinate? Point;
}

internal static class RepresentativePointAlgorithm
{
    internal static Coordinate? GetPointOnSurface(Geometry geometry)
    {
        return GetPointOnSurfaceWithCurveParameters(
            geometry, new CurveLinearizationParameters());
    }

    internal static Coordinate? GetPointOnSurfaceWithCurveParameters(
        Geometry geometry, CurveLinearizationParameters parameters)
    {
        GeometryInspection.ExtractValidatedCoordinates(geometry);
        CurveLinearizer.ValidateParameters(parameters);
        RepresentativePointState state = new RepresentativePointState();
        AddGeometryValue(geometry, parameters, state);
        if (state.SurfacePoint != null)
        {
            return CoordinateSequences.CopyCoordinateValue(state.SurfacePoint);
        }
        if (state.LinePoint != null)
        {
            return CoordinateSequences.CopyCoordinateValue(state.LinePoint);
        }
        if (state.Point != null)
        {
            return CoordinateSequences.CopyCoordinateValue(state.Point);
        }
        return null;
    }

    internal static void AddGeometryValue(
        Geometry geometry, CurveLinearizationParameters parameters,
        RepresentativePointState state)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                AddPointPayload((Point)payload, state);
                return;
            case GeometryType.LineString:
                Coordinate[] lineCoordinates = ((LineString)payload).Coordinates;
                ValidateLineCoordinates(lineCoordinates);
                AddLineCoordinates(lineCoordinates, state);
                return;
            case GeometryType.LinearRing:
                Coordinate[] ringCoordinates = ((LinearRing)payload).Coordinates;
                GeometryBuilder.ValidateLinearRingCoordinates(ringCoordinates);
                AddLineCoordinates(ringCoordinates, state);
                return;
            case GeometryType.Polygon:
                AddPolygonPayload((Polygon)payload, state);
                return;
            case GeometryType.Triangle:
                AddTrianglePayload((Triangle)payload, state);
                return;
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
                Coordinate[] curveCoordinates =
                    CurveLinearizer.LinearizeCurveGeometry(
                        geometry, parameters);
                AddLineCoordinates(curveCoordinates, state);
                return;
            case GeometryType.CurvePolygon:
                AddCurvePolygonPayload(
                    (CurvePolygon)payload, parameters, state);
                return;
            default:
                break;
        }
        Geometry[] children = ComponentWalker.GetDirectChildren(geometry);
        int index = 0;
        while (index < children.Length)
        {
            AddGeometryValue(children[index], parameters, state);
            index++;
        }
    }

    internal static void AddPointPayload(
        Point point, RepresentativePointState state)
    {
        if (point == null)
        {
            throw new InvalidGeometryException("Point payload is null.");
        }
        if (state.Point == null && point.Coordinate != null)
        {
            state.Point = CoordinateSequences.CopyCoordinateValue(
                point.Coordinate);
        }
    }

    internal static void AddLineCoordinates(
        Coordinate[] coordinates, RepresentativePointState state)
    {
        if (coordinates.Length == 0) return;
        if (state.Point == null)
        {
            state.Point = CoordinateSequences.CopyCoordinateValue(
                coordinates[0]);
        }
        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            Coordinate start = coordinates[index];
            Coordinate end = coordinates[index + 1];
            double length = SegmentDistanceAlgorithms.GetPlanarDistance(
                start, end);
            if (length > state.LineLength)
            {
                state.LineLength = length;
                state.LinePoint = SegmentDistanceAlgorithms.CreatePointAtSegmentFactor(
                    start, end, 0.5);
            }
            index++;
        }
    }

    internal static void AddPolygonPayload(
        Polygon polygon, RepresentativePointState state)
    {
        ValidatePolygonPayload(polygon);
        AddLineCoordinates(polygon.Shell.Coordinates, state);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            AddLineCoordinates(polygon.Holes[index].Coordinates, state);
            index++;
        }
        AddPolygonInteriorCandidate(polygon, state);
    }

    internal static void AddTrianglePayload(
        Triangle triangle, RepresentativePointState state)
    {
        if (triangle == null || triangle.Shell == null)
        {
            throw new InvalidGeometryException(
                "Triangle payload or shell is null.");
        }
        GeometryBuilder.ValidateLinearRingCoordinates(
            triangle.Shell.Coordinates);
        Polygon polygon = new Polygon();
        polygon.Shell = triangle.Shell;
        AddPolygonPayload(polygon, state);
    }

    internal static void AddCurvePolygonPayload(
        CurvePolygon curvePolygon, CurveLinearizationParameters parameters,
        RepresentativePointState state)
    {
        if (curvePolygon == null || curvePolygon.Holes == null)
        {
            throw new InvalidGeometryException(
                "CurvePolygon payload or hole array is null.");
        }
        if (curvePolygon.Shell == null)
        {
            if (curvePolygon.Holes.Length != 0)
            {
                throw new InvalidGeometryException(
                    "An empty curve polygon shell cannot contain holes.");
            }
            return;
        }
        Polygon polygon = new Polygon();
        polygon.Shell = new LinearRing
        {
            Coordinates = CurveLinearizer.LinearizeCurveGeometry(
                curvePolygon.Shell, parameters)
        };
        polygon.Holes = new LinearRing[curvePolygon.Holes.Length];
        int index = 0;
        while (index < curvePolygon.Holes.Length)
        {
            polygon.Holes[index] = new LinearRing
            {
                Coordinates = CurveLinearizer.LinearizeCurveGeometry(
                    curvePolygon.Holes[index], parameters)
            };
            index++;
        }
        AddPolygonPayload(polygon, state);
    }

    internal static void AddPolygonInteriorCandidate(
        Polygon polygon, RepresentativePointState state)
    {
        List<double> yOrdinates = new List<double>();
        AddRingYOrdinates(polygon.Shell.Coordinates, yOrdinates);
        int holeIndex = 0;
        while (holeIndex < polygon.Holes.Length)
        {
            AddRingYOrdinates(
                polygon.Holes[holeIndex].Coordinates, yOrdinates);
            holeIndex++;
        }
        yOrdinates.Sort();
        int index = 0;
        while (index + 1 < yOrdinates.Count)
        {
            double lower = yOrdinates[index];
            double upper = yOrdinates[index + 1];
            if (lower < upper)
            {
                double y = RobustLineIntersection.InterpolateLineOrdinate(
                    lower, upper, 0.5);
                if (y > lower && y < upper)
                {
                    AddScanlineCandidates(polygon, y, state);
                }
            }
            index++;
        }
    }

    internal static void AddScanlineCandidates(
        Polygon polygon, double y, RepresentativePointState state)
    {
        List<double> intersections = new List<double>();
        AddRingScanlineIntersections(
            polygon.Shell.Coordinates, y, intersections);
        int holeIndex = 0;
        while (holeIndex < polygon.Holes.Length)
        {
            AddRingScanlineIntersections(
                polygon.Holes[holeIndex].Coordinates, y, intersections);
            holeIndex++;
        }
        intersections.Sort();
        int index = 0;
        while (index + 1 < intersections.Count)
        {
            double left = intersections[index];
            double right = intersections[index + 1];
            if (left < right)
            {
                double x = RobustLineIntersection.InterpolateLineOrdinate(
                    left, right, 0.5);
                if (x > left && x < right)
                {
                    Coordinate candidate = new Coordinate { X = x, Y = y };
                    if (GeometryPointLocation.LocatePointInPolygon(
                        candidate, polygon) == TopologicalLocation.Interior)
                    {
                        double width = GetHorizontalWidth(left, right);
                        if (width > state.SurfaceWidth)
                        {
                            state.SurfaceWidth = width;
                            state.SurfacePoint = candidate;
                        }
                    }
                }
            }
            index += 2;
        }
    }

    internal static void AddRingYOrdinates(
        Coordinate[] coordinates, List<double> ordinates)
    {
        int index = 0;
        while (index < coordinates.Length)
        {
            ordinates.Add(coordinates[index].Y);
            index++;
        }
    }

    internal static void AddRingScanlineIntersections(
        Coordinate[] coordinates, double y, List<double> intersections)
    {
        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            Coordinate start = coordinates[index];
            Coordinate end = coordinates[index + 1];
            if (start.Y < y && end.Y > y || end.Y < y && start.Y > y)
            {
                double factor = (y - start.Y) / (end.Y - start.Y);
                intersections.Add(
                    RobustLineIntersection.InterpolateLineOrdinate(
                        start.X, end.X, factor));
            }
            index++;
        }
    }

    internal static double GetHorizontalWidth(double left, double right)
    {
        double width = right - left;
        if (double.IsFinite(width)) return width;
        Coordinate first = new Coordinate { X = left, Y = 0.0 };
        Coordinate second = new Coordinate { X = right, Y = 0.0 };
        return SegmentDistanceAlgorithms.GetPlanarDistance(first, second);
    }

    internal static void ValidatePolygonPayload(Polygon polygon)
    {
        if (polygon == null || polygon.Shell == null || polygon.Holes == null)
        {
            throw new InvalidGeometryException(
                "Polygon payload, shell, or hole array is null.");
        }
        GeometryBuilder.ValidateLinearRingCoordinates(
            polygon.Shell.Coordinates);
        if (polygon.Shell.Coordinates.Length == 0 && polygon.Holes.Length != 0)
        {
            throw new InvalidGeometryException(
                "An empty polygon shell cannot contain holes.");
        }
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            LinearRing hole = polygon.Holes[index];
            if (hole == null)
            {
                throw new InvalidGeometryException("Polygon hole is null.");
            }
            GeometryBuilder.ValidateLinearRingCoordinates(hole.Coordinates);
            if (hole.Coordinates.Length == 0)
            {
                throw new InvalidGeometryException(
                    "A polygon hole cannot be empty.");
            }
            index++;
        }
    }

    internal static void ValidateLineCoordinates(Coordinate[] coordinates)
    {
        if (coordinates == null)
        {
            throw new InvalidGeometryException(
                "LineString coordinate sequence is null.");
        }
        if (coordinates.Length == 1)
        {
            throw new InvalidGeometryException(
                "A LineString must be empty or contain at least two coordinates.");
        }
    }
}
