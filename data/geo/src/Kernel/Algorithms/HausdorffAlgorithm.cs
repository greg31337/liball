using System;
using System.Collections.Generic;

namespace geo;

internal class DiscreteMetricSample
{
    internal Coordinate Coordinate;
    internal bool HasLocation;
    internal int ComponentIndex;
    internal int SegmentIndex;
    internal double SegmentFraction;

    internal DiscreteMetricSample(
        Coordinate coordinate, bool hasLocation, int componentIndex,
        int segmentIndex, double segmentFraction)
    {
        Coordinate = coordinate;
        HasLocation = hasLocation;
        ComponentIndex = componentIndex;
        SegmentIndex = segmentIndex;
        SegmentFraction = segmentFraction;
    }
}

internal class DiscreteMetricSampleBuilder
{
    internal List<DiscreteMetricSample> Samples =
        new List<DiscreteMetricSample>();
    internal int SubdivisionCount;
    internal int NextComponentIndex;

    internal DiscreteMetricSampleBuilder(int subdivisionCount)
    {
        SubdivisionCount = subdivisionCount;
    }

    internal void AddGeometry(Geometry geometry)
    {
        GeometryInspection.ExtractValidatedCoordinates(geometry);
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            geometry, GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        int index = 0;
        while (index < leaves.Length)
        {
            Geometry leaf = leaves[index];
            if (DistanceAlgorithm.IsEmptyCoreContainer(leaf))
            {
                index++;
                continue;
            }
            RelationGraphBuilder.ValidateAtomicGeometry(leaf);
            object payload = GeometryDispatch.GetSelectedPayload(leaf);
            switch (leaf.GeometryType)
            {
                case GeometryType.Point:
                    AddPoint((Point)payload);
                    break;
                case GeometryType.LineString:
                    AddCoordinateSequence(((LineString)payload).Coordinates);
                    break;
                case GeometryType.LinearRing:
                    AddCoordinateSequence(((LinearRing)payload).Coordinates);
                    break;
                case GeometryType.Polygon:
                    AddPolygon((Polygon)payload);
                    break;
                default:
                    throw new InvalidGeometryException(
                        "Discrete distance does not support this geometry type.");
            }
            index++;
        }
    }

    internal void AddPoint(Point point)
    {
        if (point.Coordinate == null) return;
        AddSample(new DiscreteMetricSample(
            RobustLineIntersection.CopyCoordinate(point.Coordinate),
            false, 0, 0, 0.0));
    }

    internal void AddPolygon(Polygon polygon)
    {
        AddCoordinateSequence(polygon.Shell.Coordinates);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            AddCoordinateSequence(polygon.Holes[index].Coordinates);
            index++;
        }
    }

    internal void AddCoordinateSequence(Coordinate[] coordinates)
    {
        int componentIndex = NextComponentIndex;
        NextComponentIndex++;
        if (coordinates.Length == 0) return;
        bool hasLocation = coordinates.Length > 1;
        AddSample(new DiscreteMetricSample(
            RobustLineIntersection.CopyCoordinate(coordinates[0]),
            hasLocation, componentIndex, 0, 0.0));
        int segmentIndex = 0;
        while (segmentIndex + 1 < coordinates.Length)
        {
            int subdivisionIndex = 1;
            while (subdivisionIndex <= SubdivisionCount)
            {
                double fraction = (double)subdivisionIndex / SubdivisionCount;
                Coordinate coordinate =
                    SegmentDistanceAlgorithms.CreatePointAtSegmentFactor(
                        coordinates[segmentIndex],
                        coordinates[segmentIndex + 1], fraction);
                AddSample(new DiscreteMetricSample(
                    coordinate, true, componentIndex, segmentIndex, fraction));
                subdivisionIndex++;
            }
            segmentIndex++;
        }
    }

    internal void AddSample(DiscreteMetricSample sample)
    {
        if (Samples.Count >= GeometryConstants.DefaultMaximumCoordinates)
        {
            throw new InvalidGeometryException(
                "Densified geometry exceeds the coordinate limit.");
        }
        Samples.Add(sample);
    }
}

internal static class DiscreteMetricSamples
{
    internal static DiscreteMetricSample[] CreateSamples(
        Geometry geometry, int subdivisionCount)
    {
        DiscreteMetricSampleBuilder builder =
            new DiscreteMetricSampleBuilder(subdivisionCount);
        builder.AddGeometry(geometry);
        return builder.Samples.ToArray();
    }

    internal static int GetSubdivisionCount(double densifyFraction)
    {
        if (!double.IsFinite(densifyFraction) || densifyFraction <= 0.0 ||
            densifyFraction > 1.0)
        {
            throw new InvalidGeometryException(
                "Densify fraction must be finite, greater than zero, and at most one.");
        }
        double count = Math.Ceiling(1.0 / densifyFraction);
        if (!double.IsFinite(count) ||
            count > GeometryConstants.DefaultMaximumCoordinates)
        {
            throw new InvalidGeometryException(
                "Densify fraction requires too many segment subdivisions.");
        }
        return (int)count;
    }

    internal static LinearLocation CreateLocation(DiscreteMetricSample sample)
    {
        LinearLocation location = new LinearLocation();
        location.ComponentIndex = sample.ComponentIndex;
        location.SegmentIndex = sample.SegmentIndex;
        location.SegmentFraction = sample.SegmentFraction;
        return location;
    }

