using System;

namespace geo;

internal class CompensatedMeasureSum
{
    internal double Sum;
    internal double Compensation;

    internal void AddValue(double value)
    {
        if (double.IsInfinity(Sum)) return;
        if (double.IsInfinity(value))
        {
            Sum = value;
            Compensation = 0.0;
            return;
        }
        double next = Sum + value;
        if (Math.Abs(Sum) >= Math.Abs(value))
        {
            Compensation += Sum - next + value;
        }
        else
        {
            Compensation += value - next + Sum;
        }
        Sum = next;
    }

    internal double GetValue()
    {
        return Sum + Compensation;
    }
}

internal static class GeometryMeasureAlgorithms
{
    internal static int CountCoordinates(Geometry geometry)
    {
        return GeometryInspection.CountCoordinateValues(geometry);
    }

    internal static double GetProjectedArea(Geometry geometry)
    {
        CompensatedMeasureSum sum = new CompensatedMeasureSum();
        AccumulateProjectedArea(geometry, sum);
        return sum.GetValue();
    }

    internal static double GetSignedProjectedArea(Geometry geometry)
    {
        CompensatedMeasureSum sum = new CompensatedMeasureSum();
        AccumulateSignedProjectedArea(geometry, sum);
        return sum.GetValue();
    }

    internal static double GetLength(Geometry geometry)
    {
        CompensatedMeasureSum sum = new CompensatedMeasureSum();
        AccumulateLength(geometry, sum);
        return sum.GetValue();
    }

    internal static double GetPerimeter(Geometry geometry)
    {
        CompensatedMeasureSum sum = new CompensatedMeasureSum();
        AccumulatePerimeter(geometry, sum);
        return sum.GetValue();
    }

    internal static double GetSurfaceArea3D(Geometry geometry)
    {
        CompensatedMeasureSum sum = new CompensatedMeasureSum();
        AccumulateSurfaceArea3D(geometry, sum);
        return sum.GetValue();
    }

    internal static void AccumulateProjectedArea(
        Geometry geometry, CompensatedMeasureSum sum)
    {
        object payload = ValidateAndGetAreaPayload(geometry);
        if (geometry.GeometryType == GeometryType.Polygon)
        {
            Polygon polygon = (Polygon)payload;
            AddPolygonAreaTerms(polygon, sum, false);
            return;
        }
        if (geometry.GeometryType == GeometryType.Triangle)
        {
            AddTriangleAreaTerm((Triangle)payload, sum, false);
            return;
        }
        if (geometry.GeometryType == GeometryType.CurvePolygon)
        {
            CurveAggregateAlgorithms.AddCurvePolygonArea(
                (CurvePolygon)payload, sum, false);
            return;
        }
        Geometry[] children = ComponentWalker.GetDirectChildren(geometry);
        int index = 0;
        while (index < children.Length)
        {
            AccumulateProjectedArea(children[index], sum);
            index++;
        }
    }

    internal static void AccumulateSignedProjectedArea(
        Geometry geometry, CompensatedMeasureSum sum)
    {
        object payload = ValidateAndGetAreaPayload(geometry);
        if (geometry.GeometryType == GeometryType.Polygon)
        {
            Polygon polygon = (Polygon)payload;
            AddPolygonAreaTerms(polygon, sum, true);
            return;
        }
        if (geometry.GeometryType == GeometryType.Triangle)
        {
            AddTriangleAreaTerm((Triangle)payload, sum, true);
            return;
        }
        if (geometry.GeometryType == GeometryType.CurvePolygon)
        {
            CurveAggregateAlgorithms.AddCurvePolygonArea(
                (CurvePolygon)payload, sum, true);
            return;
        }
        Geometry[] children = ComponentWalker.GetDirectChildren(geometry);
        int index = 0;
        while (index < children.Length)
        {
            AccumulateSignedProjectedArea(children[index], sum);
            index++;
        }
    }

