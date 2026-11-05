using System;
using System.Collections.Generic;

namespace geo;

internal class DistancePointFacet
{
    internal Coordinate Coordinate;

    internal DistancePointFacet(Coordinate coordinate)
    {
        Coordinate = coordinate;
    }
}

internal class DistanceSegmentFacet
{
    internal Coordinate Start;
    internal Coordinate End;
    internal int ComponentIndex;
    internal int SegmentIndex;
    internal int Order;
    internal Envelope Bounds;

    internal DistanceSegmentFacet(
        Coordinate start, Coordinate end, int componentIndex,
        int segmentIndex, int order)
    {
        Start = start;
        End = end;
        ComponentIndex = componentIndex;
        SegmentIndex = segmentIndex;
        Order = order;
        Bounds = EnvelopeAlgorithms.CreateBoundsEnvelope(
            start.X < end.X ? start.X : end.X,
            start.Y < end.Y ? start.Y : end.Y,
            start.X > end.X ? start.X : end.X,
            start.Y > end.Y ? start.Y : end.Y);
    }
}

internal class DistanceSampleFacet
{
    internal Coordinate Coordinate;
    internal bool HasLocation;
    internal int ComponentIndex;
    internal int SegmentIndex;
    internal double SegmentFraction;

    internal DistanceSampleFacet(
        Coordinate coordinate, bool hasLocation,
        int componentIndex, int segmentIndex, double segmentFraction)
    {
        Coordinate = coordinate;
        HasLocation = hasLocation;
        ComponentIndex = componentIndex;
        SegmentIndex = segmentIndex;
        SegmentFraction = segmentFraction;
    }
}

internal class DistanceGeometryParts
{
    internal Geometry RootGeometry;
    internal List<DistancePointFacet> Points =
        new List<DistancePointFacet>();
    internal List<DistanceSegmentFacet> Segments =
        new List<DistanceSegmentFacet>();
    internal List<DistanceSampleFacet> Samples =
        new List<DistanceSampleFacet>();
    internal List<Polygon> Surfaces = new List<Polygon>();
    internal int NextComponentIndex;

    internal DistanceGeometryParts(Geometry geometry)
    {
        RootGeometry = geometry;
        DistanceAlgorithm.PopulateGeometryParts(geometry, this);
    }
}

internal static class DistanceAlgorithm
{
    internal static DistanceResult GetMinimumDistance(
        Geometry first, Geometry second)
    {
        GeometryRelations.ValidateSpatialReferencePair(first, second);
        DistanceGeometryParts firstParts = new DistanceGeometryParts(first);
        DistanceGeometryParts secondParts = new DistanceGeometryParts(second);
        return GetBruteForceDistance(firstParts, secondParts);
    }

    internal static DistanceResult GetIndexedMinimumDistance(
        Geometry first, Geometry second)
    {
        GeometryRelations.ValidateSpatialReferencePair(first, second);
        DistanceGeometryParts firstParts = new DistanceGeometryParts(first);
        DistanceGeometryParts secondParts = new DistanceGeometryParts(second);
        return GetIndexedDistance(firstParts, secondParts);
    }

    internal static PreparedDistanceFacets PrepareDistanceFacets(
        Geometry geometry)
    {
        return new PreparedDistanceFacets(geometry);
    }

    internal static DistanceResult GetPreparedMinimumDistance(
        PreparedDistanceFacets first, PreparedDistanceFacets second)
    {
        ValidatePreparedPair(first, second);
        return GetIndexedDistanceWithIndexes(
            first.Parts, first.SegmentIndex,
            second.Parts, second.SegmentIndex);
    }

