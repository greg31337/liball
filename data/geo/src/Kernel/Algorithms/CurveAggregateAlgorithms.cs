using System;

namespace geo;

internal static class CurveAggregateAlgorithms
{
    internal static void ValidateCompoundValue(CompoundCurve compound)
    {
        if (compound == null || compound.Curves == null)
        {
            throw new InvalidGeometryException(
                "CompoundCurve payload or component array is null.");
        }
        Coordinate? previousEnd = null;
        int index = 0;
        while (index < compound.Curves.Length)
        {
            Geometry curve = compound.Curves[index];
            if (curve == null ||
                (curve.GeometryType != GeometryType.LineString &&
                 curve.GeometryType != GeometryType.CircularString))
            {
                throw new InvalidGeometryException(
                    "CompoundCurve components must be LineString or CircularString values.");
            }
            ValidateSimpleCurveValue(curve);
            Coordinate[] coordinates =
                CoordinateSequences.GetDirectCoordinates(curve);
            if (coordinates.Length != 0)
            {
                if (previousEnd != null &&
                    !RobustLineIntersection.CoordinatesEqualPlanar(
                        previousEnd, coordinates[0]))
                {
                    throw new InvalidGeometryException(
                        "CompoundCurve components are not continuous.");
                }
                previousEnd = coordinates[coordinates.Length - 1];
            }
            index++;
        }
    }

    internal static void ValidateSimpleCurveValue(Geometry curve)
    {
        object payload = GeometryDispatch.GetSelectedPayload(curve);
        if (curve.GeometryType == GeometryType.LineString)
        {
            Coordinate[] coordinates =
                ((LineString)payload).Coordinates;
            GeometryNormalization.ValidateLineCoordinates(
                coordinates);
            ValidateCurveCoordinateLayout(
                coordinates, curve.CoordinateLayout);
            return;
        }
        if (curve.GeometryType == GeometryType.CircularString)
        {
            CircularString circular = (CircularString)payload;
            CircularStringAlgorithms.ValidateValue(circular);
            ValidateCurveCoordinateLayout(
                circular.Coordinates, curve.CoordinateLayout);
            return;
        }
        throw new InvalidGeometryException(
            "Geometry is not a simple curve component.");
    }

    internal static void ValidateCurveValue(Geometry curve)
    {
        if (curve == null)
        {
            throw new InvalidGeometryException("Curve component is null.");
        }
        object payload = GeometryDispatch.GetSelectedPayload(curve);
        switch (curve.GeometryType)
        {
            case GeometryType.LineString:
                LineString line = (LineString)payload;
                GeometryNormalization.ValidateLineCoordinates(
                    line.Coordinates);
                ValidateCurveCoordinateLayout(
                    line.Coordinates, curve.CoordinateLayout);
                return;
            case GeometryType.LinearRing:
                LinearRing ring = (LinearRing)payload;
                GeometryBuilder.ValidateLinearRingCoordinates(
                    ring.Coordinates);
                ValidateCurveCoordinateLayout(
                    ring.Coordinates, curve.CoordinateLayout);
                return;
            case GeometryType.CircularString:
                CircularString circular = (CircularString)payload;
                CircularStringAlgorithms.ValidateValue(circular);
                ValidateCurveCoordinateLayout(
                    circular.Coordinates, curve.CoordinateLayout);
                return;
            case GeometryType.CompoundCurve:
                ValidateCompoundValue((CompoundCurve)payload);
                return;
            default:
                throw new InvalidGeometryException(
                    "Geometry is not a supported curve.");
        }
    }

    internal static void ValidateCurveCoordinateLayout(
        Coordinate[] coordinates, CoordinateLayout layout)
    {
        int index = 0;
        while (index < coordinates.Length)
        {
            GeometryBuilder.ValidateCoordinateForLayout(
                coordinates[index], layout);
            index++;
        }
    }

    internal static void ValidateCurveRingValue(
        Geometry curve, bool allowEmpty)
    {
        ValidateCurveValue(curve);
        Coordinate[] coordinates = CoordinateSequences.ExtractCoordinates(
            curve, GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        if (coordinates.Length == 0)
        {
            if (allowEmpty) return;
            throw new InvalidGeometryException(
                "A CurvePolygon ring cannot be empty.");
        }
        if (coordinates.Length < 4 ||
            !RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[0], coordinates[coordinates.Length - 1]))
        {
            throw new InvalidGeometryException(
                "A CurvePolygon ring must be closed and contain at least four control positions.");
        }
        if (!GeometryInspection.HasThreeDistinctPlanarCoordinates(
            coordinates))
        {
            throw new InvalidGeometryException(
                "A CurvePolygon ring must contain at least three distinct planar positions.");
        }
    }

