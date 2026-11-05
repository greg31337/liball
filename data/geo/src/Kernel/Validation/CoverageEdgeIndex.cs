using System.Collections.Generic;

namespace geo;

internal class CoveragePolygonPair
{
    internal int FirstIndex;
    internal int SecondIndex;

    internal CoveragePolygonPair(int firstIndex, int secondIndex)
    {
        FirstIndex = firstIndex;
        SecondIndex = secondIndex;
    }
}

internal class CoverageNodingFailure
{
    internal SegmentPairCandidate Candidate;
    internal SegmentIntersectionResult Intersection;

    internal CoverageNodingFailure(
        SegmentPairCandidate candidate,
        SegmentIntersectionResult intersection)
    {
        Candidate = candidate;
        Intersection = intersection;
    }
}

internal static class CoverageEdgeIndex
{
    internal static CoveragePolygonPair[] FindCandidatePairs(
        List<Geometry> polygons, double maximumDistance)
    {
        StrTreeBuilder index = new StrTreeBuilder(8);
        int polygonIndex = 0;
        while (polygonIndex < polygons.Count)
        {
            SpatialIndexItem item = new SpatialIndexItem();
            item.Id = polygonIndex;
            item.Envelope = EnvelopeAlgorithms.CreateGeometryEnvelope(
                polygons[polygonIndex]);
            index.AddItem(item);
            polygonIndex++;
        }

        List<CoveragePolygonPair> pairs =
            new List<CoveragePolygonPair>();
        polygonIndex = 0;
        while (polygonIndex < polygons.Count)
        {
            Envelope bounds = EnvelopeAlgorithms.CreateGeometryEnvelope(
                polygons[polygonIndex]);
            SpatialIndexQuery query = new SpatialIndexQuery();
            query.Envelope = ExpandBounds(bounds, maximumDistance);
            SpatialIndexItem[] candidates = index.QueryEnvelope(query);
            int candidateIndex = 0;
            while (candidateIndex < candidates.Length)
            {
                int secondIndex = candidates[candidateIndex].Id;
                if (secondIndex > polygonIndex)
                {
                    pairs.Add(new CoveragePolygonPair(
                        polygonIndex, secondIndex));
                }
                candidateIndex++;
            }
            polygonIndex++;
        }
        return pairs.ToArray();
    }

    internal static SegmentString[] ExtractBoundarySegmentStrings(
        List<Geometry> polygons)
    {
        List<SegmentString> edges = new List<SegmentString>();
        int polygonIndex = 0;
        while (polygonIndex < polygons.Count)
        {
            Geometry polygonGeometry = polygons[polygonIndex];
            Polygon polygon = (Polygon)
                GeometryDispatch.GetSelectedPayload(polygonGeometry);
            AddRingSegmentString(
                polygon.Shell, polygonIndex, edges);
            int holeIndex = 0;
            while (holeIndex < polygon.Holes.Length)
            {
                AddRingSegmentString(
                    polygon.Holes[holeIndex], polygonIndex, edges);
                holeIndex++;
            }
            polygonIndex++;
        }
        return edges.ToArray();
    }

    internal static CoverageNodingFailure? FindFirstNonNodedIntersection(
        List<Geometry> polygons)
    {
        SegmentString[] edges =
            ExtractBoundarySegmentStrings(polygons);
        if (edges.Length < 2) return null;
        SweepLineIndex index = new SweepLineIndex(edges);
        SegmentPairCandidate[] candidates =
            index.GetCandidatePairs(true);
        int candidateIndex = 0;
        while (candidateIndex < candidates.Length)
        {
            SegmentPairCandidate candidate = candidates[candidateIndex];
            if (candidate.First.SourceIndex !=
                candidate.Second.SourceIndex)
            {
                Coordinate firstStart = candidate.First.Coordinates[
                    candidate.FirstSegmentIndex];
                Coordinate firstEnd = candidate.First.Coordinates[
                    candidate.FirstSegmentIndex + 1];
                Coordinate secondStart = candidate.Second.Coordinates[
                    candidate.SecondSegmentIndex];
                Coordinate secondEnd = candidate.Second.Coordinates[
                    candidate.SecondSegmentIndex + 1];
                SegmentIntersectionResult intersection =
                    RobustLineIntersection.GetSegmentIntersection(
                        firstStart, firstEnd,
                        secondStart, secondEnd);
                if (IndexedNoder.IntersectionRequiresNoding(
                    intersection, firstStart, firstEnd,
                    secondStart, secondEnd))
                {
                    return new CoverageNodingFailure(
                        candidate, intersection);
                }
            }
            candidateIndex++;
        }
        return null;
    }

    internal static void AddRingSegmentString(
        LinearRing ring, int polygonIndex,
        List<SegmentString> edges)
    {
        if (ring.Coordinates.Length == 0) return;
        edges.Add(SegmentNoding.CreateSegmentString(
            ring.Coordinates, ring, polygonIndex));
    }

    internal static Envelope ExpandBounds(
        Envelope bounds, double distance)
    {
        if (distance == 0.0)
        {
            return EnvelopeAlgorithms.CopyEnvelopeValue(bounds);
        }
        double minimumX = bounds.MinX - distance;
        double minimumY = bounds.MinY - distance;
        double maximumX = bounds.MaxX + distance;
        double maximumY = bounds.MaxY + distance;
        if (!double.IsFinite(minimumX)) minimumX = double.MinValue;
        if (!double.IsFinite(minimumY)) minimumY = double.MinValue;
        if (!double.IsFinite(maximumX)) maximumX = double.MaxValue;
        if (!double.IsFinite(maximumY)) maximumY = double.MaxValue;
        return EnvelopeAlgorithms.CreateBoundsEnvelope(
            minimumX, minimumY, maximumX, maximumY);
    }
}
