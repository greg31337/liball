using System.Collections.Generic;

namespace geo;

internal static class GeometrySplitter
{
    internal static Geometry SplitGeometry(
        Geometry source, Geometry splitter)
    {
        GeometryRelations.ValidateSpatialReferencePair(source, splitter);
        ValidateLinealSource(source);
        RelationGeometryParts sourceParts =
            new RelationGeometryParts(source);
        RelationGeometryParts splitterParts =
            new RelationGeometryParts(splitter);
        SegmentString[] sourceEdges = CreateSourceEdges(sourceParts);
        if (sourceEdges.Length == 0)
        {
            return CreateUnsplitResult(source);
        }

        SegmentString[] splitterEdges = CreateSplitterEdges(splitterParts);
        SegmentString[] fragments =
            IndexedNoder.CloneSegmentStrings(sourceEdges);
        if (splitterEdges.Length != 0)
        {
            fragments = IndexedNoder.NodeMutual(
                sourceEdges, splitterEdges, false).First;
        }
        CollectionRelationGraphBuilder.AddExplicitPointNodes(
            fragments, splitterParts);
        fragments = CollectionRelationGraphBuilder.SplitEdgesAtAddedNodes(
            fragments);
        if (!HasCreatedSplit(fragments, sourceEdges))
        {
            return CreateUnsplitResult(source);
        }
        return CreateSplitResult(source, sourceParts, fragments);
    }

    internal static void ValidateLinealSource(Geometry source)
    {
        GeometryInspection.EnsureGeometryGraph(source);
        if (source.GeometryType != GeometryType.LineString &&
            source.GeometryType != GeometryType.MultiLineString)
        {
            throw new InvalidGeometryException(
                "Split input must be a LineString or MultiLineString.");
        }
    }

    internal static SegmentString[] CreateSourceEdges(
        RelationGeometryParts parts)
    {
        List<SegmentString> edges = new List<SegmentString>();
        int index = 0;
        while (index < parts.CurveGeometries.Length)
        {
            Geometry curve = parts.CurveGeometries[index];
            Coordinate[] coordinates =
                RelationGraphBuilder.GetCurveCoordinates(curve);
            if (HasUsableSegment(coordinates))
            {
                edges.Add(SegmentNoding.CreateSegmentString(
                    coordinates, curve, index));
            }
            index++;
        }
        return edges.ToArray();
    }

    internal static SegmentString[] CreateSplitterEdges(
        RelationGeometryParts parts)
    {
        List<SegmentString> edges = new List<SegmentString>();
        int sourceIndex = 0;
        int curveIndex = 0;
        while (curveIndex < parts.CurveGeometries.Length)
        {
            Geometry curve = parts.CurveGeometries[curveIndex];
            AddUsableEdge(
                edges,
                RelationGraphBuilder.GetCurveCoordinates(curve),
                curve, sourceIndex);
            sourceIndex++;
            curveIndex++;
        }
        int surfaceIndex = 0;
        while (surfaceIndex < parts.SurfaceGeometries.Length)
        {
            Polygon polygon = RelationGraphBuilder.GetPolygon(
                parts.SurfaceGeometries[surfaceIndex]);
            AddUsableEdge(
                edges, polygon.Shell.Coordinates,
                polygon.Shell, sourceIndex);
            sourceIndex++;
            int holeIndex = 0;
            while (holeIndex < polygon.Holes.Length)
            {
                LinearRing hole = polygon.Holes[holeIndex];
                AddUsableEdge(
                    edges, hole.Coordinates, hole, sourceIndex);
                sourceIndex++;
                holeIndex++;
            }
            surfaceIndex++;
        }
        return edges.ToArray();
    }

    internal static void AddUsableEdge(
        List<SegmentString> edges, Coordinate[] coordinates,
        object sourceMetadata, int sourceIndex)
    {
        if (HasUsableSegment(coordinates))
        {
            edges.Add(SegmentNoding.CreateSegmentString(
                coordinates, sourceMetadata, sourceIndex));
        }
    }

    internal static bool HasUsableSegment(Coordinate[] coordinates)
    {
        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            if (!RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[index], coordinates[index + 1]))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool HasCreatedSplit(
        SegmentString[] fragments, SegmentString[] sourceEdges)
    {
        int sourceIndex = 0;
        while (sourceIndex < sourceEdges.Length)
        {
            int fragmentCount = 0;
            int fragmentIndex = 0;
            while (fragmentIndex < fragments.Length)
            {
                if (fragments[fragmentIndex].SourceIndex ==
                    sourceEdges[sourceIndex].SourceIndex)
                {
                    fragmentCount++;
                }
                fragmentIndex++;
            }
            if (fragmentCount > 1) return true;
            sourceIndex++;
        }
        return false;
    }

    internal static Geometry CreateUnsplitResult(Geometry source)
    {
        return GeometryCollections.CreateRawCollection(
            [GeometryCopy.CopyGeometry(source)],
            source.CoordinateLayout, source.SpatialReferenceId);
    }

    internal static Geometry CreateSplitResult(
        Geometry source, RelationGeometryParts sourceParts,
        SegmentString[] fragments)
    {
        List<Geometry> pieces = new List<Geometry>();
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(source);
        int sourceIndex = 0;
        while (sourceIndex < sourceParts.CurveGeometries.Length)
        {
            bool added = false;
            int fragmentIndex = 0;
            while (fragmentIndex < fragments.Length)
            {
                SegmentString fragment = fragments[fragmentIndex];
                if (fragment.SourceIndex == sourceIndex)
                {
                    pieces.Add(GeometryBuilder.CreateLineStringValue(
                        fragment.Coordinates, options));
                    added = true;
                }
                fragmentIndex++;
            }
            if (!added)
            {
                pieces.Add(GeometryCopy.CopyGeometry(
                    sourceParts.CurveGeometries[sourceIndex]));
            }
            sourceIndex++;
        }
        GeometryBuilder.ValidateComponentCount(pieces.Count);
        return GeometryCollections.CreateRawCollection(
            pieces.ToArray(), source.CoordinateLayout,
            source.SpatialReferenceId);
    }
}
