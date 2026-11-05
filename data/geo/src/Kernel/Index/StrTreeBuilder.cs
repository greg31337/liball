using System;
using System.Collections.Generic;

namespace geo;

internal class StrTreeBuilder
{
    internal int NodeCapacity;
    internal List<PackedItem> Items = new List<PackedItem>();
    internal HashSet<int> ItemIds = new HashSet<int>();
    internal object LifecycleLock = new object();
    internal PackedNode? Root;
    internal bool Built;
    internal int NextItemOrder;
    internal int NextNodeOrder;

    internal StrTreeBuilder(int nodeCapacity)
    {
        if (nodeCapacity < 2 ||
            nodeCapacity > GeometryConstants.DefaultMaximumComponents)
        {
            throw new SpatialIndexException(
                "STRtree node capacity must be between two and the maximum component count.");
        }
        NodeCapacity = nodeCapacity;
    }

    internal void AddItem(SpatialIndexItem item)
    {
        lock (LifecycleLock)
        {
            if (Built)
            {
                throw new SpatialIndexException(
                    "Cannot insert an item after the STRtree has been built.");
            }
            if (item == null)
            {
                throw new SpatialIndexException(
                    "STRtree item is null.");
            }
            bool empty = EnvelopeAlgorithms.ValidateAndCheckEmpty(
                item.Envelope);
            if (empty)
            {
                throw new SpatialIndexException(
                    "STRtree items require non-empty envelopes.");
            }
            if (ItemIds.Contains(item.Id))
            {
                throw new SpatialIndexException(
                    "STRtree item identifiers must be unique.");
            }
            if (Items.Count >= GeometryConstants.DefaultMaximumComponents)
            {
                throw new SpatialIndexException(
                    "STRtree item count exceeds the supported maximum.");
            }

            PackedItem packed = CreatePackedItemSnapshot(
                item, NextItemOrder);
            NextItemOrder++;
            Items.Add(packed);
            ItemIds.Add(item.Id);
        }
    }

    internal bool RemoveItem(int itemId)
    {
        lock (LifecycleLock)
        {
            if (Built)
            {
                throw new SpatialIndexException(
                    "Cannot remove an item after the STRtree has been built.");
            }
            int index = 0;
            while (index < Items.Count)
            {
                if (Items[index].Item.Id == itemId)
                {
                    Items.RemoveAt(index);
                    ItemIds.Remove(itemId);
                    return true;
                }
                index++;
            }
            return false;
        }
    }

    internal bool UpdateItem(SpatialIndexItem item)
    {
        lock (LifecycleLock)
        {
            if (Built)
            {
                throw new SpatialIndexException(
                    "Cannot update an item after the STRtree has been built.");
            }
            if (item == null)
            {
                throw new SpatialIndexException(
                    "STRtree item is null.");
            }
            int index = 0;
            while (index < Items.Count)
            {
                if (Items[index].Item.Id == item.Id)
                {
                    PackedItem replacement = CreatePackedItemSnapshot(
                        item, Items[index].Order);
                    Items[index] = replacement;
                    return true;
                }
                index++;
            }
            return false;
        }
    }

    internal void ClearItems()
    {
        lock (LifecycleLock)
        {
            if (Built)
            {
                throw new SpatialIndexException(
                    "Cannot clear the STRtree after it has been built.");
            }
            Items.Clear();
            ItemIds.Clear();
            Root = null;
            NextItemOrder = 0;
            NextNodeOrder = 0;
        }
    }

    internal SpatialIndexItem[] QueryEnvelope(SpatialIndexQuery query)
    {
        ValidateSpatialIndexQuery(query, false);
        PackedNode? root = BuildTree();
        if (root == null ||
            EnvelopeAlgorithms.ValidateAndCheckEmpty(query.Envelope))
        {
            return [];
        }
        List<PackedItem> matches = new List<PackedItem>();
        AddEnvelopeMatches(root, query.Envelope, matches);
        SortPackedItemsByInsertionOrder(matches);
        return CopyPackedItemResults(matches, query.MaximumResults);
    }

