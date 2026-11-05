using System;

namespace geo;

internal class CentroidCoordinateSum
{
    internal bool HasOrigin;
    internal double OriginX;
    internal double OriginY;
    internal CompensatedMeasureSum Weight = new CompensatedMeasureSum();
    internal CompensatedMeasureSum OffsetX = new CompensatedMeasureSum();
    internal CompensatedMeasureSum OffsetY = new CompensatedMeasureSum();

    internal void AddWeightedCoordinate(Coordinate coordinate, double weight)
    {
        if (weight == 0.0) return;
        if (!HasOrigin)
        {
            HasOrigin = true;
            OriginX = coordinate.X;
            OriginY = coordinate.Y;
        }
        Weight.AddValue(weight);
        OffsetX.AddValue((coordinate.X - OriginX) * weight);
        OffsetY.AddValue((coordinate.Y - OriginY) * weight);
    }

    internal Coordinate? CreateCoordinate()
    {
        double weight = Weight.GetValue();
        if (!HasOrigin || weight == 0.0) return null;
        Coordinate coordinate = new Coordinate();
        coordinate.X = VectorMath.RequireFiniteResult(
            OriginX + OffsetX.GetValue() / weight, "Centroid X ordinate");
        coordinate.Y = VectorMath.RequireFiniteResult(
            OriginY + OffsetY.GetValue() / weight, "Centroid Y ordinate");
        return coordinate;
    }
}

internal class RingCentroidValue
{
    internal double Area;
    internal Coordinate? Coordinate;
}

internal class CentroidState
{
    internal CentroidCoordinateSum Area = new CentroidCoordinateSum();
    internal CentroidCoordinateSum Line = new CentroidCoordinateSum();
    internal CentroidCoordinateSum Point = new CentroidCoordinateSum();
}

internal static class CentroidAccumulator
{
    internal static Coordinate? GetCentroid(Geometry geometry)
    {
        return GetCentroidWithCurveParameters(
            geometry, new CurveLinearizationParameters());
    }

    internal static Coordinate? GetCentroidWithCurveParameters(
        Geometry geometry, CurveLinearizationParameters parameters)
    {
        GeometryInspection.ExtractValidatedCoordinates(geometry);
        CurveLinearizer.ValidateParameters(parameters);
        CentroidState state = new CentroidState();
        AccumulateGeometryValue(geometry, parameters, state);
        Coordinate? coordinate = state.Area.CreateCoordinate();
        if (coordinate != null) return coordinate;
        coordinate = state.Line.CreateCoordinate();
        if (coordinate != null) return coordinate;
        return state.Point.CreateCoordinate();
    }

