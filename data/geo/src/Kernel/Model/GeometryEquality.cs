using System;

namespace geo;

internal static class GeometryEquality
{
    internal static bool AreExactlyEqual(Geometry first, Geometry second)
    {
        ValidateEqualityInputs(first, second);
        return AreStructuresEqual(first, second, 0.0);
    }

    internal static bool AreEqualWithinTolerance(
        Geometry first, Geometry second, double tolerance)
    {
        ValidateTolerance(tolerance);
        ValidateEqualityInputs(first, second);
        return AreStructuresEqual(first, second, tolerance);
    }

    internal static bool AreNormalizedEqual(Geometry first, Geometry second)
    {
        ValidateEqualityInputs(first, second);
        Geometry firstNormalized =
            GeometryNormalization.NormalizeGeometryValue(first);
        Geometry secondNormalized =
            GeometryNormalization.NormalizeGeometryValue(second);
        return AreStructuresEqual(
            firstNormalized, secondNormalized, 0.0);
    }

    internal static bool AreTopologicallyEqual(
        Geometry first, Geometry second)
    {
        GeometryRelations.ValidateSpatialReferencePair(first, second);
        RelationMatrix matrix = RelationGraphBuilder.BuildRelation(
            first, second);
        if (matrix.LeftDimension == TopologicalDimension.Empty ||
            matrix.RightDimension == TopologicalDimension.Empty)
        {
            return matrix.LeftDimension == TopologicalDimension.Empty &&
                matrix.RightDimension == TopologicalDimension.Empty;
        }
        if (matrix.LeftDimension != matrix.RightDimension) return false;
        return RelationMatrixAlgorithms.MatchesPattern(
            matrix, De9ImPatterns.TopologicallyEquals);
    }

    internal static void ValidateEqualityInputs(
        Geometry first, Geometry second)
    {
        GeometryRelations.ValidateSpatialReferencePair(first, second);
        GeometryInspection.ExtractValidatedCoordinates(first);
        GeometryInspection.ExtractValidatedCoordinates(second);
    }

    internal static void ValidateTolerance(double tolerance)
    {
        if (!double.IsFinite(tolerance) || tolerance < 0.0)
        {
            throw new InvalidGeometryException(
                "Geometry equality tolerance must be finite and non-negative.");
        }
    }

