using System.Collections.Generic;

namespace geo;

internal enum OverlayOperation
{
    Intersection = 0,
    Union = 1,
    Difference = 2,
    SymmetricDifference = 3
}

internal class OverlayPrecisionBounds
{
    internal double MinimumX = double.PositiveInfinity;
    internal double MinimumY = double.PositiveInfinity;
    internal double MaximumX = double.NegativeInfinity;
    internal double MaximumY = double.NegativeInfinity;
}

internal static class OverlayEngine
{
    internal static Geometry GetIntersection(
        Geometry first, Geometry second)
    {
        return ComputeOverlay(first, second, OverlayOperation.Intersection);
    }

    internal static Geometry GetUnion(
        Geometry first, Geometry second)
    {
        return ComputeOverlay(first, second, OverlayOperation.Union);
    }

    internal static Geometry GetDifference(
        Geometry first, Geometry second)
    {
        return ComputeOverlay(first, second, OverlayOperation.Difference);
    }

    internal static Geometry GetSymmetricDifference(
        Geometry first, Geometry second)
    {
        return ComputeOverlay(
            first, second, OverlayOperation.SymmetricDifference);
    }

    internal static Geometry GetIntersectionWithPrecision(
        Geometry first, Geometry second, PrecisionModel precisionModel)
    {
        return ComputeOverlayWithPrecision(
            first, second, OverlayOperation.Intersection, precisionModel);
    }

    internal static Geometry GetUnionWithPrecision(
        Geometry first, Geometry second, PrecisionModel precisionModel)
    {
        return ComputeOverlayWithPrecision(
            first, second, OverlayOperation.Union, precisionModel);
    }

    internal static Geometry GetDifferenceWithPrecision(
        Geometry first, Geometry second, PrecisionModel precisionModel)
    {
        return ComputeOverlayWithPrecision(
            first, second, OverlayOperation.Difference, precisionModel);
    }

    internal static Geometry GetSymmetricDifferenceWithPrecision(
        Geometry first, Geometry second, PrecisionModel precisionModel)
    {
        return ComputeOverlayWithPrecision(
            first, second, OverlayOperation.SymmetricDifference,
            precisionModel);
    }

    internal static Geometry ComputeOverlay(
        Geometry first, Geometry second, OverlayOperation operation)
    {
        GeometryRelations.ValidateSpatialReferencePair(first, second);
        ValidateOperation(operation);
        RelationGeometryParts firstParts = new RelationGeometryParts(first);
        RelationGeometryParts secondParts = new RelationGeometryParts(second);
        if (first.CoordinateLayout != second.CoordinateLayout)
        {
            throw new InvalidGeometryException(
                "Overlay inputs must use the same coordinate layout.");
        }
        SegmentString[] firstEdges =
            CollectionRelationGraphBuilder.CreateEdges(firstParts);
        SegmentString[] secondEdges =
            CollectionRelationGraphBuilder.CreateEdges(secondParts);
        RelationNodedEdges noded = CollectionRelationGraphBuilder.NodeEdges(
            firstEdges, secondEdges, firstParts, secondParts);
        SegmentString[] combined = IndexedNoder.CombineGroups(
            noded.First, noded.Second);
        if (combined.Length != 0)
        {
            NodingResult validation =
                FullyNodedValidator.ValidateOutput(combined);
            if (!validation.IsValid)
            {
                return ComputeOverlayWithPrecision(
                    first, second, operation,
                    DeriveSafePrecisionModel(first, second));
            }
        }

        return BuildOverlayResult(
            first, second, firstParts, secondParts, noded, operation);
    }