    internal static void AccumulateGeometryValue(
        Geometry geometry, CurveLinearizationParameters parameters,
        CentroidState state)
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
                AddLinearizedCurve(geometry, parameters, state);
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
            AccumulateGeometryValue(children[index], parameters, state);
            index++;
        }
    }

    internal static void AddPointPayload(Point point, CentroidState state)
    {
        if (point == null)
        {
            throw new InvalidGeometryException("Point payload is null.");
        }
        if (point.Coordinate != null)
        {
            state.Point.AddWeightedCoordinate(point.Coordinate, 1.0);
        }
    }

    internal static void AddLinearizedCurve(
        Geometry curve, CurveLinearizationParameters parameters,
        CentroidState state)
    {
        Coordinate[] coordinates = CurveLinearizer.LinearizeCurveGeometry(
            curve, parameters);
        AddLineCoordinates(coordinates, state);
    }

    internal static void AddPolygonPayload(
        Polygon polygon, CentroidState state)
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
        AddSurfaceRing(polygon.Shell.Coordinates, 1.0, state);
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
            AddSurfaceRing(hole.Coordinates, -1.0, state);
            index++;
        }
    }

    internal static void AddTrianglePayload(
        Triangle triangle, CentroidState state)
    {
        if (triangle == null || triangle.Shell == null)
        {
            throw new InvalidGeometryException(
                "Triangle payload or shell is null.");
        }
        GeometryBuilder.ValidateLinearRingCoordinates(
            triangle.Shell.Coordinates);
        AddSurfaceRing(triangle.Shell.Coordinates, 1.0, state);
    }

    internal static void AddCurvePolygonPayload(
        CurvePolygon polygon, CurveLinearizationParameters parameters,
        CentroidState state)
    {
        if (polygon == null || polygon.Holes == null)
        {
            throw new InvalidGeometryException(
                "CurvePolygon payload or hole array is null.");
        }
        if (polygon.Shell == null)
        {
            if (polygon.Holes.Length != 0)
            {
                throw new InvalidGeometryException(
                    "An empty curve polygon shell cannot contain holes.");
            }
            return;
        }
        Coordinate[] shell = CurveLinearizer.LinearizeCurveGeometry(
            polygon.Shell, parameters);
        GeometryBuilder.ValidateLinearRingCoordinates(shell);
        AddSurfaceRing(shell, 1.0, state);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            Coordinate[] hole = CurveLinearizer.LinearizeCurveGeometry(
                polygon.Holes[index], parameters);
            GeometryBuilder.ValidateLinearRingCoordinates(hole);
            if (hole.Length == 0)
            {
                throw new InvalidGeometryException(
                    "A curve polygon hole cannot be empty.");
            }
            AddSurfaceRing(hole, -1.0, state);
            index++;
        }
    }

    internal static void AddSurfaceRing(
        Coordinate[] coordinates, double areaDirection,
        CentroidState state)
    {
        AddLineCoordinates(coordinates, state);
        RingCentroidValue ring = GetRingCentroid(coordinates);
        if (ring.Coordinate != null)
        {
            state.Area.AddWeightedCoordinate(
                ring.Coordinate, areaDirection * ring.Area);
        }
    }

    internal static RingCentroidValue GetRingCentroid(
        Coordinate[] coordinates)
    {
        RingCentroidValue result = new RingCentroidValue();
        if (coordinates.Length < 3) return result;
        Coordinate origin = coordinates[0];
        CompensatedMeasureSum twiceArea = new CompensatedMeasureSum();
        CompensatedMeasureSum xNumerator = new CompensatedMeasureSum();
        CompensatedMeasureSum yNumerator = new CompensatedMeasureSum();
        int index = 1;
        while (index + 1 < coordinates.Length)
        {
            double firstX = coordinates[index].X - origin.X;
            double firstY = coordinates[index].Y - origin.Y;
            double secondX = coordinates[index + 1].X - origin.X;
            double secondY = coordinates[index + 1].Y - origin.Y;
            double cross = firstX * secondY - firstY * secondX;
            twiceArea.AddValue(cross);
            xNumerator.AddValue((firstX + secondX) * cross);
            yNumerator.AddValue((firstY + secondY) * cross);
            index++;
        }
        double areaTwice = twiceArea.GetValue();
        if (areaTwice == 0.0) return result;
        Coordinate coordinate = new Coordinate();
        coordinate.X = VectorMath.RequireFiniteResult(
            origin.X + xNumerator.GetValue() / (3.0 * areaTwice),
            "Ring centroid X ordinate");
        coordinate.Y = VectorMath.RequireFiniteResult(
            origin.Y + yNumerator.GetValue() / (3.0 * areaTwice),
            "Ring centroid Y ordinate");
        result.Area = Math.Abs(areaTwice) / 2.0;
        result.Coordinate = coordinate;
        return result;
    }

    internal static void AddLineCoordinates(
        Coordinate[] coordinates, CentroidState state)
    {
        if (coordinates.Length != 0)
        {
            state.Point.AddWeightedCoordinate(coordinates[0], 1.0);
        }
        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            Coordinate first = coordinates[index];
            Coordinate second = coordinates[index + 1];
            double length = SegmentDistanceAlgorithms.GetPlanarDistance(
                first, second);
            if (length != 0.0)
            {
                Coordinate midpoint = new Coordinate();
                midpoint.X = GetMidpointOrdinate(first.X, second.X);
                midpoint.Y = GetMidpointOrdinate(first.Y, second.Y);
                state.Line.AddWeightedCoordinate(midpoint, length);
            }
            index++;
        }
    }

    internal static double GetMidpointOrdinate(double first, double second)
    {
        double difference = second - first;
        if (double.IsFinite(difference)) return first + difference / 2.0;
        return first / 2.0 + second / 2.0;
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