    internal static bool AreStructuresEqual(
        Geometry first, Geometry second, double tolerance)
    {
        if (first.GeometryType != second.GeometryType ||
            first.CoordinateLayout != second.CoordinateLayout ||
            first.SpatialReferenceId != second.SpatialReferenceId)
        {
            return false;
        }
        object firstPayload = GeometryDispatch.GetSelectedPayload(first);
        object secondPayload = GeometryDispatch.GetSelectedPayload(second);
        switch (first.GeometryType)
        {
            case GeometryType.Point:
                return ArePointsEqual(
                    (Point)firstPayload, (Point)secondPayload, tolerance);
            case GeometryType.LineString:
                return AreCoordinateArraysEqual(
                    ((LineString)firstPayload).Coordinates,
                    ((LineString)secondPayload).Coordinates, tolerance);
            case GeometryType.LinearRing:
                return AreCoordinateArraysEqual(
                    ((LinearRing)firstPayload).Coordinates,
                    ((LinearRing)secondPayload).Coordinates, tolerance);
            case GeometryType.Polygon:
                return ArePolygonsEqual(
                    (Polygon)firstPayload, (Polygon)secondPayload, tolerance);
            case GeometryType.MultiPoint:
                return ArePointArraysEqual(
                    ((MultiPoint)firstPayload).Points,
                    ((MultiPoint)secondPayload).Points, tolerance);
            case GeometryType.MultiLineString:
                return AreLineStringArraysEqual(
                    ((MultiLineString)firstPayload).LineStrings,
                    ((MultiLineString)secondPayload).LineStrings, tolerance);
            case GeometryType.MultiPolygon:
                return ArePolygonArraysEqual(
                    ((MultiPolygon)firstPayload).Polygons,
                    ((MultiPolygon)secondPayload).Polygons, tolerance);
            case GeometryType.GeometryCollection:
                return AreGeometryArraysEqual(
                    ((GeometryCollection)firstPayload).Geometries,
                    ((GeometryCollection)secondPayload).Geometries, tolerance);
            case GeometryType.Triangle:
                return AreLinearRingsEqual(
                    ((Triangle)firstPayload).Shell,
                    ((Triangle)secondPayload).Shell, tolerance);
            case GeometryType.Tin:
                return AreTriangleArraysEqual(
                    ((Tin)firstPayload).Triangles,
                    ((Tin)secondPayload).Triangles, tolerance);
            case GeometryType.PolyhedralSurface:
                return ArePolygonArraysEqual(
                    ((PolyhedralSurface)firstPayload).Patches,
                    ((PolyhedralSurface)secondPayload).Patches, tolerance);
            case GeometryType.CircularString:
                return AreCoordinateArraysEqual(
                    ((CircularString)firstPayload).Coordinates,
                    ((CircularString)secondPayload).Coordinates, tolerance);
            case GeometryType.CompoundCurve:
                return AreGeometryArraysEqual(
                    ((CompoundCurve)firstPayload).Curves,
                    ((CompoundCurve)secondPayload).Curves, tolerance);
            case GeometryType.CurvePolygon:
                return AreCurvePolygonsEqual(
                    (CurvePolygon)firstPayload,
                    (CurvePolygon)secondPayload, tolerance);
            case GeometryType.MultiCurve:
                return AreGeometryArraysEqual(
                    ((MultiCurve)firstPayload).Curves,
                    ((MultiCurve)secondPayload).Curves, tolerance);
            case GeometryType.MultiSurface:
                return AreGeometryArraysEqual(
                    ((MultiSurface)firstPayload).Surfaces,
                    ((MultiSurface)secondPayload).Surfaces, tolerance);
            default:
                throw new InvalidGeometryException(
                    "Geometry type is not supported for structural equality.");
        }
    }

    internal static bool AreCoordinatesEqual(
        Coordinate first, Coordinate second, double tolerance)
    {
        if (first == null || second == null)
        {
            throw new InvalidGeometryException(
                "Geometry equality coordinate is null.");
        }
        return AreOrdinatesEqual(first.X, second.X, tolerance) &&
            AreOrdinatesEqual(first.Y, second.Y, tolerance) &&
            AreOrdinatesEqual(first.Z, second.Z, tolerance) &&
            AreOrdinatesEqual(first.M, second.M, tolerance);
    }

    internal static bool AreOrdinatesEqual(
        double first, double second, double tolerance)
    {
        if (double.IsNaN(first) || double.IsNaN(second))
        {
            return double.IsNaN(first) && double.IsNaN(second);
        }
        if (first == second) return true;
        double difference = Math.Abs(first - second);
        if (difference <= tolerance) return true;
        if (tolerance == 0.0) return false;
        double scale = Math.Max(Math.Abs(first), Math.Abs(second));
        double roundingAllowance = scale * 4.4408920985006262E-16;
        return difference <= tolerance + roundingAllowance;
    }

