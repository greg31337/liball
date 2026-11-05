namespace geo;

internal static class MinimumClearanceAlgorithm
{
    internal static DistanceResult GetMinimumClearance(Geometry geometry)
    {
        DiscreteMetricSample[] vertices =
            DiscreteMetricSamples.CreateSamples(geometry, 1);
        DistanceGeometryParts parts = new DistanceGeometryParts(geometry);
        DistanceResult best = new DistanceResult();
        AddVertexPairCandidates(vertices, best);
        AddVertexSegmentCandidates(vertices, parts, best);
        return best;
    }

    internal static void AddVertexPairCandidates(
        DiscreteMetricSample[] vertices, DistanceResult best)
    {
        int firstIndex = 0;
        while (firstIndex < vertices.Length)
        {
            int secondIndex = firstIndex + 1;
            while (secondIndex < vertices.Length)
            {
                DiscreteMetricSample first = vertices[firstIndex];
                DiscreteMetricSample second = vertices[secondIndex];
                if (!RobustLineIntersection.CoordinatesEqualPlanar(
                    first.Coordinate, second.Coordinate))
                {
                    DistanceResult candidate =
                        DiscreteMetricSamples.CreatePairResult(first, second);
                    DistanceAlgorithm.SelectCandidate(best, candidate);
                }
                secondIndex++;
            }
            firstIndex++;
        }
    }

    internal static void AddVertexSegmentCandidates(
        DiscreteMetricSample[] vertices, DistanceGeometryParts parts,
        DistanceResult best)
    {
        int vertexIndex = 0;
        while (vertexIndex < vertices.Length)
        {
            DiscreteMetricSample vertex = vertices[vertexIndex];
            int segmentIndex = 0;
            while (segmentIndex < parts.Segments.Count)
            {
                DistanceSegmentFacet segment = parts.Segments[segmentIndex];
                if (!IsSegmentEndpoint(vertex.Coordinate, segment))
                {
                    DistanceResult candidate =
                        DistanceAlgorithm.CreatePointSegmentCandidate(
                            new DistancePointFacet(vertex.Coordinate),
                            segment, true);
                    if (vertex.HasLocation)
                    {
                        candidate.FirstLocation =
                            DiscreteMetricSamples.CreateLocation(vertex);
                    }
                    DistanceAlgorithm.SelectCandidate(best, candidate);
                }
                segmentIndex++;
            }
            vertexIndex++;
        }
    }

    internal static bool IsSegmentEndpoint(
        Coordinate vertex, DistanceSegmentFacet segment)
    {
        return RobustLineIntersection.CoordinatesEqualPlanar(
            vertex, segment.Start) ||
            RobustLineIntersection.CoordinatesEqualPlanar(
                vertex, segment.End);
    }
}
