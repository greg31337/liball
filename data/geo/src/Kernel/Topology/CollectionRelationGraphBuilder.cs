using System.Collections.Generic;

namespace geo;

internal class RelationGeometryParts
{
    internal Geometry Root;
    internal Geometry[] PointGeometries = [];
    internal Geometry[] CurveGeometries = [];
    internal Geometry[] SurfaceGeometries = [];
    internal BoundaryEndpointNode[] EndpointNodes = [];
    internal TopologicalDimension Dimension;

    internal RelationGeometryParts(Geometry geometry)
    {
        Root = geometry;
        CollectionRelationGraphBuilder.PopulateParts(this);
    }
}

internal class RelationNodedEdges
{
    internal SegmentString[] First = [];
    internal SegmentString[] Second = [];
}

internal static class CollectionRelationGraphBuilder
{
    internal static bool SupportsCoreGeometry(Geometry geometry)
    {
        GeometryInspection.EnsureGeometryGraph(geometry);
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        int index = 0;
        while (index < leaves.Length)
        {
            Geometry leaf = leaves[index];
            switch (leaf.GeometryType)
            {
                case GeometryType.Point:
                case GeometryType.LineString:
                case GeometryType.LinearRing:
                case GeometryType.Polygon:
                    break;
                case GeometryType.MultiPoint:
                case GeometryType.MultiLineString:
                case GeometryType.MultiPolygon:
                case GeometryType.GeometryCollection:
                    if (!GeometryInspection.IsEmptyValue(leaf)) return false;
                    break;
                default:
                    return false;
            }
            index++;
        }
        return true;
    }

    internal static RelationMatrix BuildRelation(
        Geometry first, Geometry second, BoundaryNodeRule boundaryNodeRule)
    {
        GeometryBoundary.ValidateBoundaryNodeRule(boundaryNodeRule);
        RelationGeometryParts firstParts = new RelationGeometryParts(first);
        RelationGeometryParts secondParts = new RelationGeometryParts(second);
        RelationMatrix matrix = new RelationMatrix
        {
            LeftDimension = firstParts.Dimension,
            RightDimension = secondParts.Dimension
        };
        RelationMatrixAlgorithms.SetCell(
            matrix, TopologicalLocation.Exterior,
            TopologicalLocation.Exterior, RelationDimension.Surface);

        SegmentString[] firstEdges = CreateEdges(firstParts);
        SegmentString[] secondEdges = CreateEdges(secondParts);
        RelationNodedEdges noded = NodeEdges(
            firstEdges, secondEdges, firstParts, secondParts);
        LabelEdgeFragments(
            matrix, noded.First, firstParts, secondParts,
            boundaryNodeRule, true);
        LabelEdgeFragments(
            matrix, noded.Second, secondParts, firstParts,
            boundaryNodeRule, false);
        LabelEdgeVertices(
            matrix, noded.First, firstParts, secondParts,
            boundaryNodeRule, true);
        LabelEdgeVertices(
            matrix, noded.Second, firstParts, secondParts,
            boundaryNodeRule, false);
        LabelMutualEdgeIntersections(
            matrix, firstEdges, secondEdges,
            firstParts, secondParts, boundaryNodeRule);
        LabelExplicitPoints(
            matrix, firstParts, firstParts, secondParts,
            boundaryNodeRule);
        LabelExplicitPoints(
            matrix, secondParts, firstParts, secondParts,
            boundaryNodeRule);
        LabelSurfaceFaces(
            matrix, noded, firstParts, secondParts,
            boundaryNodeRule);
        return matrix;
    }

