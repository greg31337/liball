using System;
using System.Collections.Generic;

namespace geo;

internal static class BufferValidator
{
    internal static ValidationResult ValidateBufferResult(
        Geometry source, double distance, Geometry result,
        BufferParameters parameters)
    {
        if (!double.IsFinite(distance))
        {
            throw new InvalidGeometryException(
                "Buffer validation distance must be finite.");
        }
        OffsetSegmentGenerator.ValidateBufferParameters(parameters);
        GeometryInspection.EnsureGeometryGraph(source);
        GeometryInspection.EnsureGeometryGraph(result);
        if (source.SpatialReferenceId != result.SpatialReferenceId ||
            source.CoordinateLayout != result.CoordinateLayout)
        {
            return CreateFailure(
                ValidationError.InvalidCoordinateLayout,
                "Buffer source and result metadata do not match.",
                null, "BufferResult");
        }
        if (!IsPolygonalResult(result))
        {
            return CreateFailure(
                ValidationError.BufferResultNotPolygonal,
                "A buffer result must be a Polygon or MultiPolygon.",
                GetFirstCoordinate(result), "BufferResult");
        }

        ValidationResult areaValidation = ValidateAreaDirection(
            source, distance, result, parameters);
        if (!areaValidation.IsValid) return areaValidation;

        Geometry containmentSource = CreateContainmentSource(
            source, distance, parameters);
        if (distance > 0.0 || parameters.SingleSided)
        {
            Coordinate? exterior = FindExteriorSample(
                containmentSource, result);
            if (exterior != null)
            {
                return CreateFailure(
                    ValidationError.BufferContainmentFailure,
                    "A contributing source location lies outside the buffer result.",
                    exterior, "BufferResult/Containment");
            }
        }
        if (distance <= 0.0 && !parameters.SingleSided &&
            !GeometryInspection.IsEmptyValue(result))
        {
            Geometry polygonalSource = CreatePolygonalSource(source);
            Coordinate? exterior = FindExteriorSample(
                result, polygonalSource);
            if (exterior != null)
            {
                return CreateFailure(
                    ValidationError.BufferContainmentFailure,
                    "A non-positive buffer location lies outside the source area.",
                    exterior, "BufferResult/Containment");
            }
        }

        if (distance != 0.0 &&
            !GeometryInspection.IsEmptyValue(result))
        {
            ValidationResult distanceValidation = ValidateBoundaryDistance(
                source, distance, result, parameters);
            if (!distanceValidation.IsValid) return distanceValidation;
        }
        return new ValidationResult();
    }

    internal static ValidationResult ValidateAreaDirection(
        Geometry source, double distance, Geometry result,
        BufferParameters parameters)
    {
        if (parameters.SingleSided) return new ValidationResult();
        double sourceArea =
            GeometryMeasureAlgorithms.GetProjectedArea(source);
        double resultArea =
            GeometryMeasureAlgorithms.GetProjectedArea(result);
        double tolerance = Math.Max(1.0, sourceArea) * 1e-12;
        if (distance > 0.0 && resultArea + tolerance < sourceArea)
        {
            return CreateFailure(
                ValidationError.BufferAreaFailure,
                "A positive buffer has less area than its source.",
                GetFirstCoordinate(result), "BufferResult/Area");
        }
        if (distance <= 0.0 && resultArea > sourceArea + tolerance)
        {
            return CreateFailure(
                ValidationError.BufferAreaFailure,
                "A non-positive buffer has more area than its source.",
                GetFirstCoordinate(result), "BufferResult/Area");
        }
        return new ValidationResult();
    }

    internal static ValidationResult ValidateBoundaryDistance(
        Geometry source, double distance, Geometry result,
        BufferParameters parameters)
    {
        Geometry reference = CreateDistanceReference(
            source, distance, parameters);
        if (GeometryInspection.IsEmptyValue(reference))
        {
            return new ValidationResult();
        }
        Geometry boundary = GeometryBoundary.CreateBoundaryValue(
            result, BoundaryNodeRule.Mod2);
        DistanceResult measured = GetDirectedBoundaryDistance(
            boundary, reference);
        if (!measured.HasResult) return new ValidationResult();
        double magnitude = Math.Abs(distance);
        double allowance = GetDistanceAllowance(
            magnitude, parameters.Precision);
        double lower = magnitude - allowance;
        if (lower < 0.0) lower = 0.0;
        double upperFactor = GetMaximumDistanceFactor(
            source, parameters);
        double upper = magnitude * upperFactor + allowance;
        if (measured.Distance < lower || measured.Distance > upper)
        {
            Coordinate? witness = measured.FirstCoordinate;
            return CreateFailure(
                ValidationError.BufferDistanceFailure,
                $"Buffer boundary distance {measured.Distance} is outside " +
                    $"the permitted range [{lower}, {upper}].",
                witness, "BufferResult/Boundary");
        }
        return new ValidationResult();
    }

    internal static DistanceResult GetDirectedBoundaryDistance(
        Geometry boundary, Geometry reference)
    {
        DiscreteMetricSample[] samples =
            DiscreteMetricSamples.CreateSamples(boundary, 4);
        DistanceGeometryParts target = new DistanceGeometryParts(reference);
        DistanceResult farthest = new DistanceResult();
        int index = 0;
        while (index < samples.Length)
        {
            DistanceResult candidate =
                HausdorffAlgorithm.GetPointToGeometryDistance(
                    samples[index], target);
            if (candidate.HasResult &&
                (!farthest.HasResult ||
                    candidate.Distance > farthest.Distance))
            {
                DistanceAlgorithm.CopyResult(candidate, farthest);
            }
            index++;
        }
        return farthest;
    }