    internal SpatialIndexItem[] QueryGeometry(SpatialIndexQuery query)
    {
        ValidateSpatialIndexQuery(query, query.RefineWithGeometry);
        PackedNode? root = BuildTree();
        if (root == null ||
            EnvelopeAlgorithms.ValidateAndCheckEmpty(query.Envelope))
        {
            return [];
        }
        List<PackedItem> candidates = new List<PackedItem>();
        AddEnvelopeMatches(root, query.Envelope, candidates);
        SortPackedItemsByInsertionOrder(candidates);
        if (!query.RefineWithGeometry)
        {
            return CopyPackedItemResults(
                candidates, query.MaximumResults);
        }
        Geometry? queryGeometry = query.Geometry;
        if (queryGeometry == null)
        {
            throw new SpatialIndexException(
                "Geometry refinement requires a query geometry.");
        }

        List<PackedItem> matches = new List<PackedItem>();
        int index = 0;
        while (index < candidates.Count)
        {
            PackedItem candidate = candidates[index];
            if (candidate.Item.Geometry != null &&
                GeometryPredicateAlgorithms.Intersects(
                    candidate.Item.Geometry, queryGeometry))
            {
                matches.Add(candidate);
            }
            index++;
        }
        return CopyPackedItemResults(matches, query.MaximumResults);
    }

    internal SpatialIndexItem? QueryNearest(SpatialIndexQuery query)
    {
        ValidateSpatialIndexDistanceQuery(query);
        PackedItem[] matches = BranchAndBoundNearest.FindNearestItems(
            BuildTree(), query.Envelope, query.Geometry,
            query.RefineWithGeometry, 1, query.MaximumDistance);
        if (matches.Length == 0) return null;
        return CopySpatialIndexItemValue(matches[0].Item);
    }

    internal SpatialIndexItem[] QueryKNearest(SpatialIndexQuery query)
    {
        ValidateSpatialIndexDistanceQuery(query);
        PackedItem[] matches = BranchAndBoundNearest.FindNearestItems(
            BuildTree(), query.Envelope, query.Geometry,
            query.RefineWithGeometry, query.MaximumResults,
            query.MaximumDistance);
        return CopyPackedItemArrayResults(matches);
    }

    internal SpatialIndexItem[] QueryWithinDistance(SpatialIndexQuery query)
    {
        ValidateSpatialIndexDistanceQuery(query);
        PackedItem[] matches = BranchAndBoundNearest.FindNearestItems(
            BuildTree(), query.Envelope, query.Geometry,
            query.RefineWithGeometry, query.MaximumResults,
            query.MaximumDistance);
        return CopyPackedItemArrayResults(matches);
    }

    internal SpatialIndexItem[] EnumerateItems()
    {
        BuildTree();
        List<PackedItem> items = new List<PackedItem>();
        int index = 0;
        while (index < Items.Count)
        {
            items.Add(Items[index]);
            index++;
        }
        SortPackedItemsByInsertionOrder(items);
        return CopyPackedItemResults(items, 0);
    }

    internal int CountItems()
    {
        lock (LifecycleLock)
        {
            return Items.Count;
        }
    }

    internal PackedNode? BuildTree()
    {
        lock (LifecycleLock)
        {
            if (Built) return Root;
            if (Items.Count == 0)
            {
                Built = true;
                Root = null;
                return null;
            }

            NextNodeOrder = 0;
            PackedItem[] entries = Items.ToArray();
            PackedNode[] level = CreatePackedLeafLevel(entries);
            while (level.Length > 1)
            {
                level = CreatePackedParentLevel(level);
            }
            Root = level[0];
            Built = true;
            return Root;
        }
    }

