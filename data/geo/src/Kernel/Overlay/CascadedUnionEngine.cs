using System.Collections.Generic;

namespace geo;

internal class CascadedUnionItem
{
    internal Geometry Geometry;
    internal Geometry NormalizedGeometry;
    internal Envelope Bounds;

    internal CascadedUnionItem(Geometry geometry)
    {
        Geometry = geometry;
        NormalizedGeometry =
            GeometryNormalization.NormalizeGeometryValue(geometry);
        Bounds = EnvelopeAlgorithms.CreateGeometryEnvelope(geometry);
    }
}

internal static class CascadedUnionEngine
{
    internal static Geometry GetCascadedUnion(Geometry geometry)
    {
        RelationGeometryParts parts = new RelationGeometryParts(geometry);
        ValidatePolygonalInput(parts.Root);
        if (parts.SurfaceGeometries.Length == 0)
        {
            return UnaryUnionEngine.GetUnaryUnion(geometry);
        }
        CascadedUnionItem[] items =
            new CascadedUnionItem[parts.SurfaceGeometries.Length];
        int index = 0;
        while (index < items.Length)
        {
            items[index] = new CascadedUnionItem(
                parts.SurfaceGeometries[index]);
            index++;
        }
        System.Array.Sort(items, (first, second) =>
            CompareItems(first, second));
        return UnionRange(items, 0, items.Length);
    }

    internal static Geometry UnionRange(
        CascadedUnionItem[] items, int startIndex, int count)
    {
        if (count <= 0)
        {
            throw new InvalidGeometryException(
                "Cascaded union range must contain a geometry.");
        }
        if (count == 1)
        {
            return UnaryUnionEngine.GetUnaryUnion(
                items[startIndex].Geometry);
        }
        int leftCount = count / 2;
        Geometry left = UnionRange(items, startIndex, leftCount);
        Geometry right = UnionRange(
            items, startIndex + leftCount, count - leftCount);
        return OverlayEngine.GetUnion(left, right);
    }

    internal static int CompareItems(
        CascadedUnionItem first, CascadedUnionItem second)
    {
        int comparison = CompareBounds(first.Bounds, second.Bounds);
        if (comparison != 0) return comparison;
        return GeometryNormalization.CompareGeometryValues(
            first.NormalizedGeometry, second.NormalizedGeometry);
    }

    internal static int CompareBounds(Envelope first, Envelope second)
    {
        if (first.MinX < second.MinX) return -1;
        if (first.MinX > second.MinX) return 1;
        if (first.MinY < second.MinY) return -1;
        if (first.MinY > second.MinY) return 1;
        if (first.MaxX < second.MaxX) return -1;
        if (first.MaxX > second.MaxX) return 1;
        if (first.MaxY < second.MaxY) return -1;
        if (first.MaxY > second.MaxY) return 1;
        return 0;
    }

    internal static void ValidatePolygonalInput(Geometry geometry)
    {
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        int index = 0;
        while (index < leaves.Length)
        {
            GeometryType geometryType = leaves[index].GeometryType;
            if (geometryType != GeometryType.Polygon &&
                geometryType != GeometryType.MultiPolygon &&
                geometryType != GeometryType.GeometryCollection)
            {
                throw new InvalidGeometryException(
                    "Cascaded union requires polygonal components only.");
            }
            index++;
        }
    }
}

internal class DisjointUnionItem
{
    internal Geometry Geometry;
    internal Envelope Bounds;

    internal DisjointUnionItem(Geometry geometry)
    {
        Geometry = geometry;
        Bounds = EnvelopeAlgorithms.CreateGeometryEnvelope(geometry);
    }
}

