namespace geo;

internal static class TopologyEdgeMerger
{
    internal static TopologyEdge AddMergedEdge(
        PlanarGraph graph, Coordinate[] coordinates, TopologyLabel label)
    {
        if (graph == null || coordinates == null || label == null)
        {
            throw new InvalidGeometryException("Merged topology edge input is null.");
        }
        bool canonicalForward = IsCanonicalForward(coordinates);
        Coordinate[] canonicalCoordinates = CreateCanonicalCoordinates(
            coordinates, canonicalForward);
        TopologyLabel canonicalLabel = label.CopyForDirection(canonicalForward);
        TopologyEdge? existing = FindMatchingEdge(graph, canonicalCoordinates);
        if (existing == null)
        {
            existing = FindReverseMatchingEdge(graph, canonicalCoordinates);
            if (existing != null)
            {
                canonicalCoordinates = CreateCanonicalCoordinates(
                    canonicalCoordinates, false);
                canonicalLabel = canonicalLabel.CopyForDirection(false);
            }
        }
        if (existing == null)
        {
            return graph.AddEdge(canonicalCoordinates, canonicalLabel);
        }
        existing.Label.MergeLabel(canonicalLabel);
        ReconcileEdgeCoordinates(existing, canonicalCoordinates);
        existing.MergeCount++;
        graph.FindOrAddNode(existing.Coordinates[0]);
        graph.FindOrAddNode(
            existing.Coordinates[existing.Coordinates.Length - 1]);
        return existing;
    }

    internal static bool IsCanonicalForward(Coordinate[] coordinates)
    {
        if (coordinates == null || coordinates.Length < 2)
        {
            throw new InvalidGeometryException(
                "Canonical topology edge requires at least two coordinates.");
        }
        int index = 0;
        while (index < coordinates.Length)
        {
            VectorMath.ValidateCoordinateValue(
                coordinates[index], "Canonical topology edge coordinate");
            int comparison = RobustLineIntersection.ComparePlanarCoordinates(
                coordinates[index], coordinates[coordinates.Length - 1 - index]);
            if (comparison < 0) return true;
            if (comparison > 0) return false;
            index++;
        }
        return true;
    }

    internal static Coordinate[] CreateCanonicalCoordinates(
        Coordinate[] coordinates, bool forward)
    {
        Coordinate[] canonical = new Coordinate[coordinates.Length];
        int index = 0;
        while (index < coordinates.Length)
        {
            int sourceIndex = forward ? index : coordinates.Length - 1 - index;
            canonical[index] = RobustLineIntersection.CopyCoordinate(
                coordinates[sourceIndex]);
            if (canonical[index].X == 0.0) canonical[index].X = 0.0;
            if (canonical[index].Y == 0.0) canonical[index].Y = 0.0;
            index++;
        }
        return canonical;
    }

    internal static TopologyEdge? FindMatchingEdge(
        PlanarGraph graph, Coordinate[] canonicalCoordinates)
    {
        int index = 0;
        while (index < graph.Edges.Count)
        {
            TopologyEdge edge = graph.Edges[index];
            if (SequencesEqualPlanar(
                edge.Coordinates, canonicalCoordinates)) return edge;
            index++;
        }
        return null;
    }

    internal static TopologyEdge? FindReverseMatchingEdge(
        PlanarGraph graph, Coordinate[] canonicalCoordinates)
    {
        int index = 0;
        while (index < graph.Edges.Count)
        {
            TopologyEdge edge = graph.Edges[index];
            if (SequencesEqualReversePlanar(
                edge.Coordinates, canonicalCoordinates)) return edge;
            index++;
        }
        return null;
    }

    internal static bool SequencesEqualPlanar(
        Coordinate[] first, Coordinate[] second)
    {
        if (first.Length != second.Length) return false;
        int index = 0;
        while (index < first.Length)
        {
            if (!RobustLineIntersection.CoordinatesEqualPlanar(
                first[index], second[index])) return false;
            index++;
        }
        return true;
    }

    internal static bool SequencesEqualReversePlanar(
        Coordinate[] first, Coordinate[] second)
    {
        if (first.Length != second.Length) return false;
        int index = 0;
        while (index < first.Length)
        {
            if (!RobustLineIntersection.CoordinatesEqualPlanar(
                first[index], second[second.Length - 1 - index])) return false;
            index++;
        }
        return true;
    }

    internal static void ReconcileEdgeCoordinates(
        TopologyEdge edge, Coordinate[] contribution)
    {
        if (edge.Coordinates.Length != contribution.Length)
        {
            throw new InvalidGeometryException(
                "Merged topology edge coordinate counts differ.");
        }
        int index = 0;
        while (index < edge.Coordinates.Length)
        {
            OrdinateReconciler.ReconcileCoordinateInto(
                edge.Coordinates[index], contribution[index]);
            index++;
        }
    }

    internal static int CompareEdgeSequences(
        TopologyEdge first, TopologyEdge second)
    {
        int count = first.Coordinates.Length < second.Coordinates.Length ?
            first.Coordinates.Length : second.Coordinates.Length;
        int index = 0;
        while (index < count)
        {
            int comparison = RobustLineIntersection.ComparePlanarCoordinates(
                first.Coordinates[index], second.Coordinates[index]);
            if (comparison != 0) return comparison;
            index++;
        }
        if (first.Coordinates.Length < second.Coordinates.Length) return -1;
        if (first.Coordinates.Length > second.Coordinates.Length) return 1;
        return first.Id.CompareTo(second.Id);
    }
}