    internal PackedItem CreatePackedItemSnapshot(
        SpatialIndexItem item, int order)
    {
        SpatialIndexItem snapshot = new SpatialIndexItem();
        snapshot.Id = item.Id;
        snapshot.Envelope = EnvelopeAlgorithms.CopyEnvelopeValue(
            item.Envelope);
        if (item.Geometry != null)
        {
            ValidationResult validation =
                GeometryValidityChecker.ValidateGeometry(item.Geometry);
            if (!validation.IsValid)
            {
                throw new InvalidGeometryException(
                    "STRtree item geometry is invalid: " +
                    validation.Message);
            }
            Envelope geometryBounds =
                EnvelopeAlgorithms.CreateGeometryEnvelope(item.Geometry);
            if (!EnvelopeAlgorithms.ValidateAndCheckEmpty(geometryBounds) &&
                !EnvelopeAlgorithms.CoversEnvelopeValue(
                    snapshot.Envelope, geometryBounds))
            {
                throw new SpatialIndexException(
                    "STRtree item envelope must cover its geometry.");
            }
            snapshot.Geometry = GeometryCopy.CopyGeometry(item.Geometry);
        }
        snapshot.Payload = item.Payload;

        Coordinate? center = EnvelopeAlgorithms.GetEnvelopeCenter(
            snapshot.Envelope);
        if (center == null)
        {
            throw new SpatialIndexException(
                "STRtree item envelope cannot be empty.");
        }
        PackedItem packed = new PackedItem();
        packed.Item = snapshot;
        packed.CenterX = center.X;
        packed.CenterY = center.Y;
        packed.Order = order;
        return packed;
    }

    internal PackedNode[] CreatePackedLeafLevel(PackedItem[] entries)
    {
        PrepareStableItemSortPositions(entries);
        Array.Sort(entries, (first, second) =>
            ComparePackedItemsByX(first, second));
        int parentCount = DivideRoundingUp(
            entries.Length, NodeCapacity);
        int sliceCount = GetCeilingSquareRoot(parentCount);
        int sliceCapacity = DivideRoundingUp(
            entries.Length, sliceCount);
        PackedInterval[] slices =
            IntervalList.CreateFixedSizeIntervals(
                entries.Length, sliceCapacity);
        List<PackedNode> leaves = new List<PackedNode>();
        int sliceIndex = 0;
        while (sliceIndex < slices.Length)
        {
            PackedItem[] slice = CopyPackedItemInterval(
                entries, slices[sliceIndex]);
            PrepareStableItemSortPositions(slice);
            Array.Sort(slice, (first, second) =>
                ComparePackedItemsByY(first, second));
            PackedInterval[] groups =
                IntervalList.CreateFixedSizeIntervals(
                    slice.Length, NodeCapacity);
            int groupIndex = 0;
            while (groupIndex < groups.Length)
            {
                leaves.Add(CreatePackedLeafNode(
                    slice, groups[groupIndex]));
                groupIndex++;
            }
            sliceIndex++;
        }
        return leaves.ToArray();
    }

    internal PackedNode[] CreatePackedParentLevel(PackedNode[] children)
    {
        PrepareStableNodeSortPositions(children);
        Array.Sort(children, (first, second) =>
            ComparePackedNodesByX(first, second));
        int parentCount = DivideRoundingUp(
            children.Length, NodeCapacity);
        int sliceCount = GetCeilingSquareRoot(parentCount);
        int sliceCapacity = DivideRoundingUp(
            children.Length, sliceCount);
        PackedInterval[] slices =
            IntervalList.CreateFixedSizeIntervals(
                children.Length, sliceCapacity);
        List<PackedNode> parents = new List<PackedNode>();
        int sliceIndex = 0;
        while (sliceIndex < slices.Length)
        {
            PackedNode[] slice = CopyPackedNodeInterval(
                children, slices[sliceIndex]);
            PrepareStableNodeSortPositions(slice);
            Array.Sort(slice, (first, second) =>
                ComparePackedNodesByY(first, second));
            PackedInterval[] groups =
                IntervalList.CreateFixedSizeIntervals(
                    slice.Length, NodeCapacity);
            int groupIndex = 0;
            while (groupIndex < groups.Length)
            {
                parents.Add(CreatePackedBranchNode(
                    slice, groups[groupIndex]));
                groupIndex++;
            }
            sliceIndex++;
        }
        return parents.ToArray();
    }