    internal static Geometry ComputeOverlayWithPrecision(
        Geometry first, Geometry second, OverlayOperation operation,
        PrecisionModel precisionModel)
    {
        GeometryRelations.ValidateSpatialReferencePair(first, second);
        ValidateOperation(operation);
        SnapRoundingNoder.ValidateFixedPrecision(precisionModel);
        RelationGeometryParts originalFirst =
            new RelationGeometryParts(first);
        RelationGeometryParts originalSecond =
            new RelationGeometryParts(second);
        if (first.CoordinateLayout != second.CoordinateLayout)
        {
            throw new InvalidGeometryException(
                "Overlay inputs must use the same coordinate layout.");
        }
        Geometry reducedFirst = OverlayPrecisionReducer.ReduceInput(
            originalFirst, precisionModel);
        Geometry reducedSecond = OverlayPrecisionReducer.ReduceInput(
            originalSecond, precisionModel);
        RelationGeometryParts firstParts =
            new RelationGeometryParts(reducedFirst);
        RelationGeometryParts secondParts =
            new RelationGeometryParts(reducedSecond);
        SegmentString[] firstEdges =
            CollectionRelationGraphBuilder.CreateEdges(firstParts);
        SegmentString[] secondEdges =
            CollectionRelationGraphBuilder.CreateEdges(secondParts);
        RelationNodedEdges noded = NodeEdgesToGrid(
            firstEdges, secondEdges, firstParts,
            secondParts, precisionModel);
        return BuildOverlayResult(
            first, second, firstParts, secondParts, noded, operation);
    }

    internal static Geometry BuildOverlayResult(
        Geometry first, Geometry second,
        RelationGeometryParts firstParts,
        RelationGeometryParts secondParts,
        RelationNodedEdges noded, OverlayOperation operation)
    {

        PlanarGraph areaGraph = BuildAreaGraph(
            noded, firstParts, secondParts);
        SelectAreaResultEdges(
            areaGraph, firstParts, secondParts, operation);
        Polygon[] polygons = ExtractAreaPolygons(areaGraph);

        PlanarGraph lineGraph = BuildLineGraph(noded);
        TopologyEdge[] lines = SelectLineResultEdges(
            lineGraph, firstParts, secondParts, polygons, operation);
        Coordinate[] points = SelectPointResults(
            lineGraph, firstParts, secondParts,
            polygons, lines, operation);
        GeometryType emptyType = GetEmptyResultType(
            first, second, operation);
        return OverlayResultExtractor.CreateResult(
            polygons, lines, points, first, emptyType);
    }

    internal static RelationNodedEdges NodeEdgesToGrid(
        SegmentString[] firstEdges, SegmentString[] secondEdges,
        RelationGeometryParts firstParts,
        RelationGeometryParts secondParts,
        PrecisionModel precisionModel)
    {
        int secondSourceOffset = firstEdges.Length;
        int index = 0;
        while (index < secondEdges.Length)
        {
            secondEdges[index].SourceIndex += secondSourceOffset;
            index++;
        }
        SegmentString[] combined = IndexedNoder.CombineGroups(
            firstEdges, secondEdges);
        SnapRoundingResult snapped = SnapRoundingNoder.NodeToGrid(
            combined, precisionModel);
        CollectionRelationGraphBuilder.AddExplicitPointNodes(
            snapped.Output, firstParts);
        CollectionRelationGraphBuilder.AddExplicitPointNodes(
            snapped.Output, secondParts);
        SegmentString[] split =
            CollectionRelationGraphBuilder.SplitEdgesAtAddedNodes(
                snapped.Output);
        List<SegmentString> first = new List<SegmentString>();
        List<SegmentString> second = new List<SegmentString>();
        index = 0;
        while (index < split.Length)
        {
            SegmentString edge = split[index];
            if (edge.SourceIndex < secondSourceOffset)
            {
                first.Add(edge);
            }
            else
            {
                edge.SourceIndex -= secondSourceOffset;
                second.Add(edge);
            }
            index++;
        }
        RelationNodedEdges result = new RelationNodedEdges();
        result.First = first.ToArray();
        result.Second = second.ToArray();
        SegmentString[] validationEdges = IndexedNoder.CombineGroups(
            result.First, result.Second);
        if (validationEdges.Length != 0)
        {
            FullyNodedValidator.EnsureFullyNoded(validationEdges);
        }
        return result;
    }