internal static class DisjointSubsetUnionEngine
{
    internal static Geometry GetDisjointSubsetUnion(
        Geometry first, Geometry second)
    {
        GeometryRelations.ValidateSpatialReferencePair(first, second);
        RelationGeometryParts firstParts = new RelationGeometryParts(first);
        RelationGeometryParts secondParts = new RelationGeometryParts(second);
        if (first.CoordinateLayout != second.CoordinateLayout)
        {
            throw new InvalidGeometryException(
                "Disjoint-subset union inputs must use the same coordinate layout.");
        }
        List<DisjointUnionItem> items = new List<DisjointUnionItem>();
        AddLeafItems(items, firstParts.Root);
        AddLeafItems(items, secondParts.Root);
        if (items.Count == 0)
        {
            List<Geometry> empty = new List<Geometry>();
            return OverlayResultExtractor.CreateCollectionResult(
                empty, first,
                OverlayEngine.GetEmptyResultType(
                    first, second, OverlayOperation.Union));
        }
        items.Sort((firstItem, secondItem) =>
            CompareDisjointItems(firstItem, secondItem));
        int[] parents = CreateParents(items.Count);
        ConnectIntersectingItems(items, parents);
        List<Geometry> subsetResults = new List<Geometry>();
        int itemIndex = 0;
        while (itemIndex < items.Count)
        {
            if (FindRoot(parents, itemIndex) == itemIndex)
            {
                Geometry subset = CreateSubsetGeometry(
                    items, parents, itemIndex, first);
                subsetResults.Add(
                    UnaryUnionEngine.GetUnaryUnion(subset));
            }
            itemIndex++;
        }
        return CombineSubsetResults(subsetResults, first);
    }

    internal static void AddLeafItems(
        List<DisjointUnionItem> items, Geometry geometry)
    {
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        int index = 0;
        while (index < leaves.Length)
        {
            if (!GeometryInspection.IsEmptyValue(leaves[index]))
            {
                items.Add(new DisjointUnionItem(leaves[index]));
            }
            index++;
        }
    }

    internal static int[] CreateParents(int count)
    {
        int[] parents = new int[count];
        int index = 0;
        while (index < count)
        {
            parents[index] = index;
            index++;
        }
        return parents;
    }

    internal static void ConnectIntersectingItems(
        List<DisjointUnionItem> items, int[] parents)
    {
        int firstIndex = 0;
        while (firstIndex < items.Count)
        {
            int secondIndex = firstIndex + 1;
            while (secondIndex < items.Count)
            {
                if (items[secondIndex].Bounds.MinX >
                    items[firstIndex].Bounds.MaxX)
                {
                    break;
                }
                if (EnvelopeAlgorithms.IntersectsEnvelopeValue(
                    items[firstIndex].Bounds,
                    items[secondIndex].Bounds))
                {
                    UnionRoots(parents, firstIndex, secondIndex);
                }
                secondIndex++;
            }
            firstIndex++;
        }
    }

    internal static int CompareDisjointItems(
        DisjointUnionItem first, DisjointUnionItem second)
    {
        return CascadedUnionEngine.CompareBounds(
            first.Bounds, second.Bounds);
    }

    internal static int FindRoot(int[] parents, int index)
    {
        int root = index;
        while (parents[root] != root)
        {
            root = parents[root];
        }
        int current = index;
        while (parents[current] != current)
        {
            int next = parents[current];
            parents[current] = root;
            current = next;
        }
        return root;
    }

    internal static void UnionRoots(
        int[] parents, int firstIndex, int secondIndex)
    {
        int firstRoot = FindRoot(parents, firstIndex);
        int secondRoot = FindRoot(parents, secondIndex);
        if (firstRoot == secondRoot) return;
        if (firstRoot < secondRoot)
        {
            parents[secondRoot] = firstRoot;
        }
        else
        {
            parents[firstRoot] = secondRoot;
        }
    }

    internal static Geometry CreateSubsetGeometry(
        List<DisjointUnionItem> items, int[] parents,
        int root, Geometry metadataSource)
    {
        List<Geometry> components = new List<Geometry>();
        int index = 0;
        while (index < items.Count)
        {
            if (FindRoot(parents, index) == root)
            {
                components.Add(items[index].Geometry);
            }
            index++;
        }
        return GeometryCollections.CreateRawCollection(
            components.ToArray(), metadataSource.CoordinateLayout,
            metadataSource.SpatialReferenceId);
    }

    internal static Geometry CombineSubsetResults(
        List<Geometry> subsetResults, Geometry metadataSource)
    {
        List<Geometry> components = new List<Geometry>();
        int resultIndex = 0;
        while (resultIndex < subsetResults.Count)
        {
            Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
                subsetResults[resultIndex],
                GeometryConstants.DefaultMaximumNestingDepth,
                GeometryConstants.DefaultMaximumComponents);
            int leafIndex = 0;
            while (leafIndex < leaves.Length)
            {
                if (!GeometryInspection.IsEmptyValue(leaves[leafIndex]))
                {
                    components.Add(leaves[leafIndex]);
                }
                leafIndex++;
            }
            resultIndex++;
        }
        return OverlayResultExtractor.CreateCollectionResult(
            components, metadataSource, GeometryType.GeometryCollection);
    }
}