    internal PackedNode CreatePackedLeafNode(
        PackedItem[] source, PackedInterval interval)
    {
        PackedItem[] items = new PackedItem[interval.Count];
        Envelope bounds = EnvelopeAlgorithms.CreateEmptyEnvelope();
        int index = 0;
        while (index < interval.Count)
        {
            PackedItem item = source[interval.Start + index];
            items[index] = item;
            bounds = EnvelopeAlgorithms.ExpandEnvelopeByEnvelope(
                bounds, item.Item.Envelope);
            index++;
        }
        PackedNode node = new PackedNode(
            bounds, [], items, 0, NextNodeOrder);
        NextNodeOrder++;
        return node;
    }

    internal PackedNode CreatePackedBranchNode(
        PackedNode[] source, PackedInterval interval)
    {
        PackedNode[] children = new PackedNode[interval.Count];
        Envelope bounds = EnvelopeAlgorithms.CreateEmptyEnvelope();
        int index = 0;
        while (index < interval.Count)
        {
            PackedNode child = source[interval.Start + index];
            children[index] = child;
            bounds = EnvelopeAlgorithms.ExpandEnvelopeByEnvelope(
                bounds, child.Bounds);
            index++;
        }
        int level = children[0].Level + 1;
        PackedNode node = new PackedNode(
            bounds, children, [], level, NextNodeOrder);
        NextNodeOrder++;
        return node;
    }

    internal static PackedItem[] CopyPackedItemInterval(
        PackedItem[] source, PackedInterval interval)
    {
        PackedItem[] copy = new PackedItem[interval.Count];
        int index = 0;
        while (index < interval.Count)
        {
            copy[index] = source[interval.Start + index];
            index++;
        }
        return copy;
    }

    internal static PackedNode[] CopyPackedNodeInterval(
        PackedNode[] source, PackedInterval interval)
    {
        PackedNode[] copy = new PackedNode[interval.Count];
        int index = 0;
        while (index < interval.Count)
        {
            copy[index] = source[interval.Start + index];
            index++;
        }
        return copy;
    }

    internal static void PrepareStableItemSortPositions(
        PackedItem[] items)
    {
        int index = 0;
        while (index < items.Length)
        {
            items[index].SortPosition = index;
            index++;
        }
    }

    internal static void PrepareStableNodeSortPositions(
        PackedNode[] nodes)
    {
        int index = 0;
        while (index < nodes.Length)
        {
            nodes[index].SortPosition = index;
            index++;
        }
    }

    internal static int ComparePackedItemsByX(
        PackedItem first, PackedItem second)
    {
        if (first.CenterX < second.CenterX) return -1;
        if (first.CenterX > second.CenterX) return 1;
        return first.SortPosition.CompareTo(second.SortPosition);
    }

    internal static int ComparePackedItemsByY(
        PackedItem first, PackedItem second)
    {
        if (first.CenterY < second.CenterY) return -1;
        if (first.CenterY > second.CenterY) return 1;
        return first.SortPosition.CompareTo(second.SortPosition);
    }

    internal static int ComparePackedNodesByX(
        PackedNode first, PackedNode second)
    {
        if (first.CenterX < second.CenterX) return -1;
        if (first.CenterX > second.CenterX) return 1;
        return first.SortPosition.CompareTo(second.SortPosition);
    }

    internal static int ComparePackedNodesByY(
        PackedNode first, PackedNode second)
    {
        if (first.CenterY < second.CenterY) return -1;
        if (first.CenterY > second.CenterY) return 1;
        return first.SortPosition.CompareTo(second.SortPosition);
    }

    internal static int DivideRoundingUp(int value, int divisor)
    {
        return value / divisor + (value % divisor == 0 ? 0 : 1);
    }

    internal static int GetCeilingSquareRoot(int value)
    {
        if (value < 1)
        {
            throw new SpatialIndexException(
                "STRtree packing requires a positive group count.");
        }
        int result = (int)Math.Sqrt(value);
        if (result * result < value) result++;
        return result;
    }

