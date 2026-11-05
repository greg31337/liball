using System.Collections.Generic;

namespace geo;

internal static class GeometryNoder
{
    internal static Geometry NodeGeometry(Geometry geometry)
    {
        RelationGeometryParts parts = new RelationGeometryParts(geometry);
        SegmentString[] edges = GeometrySplitter.CreateSplitterEdges(parts);
        SegmentString[] noded = edges.Length == 0 ? [] :
            IndexedNoder.NodeSelf(edges, false).First;
        EnsureNoded(noded);
        return CreateLineworkResult(noded, geometry);
    }

    internal static Geometry[] NodeMutualGeometries(
        Geometry first, Geometry second)
    {
        GeometryRelations.ValidateSpatialReferencePair(first, second);
        RelationGeometryParts firstParts =
            new RelationGeometryParts(first);
        RelationGeometryParts secondParts =
            new RelationGeometryParts(second);
        SegmentString[] firstEdges =
            GeometrySplitter.CreateSplitterEdges(firstParts);
        SegmentString[] secondEdges =
            GeometrySplitter.CreateSplitterEdges(secondParts);
        NodingResult noded = IndexedNoder.NodeMutual(
            firstEdges, secondEdges, false);
        EnsureNoded(IndexedNoder.CombineGroups(
            noded.First, noded.Second));
        return [
            CreateLineworkResult(noded.First, first),
            CreateLineworkResult(noded.Second, second)
        ];
    }

    internal static void EnsureNoded(SegmentString[] edges)
    {
        if (edges.Length != 0)
        {
            FullyNodedValidator.EnsureFullyNoded(edges);
        }
    }

    internal static Geometry CreateLineworkResult(
        SegmentString[] edges, Geometry metadataSource)
    {
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(
                metadataSource);
        if (edges.Length == 0)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.LineString, options);
        }
        List<Geometry> lines = new List<Geometry>();
        int index = 0;
        while (index < edges.Length)
        {
            lines.Add(GeometryBuilder.CreateLineStringValue(
                edges[index].Coordinates, options));
            index++;
        }
        return FactoryConstructions.CreateNarrowestCollectionValue(
            lines.ToArray(), options);
    }
}