    internal static bool IsWithinDistance(
        Geometry first, Geometry second, double maximumDistance)
    {
        ValidateDistanceLimit(maximumDistance);
        GeometryRelations.ValidateSpatialReferencePair(first, second);
        DistanceGeometryParts firstParts = new DistanceGeometryParts(first);
        DistanceGeometryParts secondParts = new DistanceGeometryParts(second);
        Envelope firstBounds = EnvelopeAlgorithms.CreateGeometryEnvelope(first);
        Envelope secondBounds = EnvelopeAlgorithms.CreateGeometryEnvelope(second);
        if (!PassesDistanceEnvelopeFilter(
            firstParts, firstBounds, secondParts, secondBounds,
            maximumDistance))
        {
            return false;
        }
        SegmentSpatialIndex firstIndex = new SegmentSpatialIndex(
            firstParts.Segments.ToArray());
        SegmentSpatialIndex secondIndex = new SegmentSpatialIndex(
            secondParts.Segments.ToArray());
        return AreFacetCandidatesWithinDistance(
            firstParts, firstIndex, secondParts, secondIndex,
            maximumDistance);
    }

    internal static bool ArePreparedWithinDistance(
        PreparedDistanceFacets first, PreparedDistanceFacets second,
        double maximumDistance)
    {
        ValidateDistanceLimit(maximumDistance);
        ValidatePreparedPair(first, second);
        return ArePartsWithinDistance(
            first.Parts, first.SegmentIndex, first.Bounds,
            second.Parts, second.SegmentIndex, second.Bounds,
            maximumDistance);
    }

    internal static double GetEnvelopeDistance(
        Geometry first, Geometry second)
    {
        GeometryRelations.ValidateSpatialReferencePair(first, second);
        GeometryInspection.ExtractValidatedCoordinates(first);
        GeometryInspection.ExtractValidatedCoordinates(second);
        Envelope firstBounds = EnvelopeAlgorithms.CreateGeometryEnvelope(first);
        Envelope secondBounds = EnvelopeAlgorithms.CreateGeometryEnvelope(second);
        return EnvelopeAlgorithms.DistanceBetweenEnvelopes(
            firstBounds, secondBounds);
    }

    internal static double GetPreparedEnvelopeDistance(
        PreparedDistanceFacets first, PreparedDistanceFacets second)
    {
        ValidatePreparedPair(first, second);
        return EnvelopeAlgorithms.DistanceBetweenEnvelopes(
            first.Bounds, second.Bounds);
    }

    internal static DistanceResult GetBruteForceDistance(
        DistanceGeometryParts first, DistanceGeometryParts second)
    {
        DistanceResult best = new DistanceResult();
        AddPointPointCandidates(first, second, best);
        AddPointSegmentCandidates(first, second, best);
        AddSegmentPointCandidates(first, second, best);
        AddSegmentSegmentCandidates(first, second, best);
        AddContainmentCandidates(first, second, best);
        return best;
    }

    internal static DistanceResult GetIndexedDistance(
        DistanceGeometryParts first, DistanceGeometryParts second)
    {
        SegmentSpatialIndex secondIndex = new SegmentSpatialIndex(
            second.Segments.ToArray());
        SegmentSpatialIndex firstIndex = new SegmentSpatialIndex(
            first.Segments.ToArray());
        return GetIndexedDistanceWithIndexes(
            first, firstIndex, second, secondIndex);
    }

    internal static DistanceResult GetIndexedDistanceWithIndexes(
        DistanceGeometryParts first, SegmentSpatialIndex firstIndex,
        DistanceGeometryParts second, SegmentSpatialIndex secondIndex)
    {
        DistanceResult best = new DistanceResult();
        AddPointPointCandidates(first, second, best);
        AddIndexedPointSegmentCandidates(
            first.Points, secondIndex, true, best);
        AddIndexedPointSegmentCandidates(
            second.Points, firstIndex, false, best);
        AddIndexedSegmentSegmentCandidates(
            first.Segments, secondIndex, best);
        AddContainmentCandidates(first, second, best);
        return best;
    }