    internal static void ValidateSpatialIndexQuery(
        SpatialIndexQuery query, bool requireGeometry)
    {
        if (query == null)
        {
            throw new SpatialIndexException(
                "Spatial index query is null.");
        }
        EnvelopeAlgorithms.ValidateAndCheckEmpty(query.Envelope);
        if (query.MaximumResults < 0)
        {
            throw new SpatialIndexException(
                "Spatial index maximum result count cannot be negative.");
        }
        if (requireGeometry && query.Geometry == null)
        {
            throw new SpatialIndexException(
                "Geometry refinement requires a query geometry.");
        }
        if (requireGeometry && query.Geometry != null)
        {
            ValidationResult validation =
                GeometryValidityChecker.ValidateGeometry(query.Geometry);
            if (!validation.IsValid)
            {
                throw new InvalidGeometryException(
                    "Spatial index query geometry is invalid: " +
                    validation.Message);
            }
            if (requireGeometry &&
                !CollectionRelationGraphBuilder.SupportsCoreGeometry(
                    query.Geometry))
            {
                throw new InvalidGeometryException(
                    "Geometry refinement supports only core linear geometry types.");
            }
            if (requireGeometry)
            {
                Envelope geometryBounds =
                    EnvelopeAlgorithms.CreateGeometryEnvelope(
                        query.Geometry);
                if (!EnvelopeAlgorithms.ValidateAndCheckEmpty(
                    geometryBounds) &&
                    !EnvelopeAlgorithms.CoversEnvelopeValue(
                        query.Envelope, geometryBounds))
                {
                    throw new SpatialIndexException(
                        "Spatial index query envelope must cover its geometry.");
                }
            }
        }
    }

    internal static void ValidateSpatialIndexDistanceQuery(
        SpatialIndexQuery query)
    {
        if (query == null)
        {
            throw new SpatialIndexException(
                "Spatial index query is null.");
        }
        ValidateSpatialIndexQuery(query, query.RefineWithGeometry);
        if (double.IsNaN(query.MaximumDistance) ||
            query.MaximumDistance < 0.0)
        {
            throw new SpatialIndexException(
                "Spatial index maximum distance must be non-negative or positive infinity.");
        }
    }

    internal static void AddEnvelopeMatches(
        PackedNode node, Envelope queryBounds,
        List<PackedItem> matches)
    {
        if (!EnvelopeAlgorithms.IntersectsEnvelopeValue(
            node.Bounds, queryBounds))
        {
            return;
        }
        if (node.IsLeafNode())
        {
            int itemIndex = 0;
            while (itemIndex < node.Items.Length)
            {
                PackedItem item = node.Items[itemIndex];
                if (EnvelopeAlgorithms.IntersectsEnvelopeValue(
                    item.Item.Envelope, queryBounds))
                {
                    matches.Add(item);
                }
                itemIndex++;
            }
            return;
        }
        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            AddEnvelopeMatches(
                node.Children[childIndex], queryBounds, matches);
            childIndex++;
        }
    }

    internal static void SortPackedItemsByInsertionOrder(
        List<PackedItem> items)
    {
        items.Sort((first, second) =>
            first.Order.CompareTo(second.Order));
    }

    internal static SpatialIndexItem[] CopyPackedItemResults(
        List<PackedItem> items, int maximumResults)
    {
        int count = items.Count;
        if (maximumResults > 0 && count > maximumResults)
        {
            count = maximumResults;
        }
        SpatialIndexItem[] results = new SpatialIndexItem[count];
        int index = 0;
        while (index < count)
        {
            results[index] = CopySpatialIndexItemValue(
                items[index].Item);
            index++;
        }
        return results;
    }

    internal static SpatialIndexItem[] CopyPackedItemArrayResults(
        PackedItem[] items)
    {
        SpatialIndexItem[] results =
            new SpatialIndexItem[items.Length];
        int index = 0;
        while (index < items.Length)
        {
            results[index] = CopySpatialIndexItemValue(
                items[index].Item);
            index++;
        }
        return results;
    }

    internal static SpatialIndexItem CopySpatialIndexItemValue(
        SpatialIndexItem item)
    {
        SpatialIndexItem copy = new SpatialIndexItem();
        copy.Id = item.Id;
        copy.Envelope = EnvelopeAlgorithms.CopyEnvelopeValue(
            item.Envelope);
        if (item.Geometry != null)
        {
            copy.Geometry = GeometryCopy.CopyGeometry(item.Geometry);
        }
        copy.Payload = item.Payload;
        return copy;
    }
}