    internal static bool AreCoordinateArraysEqual(
        Coordinate[] first, Coordinate[] second, double tolerance)
    {
        if (first == null || second == null)
        {
            throw new InvalidGeometryException(
                "Geometry equality coordinate sequence is null.");
        }
        if (first.Length != second.Length) return false;
        int index = 0;
        while (index < first.Length)
        {
            if (!AreCoordinatesEqual(first[index], second[index], tolerance))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool ArePointsEqual(
        Point first, Point second, double tolerance)
    {
        if (first == null || second == null)
        {
            throw new InvalidGeometryException(
                "Geometry equality point payload is null.");
        }
        if (first.Coordinate == null || second.Coordinate == null)
        {
            return first.Coordinate == null && second.Coordinate == null;
        }
        return AreCoordinatesEqual(
            first.Coordinate, second.Coordinate, tolerance);
    }

    internal static bool AreLinearRingsEqual(
        LinearRing first, LinearRing second, double tolerance)
    {
        if (first == null || second == null)
        {
            throw new InvalidGeometryException(
                "Geometry equality ring payload is null.");
        }
        return AreCoordinateArraysEqual(
            first.Coordinates, second.Coordinates, tolerance);
    }

    internal static bool ArePolygonsEqual(
        Polygon first, Polygon second, double tolerance)
    {
        if (first == null || second == null ||
            first.Shell == null || second.Shell == null ||
            first.Holes == null || second.Holes == null)
        {
            throw new InvalidGeometryException(
                "Geometry equality polygon payload is invalid.");
        }
        if (!AreLinearRingsEqual(
            first.Shell, second.Shell, tolerance)) return false;
        if (first.Holes.Length != second.Holes.Length) return false;
        int index = 0;
        while (index < first.Holes.Length)
        {
            if (!AreLinearRingsEqual(
                first.Holes[index], second.Holes[index], tolerance))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool ArePointArraysEqual(
        Point[] first, Point[] second, double tolerance)
    {
        if (first == null || second == null)
        {
            throw new InvalidGeometryException(
                "Geometry equality point component array is null.");
        }
        if (first.Length != second.Length) return false;
        int index = 0;
        while (index < first.Length)
        {
            if (!ArePointsEqual(first[index], second[index], tolerance))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool AreLineStringArraysEqual(
        LineString[] first, LineString[] second, double tolerance)
    {
        if (first == null || second == null)
        {
            throw new InvalidGeometryException(
                "Geometry equality line component array is null.");
        }
        if (first.Length != second.Length) return false;
        int index = 0;
        while (index < first.Length)
        {
            LineString firstLine = first[index];
            LineString secondLine = second[index];
            if (firstLine == null || secondLine == null)
            {
                throw new InvalidGeometryException(
                    "Geometry equality line component is null.");
            }
            if (!AreCoordinateArraysEqual(
                firstLine.Coordinates, secondLine.Coordinates, tolerance))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool ArePolygonArraysEqual(
        Polygon[] first, Polygon[] second, double tolerance)
    {
        if (first == null || second == null)
        {
            throw new InvalidGeometryException(
                "Geometry equality polygon component array is null.");
        }
        if (first.Length != second.Length) return false;
        int index = 0;
        while (index < first.Length)
        {
            if (!ArePolygonsEqual(first[index], second[index], tolerance))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool AreGeometryArraysEqual(
        Geometry[] first, Geometry[] second, double tolerance)
    {
        if (first == null || second == null)
        {
            throw new InvalidGeometryException(
                "Geometry equality component array is null.");
        }
        if (first.Length != second.Length) return false;
        int index = 0;
        while (index < first.Length)
        {
            if (first[index] == null || second[index] == null)
            {
                throw new InvalidGeometryException(
                    "Geometry equality component is null.");
            }
            if (!AreStructuresEqual(first[index], second[index], tolerance))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool AreTriangleArraysEqual(
        Triangle[] first, Triangle[] second, double tolerance)
    {
        if (first == null || second == null)
        {
            throw new InvalidGeometryException(
                "Geometry equality triangle component array is null.");
        }
        if (first.Length != second.Length) return false;
        int index = 0;
        while (index < first.Length)
        {
            Triangle firstTriangle = first[index];
            Triangle secondTriangle = second[index];
            if (firstTriangle == null || secondTriangle == null ||
                firstTriangle.Shell == null || secondTriangle.Shell == null)
            {
                throw new InvalidGeometryException(
                    "Geometry equality triangle component is invalid.");
            }
            if (!AreLinearRingsEqual(
                firstTriangle.Shell, secondTriangle.Shell, tolerance))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool AreCurvePolygonsEqual(
        CurvePolygon first, CurvePolygon second, double tolerance)
    {
        if (first == null || second == null ||
            first.Holes == null || second.Holes == null)
        {
            throw new InvalidGeometryException(
                "Geometry equality curve polygon payload is invalid.");
        }
        if (first.Shell == null || second.Shell == null)
        {
            if (first.Shell != null || second.Shell != null) return false;
        }
        else if (!AreStructuresEqual(
            first.Shell, second.Shell, tolerance))
        {
            return false;
        }
        return AreGeometryArraysEqual(
            first.Holes, second.Holes, tolerance);
    }
}