    internal static void PopulateParts(RelationGeometryParts parts)
    {
        GeometryInspection.EnsureGeometryGraph(parts.Root);
        GeometryInspection.ExtractValidatedCoordinates(parts.Root);
        parts.Dimension = GeometryInspection.GetTopologicalDimensionValue(
            parts.Root);
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            parts.Root,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        List<Geometry> points = new List<Geometry>();
        List<Geometry> curves = new List<Geometry>();
        List<Geometry> surfaces = new List<Geometry>();
        List<BoundaryEndpointNode> endpoints =
            new List<BoundaryEndpointNode>();
        Dictionary<string, int> endpointIndexes =
            new Dictionary<string, int>();
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
            if (!GeometryInspection.IsEmptyValue(leaf))
            {
                switch (leaf.GeometryType)
                {
                    case GeometryType.Point:
                        points.Add(leaf);
                        break;
                    case GeometryType.LineString:
                    case GeometryType.LinearRing:
                        curves.Add(leaf);
                        AddCurveEndpoints(
                            leaf, endpoints, endpointIndexes);
                        break;
                    case GeometryType.Polygon:
                        surfaces.Add(leaf);
                        break;
                    default:
                        throw new InvalidGeometryException(
                            "Relation component type is not supported.");
                }
            }
            index++;
        }
        parts.PointGeometries = points.ToArray();
        parts.CurveGeometries = curves.ToArray();
        parts.SurfaceGeometries = surfaces.ToArray();
        parts.EndpointNodes = endpoints.ToArray();
    }

    internal static bool IsEmptyCoreContainer(Geometry geometry)
    {
        bool container = false;
        switch (geometry.GeometryType)
        {
            case GeometryType.MultiPoint:
            case GeometryType.MultiLineString:
            case GeometryType.MultiPolygon:
            case GeometryType.GeometryCollection:
                container = true;
                break;
            default:
                break;
        }
        if (!container)
        {
            return false;
        }
        if (!GeometryInspection.IsEmptyValue(geometry))
        {
            throw new InvalidGeometryException(
                "A non-empty relation container must flatten to atomic components.");
        }
        return true;
    }

    internal static void AddCurveEndpoints(
        Geometry curve, List<BoundaryEndpointNode> endpoints,
        Dictionary<string, int> endpointIndexes)
    {
        Coordinate[] coordinates = RelationGraphBuilder.GetCurveCoordinates(curve);
        GeometryBoundary.AddEndpointNode(
            coordinates[0], endpoints, endpointIndexes);
        GeometryBoundary.AddEndpointNode(
            coordinates[coordinates.Length - 1], endpoints, endpointIndexes);
    }

    internal static SegmentString[] CreateEdges(RelationGeometryParts parts)
    {
        List<SegmentString> edges = new List<SegmentString>();
        int sourceIndex = 0;
        int curveIndex = 0;
        while (curveIndex < parts.CurveGeometries.Length)
        {
            Geometry curve = parts.CurveGeometries[curveIndex];
            edges.Add(SegmentNoding.CreateSegmentString(
                RelationGraphBuilder.GetCurveCoordinates(curve),
                curve, sourceIndex));
            sourceIndex++;
            curveIndex++;
        }
        int surfaceIndex = 0;
        while (surfaceIndex < parts.SurfaceGeometries.Length)
        {
            Geometry surface = parts.SurfaceGeometries[surfaceIndex];
            Polygon polygon = RelationGraphBuilder.GetPolygon(surface);
            edges.Add(SegmentNoding.CreateSegmentString(
                polygon.Shell.Coordinates, polygon.Shell, sourceIndex));
            sourceIndex++;
            int holeIndex = 0;
            while (holeIndex < polygon.Holes.Length)
            {
                LinearRing hole = polygon.Holes[holeIndex];
                edges.Add(SegmentNoding.CreateSegmentString(
                    hole.Coordinates, hole, sourceIndex));
                sourceIndex++;
                holeIndex++;
            }
            surfaceIndex++;
        }
        return edges.ToArray();
    }

    internal static RelationNodedEdges NodeEdges(
        SegmentString[] first, SegmentString[] second,
        RelationGeometryParts firstParts,
        RelationGeometryParts secondParts)
    {
        SegmentString[] firstNoded = NodeSelfEdges(first);
        SegmentString[] secondNoded = NodeSelfEdges(second);
        AddExplicitPointNodes(firstNoded, firstParts);
        AddExplicitPointNodes(firstNoded, secondParts);
        AddExplicitPointNodes(secondNoded, firstParts);
        AddExplicitPointNodes(secondNoded, secondParts);
        firstNoded = SplitEdgesAtAddedNodes(firstNoded);
        secondNoded = SplitEdgesAtAddedNodes(secondNoded);
        RelationNodedEdges result = new RelationNodedEdges();
        if (firstNoded.Length == 0 || secondNoded.Length == 0)
        {
            result.First = firstNoded;
            result.Second = secondNoded;
            return result;
        }
        NodingResult mutual = IndexedNoder.NodeMutual(
            firstNoded, secondNoded, false);
        result.First = mutual.First;
        result.Second = mutual.Second;
        return result;
    }

    internal static SegmentString[] NodeSelfEdges(SegmentString[] edges)
    {
        if (edges.Length == 0) return [];
        return IndexedNoder.NodeSelf(edges, false).First;
    }

    internal static void AddExplicitPointNodes(
        SegmentString[] edges, RelationGeometryParts parts)
    {
        int pointIndex = 0;
        while (pointIndex < parts.PointGeometries.Length)
        {
            Point payload = (Point)GeometryDispatch.GetSelectedPayload(
                parts.PointGeometries[pointIndex]);
            if (payload.Coordinate == null)
            {
                throw new InvalidGeometryException(
                    "A non-empty point component requires a coordinate.");
            }
            int edgeIndex = 0;
            while (edgeIndex < edges.Length)
            {
                Coordinate[] coordinates = edges[edgeIndex].Coordinates;
                int segmentIndex = 0;
                while (segmentIndex + 1 < coordinates.Length)
                {
                    if (SegmentDistanceAlgorithms.IsPointOnSegment(
                        payload.Coordinate, coordinates[segmentIndex],
                        coordinates[segmentIndex + 1]))
                    {
                        SegmentNoding.AddIntersectionNode(
                            edges[edgeIndex], payload.Coordinate,
                            segmentIndex);
                    }
                    segmentIndex++;
                }
                edgeIndex++;
            }
            pointIndex++;
        }
    }

    internal static SegmentString[] SplitEdgesAtAddedNodes(
        SegmentString[] edges)
    {
        if (edges.Length == 0) return [];
        return IndexedNoder.SplitGroup(edges, 0, edges.Length);
    }

    internal static void LabelEdgeFragments(
        RelationMatrix matrix, SegmentString[] edges,
        RelationGeometryParts owner, RelationGeometryParts other,
        BoundaryNodeRule boundaryNodeRule, bool ownerIsFirst)
    {
        int edgeIndex = 0;
        while (edgeIndex < edges.Length)
        {
            Coordinate[] coordinates = edges[edgeIndex].Coordinates;
            int segmentIndex = 0;
            while (segmentIndex + 1 < coordinates.Length)
            {
                Coordinate midpoint = RelationGraphBuilder.CreateMidpoint(
                    coordinates[segmentIndex], coordinates[segmentIndex + 1]);
                TopologicalLocation ownerLocation = LocateOwnerEdgeMidpoint(
                    edges[edgeIndex], midpoint, owner, boundaryNodeRule);
                TopologicalLocation otherLocation = LocatePoint(
                    midpoint, other, boundaryNodeRule);
                if (ownerIsFirst)
                {
                    RelationMatrixAlgorithms.SetCellAtLeast(
                        matrix, ownerLocation, otherLocation,
                        RelationDimension.Curve);
                }
                else
                {
                    RelationMatrixAlgorithms.SetCellAtLeast(
                        matrix, otherLocation, ownerLocation,
                        RelationDimension.Curve);
                }
                segmentIndex++;
            }
            edgeIndex++;
        }
    }

    internal static TopologicalLocation LocateOwnerEdgeMidpoint(
        SegmentString edge, Coordinate midpoint,
        RelationGeometryParts owner, BoundaryNodeRule boundaryNodeRule)
    {
        TopologicalLocation location = LocatePoint(
            midpoint, owner, boundaryNodeRule);
        if (location != TopologicalLocation.Exterior) return location;
        if (edge.SourceIndex < owner.CurveGeometries.Length)
        {
            return TopologicalLocation.Interior;
        }
        return TopologicalLocation.Boundary;
    }

    internal static void LabelEdgeVertices(
        RelationMatrix matrix, SegmentString[] edges,
        RelationGeometryParts first, RelationGeometryParts second,
        BoundaryNodeRule boundaryNodeRule, bool edgesBelongToFirst)
    {
        int edgeIndex = 0;
        while (edgeIndex < edges.Length)
        {
            Coordinate[] coordinates = edges[edgeIndex].Coordinates;
            int coordinateIndex = 0;
            while (coordinateIndex < coordinates.Length)
            {
                Coordinate coordinate = coordinates[coordinateIndex];
                RelationGeometryParts owner = second;
                RelationGeometryParts other = first;
                if (edgesBelongToFirst)
                {
                    owner = first;
                    other = second;
                }
                TopologicalLocation ownerLocation = LocateOwnerEdgeVertex(
                    edges[edgeIndex], coordinate, owner, boundaryNodeRule);
                TopologicalLocation otherLocation = LocatePoint(
                    coordinate, other, boundaryNodeRule);
                if (edgesBelongToFirst)
                {
                    RelationMatrixAlgorithms.SetCellAtLeast(
                        matrix, ownerLocation, otherLocation,
                        RelationDimension.Point);
                }
                else
                {
                    RelationMatrixAlgorithms.SetCellAtLeast(
                        matrix, otherLocation, ownerLocation,
                        RelationDimension.Point);
                }
                coordinateIndex++;
            }
            edgeIndex++;
        }
    }

    internal static TopologicalLocation LocateOwnerEdgeVertex(
        SegmentString edge, Coordinate coordinate,
        RelationGeometryParts owner, BoundaryNodeRule boundaryNodeRule)
    {
        TopologicalLocation location = LocatePoint(
            coordinate, owner, boundaryNodeRule);
        if (location != TopologicalLocation.Exterior) return location;
        if (edge.SourceIndex < owner.CurveGeometries.Length)
        {
            if (IsSelectedEndpoint(
                coordinate, owner.EndpointNodes, boundaryNodeRule))
            {
                return TopologicalLocation.Boundary;
            }
            return TopologicalLocation.Interior;
        }
        return TopologicalLocation.Boundary;
    }

    internal static void LabelMutualEdgeIntersections(
        RelationMatrix matrix,
        SegmentString[] firstEdges, SegmentString[] secondEdges,
        RelationGeometryParts first, RelationGeometryParts second,
        BoundaryNodeRule boundaryNodeRule)
    {
        SegmentString[] combined = IndexedNoder.CombineGroups(
            firstEdges, secondEdges);
        SweepLineIndex index = new SweepLineIndex(combined);
        SegmentPairCandidate[] candidates = index.GetCandidatePairs(true);
        int candidateIndex = 0;
        while (candidateIndex < candidates.Length)
        {
            SegmentPairCandidate candidate = candidates[candidateIndex];
            bool candidateFirstInFirst = IndexedNoder.IsInPrefix(
                combined, candidate.First, firstEdges.Length);
            bool candidateSecondInFirst = IndexedNoder.IsInPrefix(
                combined, candidate.Second, firstEdges.Length);
            if (candidateFirstInFirst != candidateSecondInFirst)
            {
                SegmentString firstEdge = candidate.Second;
                SegmentString secondEdge = candidate.First;
                int firstSegmentIndex = candidate.SecondSegmentIndex;
                int secondSegmentIndex = candidate.FirstSegmentIndex;
                if (candidateFirstInFirst)
                {
                    firstEdge = candidate.First;
                    secondEdge = candidate.Second;
                    firstSegmentIndex = candidate.FirstSegmentIndex;
                    secondSegmentIndex = candidate.SecondSegmentIndex;
                }
                SegmentIntersectionResult intersection =
                    RobustLineIntersection.GetSegmentIntersection(
                        firstEdge.Coordinates[firstSegmentIndex],
                        firstEdge.Coordinates[firstSegmentIndex + 1],
                        secondEdge.Coordinates[secondSegmentIndex],
                        secondEdge.Coordinates[secondSegmentIndex + 1]);
                LabelMutualEdgeIntersectionWitness(
                    matrix, intersection.FirstIntersection,
                    firstEdge, secondEdge,
                    first, second, boundaryNodeRule);
                LabelMutualEdgeIntersectionWitness(
                    matrix, intersection.SecondIntersection,
                    firstEdge, secondEdge,
                    first, second, boundaryNodeRule);
            }
            candidateIndex++;
        }
    }

    internal static void LabelMutualEdgeIntersectionWitness(
        RelationMatrix matrix, Coordinate? witness,
        SegmentString firstEdge, SegmentString secondEdge,
        RelationGeometryParts first, RelationGeometryParts second,
        BoundaryNodeRule boundaryNodeRule)
    {
        if (witness == null) return;
        TopologicalLocation firstLocation = LocateOwnerEdgeVertex(
            firstEdge, witness, first, boundaryNodeRule);
        TopologicalLocation secondLocation = LocateOwnerEdgeVertex(
            secondEdge, witness, second, boundaryNodeRule);
        RelationMatrixAlgorithms.SetCellAtLeast(
            matrix, firstLocation, secondLocation,
            RelationDimension.Point);
    }

    internal static void LabelExplicitPoints(
        RelationMatrix matrix, RelationGeometryParts source,
        RelationGeometryParts first, RelationGeometryParts second,
        BoundaryNodeRule boundaryNodeRule)
    {
        int index = 0;
        while (index < source.PointGeometries.Length)
        {
            Point point = (Point)GeometryDispatch.GetSelectedPayload(
                source.PointGeometries[index]);
            if (point.Coordinate != null)
            {
                LabelPoint(
                    matrix, point.Coordinate, first, second,
                    boundaryNodeRule);
            }
            index++;
        }
    }

    internal static void LabelPoint(
        RelationMatrix matrix, Coordinate point,
        RelationGeometryParts first, RelationGeometryParts second,
        BoundaryNodeRule boundaryNodeRule)
    {
        TopologicalLocation firstLocation = LocatePoint(
            point, first, boundaryNodeRule);
        TopologicalLocation secondLocation = LocatePoint(
            point, second, boundaryNodeRule);
        RelationMatrixAlgorithms.SetCellAtLeast(
            matrix, firstLocation, secondLocation,
            RelationDimension.Point);
    }

    internal static void LabelSurfaceFaces(
        RelationMatrix matrix, RelationNodedEdges noded,
        RelationGeometryParts first, RelationGeometryParts second,
        BoundaryNodeRule boundaryNodeRule)
    {
        if (first.SurfaceGeometries.Length == 0 &&
            second.SurfaceGeometries.Length == 0) return;
        List<double> eventY = new List<double>();
        AddEdgeYOrdinates(noded.First, eventY);
        AddEdgeYOrdinates(noded.Second, eventY);
        eventY.Sort();
        int yIndex = 0;
        while (yIndex + 1 < eventY.Count)
        {
            double lowerY = eventY[yIndex];
            double upperY = eventY[yIndex + 1];
            if (lowerY < upperY)
            {
                double y = RelationGraphBuilder.MidpointOrdinate(
                    lowerY, upperY);
                if (y > lowerY && y < upperY)
                {
                    LabelScanlineFaces(
                        matrix, y, first, second,
                        boundaryNodeRule);
                }
            }
            yIndex++;
        }
    }

    internal static void AddEdgeYOrdinates(
        SegmentString[] edges, List<double> ordinates)
    {
        int edgeIndex = 0;
        while (edgeIndex < edges.Length)
        {
            Coordinate[] coordinates = edges[edgeIndex].Coordinates;
            int coordinateIndex = 0;
            while (coordinateIndex < coordinates.Length)
            {
                ordinates.Add(coordinates[coordinateIndex].Y);
                coordinateIndex++;
            }
            edgeIndex++;
        }
    }

    internal static void LabelScanlineFaces(
        RelationMatrix matrix, double y,
        RelationGeometryParts first, RelationGeometryParts second,
        BoundaryNodeRule boundaryNodeRule)
    {
        List<double> intersections = new List<double>();
        AddSurfaceScanlineIntersections(first, y, intersections);
        AddSurfaceScanlineIntersections(second, y, intersections);
        intersections.Sort();
        int index = 0;
        while (index + 1 < intersections.Count)
        {
            double left = intersections[index];
            double right = intersections[index + 1];
            if (left < right)
            {
                double x = RelationGraphBuilder.MidpointOrdinate(left, right);
                if (x > left && x < right)
                {
                    Coordinate point = new Coordinate { X = x, Y = y };
                    TopologicalLocation firstLocation = LocateAreaPoint(
                        point, first);
                    TopologicalLocation secondLocation = LocateAreaPoint(
                        point, second);
                    if (firstLocation != TopologicalLocation.Boundary &&
                        secondLocation != TopologicalLocation.Boundary)
                    {
                        RelationMatrixAlgorithms.SetCellAtLeast(
                            matrix, firstLocation, secondLocation,
                            RelationDimension.Surface);
                    }
                }
            }
            index++;
        }
    }

    internal static void AddSurfaceScanlineIntersections(
        RelationGeometryParts parts, double y,
        List<double> intersections)
    {
        int surfaceIndex = 0;
        while (surfaceIndex < parts.SurfaceGeometries.Length)
        {
            Polygon polygon = RelationGraphBuilder.GetPolygon(
                parts.SurfaceGeometries[surfaceIndex]);
            RelationGraphBuilder.AddRingScanlineIntersections(
                polygon.Shell, y, intersections);
            int holeIndex = 0;
            while (holeIndex < polygon.Holes.Length)
            {
                RelationGraphBuilder.AddRingScanlineIntersections(
                    polygon.Holes[holeIndex], y, intersections);
                holeIndex++;
            }
            surfaceIndex++;
        }
    }

    internal static TopologicalLocation LocateAreaPoint(
        Coordinate point, RelationGeometryParts parts)
    {
        TopologicalLocation result = TopologicalLocation.Exterior;
        int index = 0;
        while (index < parts.SurfaceGeometries.Length)
        {
            Polygon polygon = RelationGraphBuilder.GetPolygon(
                parts.SurfaceGeometries[index]);
            TopologicalLocation location =
                GeometryPointLocation.LocatePointInPolygon(point, polygon);
            if (location == TopologicalLocation.Interior) return location;
            if (location == TopologicalLocation.Boundary) result = location;
            index++;
        }
        return result;
    }

    internal static TopologicalLocation LocatePoint(
        Coordinate point, RelationGeometryParts parts,
        BoundaryNodeRule boundaryNodeRule)
    {
        int index = 0;
        while (index < parts.PointGeometries.Length)
        {
            Point payload = (Point)GeometryDispatch.GetSelectedPayload(
                parts.PointGeometries[index]);
            if (payload.Coordinate != null &&
                RobustLineIntersection.CoordinatesEqualPlanar(
                    point, payload.Coordinate))
            {
                return TopologicalLocation.Interior;
            }
            index++;
        }

        bool surfaceBoundary = false;
        index = 0;
        while (index < parts.SurfaceGeometries.Length)
        {
            Polygon polygon = RelationGraphBuilder.GetPolygon(
                parts.SurfaceGeometries[index]);
            TopologicalLocation location =
                GeometryPointLocation.LocatePointInPolygon(point, polygon);
            if (location == TopologicalLocation.Interior)
            {
                return TopologicalLocation.Interior;
            }
            if (location == TopologicalLocation.Boundary)
            {
                surfaceBoundary = true;
            }
            index++;
        }

        bool onCurve = false;
        index = 0;
        while (index < parts.CurveGeometries.Length)
        {
            Geometry curve = parts.CurveGeometries[index];
            bool forceClosed = curve.GeometryType == GeometryType.LinearRing;
            TopologicalLocation location =
                GeometryPointLocation.LocatePointOnCoordinateLine(
                    point, RelationGraphBuilder.GetCurveCoordinates(curve),
                    forceClosed);
            if (location != TopologicalLocation.Exterior)
            {
                onCurve = true;
            }
            index++;
        }
        if (onCurve && IsSelectedEndpoint(
            point, parts.EndpointNodes, boundaryNodeRule))
        {
            return TopologicalLocation.Boundary;
        }
        if (onCurve) return TopologicalLocation.Interior;
        if (surfaceBoundary) return TopologicalLocation.Boundary;
        return TopologicalLocation.Exterior;
    }

    internal static bool IsSelectedEndpoint(
        Coordinate point, BoundaryEndpointNode[] endpointNodes,
        BoundaryNodeRule boundaryNodeRule)
    {
        int index = 0;
        while (index < endpointNodes.Length)
        {
            BoundaryEndpointNode node = endpointNodes[index];
            if (RobustLineIntersection.CoordinatesEqualPlanar(
                point, node.Coordinate))
            {
                return GeometryBoundary.ShouldIncludeEndpointNode(
                    node.Valence, boundaryNodeRule);
            }
            index++;
        }
        return false;
    }
}
