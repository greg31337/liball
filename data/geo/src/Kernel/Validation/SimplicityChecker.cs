using System.Collections.Generic;

namespace geo;

internal class SimplicityLine
{
    internal Coordinate[] Coordinates;
    internal string Path;

    internal SimplicityLine(
        Coordinate[] coordinates, string path)
    {
        Coordinates = CoordinateSequences.CopyCoordinateSequence(
            coordinates);
        Path = path;
    }
}

internal static class SimplicityChecker
{
    internal static ValidationResult CheckSimplicity(
        Geometry? geometry, BoundaryNodeRule boundaryNodeRule)
    {
        GeometryBoundary.ValidateBoundaryNodeRule(boundaryNodeRule);
        ValidationResult structure =
            StructuralValidator.ValidateGeometry(geometry);
        if (!structure.IsValid || geometry == null) return structure;
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                return new ValidationResult();
            case GeometryType.MultiPoint:
                return CheckMultiPoint(
                    geometry.MultiPoint, "MultiPoint");
            case GeometryType.LineString:
                LineString line = (LineString)
                    GeometryDispatch.GetSelectedPayload(geometry);
                return CheckLinealCoordinates(
                    [line.Coordinates],
                    ["LineString"], boundaryNodeRule);
            case GeometryType.LinearRing:
                LinearRing ring = (LinearRing)
                    GeometryDispatch.GetSelectedPayload(geometry);
                return CheckLinealCoordinates(
                    [ring.Coordinates],
                    ["LinearRing"], boundaryNodeRule);
            case GeometryType.MultiLineString:
                return CheckMultiLine(
                    geometry.MultiLineString, boundaryNodeRule);
            default:
                return StructuralValidator.CreateFailureResult(
                    ValidationError.InvalidGeometryType,
                    "Simplicity checking supports point and core lineal geometry.",
                    null, geometry.GeometryType.ToString());
        }
    }

    internal static ValidationResult CheckMultiPoint(
        MultiPoint? multiPoint, string path)
    {
        if (multiPoint == null)
        {
            return StructuralValidator.CreateFailureResult(
                ValidationError.NullGeometry,
                "MultiPoint payload is null.", null, path);
        }
        Dictionary<string, Coordinate> coordinates =
            new Dictionary<string, Coordinate>();
        int index = 0;
        while (index < multiPoint.Points.Length)
        {
            Coordinate? coordinate = multiPoint.Points[index].Coordinate;
            if (coordinate != null)
            {
                PlanarNodeKey key = new PlanarNodeKey(coordinate);
                if (coordinates.ContainsKey(key.Identity))
                {
                    return StructuralValidator.CreateFailureResult(
                        ValidationError.RepeatedCoordinate,
                        "MultiPoint contains a repeated planar coordinate.",
                        coordinate,
                        path + "/Points[" + index + "]");
                }
                coordinates.Add(key.Identity, coordinate);
            }
            index++;
        }
        return new ValidationResult();
    }

    internal static ValidationResult CheckMultiLine(
        MultiLineString? multiLine,
        BoundaryNodeRule boundaryNodeRule)
    {
        if (multiLine == null)
        {
            return StructuralValidator.CreateFailureResult(
                ValidationError.NullGeometry,
                "MultiLineString payload is null.",
                null, "MultiLineString");
        }
        Coordinate[][] coordinates =
            new Coordinate[multiLine.LineStrings.Length][];
        string[] paths = new string[multiLine.LineStrings.Length];
        int index = 0;
        while (index < coordinates.Length)
        {
            coordinates[index] =
                multiLine.LineStrings[index].Coordinates;
            paths[index] =
                "MultiLineString/LineStrings[" + index + "]";
            index++;
        }
        return CheckLinealCoordinates(
            coordinates, paths, boundaryNodeRule);
    }

    internal static ValidationResult CheckLinealCoordinates(
        Coordinate[][] sources, string[] paths,
        BoundaryNodeRule boundaryNodeRule)
    {
        List<SegmentString> segmentStrings =
            new List<SegmentString>();
        int index = 0;
        while (index < sources.Length)
        {
            Coordinate[] cleaned =
                DouglasPeucker.RemoveAdjacentDuplicates(
                    sources[index], false);
            SimplicityLine line = new SimplicityLine(
                cleaned, paths[index]);
            if (cleaned.Length >= 2 &&
                !AllCoordinatesEqual(cleaned))
            {
                segmentStrings.Add(
                    SegmentNoding.CreateSegmentString(
                        cleaned, line, index));
            }
            index++;
        }
        if (segmentStrings.Count == 0)
        {
            return new ValidationResult();
        }
        SweepLineIndex sweep = new SweepLineIndex(
            segmentStrings.ToArray());
        SegmentPairCandidate[] candidates =
            sweep.GetCandidatePairs(true);
        index = 0;
        while (index < candidates.Length)
        {
            ValidationResult? failure = CheckSegmentPair(
                candidates[index], boundaryNodeRule);
            if (failure != null) return failure;
            index++;
        }
        return new ValidationResult();
    }

    internal static ValidationResult? CheckSegmentPair(
        SegmentPairCandidate candidate,
        BoundaryNodeRule boundaryNodeRule)
    {
        Coordinate firstStart =
            candidate.First.Coordinates[
                candidate.FirstSegmentIndex];
        Coordinate firstEnd =
            candidate.First.Coordinates[
                candidate.FirstSegmentIndex + 1];
        Coordinate secondStart =
            candidate.Second.Coordinates[
                candidate.SecondSegmentIndex];
        Coordinate secondEnd =
            candidate.Second.Coordinates[
                candidate.SecondSegmentIndex + 1];
        SegmentIntersectionResult intersection =
            RobustLineIntersection.GetSegmentIntersection(
                firstStart, firstEnd, secondStart, secondEnd);
        if (intersection.Type == SegmentIntersectionType.None)
        {
            return null;
        }
        SimplicityLine firstLine = GetSourceLine(candidate.First);
        SimplicityLine secondLine = GetSourceLine(candidate.Second);
        if (candidate.First == candidate.Second)
        {
            if (SelfIntersectionIsAllowed(candidate, intersection))
            {
                return null;
            }
            return CreateIntersectionFailure(
                intersection, firstLine.Path,
                candidate.SecondSegmentIndex);
        }
        if (intersection.Type ==
            SegmentIntersectionType.CollinearOverlap)
        {
            return CreateIntersectionFailure(
                intersection, secondLine.Path,
                candidate.SecondSegmentIndex);
        }
        Coordinate? firstWitness = intersection.FirstIntersection;
        if (firstWitness != null && !MutualEndpointIsAllowed(
            firstWitness, firstLine, secondLine,
            boundaryNodeRule))
        {
            return CreateWitnessFailure(
                firstWitness, secondLine.Path,
                candidate.SecondSegmentIndex);
        }
        Coordinate? secondWitness = intersection.SecondIntersection;
        if (secondWitness != null && !MutualEndpointIsAllowed(
            secondWitness, firstLine, secondLine,
            boundaryNodeRule))
        {
            return CreateWitnessFailure(
                secondWitness, secondLine.Path,
                candidate.SecondSegmentIndex);
        }
        return null;
    }

    internal static SimplicityLine GetSourceLine(
        SegmentString segmentString)
    {
        object? metadata = segmentString.SourceMetadata;
        if (metadata == null)
        {
            throw new InvalidGeometryException(
                "Simplicity segment metadata is null.");
        }
        return (SimplicityLine)metadata;
    }

    internal static bool SelfIntersectionIsAllowed(
        SegmentPairCandidate candidate,
        SegmentIntersectionResult intersection)
    {
        if (intersection.Type ==
            SegmentIntersectionType.CollinearOverlap)
        {
            return false;
        }
        int first = candidate.FirstSegmentIndex;
        int second = candidate.SecondSegmentIndex;
        int difference = first - second;
        if (difference < 0) difference = -difference;
        if (difference == 1) return true;
        Coordinate[] coordinates = candidate.First.Coordinates;
        bool closed = RobustLineIntersection.CoordinatesEqualPlanar(
            coordinates[0],
            coordinates[coordinates.Length - 1]);
        if (!closed) return false;
        int lastSegment = coordinates.Length - 2;
        return first == 0 && second == lastSegment ||
            second == 0 && first == lastSegment;
    }

    internal static bool MutualEndpointIsAllowed(
        Coordinate witness, SimplicityLine first,
        SimplicityLine second,
        BoundaryNodeRule boundaryNodeRule)
    {
        if (!IsLineEndpoint(witness, first.Coordinates) ||
            !IsLineEndpoint(witness, second.Coordinates))
        {
            return false;
        }
        if (IsClosed(first.Coordinates) &&
            !GeometryBoundary.ShouldIncludeEndpointNode(
                2, boundaryNodeRule))
        {
            return false;
        }
        if (IsClosed(second.Coordinates) &&
            !GeometryBoundary.ShouldIncludeEndpointNode(
                2, boundaryNodeRule))
        {
            return false;
        }
        return true;
    }

    internal static bool IsLineEndpoint(
        Coordinate coordinate, Coordinate[] line)
    {
        if (line.Length == 0) return false;
        return RobustLineIntersection.CoordinatesEqualPlanar(
            coordinate, line[0]) ||
            RobustLineIntersection.CoordinatesEqualPlanar(
            coordinate, line[line.Length - 1]);
    }

    internal static bool IsClosed(Coordinate[] line)
    {
        if (line.Length == 0) return false;
        return RobustLineIntersection.CoordinatesEqualPlanar(
            line[0], line[line.Length - 1]);
    }

    internal static bool AllCoordinatesEqual(Coordinate[] coordinates)
    {
        int index = 1;
        while (index < coordinates.Length)
        {
            if (!RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[0], coordinates[index]))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static ValidationResult CreateIntersectionFailure(
        SegmentIntersectionResult intersection,
        string path, int segmentIndex)
    {
        Coordinate? coordinate = intersection.FirstIntersection;
        return CreateWitnessFailure(
            coordinate, path, segmentIndex);
    }

    internal static ValidationResult CreateWitnessFailure(
        Coordinate? coordinate, string path, int segmentIndex)
    {
        return StructuralValidator.CreateFailureResult(
            ValidationError.SelfIntersection,
            "Lineal geometry contains a non-simple intersection.",
            coordinate, path + "/Segments[" + segmentIndex + "]");
    }
}