    internal static void ValidateCurvePolygonValue(
        CurvePolygon polygon)
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
                    "An empty CurvePolygon cannot contain holes.");
            }
            return;
        }
        ValidateCurveRingValue(polygon.Shell, false);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            ValidateCurveRingValue(polygon.Holes[index], false);
            index++;
        }
    }

    internal static void ValidateExtendedAggregateValue(Geometry geometry)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.CompoundCurve:
                ValidateCompoundValue((CompoundCurve)payload);
                return;
            case GeometryType.CurvePolygon:
                ValidateCurvePolygonValue((CurvePolygon)payload);
                return;
            case GeometryType.MultiCurve:
                MultiCurve multiCurve = (MultiCurve)payload;
                if (multiCurve.Curves == null)
                {
                    throw new InvalidGeometryException(
                        "MultiCurve component array is null.");
                }
                int curveIndex = 0;
                while (curveIndex < multiCurve.Curves.Length)
                {
                    ValidateCurveValue(multiCurve.Curves[curveIndex]);
                    curveIndex++;
                }
                return;
            case GeometryType.MultiSurface:
                MultiSurface multiSurface = (MultiSurface)payload;
                if (multiSurface.Surfaces == null)
                {
                    throw new InvalidGeometryException(
                        "MultiSurface component array is null.");
                }
                int surfaceIndex = 0;
                while (surfaceIndex < multiSurface.Surfaces.Length)
                {
                    Geometry surface = multiSurface.Surfaces[surfaceIndex];
                    if (surface == null)
                    {
                        throw new InvalidGeometryException(
                            "MultiSurface component is null.");
                    }
                    if (surface.GeometryType == GeometryType.CurvePolygon)
                    {
                        ValidateCurvePolygonValue((CurvePolygon)
                            GeometryDispatch.GetSelectedPayload(surface));
                    }
                    else if (surface.GeometryType != GeometryType.Polygon &&
                        surface.GeometryType != GeometryType.Triangle &&
                        surface.GeometryType != GeometryType.Tin &&
                        surface.GeometryType != GeometryType.PolyhedralSurface)
                    {
                        throw new InvalidGeometryException(
                            "MultiSurface contains an unsupported surface value.");
                    }
                    surfaceIndex++;
                }
                return;
            default:
                return;
        }
    }

    internal static void ValidateExtendedTreeValue(Geometry geometry)
    {
        ValidateExtendedAggregateValue(geometry);
        Geometry[] children = ComponentWalker.GetDirectChildren(geometry);
        int index = 0;
        while (index < children.Length)
        {
            ValidateExtendedTreeValue(children[index]);
            index++;
        }
    }

    internal static double GetCurveLength(Geometry curve)
    {
        ValidateCurveValue(curve);
        object payload = GeometryDispatch.GetSelectedPayload(curve);
        CompensatedMeasureSum sum = new CompensatedMeasureSum();
        switch (curve.GeometryType)
        {
            case GeometryType.LineString:
                GeometryMeasureAlgorithms.AddCoordinateSequenceLength(
                    ((LineString)payload).Coordinates, sum);
                break;
            case GeometryType.LinearRing:
                GeometryMeasureAlgorithms.AddCoordinateSequenceLength(
                    ((LinearRing)payload).Coordinates, sum);
                break;
            case GeometryType.CircularString:
                sum.AddValue(CircularStringAlgorithms.GetLength(
                    (CircularString)payload));
                break;
            case GeometryType.CompoundCurve:
                CompoundCurve compound = (CompoundCurve)payload;
                int index = 0;
                while (index < compound.Curves.Length)
                {
                    sum.AddValue(GetCurveLength(compound.Curves[index]));
                    index++;
                }
                break;
            default:
                break;
        }
        return sum.GetValue();
    }

    internal static double GetSignedCurveRingArea(Geometry curve)
    {
        ValidateCurveRingValue(curve, true);
        Coordinate[] coordinates = CoordinateSequences.ExtractCoordinates(
            curve, GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        if (coordinates.Length == 0) return 0.0;
        Coordinate origin = coordinates[0];
        CompensatedMeasureSum sum = new CompensatedMeasureSum();
        AccumulateCurveArea(curve, origin, sum);
        return sum.GetValue();
    }

    internal static void AccumulateCurveArea(
        Geometry curve, Coordinate origin, CompensatedMeasureSum sum)
    {
        object payload = GeometryDispatch.GetSelectedPayload(curve);
        switch (curve.GeometryType)
        {
            case GeometryType.LineString:
                AddCoordinateArea(
                    ((LineString)payload).Coordinates, origin, sum);
                return;
            case GeometryType.LinearRing:
                AddCoordinateArea(
                    ((LinearRing)payload).Coordinates, origin, sum);
                return;
            case GeometryType.CircularString:
                sum.AddValue(
                    CircularStringAlgorithms.GetSignedAreaContribution(
                        (CircularString)payload, origin));
                return;
            case GeometryType.CompoundCurve:
                CompoundCurve compound = (CompoundCurve)payload;
                int index = 0;
                while (index < compound.Curves.Length)
                {
                    AccumulateCurveArea(
                        compound.Curves[index], origin, sum);
                    index++;
                }
                return;
            default:
                throw new InvalidGeometryException(
                    "Geometry is not a supported curve ring.");
        }
    }

    internal static void AddCoordinateArea(
        Coordinate[] coordinates, Coordinate origin,
        CompensatedMeasureSum sum)
    {
        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            sum.AddValue(
                CircularStringAlgorithms.GetChordAreaRelativeToOrigin(
                    coordinates[index], coordinates[index + 1], origin));
            index++;
        }
    }

    internal static void AddCurvePolygonArea(
        CurvePolygon polygon, CompensatedMeasureSum sum, bool signed)
    {
        ValidateCurvePolygonValue(polygon);
        if (polygon.Shell == null) return;
        double shellArea = GetSignedCurveRingArea(polygon.Shell);
        double direction = shellArea < 0.0 ? -1.0 : 1.0;
        if (!signed) direction = 1.0;
        sum.AddValue(direction * Math.Abs(shellArea));
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            double holeArea = GetSignedCurveRingArea(
                polygon.Holes[index]);
            sum.AddValue(-direction * Math.Abs(holeArea));
            index++;
        }
    }

    internal static double GetCurvePolygonPerimeter(
        CurvePolygon polygon)
    {
        ValidateCurvePolygonValue(polygon);
        if (polygon.Shell == null) return 0.0;
        CompensatedMeasureSum sum = new CompensatedMeasureSum();
        sum.AddValue(GetCurveLength(polygon.Shell));
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            sum.AddValue(GetCurveLength(polygon.Holes[index]));
            index++;
        }
        return sum.GetValue();
    }
}