    internal static bool ArePartsWithinDistance(
        DistanceGeometryParts first, SegmentSpatialIndex firstIndex,
        Envelope firstBounds, DistanceGeometryParts second,
        SegmentSpatialIndex secondIndex, Envelope secondBounds,
        double maximumDistance)
    {
        if (!PassesDistanceEnvelopeFilter(
            first, firstBounds, second, secondBounds, maximumDistance))
        {
            return false;
        }
        return AreFacetCandidatesWithinDistance(
            first, firstIndex, second, secondIndex, maximumDistance);
    }

    internal static bool PassesDistanceEnvelopeFilter(
        DistanceGeometryParts first, Envelope firstBounds,
        DistanceGeometryParts second, Envelope secondBounds,
        double maximumDistance)
    {
        if (!HasDistanceFacets(first) || !HasDistanceFacets(second))
        {
            return false;
        }
        double envelopeDistance = EnvelopeAlgorithms.DistanceBetweenEnvelopes(
            firstBounds, secondBounds);
        return IsDistanceWithinLimit(envelopeDistance, maximumDistance);
    }

    internal static bool AreFacetCandidatesWithinDistance(
        DistanceGeometryParts first, SegmentSpatialIndex firstIndex,
        DistanceGeometryParts second, SegmentSpatialIndex secondIndex,
        double maximumDistance)
    {
        if (HasContainment(first, second) || HasContainment(second, first))
        {
            return true;
        }
        if (AnyPointPairWithinDistance(
            first.Points, second.Points, maximumDistance)) return true;
        if (AnyIndexedPointSegmentWithinDistance(
            first.Points, secondIndex, maximumDistance)) return true;
        if (AnyIndexedPointSegmentWithinDistance(
            second.Points, firstIndex, maximumDistance)) return true;
        return AnyIndexedSegmentPairWithinDistance(
            first.Segments, secondIndex, maximumDistance);
    }

    internal static bool HasDistanceFacets(DistanceGeometryParts parts)
    {
        return parts.Points.Count != 0 || parts.Segments.Count != 0;
    }

    internal static bool HasContainment(
        DistanceGeometryParts samples, DistanceGeometryParts surfaces)
    {
        int sampleIndex = 0;
        while (sampleIndex < samples.Samples.Count)
        {
            int surfaceIndex = 0;
            while (surfaceIndex < surfaces.Surfaces.Count)
            {
                TopologicalLocation location =
                    GeometryPointLocation.LocatePointInPolygon(
                        samples.Samples[sampleIndex].Coordinate,
                        surfaces.Surfaces[surfaceIndex]);
                if (location != TopologicalLocation.Exterior) return true;
                surfaceIndex++;
            }
            sampleIndex++;
        }
        return false;
    }

    internal static bool AnyPointPairWithinDistance(
        List<DistancePointFacet> first, List<DistancePointFacet> second,
        double maximumDistance)
    {
        int firstIndex = 0;
        while (firstIndex < first.Count)
        {
            int secondIndex = 0;
            while (secondIndex < second.Count)
            {
                double distance = SegmentDistanceAlgorithms.GetPlanarDistance(
                    first[firstIndex].Coordinate,
                    second[secondIndex].Coordinate);
                if (IsDistanceWithinLimit(distance, maximumDistance))
                {
                    return true;
                }
                secondIndex++;
            }
            firstIndex++;
        }
        return false;
    }

    internal static bool AnyIndexedPointSegmentWithinDistance(
        List<DistancePointFacet> points, SegmentSpatialIndex index,
        double maximumDistance)
    {
        int pointIndex = 0;
        while (pointIndex < points.Count)
        {
            DistancePointFacet point = points[pointIndex];
            Envelope bounds = EnvelopeAlgorithms.CreateCoordinateEnvelope(
                point.Coordinate);
            DistanceSegmentFacet[] segments = QuerySegmentsWithinDistance(
                index, bounds, maximumDistance);
            int segmentIndex = 0;
            while (segmentIndex < segments.Length)
            {
                DistanceResult result =
                    SegmentDistanceAlgorithms.GetNearestPointSegment(
                        point.Coordinate, segments[segmentIndex].Start,
                        segments[segmentIndex].End);
                if (IsDistanceWithinLimit(result.Distance, maximumDistance))
                {
                    return true;
                }
                segmentIndex++;
            }
            pointIndex++;
        }
        return false;
    }