    internal static PrecisionModel DeriveSafePrecisionModel(
        Geometry first, Geometry second)
    {
        Coordinate[] firstCoordinates =
            GeometryInspection.ExtractValidatedCoordinates(first);
        Coordinate[] secondCoordinates =
            GeometryInspection.ExtractValidatedCoordinates(second);
        OverlayPrecisionBounds bounds = new OverlayPrecisionBounds();
        UpdatePrecisionBounds(firstCoordinates, bounds);
        UpdatePrecisionBounds(secondCoordinates, bounds);
        PrecisionModel precisionModel = new PrecisionModel();
        precisionModel.Type = PrecisionModelType.Fixed;
        if (double.IsPositiveInfinity(bounds.MinimumX))
        {
            precisionModel.Scale = 1.0;
            return precisionModel;
        }
        double width = bounds.MaximumX - bounds.MinimumX;
        double height = bounds.MaximumY - bounds.MinimumY;
        double magnitude = width > height ? width : height;
        if (double.IsFinite(magnitude))
        {
            precisionModel.OffsetX = bounds.MinimumX;
            precisionModel.OffsetY = bounds.MinimumY;
        }
        else
        {
            precisionModel.OffsetX = 0.0;
            precisionModel.OffsetY = 0.0;
            magnitude = GetMaximumMagnitude(
                firstCoordinates, secondCoordinates);
        }
        precisionModel.Scale = CreateSafeScale(magnitude);
        SnapRoundingNoder.ValidateFixedPrecision(precisionModel);
        return precisionModel;
    }

    internal static void UpdatePrecisionBounds(
        Coordinate[] coordinates, OverlayPrecisionBounds bounds)
    {
        int index = 0;
        while (index < coordinates.Length)
        {
            Coordinate coordinate = coordinates[index];
            if (coordinate.X < bounds.MinimumX)
            {
                bounds.MinimumX = coordinate.X;
            }
            if (coordinate.Y < bounds.MinimumY)
            {
                bounds.MinimumY = coordinate.Y;
            }
            if (coordinate.X > bounds.MaximumX)
            {
                bounds.MaximumX = coordinate.X;
            }
            if (coordinate.Y > bounds.MaximumY)
            {
                bounds.MaximumY = coordinate.Y;
            }
            index++;
        }
    }

    internal static double GetMaximumMagnitude(
        Coordinate[] first, Coordinate[] second)
    {
        double maximum = 0.0;
        int index = 0;
        while (index < first.Length)
        {
            double x = System.Math.Abs(first[index].X);
            double y = System.Math.Abs(first[index].Y);
            if (x > maximum) maximum = x;
            if (y > maximum) maximum = y;
            index++;
        }
        index = 0;
        while (index < second.Length)
        {
            double x = System.Math.Abs(second[index].X);
            double y = System.Math.Abs(second[index].Y);
            if (x > maximum) maximum = x;
            if (y > maximum) maximum = y;
            index++;
        }
        return maximum;
    }

    internal static double CreateSafeScale(double magnitude)
    {
        if (!double.IsFinite(magnitude) || magnitude <= 0.0) return 1.0;
        double exponent = 12.0 -
            System.Math.Floor(System.Math.Log10(magnitude));
        if (exponent > 300.0) exponent = 300.0;
        if (exponent < -300.0) exponent = -300.0;
        double scale = System.Math.Pow(10.0, exponent);
        if (!double.IsFinite(scale) || scale <= 0.0) return 1.0;
        return scale;
    }

    internal static PlanarGraph BuildAreaGraph(
        RelationNodedEdges noded, RelationGeometryParts first,
        RelationGeometryParts second)
    {
        PlanarGraph graph = new PlanarGraph();
        AddAreaEdges(graph, noded.First, first, 0);
        AddAreaEdges(graph, noded.Second, second, 1);
        return graph;
    }

