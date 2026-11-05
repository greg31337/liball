namespace geo;

internal static class BufferPolygonizer
{
    internal static Geometry NodeAndPolygonizeSurface(Geometry raw)
    {
        if (raw.GeometryType != GeometryType.Polygon ||
            raw.Polygon == null || raw.Polygon.Holes.Length != 0)
        {
            throw new InvalidGeometryException(
                "Raw buffer polygonization requires one shell and no holes.");
        }
        Coordinate[] shell = raw.Polygon.Shell.Coordinates;
        int orientation = RingMath.GetExactSignedAreaSign(shell);
        if (orientation == 0)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.Polygon,
                GeometryNormalization.CreateNormalizationOptions(raw));
        }
        SegmentString segmentString = SegmentNoding.CreateSegmentString(
            shell, raw.Polygon.Shell, 0);
        SegmentString[] noded = NodeUntilStable([segmentString]);
        PlanarGraph graph = new PlanarGraph();
        TopologyLabel label = CreateBoundaryLabel();
        int index = 0;
        while (index < noded.Length)
        {
            SegmentString fragment = noded[index];
            Coordinate[] coordinates = orientation > 0 ?
                fragment.Coordinates :
                BufferCurveBuilder.ReverseCoordinates(
                    fragment.Coordinates);
            int segmentIndex = 0;
            while (segmentIndex + 1 < coordinates.Length)
            {
                if (!RobustLineIntersection.CoordinatesEqualPlanar(
                    coordinates[segmentIndex],
                    coordinates[segmentIndex + 1]))
                {
                    graph.AddMergedEdge(
                        [coordinates[segmentIndex],
                            coordinates[segmentIndex + 1]], label);
                }
                segmentIndex++;
            }
            index++;
        }
        index = 0;
        while (index < graph.Edges.Count)
        {
            TopologyEdge edge = graph.Edges[index];
            int boundaryDirection = GetBoundaryDirection(
                edge, shell);
            if (boundaryDirection != 0)
            {
                graph.SetResultDirection(
                    edge, boundaryDirection > 0, true);
            }
            index++;
        }
        TopologyRing[] maximal =
            TopologyRingBuilder.BuildMaximalRings(graph);
        TopologyRing[] minimal =
            TopologyRingBuilder.BuildMinimalRings(maximal);
        Polygon[] polygons = TopologyPolygonBuilder.BuildPolygons(minimal);
        return OverlayResultExtractor.CreateResult(
            polygons, [], [], raw, GeometryType.Polygon);
    }

    internal static SegmentString[] NodeUntilStable(
        SegmentString[] source)
    {
        SegmentString[] current = source;
        int iteration = 0;
        while (iteration < 8)
        {
            NodingResult validation =
                FullyNodedValidator.ValidateOutput(current);
            if (validation.IsValid) return current;
            current = IndexedNoder.NodeSelf(current, false).First;
            iteration++;
        }
        FullyNodedValidator.EnsureFullyNoded(current);
        return current;
    }

    internal static int GetBoundaryDirection(
        TopologyEdge edge, Coordinate[] rawRing)
    {
        Coordinate start = edge.Coordinates[0];
        Coordinate end = edge.Coordinates[edge.Coordinates.Length - 1];
        Coordinate direction = OffsetSegmentGenerator.CreateUnitDirection(
            start, end);
        Coordinate midpoint = RobustLineIntersection.InterpolateLineCoordinate(
            start, end, 0.5);
        double length = BufferCurveBuilder.GetPlanarDistance(start, end);
        double offset = length * 0.000001;
        int attempt = 0;
        while (attempt < 12)
        {
            Coordinate left =
                OffsetSegmentGenerator.CreateProjectedCoordinate(
                    midpoint, -direction.Y, direction.X, offset);
            Coordinate right =
                OffsetSegmentGenerator.CreateProjectedCoordinate(
                    midpoint, direction.Y, -direction.X, offset);
            TopologicalLocation leftLocation =
                RingPointLocation.LocatePointByWinding(left, rawRing);
            TopologicalLocation rightLocation =
                RingPointLocation.LocatePointByWinding(right, rawRing);
            if (leftLocation != TopologicalLocation.Boundary &&
                rightLocation != TopologicalLocation.Boundary)
            {
                bool leftInterior =
                    leftLocation == TopologicalLocation.Interior;
                bool rightInterior =
                    rightLocation == TopologicalLocation.Interior;
                if (leftInterior != rightInterior)
                {
                    return leftInterior ? 1 : -1;
                }
                return 0;
            }
            offset /= 2.0;
            attempt++;
        }
        throw new TopologyException(
            "A noded buffer edge side could not be classified.");
    }

    internal static TopologyLabel CreateBoundaryLabel()
    {
        TopologyLabel label = new TopologyLabel();
        label.SetAreaLocations(
            0, TopologicalLocation.Boundary,
            TopologicalLocation.Interior,
            TopologicalLocation.Exterior);
        return label;
    }
}