    internal static bool AnyIndexedSegmentPairWithinDistance(
        List<DistanceSegmentFacet> first, SegmentSpatialIndex second,
        double maximumDistance)
    {
        int firstIndex = 0;
        while (firstIndex < first.Count)
        {
            DistanceSegmentFacet firstSegment = first[firstIndex];
            DistanceSegmentFacet[] candidates = QuerySegmentsWithinDistance(
                second, firstSegment.Bounds, maximumDistance);
            int candidateIndex = 0;
            while (candidateIndex < candidates.Length)
            {
                DistanceSegmentFacet secondSegment = candidates[candidateIndex];
                DistanceResult result =
                    SegmentDistanceAlgorithms.GetNearestSegmentPair(
                        firstSegment.Start, firstSegment.End,
                        secondSegment.Start, secondSegment.End);
                if (IsDistanceWithinLimit(result.Distance, maximumDistance))
                {
                    return true;
                }
                candidateIndex++;
            }
            firstIndex++;
        }
        return false;
    }

    internal static DistanceSegmentFacet[] QuerySegmentsWithinDistance(
        SegmentSpatialIndex index, Envelope bounds, double maximumDistance)
    {
        double searchDistance = GetDistanceSearchLimit(maximumDistance);
        if (!CanExpandBounds(bounds, searchDistance))
        {
            return index.GetAllSegments();
        }
        Envelope search = EnvelopeAlgorithms.ExpandEnvelopeByDistance(
            bounds, searchDistance);
        return index.Query(search);
    }

    internal static double GetDistanceSearchLimit(double maximumDistance)
    {
        if (maximumDistance == 0.0) return 0.0;
        double allowance = maximumDistance * 4.4408920985006262E-16;
        return maximumDistance + allowance;
    }

    internal static bool IsDistanceWithinLimit(
        double distance, double maximumDistance)
    {
        if (distance <= maximumDistance) return true;
        if (!double.IsFinite(distance) || maximumDistance == 0.0)
        {
            return false;
        }
        double scale = Math.Max(distance, maximumDistance);
        double roundingAllowance = scale * 4.4408920985006262E-16;
        return distance <= maximumDistance + roundingAllowance;
    }

    internal static void ValidateDistanceLimit(double maximumDistance)
    {
        if (!double.IsFinite(maximumDistance) || maximumDistance < 0.0)
        {
            throw new InvalidGeometryException(
                "Maximum distance must be finite and non-negative.");
        }
    }

    internal static void ValidatePreparedPair(
        PreparedDistanceFacets first, PreparedDistanceFacets second)
    {
        if (first == null || second == null)
        {
            throw new InvalidGeometryException(
                "Prepared distance facets are null.");
        }
        GeometryRelations.ValidateSpatialReferencePair(
            first.GeometrySnapshot, second.GeometrySnapshot);
    }

    internal static void PopulateGeometryParts(
        Geometry geometry, DistanceGeometryParts parts)
    {
        GeometryInspection.ExtractValidatedCoordinates(geometry);
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            geometry, GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        int index = 0;
        while (index < leaves.Length)
        {
            Geometry leaf = leaves[index];
            if (IsEmptyCoreContainer(leaf))
            {
                index++;
                continue;
            }
            RelationGraphBuilder.ValidateAtomicGeometry(leaf);
            object payload = GeometryDispatch.GetSelectedPayload(leaf);
            switch (leaf.GeometryType)
            {
                case GeometryType.Point:
                    AddPointGeometry((Point)payload, parts);
                    break;
                case GeometryType.LineString:
                    AddCoordinateSequence(
                        ((LineString)payload).Coordinates, parts);
                    break;
                case GeometryType.LinearRing:
                    AddCoordinateSequence(
                        ((LinearRing)payload).Coordinates, parts);
                    break;
                case GeometryType.Polygon:
                    AddPolygonGeometry((Polygon)payload, parts);
                    break;
                default:
                    throw new InvalidGeometryException(
                        "Core planar distance does not support this geometry type.");
            }
            index++;
        }
    }