    internal static void AddPolygonAreaTerms(
        Polygon polygon, CompensatedMeasureSum sum, bool signed)
    {
        double shellSignedArea = RingMath.GetSignedArea(
            polygon.Shell.Coordinates);
        double direction = shellSignedArea < 0.0 ? -1.0 : 1.0;
        if (!signed) direction = 1.0;
        sum.AddValue(direction * Math.Abs(shellSignedArea));
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            double holeArea = RingMath.GetSignedArea(
                polygon.Holes[index].Coordinates);
            sum.AddValue(-direction * Math.Abs(holeArea));
            index++;
        }
    }

    internal static void AddTriangleAreaTerm(
        Triangle triangle, CompensatedMeasureSum sum, bool signed)
    {
        double area = RingMath.GetSignedArea(
            triangle.Shell.Coordinates);
        if (!signed) area = Math.Abs(area);
        sum.AddValue(area);
    }

    internal static void AccumulateLength(
        Geometry geometry, CompensatedMeasureSum sum)
    {
        object payload = ValidateAndGetCorePayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.LineString:
                AddCoordinateSequenceLength(
                    ((LineString)payload).Coordinates, sum);
                return;
            case GeometryType.LinearRing:
                AddCoordinateSequenceLength(
                    ((LinearRing)payload).Coordinates, sum);
                return;
            case GeometryType.CircularString:
                sum.AddValue(CircularStringAlgorithms.GetLength(
                    (CircularString)payload));
                return;
            case GeometryType.CompoundCurve:
                sum.AddValue(CurveAggregateAlgorithms.GetCurveLength(
                    geometry));
                return;
            case GeometryType.Polygon:
                AddPolygonPerimeter((Polygon)payload, sum);
                return;
            case GeometryType.CurvePolygon:
                sum.AddValue(
                    CurveAggregateAlgorithms.GetCurvePolygonPerimeter(
                        (CurvePolygon)payload));
                return;
            case GeometryType.Triangle:
                AddCoordinateSequenceLength(
                    ((Triangle)payload).Shell.Coordinates, sum);
                return;
            case GeometryType.Tin:
                Tin tin = (Tin)payload;
                int triangleIndex = 0;
                while (triangleIndex < tin.Triangles.Length)
                {
                    AddCoordinateSequenceLength(
                        tin.Triangles[triangleIndex].Shell.Coordinates,
                        sum);
                    triangleIndex++;
                }
                return;
            case GeometryType.PolyhedralSurface:
                PolyhedralSurface surface =
                    (PolyhedralSurface)payload;
                int patchIndex = 0;
                while (patchIndex < surface.Patches.Length)
                {
                    AddPolygonPerimeter(
                        surface.Patches[patchIndex], sum);
                    patchIndex++;
                }
                return;
            default:
                break;
        }
        Geometry[] children = ComponentWalker.GetDirectChildren(geometry);
        int index = 0;
        while (index < children.Length)
        {
            AccumulateLength(children[index], sum);
            index++;
        }
    }

    internal static void AccumulatePerimeter(
        Geometry geometry, CompensatedMeasureSum sum)
    {
        object payload = ValidateAndGetCorePayload(geometry);
        if (geometry.GeometryType == GeometryType.Polygon)
        {
            AddPolygonPerimeter((Polygon)payload, sum);
            return;
        }
        if (geometry.GeometryType == GeometryType.CurvePolygon)
        {
            sum.AddValue(
                CurveAggregateAlgorithms.GetCurvePolygonPerimeter(
                    (CurvePolygon)payload));
            return;
        }
        if (geometry.GeometryType == GeometryType.Triangle)
        {
            AddCoordinateSequenceLength(
                ((Triangle)payload).Shell.Coordinates, sum);
            return;
        }
        if (geometry.GeometryType == GeometryType.Tin)
        {
            Tin tin = (Tin)payload;
            int triangleIndex = 0;
            while (triangleIndex < tin.Triangles.Length)
            {
                AddCoordinateSequenceLength(
                    tin.Triangles[triangleIndex].Shell.Coordinates,
                    sum);
                triangleIndex++;
            }
            return;
        }
        if (geometry.GeometryType == GeometryType.PolyhedralSurface)
        {
            PolyhedralSurface surface = (PolyhedralSurface)payload;
            int patchIndex = 0;
            while (patchIndex < surface.Patches.Length)
            {
                AddPolygonPerimeter(surface.Patches[patchIndex], sum);
                patchIndex++;
            }
            return;
        }
        Geometry[] children = ComponentWalker.GetDirectChildren(geometry);
        int index = 0;
        while (index < children.Length)
        {
            AccumulatePerimeter(children[index], sum);
            index++;
        }
    }

    internal static void AccumulateSurfaceArea3D(
        Geometry geometry, CompensatedMeasureSum sum)
    {
        object payload = ValidateAndGetAreaPayload(geometry);
        if (geometry.GeometryType == GeometryType.Polygon)
        {
            Polygon polygon = (Polygon)payload;
            sum.AddValue(GetRingSurfaceArea3D(
                polygon.Shell.Coordinates));
            int holeIndex = 0;
            while (holeIndex < polygon.Holes.Length)
            {
                sum.AddValue(-GetRingSurfaceArea3D(
                    polygon.Holes[holeIndex].Coordinates));
                holeIndex++;
            }
            return;
        }
        if (geometry.GeometryType == GeometryType.Triangle)
        {
            Triangle triangle = (Triangle)payload;
            sum.AddValue(GetRingSurfaceArea3D(
                triangle.Shell.Coordinates));
            return;
        }
        if (geometry.GeometryType == GeometryType.CurvePolygon)
        {
            Geometry linearized = CurveLinearizer.LinearizeCurvePolygonValue(
                (CurvePolygon)payload, new CurveLinearizationParameters(),
                GeometryNormalization.CreateNormalizationOptions(geometry));
            if (linearized.Polygon == null)
            {
                throw new InvalidGeometryException(
                    "Linearized CurvePolygon payload is missing.");
            }
            Polygon polygon = linearized.Polygon;
            sum.AddValue(GetRingSurfaceArea3D(
                polygon.Shell.Coordinates));
            int holeIndex = 0;
            while (holeIndex < polygon.Holes.Length)
            {
                sum.AddValue(-GetRingSurfaceArea3D(
                    polygon.Holes[holeIndex].Coordinates));
                holeIndex++;
            }
            return;
        }
        Geometry[] children = ComponentWalker.GetDirectChildren(geometry);
        int index = 0;
        while (index < children.Length)
        {
            AccumulateSurfaceArea3D(children[index], sum);
            index++;
        }
    }

    internal static double GetRingSurfaceArea3D(Coordinate[] coordinates)
    {
        if (coordinates.Length < 4) return 0.0;
        Coordinate origin = coordinates[0];
        double originZ = GetSurfaceOrdinate(origin.Z);
        CompensatedMeasureSum xSum = new CompensatedMeasureSum();
        CompensatedMeasureSum ySum = new CompensatedMeasureSum();
        CompensatedMeasureSum zSum = new CompensatedMeasureSum();
        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            Coordinate first = coordinates[index];
            Coordinate second = coordinates[index + 1];
            double firstX = first.X - origin.X;
            double firstY = first.Y - origin.Y;
            double firstZ = GetSurfaceOrdinate(first.Z) - originZ;
            double secondX = second.X - origin.X;
            double secondY = second.Y - origin.Y;
            double secondZ = GetSurfaceOrdinate(second.Z) - originZ;
            xSum.AddValue(firstY * secondZ - firstZ * secondY);
            ySum.AddValue(firstZ * secondX - firstX * secondZ);
            zSum.AddValue(firstX * secondY - firstY * secondX);
            index++;
        }
        return GetVectorMagnitude3D(
            xSum.GetValue(), ySum.GetValue(), zSum.GetValue()) * 0.5;
    }

    internal static double GetSurfaceOrdinate(double value)
    {
        if (double.IsNaN(value)) return 0.0;
        return value;
    }

    internal static double GetVectorMagnitude3D(
        double x, double y, double z)
    {
        double scale = Math.Abs(x);
        double candidate = Math.Abs(y);
        if (candidate > scale) scale = candidate;
        candidate = Math.Abs(z);
        if (candidate > scale) scale = candidate;
        if (scale == 0.0) return 0.0;
        if (double.IsInfinity(scale)) return double.PositiveInfinity;
        double scaledX = x / scale;
        double scaledY = y / scale;
        double scaledZ = z / scale;
        return scale * Math.Sqrt(
            scaledX * scaledX + scaledY * scaledY + scaledZ * scaledZ);
    }

    internal static void AddPolygonPerimeter(
        Polygon polygon, CompensatedMeasureSum sum)
    {
        AddCoordinateSequenceLength(polygon.Shell.Coordinates, sum);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            AddCoordinateSequenceLength(
                polygon.Holes[index].Coordinates, sum);
            index++;
        }
    }

    internal static void AddCoordinateSequenceLength(
        Coordinate[] coordinates, CompensatedMeasureSum sum)
    {
        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            double distance = SegmentDistanceAlgorithms.GetPlanarDistance(
                coordinates[index], coordinates[index + 1]);
            sum.AddValue(distance);
            index++;
        }
    }

    internal static object ValidateAndGetCorePayload(Geometry geometry)
    {
        GeometryInspection.EnsureGeometryGraph(geometry);
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
            case GeometryType.LineString:
            case GeometryType.LinearRing:
            case GeometryType.Polygon:
                RelationGraphBuilder.ValidateAtomicGeometry(geometry);
                return payload;
            case GeometryType.CircularString:
                CircularStringAlgorithms.ValidateValue(
                    (CircularString)payload);
                return payload;
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            case GeometryType.MultiCurve:
            case GeometryType.MultiSurface:
                ValidateExtendedMeasureStructure(geometry);
                return payload;
            case GeometryType.Triangle:
            case GeometryType.Tin:
            case GeometryType.PolyhedralSurface:
                ValidateSurfaceMeasureStructure(geometry);
                return payload;
            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                return payload;
            default:
                throw new InvalidGeometryException(
                    "Core planar measures do not support this geometry type.");
        }
    }

    internal static object ValidateAndGetAreaPayload(Geometry geometry)
    {
        GeometryInspection.EnsureGeometryGraph(geometry);
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
            case GeometryType.LineString:
            case GeometryType.LinearRing:
            case GeometryType.Polygon:
                RelationGraphBuilder.ValidateAtomicGeometry(geometry);
                return payload;
            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                return payload;
            case GeometryType.Triangle:
            case GeometryType.Tin:
            case GeometryType.PolyhedralSurface:
                ValidateSurfaceMeasureStructure(geometry);
                return payload;
            case GeometryType.CircularString:
                CircularStringAlgorithms.ValidateValue(
                    (CircularString)payload);
                return payload;
            case GeometryType.CompoundCurve:
            case GeometryType.CurvePolygon:
            case GeometryType.MultiCurve:
            case GeometryType.MultiSurface:
                ValidateExtendedMeasureStructure(geometry);
                return payload;
            default:
                throw new InvalidGeometryException(
                    "Area measures do not support this geometry type.");
        }
    }

    internal static void ValidateSurfaceMeasureStructure(
        Geometry geometry)
    {
        ValidationResult validation =
            StructuralValidator.ValidateGeometry(geometry);
        if (!validation.IsValid)
        {
            throw new InvalidGeometryException(
                "Surface measurement requires structurally valid geometry: " +
                validation.Message);
        }
    }

    internal static void ValidateExtendedMeasureStructure(
        Geometry geometry)
    {
        ValidationResult validation =
            StructuralValidator.ValidateGeometry(geometry);
        if (!validation.IsValid)
        {
            throw new InvalidGeometryException(
                "Curved measurement requires structurally valid geometry: " +
                validation.Message);
        }
        CurveAggregateAlgorithms.ValidateExtendedAggregateValue(
            geometry);
    }
}
