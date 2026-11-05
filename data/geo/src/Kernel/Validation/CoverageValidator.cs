using System;
using System.Collections.Generic;

namespace geo;

internal static class CoverageValidator
{
    internal static CoverageValidationResult ValidateCoverage(
        Geometry? geometry, double maximumGapWidth)
    {
        if (!double.IsFinite(maximumGapWidth) || maximumGapWidth < 0.0)
        {
            throw new InvalidGeometryException(
                "Maximum coverage gap width must be finite and non-negative.");
        }
        ValidationResult structure =
            StructuralValidator.ValidateGeometry(geometry);
        if (!structure.IsValid || geometry == null)
        {
            return CreateCoverageFailure(
                structure.Error, structure.Message,
                structure.Coordinate, structure.ComponentPath, null);
        }
        if (!CollectionRelationGraphBuilder.SupportsCoreGeometry(geometry))
        {
            return CreateCoverageFailure(
                ValidationError.InvalidGeometryType,
                "Coverage validation supports core polygonal geometry only.",
                null, geometry.GeometryType.ToString(), null);
        }

        GeometryRepairComponents components =
            GeometryRepairEngine.CollectRepairComponents(geometry);
        if (components.Points.Count != 0 || components.Lines.Count != 0)
        {
            return CreateCoverageFailure(
                ValidationError.InvalidGeometryType,
                "A coverage must contain polygonal components only.",
                null, geometry.GeometryType.ToString(), null);
        }
        int index = 0;
        while (index < components.Polygons.Count)
        {
            ValidationResult polygonValidation =
                GeometryValidityChecker.ValidateGeometry(
                    components.Polygons[index]);
            if (!polygonValidation.IsValid)
            {
                return CreateCoverageFailure(
                    polygonValidation.Error,
                    polygonValidation.Message,
                    polygonValidation.Coordinate,
                    "Coverage/Polygons[" + index + "]/" +
                    polygonValidation.ComponentPath,
                    components.Polygons[index]);
            }
            index++;
        }
        CoveragePolygonPair[] candidates =
            CoverageEdgeIndex.FindCandidatePairs(
                components.Polygons, maximumGapWidth);
        index = 0;
        while (index < candidates.Length)
        {
            CoveragePolygonPair pair = candidates[index];
            CoverageValidationResult? overlap = CheckCoverageOverlap(
                components.Polygons[pair.FirstIndex],
                components.Polygons[pair.SecondIndex],
                pair.FirstIndex, pair.SecondIndex);
            if (overlap != null) return overlap;
            index++;
        }

        CoverageNodingFailure? nodingFailure =
            CoverageEdgeIndex.FindFirstNonNodedIntersection(
                components.Polygons);
        if (nodingFailure != null)
        {
            return CreateNonNodedFailure(geometry, nodingFailure);
        }

        if (maximumGapWidth > 0.0)
        {
            index = 0;
            while (index < candidates.Length)
            {
                CoveragePolygonPair pair = candidates[index];
                CoverageValidationResult? gap = CheckCoverageGap(
                    components.Polygons[pair.FirstIndex],
                    components.Polygons[pair.SecondIndex],
                    pair.FirstIndex, pair.SecondIndex,
                    maximumGapWidth);
                if (gap != null) return gap;
                index++;
            }
        }
        return new CoverageValidationResult();
    }

    internal static CoverageValidationResult? CheckCoverageOverlap(
        Geometry first, Geometry second,
        int firstIndex, int secondIndex)
    {
        if (!CoveragePolygonsOverlap(first, second)) return null;
        Geometry intersection = OverlayEngine.GetIntersection(first, second);
        double area = GeometryMeasureAlgorithms.GetProjectedArea(
            intersection);
        if (area <= 0.0) return null;
        Coordinate? coordinate = GetFirstCoordinate(intersection);
        return CreateCoverageFailure(
            ValidationError.CoverageOverlap,
            "Coverage polygon interiors overlap.",
            coordinate,
            "Coverage/Polygons[" + secondIndex +
            "]/OverlapWith[" + firstIndex + "]",
            intersection);
    }