    internal static Geometry CreateContainmentSource(
        Geometry source, double distance, BufferParameters parameters)
    {
        bool includePoints = distance > 0.0 &&
            parameters.CapStyle != BufferCapStyle.Flat;
        bool includeLines = distance > 0.0 ||
            parameters.SingleSided && distance != 0.0;
        bool includePolygons = distance >= 0.0;
        return CreateSourceSubset(
            source, includePoints, includeLines, includePolygons, false);
    }

    internal static Geometry CreatePolygonalSource(Geometry source)
    {
        return CreateSourceSubset(
            source, false, false, true, false);
    }

    internal static Geometry CreateDistanceReference(
        Geometry source, double distance, BufferParameters parameters)
    {
        bool includePoints = distance > 0.0 &&
            parameters.CapStyle != BufferCapStyle.Flat;
        bool includeLines = distance > 0.0 ||
            parameters.SingleSided && distance != 0.0;
        bool includePolygons = distance != 0.0;
        return CreateSourceSubset(
            source, includePoints, includeLines, includePolygons, true);
    }

    internal static Geometry CreateSourceSubset(
        Geometry source, bool includePoints, bool includeLines,
        bool includePolygons, bool polygonBoundaries)
    {
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            source,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        List<Geometry> selected = new List<Geometry>();
        int index = 0;
        while (index < leaves.Length)
        {
            Geometry leaf = leaves[index];
            if (!GeometryInspection.IsEmptyValue(leaf))
            {
                if (leaf.GeometryType == GeometryType.Point && includePoints)
                {
                    selected.Add(GeometryCopy.CopyGeometry(leaf));
                }
                else if ((leaf.GeometryType == GeometryType.LineString ||
                    leaf.GeometryType == GeometryType.LinearRing) &&
                    includeLines)
                {
                    selected.Add(GeometryCopy.CopyGeometry(leaf));
                }
                else if (leaf.GeometryType == GeometryType.Polygon &&
                    includePolygons)
                {
                    if (polygonBoundaries)
                    {
                        selected.Add(GeometryBoundary.CreateBoundaryValue(
                            leaf, BoundaryNodeRule.Mod2));
                    }
                    else
                    {
                        selected.Add(GeometryCopy.CopyGeometry(leaf));
                    }
                }
            }
            index++;
        }
        return GeometryCollections.CreateRawCollection(
            selected.ToArray(), source.CoordinateLayout,
            source.SpatialReferenceId);
    }

    internal static Coordinate? FindExteriorSample(
        Geometry samplesGeometry, Geometry container)
    {
        DiscreteMetricSample[] samples =
            DiscreteMetricSamples.CreateSamples(samplesGeometry, 4);
        CurveLinearizationParameters curveParameters =
            CreateCurveParameters();
        int index = 0;
        while (index < samples.Length)
        {
            TopologicalLocation location = GeometryPointLocation.LocatePoint(
                samples[index].Coordinate, container, curveParameters);
            if (location == TopologicalLocation.Exterior)
            {
                return RobustLineIntersection.CopyCoordinate(
                    samples[index].Coordinate);
            }
            index++;
        }
        return null;
    }

    internal static CurveLinearizationParameters CreateCurveParameters()
    {
        CurveLinearizationParameters parameters =
            new CurveLinearizationParameters();
        parameters.Mode = CurveLinearizationMode.MaximumAngle;
        parameters.Tolerance = Math.PI / 2.0;
        parameters.MaximumSegments = 16;
        return parameters;
    }

    internal static bool IsPolygonalResult(Geometry result)
    {
        return result.GeometryType == GeometryType.Polygon ||
            result.GeometryType == GeometryType.MultiPolygon;
    }

    internal static double GetMaximumDistanceFactor(
        Geometry source, BufferParameters parameters)
    {
        double factor = 1.0;
        if (parameters.JoinStyle == BufferJoinStyle.Mitre &&
            parameters.MitreLimit > factor)
        {
            factor = parameters.MitreLimit;
        }
        if (parameters.CapStyle == BufferCapStyle.Square &&
            HasLinealOrPointComponents(source) &&
            Math.Sqrt(2.0) > factor)
        {
            factor = Math.Sqrt(2.0);
        }
        return factor;
    }

    internal static bool HasLinealOrPointComponents(Geometry source)
    {
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            source,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        int index = 0;
        while (index < leaves.Length)
        {
            GeometryType type = leaves[index].GeometryType;
            if (!GeometryInspection.IsEmptyValue(leaves[index]) &&
                (type == GeometryType.Point ||
                    type == GeometryType.LineString ||
                    type == GeometryType.LinearRing))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static double GetDistanceAllowance(
        double distance, PrecisionModel precision)
    {
        double allowance = Math.Max(1.0, distance) * 1e-9;
        if (precision.Type == PrecisionModelType.Fixed)
        {
            double gridAllowance = Math.Sqrt(2.0) / precision.Scale;
            if (gridAllowance > allowance) allowance = gridAllowance;
        }
        return allowance;
    }

    internal static Coordinate? GetFirstCoordinate(Geometry geometry)
    {
        Coordinate[] coordinates =
            GeometryInspection.ExtractValidatedCoordinates(geometry);
        if (coordinates.Length == 0) return null;
        return RobustLineIntersection.CopyCoordinate(coordinates[0]);
    }

    internal static ValidationResult CreateFailure(
        ValidationError error, string message,
        Coordinate? coordinate, string componentPath)
    {
        ValidationResult result = new ValidationResult();
        result.IsValid = false;
        result.Error = error;
        result.Message = message;
        if (coordinate != null)
        {
            result.Coordinate =
                RobustLineIntersection.CopyCoordinate(coordinate);
        }
        result.ComponentPath = componentPath;
        return result;
    }
}