    internal static void AddAreaEdges(
        PlanarGraph graph, SegmentString[] edges,
        RelationGeometryParts owner, int geometryIndex)
    {
        int edgeIndex = 0;
        while (edgeIndex < edges.Length)
        {
            SegmentString edge = edges[edgeIndex];
            if (edge.SourceIndex >= owner.CurveGeometries.Length)
            {
                LinearRing? ring = edge.SourceMetadata as LinearRing;
                if (ring == null)
                {
                    throw new TopologyException(
                        "A surface edge does not identify its source ring.");
                }
                bool hole = IsHoleRing(owner, ring);
                TopologyLabel label = CreateAreaLabel(
                    geometryIndex, ring, hole);
                AddIndividualEdges(graph, edge.Coordinates, label);
            }
            edgeIndex++;
        }
    }

    internal static bool IsHoleRing(
        RelationGeometryParts parts, LinearRing ring)
    {
        int surfaceIndex = 0;
        while (surfaceIndex < parts.SurfaceGeometries.Length)
        {
            Polygon polygon = RelationGraphBuilder.GetPolygon(
                parts.SurfaceGeometries[surfaceIndex]);
            if (polygon.Shell == ring) return false;
            int holeIndex = 0;
            while (holeIndex < polygon.Holes.Length)
            {
                if (polygon.Holes[holeIndex] == ring) return true;
                holeIndex++;
            }
            surfaceIndex++;
        }
        throw new TopologyException(
            "A surface edge source ring is not present in its geometry.");
    }

    internal static TopologyLabel CreateAreaLabel(
        int geometryIndex, LinearRing ring, bool hole)
    {
        RingOrientation orientation = RingMath.GetRingOrientation(
            ring.Coordinates);
        if (orientation == RingOrientation.Undefined)
        {
            throw new TopologyException(
                "An overlay surface ring has zero signed area.");
        }
        bool interiorLeft = orientation == RingOrientation.CounterClockwise;
        if (hole) interiorLeft = !interiorLeft;
        TopologyLabel label = new TopologyLabel();
        label.SetAreaLocations(
            geometryIndex, TopologicalLocation.Boundary,
            interiorLeft ? TopologicalLocation.Interior :
                TopologicalLocation.Exterior,
            interiorLeft ? TopologicalLocation.Exterior :
                TopologicalLocation.Interior);
        return label;
    }