    internal static bool CoveragePolygonsOverlap(
        Geometry first, Geometry second)
    {
        Polygon firstPolygon = (Polygon)
            GeometryDispatch.GetSelectedPayload(first);
        Polygon secondPolygon = (Polygon)
            GeometryDispatch.GetSelectedPayload(second);
        if (BoundariesCrossProperly(first, second)) return true;
        if (PolygonBoundaryHasInteriorSample(
            firstPolygon, secondPolygon)) return true;
        if (PolygonBoundaryHasInteriorSample(
            secondPolygon, firstPolygon)) return true;
        Coordinate? firstPoint =
            RepresentativePointAlgorithm.GetPointOnSurface(first);
        if (firstPoint != null &&
            ExtendedGeometryValidityChecker.LocatePointInPolygon(
                firstPoint, secondPolygon) ==
                TopologicalLocation.Interior)
        {
            return true;
        }
        Coordinate? secondPoint =
            RepresentativePointAlgorithm.GetPointOnSurface(second);
        return secondPoint != null &&
            ExtendedGeometryValidityChecker.LocatePointInPolygon(
                secondPoint, firstPolygon) ==
                TopologicalLocation.Interior;
    }

    internal static bool BoundariesCrossProperly(
        Geometry first, Geometry second)
    {
        DistanceGeometryParts firstParts =
            new DistanceGeometryParts(first);
        DistanceGeometryParts secondParts =
            new DistanceGeometryParts(second);
        SegmentSpatialIndex secondIndex = new SegmentSpatialIndex(
            secondParts.Segments.ToArray());
        int firstIndex = 0;
        while (firstIndex < firstParts.Segments.Count)
        {
            DistanceSegmentFacet firstSegment =
                firstParts.Segments[firstIndex];
            DistanceSegmentFacet[] candidates =
                secondIndex.Query(firstSegment.Bounds);
            int candidateIndex = 0;
            while (candidateIndex < candidates.Length)
            {
                DistanceSegmentFacet secondSegment =
                    candidates[candidateIndex];
                SegmentIntersectionResult intersection =
                    RobustLineIntersection.GetSegmentIntersection(
                        firstSegment.Start, firstSegment.End,
                        secondSegment.Start, secondSegment.End);
                if (intersection.Type ==
                    SegmentIntersectionType.ProperPoint)
                {
                    return true;
                }
                candidateIndex++;
            }
            firstIndex++;
        }
        return false;
    }

    internal static bool PolygonBoundaryHasInteriorSample(
        Polygon source, Polygon target)
    {
        if (RingHasInteriorSample(
            source.Shell.Coordinates, target)) return true;
        int holeIndex = 0;
        while (holeIndex < source.Holes.Length)
        {
            if (RingHasInteriorSample(
                source.Holes[holeIndex].Coordinates, target))
            {
                return true;
            }
            holeIndex++;
        }
        return false;
    }