    internal static bool IsEmptyCoreContainer(Geometry geometry)
    {
        switch (geometry.GeometryType)
        {
            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                return GeometryInspection.IsEmptyValue(geometry);
            default:
                return false;
        }
    }

    internal static void AddPointGeometry(
        Point point, DistanceGeometryParts parts)
    {
        if (point.Coordinate == null) return;
        DistancePointFacet facet = new DistancePointFacet(
            point.Coordinate);
        parts.Points.Add(facet);
        parts.Samples.Add(new DistanceSampleFacet(
            point.Coordinate, false, 0, 0, 0.0));
    }

    internal static void AddCoordinateSequence(
        Coordinate[] coordinates, DistanceGeometryParts parts)
    {
        int componentIndex = parts.NextComponentIndex;
        parts.NextComponentIndex++;
        if (coordinates.Length == 0) return;
        bool hasLocation = coordinates.Length > 1;
        parts.Samples.Add(new DistanceSampleFacet(
            coordinates[0], hasLocation, componentIndex, 0, 0.0));
        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            parts.Segments.Add(new DistanceSegmentFacet(
                coordinates[index], coordinates[index + 1],
                componentIndex, index, parts.Segments.Count));
            index++;
        }
    }

    internal static void AddPolygonGeometry(
        Polygon polygon, DistanceGeometryParts parts)
    {
        parts.Surfaces.Add(polygon);
        AddCoordinateSequence(polygon.Shell.Coordinates, parts);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            AddCoordinateSequence(polygon.Holes[index].Coordinates, parts);
            index++;
        }
    }

    internal static void AddPointPointCandidates(
        DistanceGeometryParts first, DistanceGeometryParts second,
        DistanceResult best)
    {
        int firstIndex = 0;
        while (firstIndex < first.Points.Count)
        {
            int secondIndex = 0;
            while (secondIndex < second.Points.Count)
            {
                DistanceResult candidate =
                    SegmentDistanceAlgorithms.GetNearestPointPair(
                        first.Points[firstIndex].Coordinate,
                        second.Points[secondIndex].Coordinate);
                SelectCandidate(best, candidate);
                secondIndex++;
            }
            firstIndex++;
        }
    }

    internal static void AddPointSegmentCandidates(
        DistanceGeometryParts first, DistanceGeometryParts second,
        DistanceResult best)
    {
        int pointIndex = 0;
        while (pointIndex < first.Points.Count)
        {
            int segmentIndex = 0;
            while (segmentIndex < second.Segments.Count)
            {
                DistanceResult candidate = CreatePointSegmentCandidate(
                    first.Points[pointIndex],
                    second.Segments[segmentIndex], true);
                SelectCandidate(best, candidate);
                segmentIndex++;
            }
            pointIndex++;
        }
    }

    internal static void AddSegmentPointCandidates(
        DistanceGeometryParts first, DistanceGeometryParts second,
        DistanceResult best)
    {
        int pointIndex = 0;
        while (pointIndex < second.Points.Count)
        {
            int segmentIndex = 0;
            while (segmentIndex < first.Segments.Count)
            {
                DistanceResult candidate = CreatePointSegmentCandidate(
                    second.Points[pointIndex],
                    first.Segments[segmentIndex], false);
                SelectCandidate(best, candidate);
                segmentIndex++;
            }
            pointIndex++;
        }
    }

    internal static void AddSegmentSegmentCandidates(
        DistanceGeometryParts first, DistanceGeometryParts second,
        DistanceResult best)
    {
        int firstIndex = 0;
        while (firstIndex < first.Segments.Count)
        {
            int secondIndex = 0;
            while (secondIndex < second.Segments.Count)
            {
                DistanceResult candidate = CreateSegmentSegmentCandidate(
                    first.Segments[firstIndex],
                    second.Segments[secondIndex]);
                SelectCandidate(best, candidate);
                secondIndex++;
            }
            firstIndex++;
        }
    }

    internal static void AddIndexedPointSegmentCandidates(
        List<DistancePointFacet> points, SegmentSpatialIndex index,
        bool pointsBelongToFirst, DistanceResult best)
    {
        int pointIndex = 0;
        while (pointIndex < points.Count)
        {
            DistancePointFacet point = points[pointIndex];
            Envelope bounds = EnvelopeAlgorithms.CreateCoordinateEnvelope(
                point.Coordinate);
            DistanceSegmentFacet[] candidates = QueryNearbySegments(
                index, bounds, best);
            int candidateIndex = 0;
            while (candidateIndex < candidates.Length)
            {
                DistanceResult candidate = CreatePointSegmentCandidate(
                    point, candidates[candidateIndex], pointsBelongToFirst);
                SelectCandidate(best, candidate);
                candidateIndex++;
            }
            pointIndex++;
        }
    }

    internal static void AddIndexedSegmentSegmentCandidates(
        List<DistanceSegmentFacet> firstSegments,
        SegmentSpatialIndex secondIndex, DistanceResult best)
    {
        int firstIndex = 0;
        while (firstIndex < firstSegments.Count)
        {
            DistanceSegmentFacet first = firstSegments[firstIndex];
            DistanceSegmentFacet[] candidates = QueryNearbySegments(
                secondIndex, first.Bounds, best);
            int candidateIndex = 0;
            while (candidateIndex < candidates.Length)
            {
                DistanceResult candidate = CreateSegmentSegmentCandidate(
                    first, candidates[candidateIndex]);
                SelectCandidate(best, candidate);
                candidateIndex++;
            }
            firstIndex++;
        }
    }

    internal static DistanceSegmentFacet[] QueryNearbySegments(
        SegmentSpatialIndex index, Envelope bounds, DistanceResult best)
    {
        if (!best.HasResult || !double.IsFinite(best.Distance) ||
            !CanExpandBounds(bounds, best.Distance))
        {
            return index.GetAllSegments();
        }
        Envelope search = EnvelopeAlgorithms.ExpandEnvelopeByDistance(
            bounds, best.Distance);
        return index.Query(search);
    }

    internal static bool CanExpandBounds(Envelope bounds, double distance)
    {
        return double.IsFinite(bounds.MinX - distance) &&
            double.IsFinite(bounds.MinY - distance) &&
            double.IsFinite(bounds.MaxX + distance) &&
            double.IsFinite(bounds.MaxY + distance);
    }

    internal static DistanceResult CreatePointSegmentCandidate(
        DistancePointFacet point, DistanceSegmentFacet segment,
        bool pointBelongsToFirst)
    {
        DistanceResult result =
            SegmentDistanceAlgorithms.GetNearestPointSegment(
                point.Coordinate, segment.Start, segment.End);
        SetSegmentLocation(result.SecondLocation, segment);
        if (!pointBelongsToFirst) ReverseResult(result);
        return result;
    }

    internal static DistanceResult CreateSegmentSegmentCandidate(
        DistanceSegmentFacet first, DistanceSegmentFacet second)
    {
        DistanceResult result =
            SegmentDistanceAlgorithms.GetNearestSegmentPair(
                first.Start, first.End, second.Start, second.End);
        SetSegmentLocation(result.FirstLocation, first);
        SetSegmentLocation(result.SecondLocation, second);
        return result;
    }

    internal static void SetSegmentLocation(
        LinearLocation? location, DistanceSegmentFacet segment)
    {
        if (location == null)
        {
            throw new InvalidGeometryException(
                "A segment distance candidate requires a linear location.");
        }
        location.ComponentIndex = segment.ComponentIndex;
        location.SegmentIndex = segment.SegmentIndex;
    }

    internal static void ReverseResult(DistanceResult result)
    {
        Coordinate? coordinate = result.FirstCoordinate;
        result.FirstCoordinate = result.SecondCoordinate;
        result.SecondCoordinate = coordinate;
        LinearLocation? location = result.FirstLocation;
        result.FirstLocation = result.SecondLocation;
        result.SecondLocation = location;
    }

    internal static void SelectCandidate(
        DistanceResult best, DistanceResult candidate)
    {
        if (!best.HasResult)
        {
            CopyResult(candidate, best);
            return;
        }
        DistanceResult selected =
            SegmentDistanceAlgorithms.SelectCloserResult(best, candidate);
        if (selected == candidate) CopyResult(candidate, best);
    }

    internal static void AddContainmentCandidates(
        DistanceGeometryParts first, DistanceGeometryParts second,
        DistanceResult best)
    {
        if (best.HasResult && best.Distance == 0.0) return;
        int sampleIndex = 0;
        while (sampleIndex < first.Samples.Count)
        {
            int surfaceIndex = 0;
            while (surfaceIndex < second.Surfaces.Count)
            {
                DistanceSampleFacet sample = first.Samples[sampleIndex];
                TopologicalLocation location =
                    GeometryPointLocation.LocatePointInPolygon(
                        sample.Coordinate, second.Surfaces[surfaceIndex]);
                if (location == TopologicalLocation.Interior)
                {
                    DistanceResult candidate = CreateContainmentResult(
                        sample, true);
                    CopyResult(candidate, best);
                    return;
                }
                surfaceIndex++;
            }
            sampleIndex++;
        }
        sampleIndex = 0;
        while (sampleIndex < second.Samples.Count)
        {
            int surfaceIndex = 0;
            while (surfaceIndex < first.Surfaces.Count)
            {
                DistanceSampleFacet sample = second.Samples[sampleIndex];
                TopologicalLocation location =
                    GeometryPointLocation.LocatePointInPolygon(
                        sample.Coordinate, first.Surfaces[surfaceIndex]);
                if (location == TopologicalLocation.Interior)
                {
                    DistanceResult candidate = CreateContainmentResult(
                        sample, false);
                    CopyResult(candidate, best);
                    return;
                }
                surfaceIndex++;
            }
            sampleIndex++;
        }
    }

    internal static DistanceResult CreateContainmentResult(
        DistanceSampleFacet sample, bool sampleBelongsToFirst)
    {
        Coordinate sampleCoordinate =
            CoordinateSequences.CopyCoordinateValue(sample.Coordinate);
        Coordinate surfaceCoordinate = new Coordinate
        {
            X = sample.Coordinate.X,
            Y = sample.Coordinate.Y
        };
        DistanceResult result = new DistanceResult();
        result.HasResult = true;
        result.Distance = 0.0;
        result.FirstCoordinate = sampleCoordinate;
        result.SecondCoordinate = surfaceCoordinate;
        if (sample.HasLocation)
        {
            result.FirstLocation = CreateSampleLocation(sample);
        }
        if (!sampleBelongsToFirst) ReverseResult(result);
        return result;
    }

    internal static LinearLocation CreateSampleLocation(
        DistanceSampleFacet sample)
    {
        LinearLocation location = new LinearLocation();
        location.ComponentIndex = sample.ComponentIndex;
        location.SegmentIndex = sample.SegmentIndex;
        location.SegmentFraction = sample.SegmentFraction;
        return location;
    }

    internal static void CopyResult(
        DistanceResult source, DistanceResult destination)
    {
        destination.HasResult = source.HasResult;
        destination.Distance = source.Distance;
        destination.FirstCoordinate = source.FirstCoordinate;
        destination.SecondCoordinate = source.SecondCoordinate;
        destination.FirstLocation = source.FirstLocation;
        destination.SecondLocation = source.SecondLocation;
    }
}