    internal static void AddIndividualEdges(
        PlanarGraph graph, Coordinate[] coordinates, TopologyLabel label)
    {
        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            if (!RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[index], coordinates[index + 1]))
            {
                graph.AddMergedEdge(
                    [coordinates[index], coordinates[index + 1]], label);
            }
            index++;
        }
    }

    internal static void SelectAreaResultEdges(
        PlanarGraph graph, RelationGeometryParts first,
        RelationGeometryParts second, OverlayOperation operation)
    {
        int edgeIndex = 0;
        while (edgeIndex < graph.Edges.Count)
        {
            TopologyEdge edge = graph.Edges[edgeIndex];
            Coordinate midpoint = RelationGraphBuilder.CreateMidpoint(
                edge.Coordinates[0], edge.Coordinates[1]);
            bool firstLeft = GetAreaSideMembership(
                edge, midpoint, first, 0, true);
            bool firstRight = GetAreaSideMembership(
                edge, midpoint, first, 0, false);
            bool secondLeft = GetAreaSideMembership(
                edge, midpoint, second, 1, true);
            bool secondRight = GetAreaSideMembership(
                edge, midpoint, second, 1, false);
            bool resultLeft = EvaluateOperation(
                firstLeft, secondLeft, operation);
            bool resultRight = EvaluateOperation(
                firstRight, secondRight, operation);
            if (resultLeft != resultRight)
            {
                graph.SetResultDirection(edge, resultLeft, true);
            }
            edgeIndex++;
        }
    }

    internal static bool GetAreaSideMembership(
        TopologyEdge edge, Coordinate midpoint,
        RelationGeometryParts parts, int geometryIndex, bool left)
    {
        if (CollectionRelationGraphBuilder.LocateAreaPoint(
            midpoint, parts) == TopologicalLocation.Interior)
        {
            return true;
        }
        if (edge.Label.HasSides[geometryIndex])
        {
            TopologicalLocation location = left ?
                edge.Label.GetLeftLocation(geometryIndex, true) :
                edge.Label.GetRightLocation(geometryIndex, true);
            return location == TopologicalLocation.Interior;
        }
        return false;
    }

    internal static GeometryType GetEmptyResultType(
        Geometry first, Geometry second, OverlayOperation operation)
    {
        TopologicalDimension firstDimension = GetNominalDimension(first);
        TopologicalDimension secondDimension = GetNominalDimension(second);
        TopologicalDimension resultDimension;
        switch (operation)
        {
            case OverlayOperation.Intersection:
                resultDimension = firstDimension < secondDimension ?
                    firstDimension : secondDimension;
                break;
            case OverlayOperation.Difference:
                resultDimension = firstDimension;
                break;
            case OverlayOperation.Union:
            case OverlayOperation.SymmetricDifference:
                resultDimension = firstDimension > secondDimension ?
                    firstDimension : secondDimension;
                break;
            default:
                throw new InvalidGeometryException(
                    "Overlay operation is not supported.");
        }
        switch (resultDimension)
        {
            case TopologicalDimension.Point:
                return GeometryType.Point;
            case TopologicalDimension.Curve:
                return GeometryType.LineString;
            case TopologicalDimension.Surface:
                return GeometryType.Polygon;
            default:
                return GeometryType.GeometryCollection;
        }
    }

    internal static TopologicalDimension GetNominalDimension(
        Geometry geometry)
    {
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                return TopologicalDimension.Point;
            case GeometryType.LineString:
            case GeometryType.LinearRing:
            case GeometryType.MultiLineString:
                return TopologicalDimension.Curve;
            case GeometryType.Polygon:
            case GeometryType.MultiPolygon:
                return TopologicalDimension.Surface;
            case GeometryType.GeometryCollection:
                GeometryCollection collection = (GeometryCollection)
                    GeometryDispatch.GetSelectedPayload(geometry);
                TopologicalDimension dimension =
                    TopologicalDimension.Empty;
                int index = 0;
                while (index < collection.Geometries.Length)
                {
                    TopologicalDimension child = GetNominalDimension(
                        collection.Geometries[index]);
                    if (child > dimension) dimension = child;
                    index++;
                }
                return dimension;
            default:
                throw new InvalidGeometryException(
                    "Overlay geometry type is not supported.");
        }
    }

    internal static Polygon[] ExtractAreaPolygons(PlanarGraph graph)
    {
        int resultCount = 0;
        int index = 0;
        while (index < graph.DirectedEdges.Count)
        {
            if (graph.DirectedEdges[index].InResult) resultCount++;
            index++;
        }
        if (resultCount == 0) return [];
        TopologyRing[] maximal = TopologyRingBuilder.BuildMaximalRings(graph);
        TopologyRing[] minimal = TopologyRingBuilder.BuildMinimalRings(maximal);
        return TopologyPolygonBuilder.BuildPolygons(minimal);
    }

    internal static PlanarGraph BuildLineGraph(RelationNodedEdges noded)
    {
        PlanarGraph graph = new PlanarGraph();
        AddLineGraphEdges(graph, noded.First, 0);
        AddLineGraphEdges(graph, noded.Second, 1);
        return graph;
    }

    internal static void AddLineGraphEdges(
        PlanarGraph graph, SegmentString[] edges, int geometryIndex)
    {
        TopologyLabel label = new TopologyLabel();
        label.SetLineLocation(
            geometryIndex, TopologicalLocation.Interior);
        int edgeIndex = 0;
        while (edgeIndex < edges.Length)
        {
            AddIndividualEdges(graph, edges[edgeIndex].Coordinates, label);
            edgeIndex++;
        }
    }

    internal static TopologyEdge[] SelectLineResultEdges(
        PlanarGraph graph, RelationGeometryParts first,
        RelationGeometryParts second, Polygon[] polygons,
        OverlayOperation operation)
    {
        List<TopologyEdge> selected = new List<TopologyEdge>();
        TopologyEdge[] ordered = graph.GetEdgesInPlanarOrder();
        int edgeIndex = 0;
        while (edgeIndex < ordered.Length)
        {
            TopologyEdge edge = ordered[edgeIndex];
            Coordinate midpoint = RelationGraphBuilder.CreateMidpoint(
                edge.Coordinates[0], edge.Coordinates[1]);
            bool inFirst = IsPointInInput(midpoint, first);
            bool inSecond = IsPointInInput(midpoint, second);
            if (EvaluateOperation(inFirst, inSecond, operation) &&
                !OverlayResultExtractor.IsPointCoveredByPolygons(
                    midpoint, polygons))
            {
                selected.Add(edge);
            }
            edgeIndex++;
        }
        return selected.ToArray();
    }

    internal static Coordinate[] SelectPointResults(
        PlanarGraph graph, RelationGeometryParts first,
        RelationGeometryParts second, Polygon[] polygons,
        TopologyEdge[] lines, OverlayOperation operation)
    {
        List<Coordinate> candidates = new List<Coordinate>();
        AddExplicitPointCandidates(candidates, first);
        AddExplicitPointCandidates(candidates, second);
        TopologyNode[] nodes = graph.GetNodesInPlanarOrder();
        int nodeIndex = 0;
        while (nodeIndex < nodes.Length)
        {
            OverlayResultExtractor.AddOrReconcileCoordinate(
                candidates, nodes[nodeIndex].Coordinate);
            nodeIndex++;
        }
        List<Coordinate> selected = new List<Coordinate>();
        int candidateIndex = 0;
        while (candidateIndex < candidates.Count)
        {
            Coordinate candidate = candidates[candidateIndex];
            bool inFirst = IsPointInInput(candidate, first);
            bool inSecond = IsPointInInput(candidate, second);
            if (EvaluateOperation(inFirst, inSecond, operation) &&
                !OverlayResultExtractor.IsPointCoveredByPolygons(
                    candidate, polygons) &&
                !OverlayResultExtractor.IsPointCoveredByLines(
                    candidate, lines))
            {
                OverlayResultExtractor.AddOrReconcileCoordinate(
                    selected, candidate);
            }
            candidateIndex++;
        }
        return selected.ToArray();
    }

    internal static void AddExplicitPointCandidates(
        List<Coordinate> candidates, RelationGeometryParts parts)
    {
        int index = 0;
        while (index < parts.PointGeometries.Length)
        {
            Point point = (Point)GeometryDispatch.GetSelectedPayload(
                parts.PointGeometries[index]);
            if (point.Coordinate != null)
            {
                OverlayResultExtractor.AddOrReconcileCoordinate(
                    candidates, point.Coordinate);
            }
            index++;
        }
    }

    internal static bool IsPointInInput(
        Coordinate point, RelationGeometryParts parts)
    {
        return CollectionRelationGraphBuilder.LocatePoint(
            point, parts, BoundaryNodeRule.Mod2) !=
            TopologicalLocation.Exterior;
    }

    internal static bool EvaluateOperation(
        bool inFirst, bool inSecond, OverlayOperation operation)
    {
        switch (operation)
        {
            case OverlayOperation.Intersection:
                return inFirst && inSecond;
            case OverlayOperation.Union:
                return inFirst || inSecond;
            case OverlayOperation.Difference:
                return inFirst && !inSecond;
            case OverlayOperation.SymmetricDifference:
                return inFirst != inSecond;
            default:
                throw new InvalidGeometryException(
                    "Overlay operation is not supported.");
        }
    }

    internal static void ValidateOperation(OverlayOperation operation)
    {
        switch (operation)
        {
            case OverlayOperation.Intersection:
            case OverlayOperation.Union:
            case OverlayOperation.Difference:
            case OverlayOperation.SymmetricDifference:
                return;
            default:
                throw new InvalidGeometryException(
                    "Overlay operation is not supported.");
        }
    }
}