    internal static DistanceResult CreatePairResult(
        DiscreteMetricSample first, DiscreteMetricSample second)
    {
        DistanceResult result = SegmentDistanceAlgorithms.GetNearestPointPair(
            first.Coordinate, second.Coordinate);
        if (first.HasLocation) result.FirstLocation = CreateLocation(first);
        if (second.HasLocation) result.SecondLocation = CreateLocation(second);
        return result;
    }

    internal static DistanceResult SelectCloserResult(
        DistanceResult current, DistanceResult candidate)
    {
        return SegmentDistanceAlgorithms.SelectCloserResult(current, candidate);
    }

    internal static DistanceResult SelectFartherResult(
        DistanceResult current, DistanceResult candidate)
    {
        Coordinate? currentFirst = current.FirstCoordinate;
        Coordinate? currentSecond = current.SecondCoordinate;
        Coordinate? candidateFirst = candidate.FirstCoordinate;
        Coordinate? candidateSecond = candidate.SecondCoordinate;
        if (currentFirst == null || currentSecond == null ||
            candidateFirst == null || candidateSecond == null)
        {
            throw new InvalidGeometryException(
                "Discrete-distance candidates require two witness coordinates.");
        }
        ExactDyadicValue currentSquared =
            SegmentDistanceAlgorithms.CreateSquaredSegmentLength(
                currentFirst, currentSecond);
        ExactDyadicValue candidateSquared =
            SegmentDistanceAlgorithms.CreateSquaredSegmentLength(
                candidateFirst, candidateSecond);
        if (ExactDyadicArithmetic.CompareAbsoluteValues(
            candidateSquared, currentSquared) > 0) return candidate;
        return current;
    }
}

internal static class HausdorffAlgorithm
{
    internal static DistanceResult GetDistance(
        Geometry first, Geometry second)
    {
        return GetDistanceWithSubdivisions(first, second, 1);
    }

    internal static DistanceResult GetDensifiedDistance(
        Geometry first, Geometry second, double densifyFraction)
    {
        int subdivisionCount =
            DiscreteMetricSamples.GetSubdivisionCount(densifyFraction);
        return GetDistanceWithSubdivisions(
            first, second, subdivisionCount);
    }

    internal static DistanceResult GetDistanceWithSubdivisions(
        Geometry first, Geometry second, int subdivisionCount)
    {
        GeometryRelations.ValidateSpatialReferencePair(first, second);
        DiscreteMetricSample[] firstSamples =
            DiscreteMetricSamples.CreateSamples(first, subdivisionCount);
        DiscreteMetricSample[] secondSamples =
            DiscreteMetricSamples.CreateSamples(second, subdivisionCount);
        DistanceGeometryParts firstParts = new DistanceGeometryParts(first);
        DistanceGeometryParts secondParts = new DistanceGeometryParts(second);
        DistanceResult best = new DistanceResult();
        AddDirectedCandidates(firstSamples, secondParts, true, best);
        AddDirectedCandidates(secondSamples, firstParts, false, best);
        return best;
    }

    internal static void AddDirectedCandidates(
        DiscreteMetricSample[] source, DistanceGeometryParts target,
        bool sourceBelongsToFirst, DistanceResult best)
    {
        int index = 0;
        while (index < source.Length)
        {
            DistanceResult candidate = GetPointToGeometryDistance(
                source[index], target);
            if (candidate.HasResult)
            {
                if (source[index].HasLocation)
                {
                    candidate.FirstLocation =
                        DiscreteMetricSamples.CreateLocation(source[index]);
                }
                if (!sourceBelongsToFirst)
                {
                    DistanceAlgorithm.ReverseResult(candidate);
                }
                SelectFarthestCandidate(best, candidate);
            }
            index++;
        }
    }

    internal static DistanceResult GetPointToGeometryDistance(
        DiscreteMetricSample sample, DistanceGeometryParts target)
    {
        DistanceResult best = new DistanceResult();
        DistancePointFacet point = new DistancePointFacet(sample.Coordinate);
        int pointIndex = 0;
        while (pointIndex < target.Points.Count)
        {
            DistanceResult candidate =
                SegmentDistanceAlgorithms.GetNearestPointPair(
                    sample.Coordinate,
                    target.Points[pointIndex].Coordinate);
            DistanceAlgorithm.SelectCandidate(best, candidate);
            pointIndex++;
        }
        int segmentIndex = 0;
        while (segmentIndex < target.Segments.Count)
        {
            DistanceResult candidate =
                DistanceAlgorithm.CreatePointSegmentCandidate(
                    point, target.Segments[segmentIndex], true);
            DistanceAlgorithm.SelectCandidate(best, candidate);
            segmentIndex++;
        }
        if (best.HasResult && best.Distance == 0.0) return best;
        int surfaceIndex = 0;
        while (surfaceIndex < target.Surfaces.Count)
        {
            TopologicalLocation location =
                GeometryPointLocation.LocatePointInPolygon(
                    sample.Coordinate, target.Surfaces[surfaceIndex]);
            if (location == TopologicalLocation.Interior)
            {
                DistanceSampleFacet containmentSample =
                    new DistanceSampleFacet(
                        sample.Coordinate, false, 0, 0, 0.0);
                return DistanceAlgorithm.CreateContainmentResult(
                    containmentSample, true);
            }
            surfaceIndex++;
        }
        return best;
    }

    internal static void SelectFarthestCandidate(
        DistanceResult best, DistanceResult candidate)
    {
        if (!best.HasResult)
        {
            DistanceAlgorithm.CopyResult(candidate, best);
            return;
        }
        DistanceResult selected = DiscreteMetricSamples.SelectFartherResult(
            best, candidate);
        if (selected == candidate)
        {
            DistanceAlgorithm.CopyResult(candidate, best);
        }
    }
}
