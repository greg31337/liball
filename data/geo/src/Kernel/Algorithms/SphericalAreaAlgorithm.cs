using System;
using System.Collections.Generic;

namespace geo;

internal static class SphericalAreaAlgorithm
{
    internal static double GetSphericalArea(
        Geometry polygonal, SpatialUnit outputUnit)
    {
        double radiansPerUnit =
            GeographicMath.GetRadiansPerUnit(outputUnit);
        ValidationResult validation =
            StructuralValidator.ValidateGeometry(polygonal);
        if (!validation.IsValid)
        {
            throw new InvalidGeometryException(
                "Spherical area requires structurally valid geometry: " +
                validation.Message);
        }

        CompensatedMeasureSum steradians =
            new CompensatedMeasureSum();
        object payload = GeometryDispatch.GetSelectedPayload(polygonal);
        if (polygonal.GeometryType == GeometryType.Polygon)
        {
            AddPolygonSteradians((Polygon)payload, steradians);
        }
        else if (polygonal.GeometryType == GeometryType.MultiPolygon)
        {
            MultiPolygon multiPolygon = (MultiPolygon)payload;
            int index = 0;
            while (index < multiPolygon.Polygons.Length)
            {
                AddPolygonSteradians(
                    multiPolygon.Polygons[index], steradians);
                index++;
            }
        }
        else
        {
            throw new InvalidGeometryException(
                "Spherical area requires a Polygon or MultiPolygon.");
        }

        double unitScale = 1.0 / radiansPerUnit;
        double result = steradians.GetValue() *
            unitScale * unitScale;
        if (!double.IsFinite(result))
        {
            throw new InvalidGeometryException(
                "The spherical area result is not finite.");
        }
        if (result == 0.0) return 0.0;
        return result;
    }

    internal static void AddPolygonSteradians(
        Polygon polygon, CompensatedMeasureSum sum)
    {
        double shellArea = Math.Abs(GetSignedRingSteradians(
            polygon.Shell.Coordinates));
        CompensatedMeasureSum polygonArea =
            new CompensatedMeasureSum();
        polygonArea.AddValue(shellArea);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            double holeArea = Math.Abs(GetSignedRingSteradians(
                polygon.Holes[index].Coordinates));
            polygonArea.AddValue(-holeArea);
            index++;
        }
        double area = polygonArea.GetValue();
        double tolerance = 1.0e-12 * Math.Max(1.0, shellArea);
        if (area < -tolerance)
        {
            throw new InvalidGeometryException(
                "Spherical polygon holes exceed the shell area.");
        }
        if (area < 0.0) area = 0.0;
        sum.AddValue(area);
    }

    internal static double GetSignedRingSteradians(
        Coordinate[] coordinates)
    {
        if (coordinates.Length == 0) return 0.0;

        double[][] vectors = new double[coordinates.Length][];
        double[] vectorSum = [0.0, 0.0, 0.0];
        int index = 0;
        while (index < coordinates.Length)
        {
            GeographicMath.ValidateGeographicCoordinate(
                coordinates[index]);
            vectors[index] = GeographicLineAlgorithms.ToUnitVector(
                coordinates[index]);
            if (index + 1 < coordinates.Length)
            {
                vectorSum[0] += vectors[index][0];
                vectorSum[1] += vectors[index][1];
                vectorSum[2] += vectors[index][2];
            }
            index++;
        }

        ValidateRingEdges(vectors);
        List<double[]> references = new List<double[]>();
        double[] center =
            GeographicLineAlgorithms.NormalizeVector(vectorSum);
        if (center.Length != 0) references.Add(center);
        references.Add([1.0, 0.0, 0.0]);
        references.Add([0.0, 1.0, 0.0]);
        references.Add([0.0, 0.0, 1.0]);
        references.Add([-1.0, 0.0, 0.0]);
        references.Add([0.0, -1.0, 0.0]);
        references.Add([0.0, 0.0, -1.0]);

        index = 0;
        while (index < references.Count)
        {
            double area = GetFanSteradians(
                vectors, references[index]);
            if (!double.IsNaN(area))
            {
                return NormalizeSignedSteradians(area);
            }
            index++;
        }
        throw new InvalidGeometryException(
            "The spherical ring has an ambiguous geodesic boundary.");
    }

    internal static void ValidateRingEdges(double[][] vectors)
    {
        int index = 0;
        while (index + 1 < vectors.Length)
        {
            double angle = GeographicLineAlgorithms.VectorAngle(
                vectors[index], vectors[index + 1]);
            if (Math.Abs(Math.PI - angle) <= 1.0e-12)
            {
                throw new InvalidGeometryException(
                    "A spherical polygon edge cannot join antipodal coordinates.");
            }
            index++;
        }
    }

    internal static double GetFanSteradians(
        double[][] vectors, double[] reference)
    {
        CompensatedMeasureSum sum = new CompensatedMeasureSum();
        int index = 0;
        while (index + 1 < vectors.Length)
        {
            double[] first = vectors[index];
            double[] second = vectors[index + 1];
            double[] cross = GeographicLineAlgorithms.CrossVector(
                first, second);
            double numerator = GeographicLineAlgorithms.DotVector(
                reference, cross);
            double denominator = 1.0 +
                GeographicLineAlgorithms.DotVector(reference, first) +
                GeographicLineAlgorithms.DotVector(first, second) +
                GeographicLineAlgorithms.DotVector(second, reference);
            if (Math.Abs(numerator) <= 1.0e-15 &&
                Math.Abs(denominator) <= 1.0e-15)
            {
                return double.NaN;
            }
            sum.AddValue(2.0 * Math.Atan2(
                numerator, denominator));
            index++;
        }
        return sum.GetValue();
    }

    internal static double NormalizeSignedSteradians(double area)
    {
        double fullSphere = 4.0 * Math.PI;
        double halfSphere = 2.0 * Math.PI;
        double normalized = area % fullSphere;
        if (normalized <= -halfSphere)
        {
            normalized += fullSphere;
        }
        else if (normalized > halfSphere)
        {
            normalized -= fullSphere;
        }
        if (Math.Abs(normalized) <= 1.0e-15) return 0.0;
        return normalized;
    }
}