    internal static bool RingHasInteriorSample(
        Coordinate[] coordinates, Polygon target)
    {
        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            if (ExtendedGeometryValidityChecker.LocatePointInPolygon(
                coordinates[index], target) ==
                TopologicalLocation.Interior)
            {
                return true;
            }
            if (!PolygonHasMatchingSegment(
                coordinates[index], coordinates[index + 1], target))
            {
                Coordinate midpoint = VectorMath.InterpolateCoordinate(
                    coordinates[index], coordinates[index + 1], 0.5);
                if (ExtendedGeometryValidityChecker.LocatePointInPolygon(
                    midpoint, target) == TopologicalLocation.Interior)
                {
                    return true;
                }
            }
            index++;
        }
        return false;
    }

    internal static bool PolygonHasMatchingSegment(
        Coordinate start, Coordinate end, Polygon polygon)
    {
        if (RingHasMatchingSegment(
            start, end, polygon.Shell.Coordinates)) return true;
        int holeIndex = 0;
        while (holeIndex < polygon.Holes.Length)
        {
            if (RingHasMatchingSegment(
                start, end,
                polygon.Holes[holeIndex].Coordinates))
            {
                return true;
            }
            holeIndex++;
        }
        return false;
    }

    internal static bool RingHasMatchingSegment(
        Coordinate start, Coordinate end, Coordinate[] coordinates)
    {
        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            bool forward =
                RobustLineIntersection.CoordinatesEqualPlanar(
                    start, coordinates[index]) &&
                RobustLineIntersection.CoordinatesEqualPlanar(
                    end, coordinates[index + 1]);
            bool reverse =
                RobustLineIntersection.CoordinatesEqualPlanar(
                    start, coordinates[index + 1]) &&
                RobustLineIntersection.CoordinatesEqualPlanar(
                    end, coordinates[index]);
            if (forward || reverse) return true;
            index++;
        }
        return false;
    }

    internal static CoverageValidationResult? CheckCoverageGap(
        Geometry first, Geometry second,
        int firstIndex, int secondIndex, double maximumGapWidth)
    {
        DistanceResult distance = DistanceAlgorithm.GetIndexedMinimumDistance(
            first, second);
        if (!distance.HasResult || distance.Distance <= 0.0 ||
            distance.Distance > maximumGapWidth ||
            distance.FirstCoordinate == null ||
            distance.SecondCoordinate == null)
        {
            return null;
        }
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(first);
        Geometry witness = GeometryBuilder.CreateLineStringValue(
            [distance.FirstCoordinate, distance.SecondCoordinate], options);
        return CreateCoverageFailure(
            ValidationError.CoverageGap,
            $"Coverage polygons have a gap of width {distance.Distance}.",
            distance.FirstCoordinate,
            "Coverage/Polygons[" + secondIndex +
            "]/GapWith[" + firstIndex + "]",
            witness);
    }

    internal static CoverageValidationResult CreateNonNodedFailure(
        Geometry source, CoverageNodingFailure failure)
    {
        SegmentPairCandidate candidate = failure.Candidate;
        Coordinate firstStart = candidate.First.Coordinates[
            candidate.FirstSegmentIndex];
        Coordinate firstEnd = candidate.First.Coordinates[
            candidate.FirstSegmentIndex + 1];
        Coordinate secondStart = candidate.Second.Coordinates[
            candidate.SecondSegmentIndex];
        Coordinate secondEnd = candidate.Second.Coordinates[
            candidate.SecondSegmentIndex + 1];
        LineString firstLine = new LineString();
        firstLine.Coordinates =
        [
            CoordinateSequences.CopyCoordinateValue(firstStart),
            CoordinateSequences.CopyCoordinateValue(firstEnd)
        ];
        LineString secondLine = new LineString();
        secondLine.Coordinates =
        [
            CoordinateSequences.CopyCoordinateValue(secondStart),
            CoordinateSequences.CopyCoordinateValue(secondEnd)
        ];
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(source);
        Geometry witness = GeometryBuilder.CreateMultiLineStringValue(
            [firstLine, secondLine], options);
        Coordinate? coordinate =
            failure.Intersection.FirstIntersection;
        if (!IndexedNoder.WitnessRequiresNoding(
            coordinate, firstStart, firstEnd,
            secondStart, secondEnd))
        {
            coordinate = failure.Intersection.SecondIntersection;
        }
        int firstIndex = candidate.First.SourceIndex;
        int secondIndex = candidate.Second.SourceIndex;
        if (firstIndex > secondIndex)
        {
            int swap = firstIndex;
            firstIndex = secondIndex;
            secondIndex = swap;
        }
        return CreateCoverageFailure(
            ValidationError.NonNodedIntersection,
            "Coverage boundaries intersect without matching nodes.",
            coordinate,
            "Coverage/Polygons[" + secondIndex +
            "]/NonNodedWith[" + firstIndex + "]",
            witness);
    }

    internal static Coordinate? GetFirstCoordinate(Geometry geometry)
    {
        Coordinate[] coordinates =
            GeometryInspection.ExtractValidatedCoordinates(geometry);
        if (coordinates.Length == 0) return null;
        return CoordinateSequences.CopyCoordinateValue(coordinates[0]);
    }

    internal static CoverageValidationResult CreateCoverageFailure(
        ValidationError error, string message, Coordinate? coordinate,
        string path, Geometry? invalidGeometry)
    {
        CoverageValidationResult result = new CoverageValidationResult();
        result.IsValid = false;
        result.Error = error;
        result.Message = message;
        result.ComponentPath = path;
        if (coordinate != null)
        {
            result.Coordinate =
                CoordinateSequences.CopyCoordinateValue(coordinate);
        }
        if (invalidGeometry != null)
        {
            result.InvalidGeometry =
                GeometryCopy.CopyGeometry(invalidGeometry);
        }
        return result;
    }
}
