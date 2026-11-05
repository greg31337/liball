using System.Collections.Generic;

namespace geo;

internal class RTreeBuilder
{
    internal int NodeCapacity;
    internal int MinimumNodeSize;
    internal RTreeNode Root;
    internal HashSet<int> ItemIds = new HashSet<int>();
    internal object MutationLock = new object();
    internal int ItemCount;
    internal int NextItemOrder;
    internal int NextNodeOrder;

    internal RTreeBuilder(int nodeCapacity)
    {
        if (nodeCapacity < 2 ||
            nodeCapacity > GeometryConstants.DefaultMaximumComponents)
        {
            throw new SpatialIndexException(
                "R-tree node capacity must be between two and the maximum component count.");
        }
        NodeCapacity = nodeCapacity;
        MinimumNodeSize = (nodeCapacity + 1) / 2;
        Root = new RTreeNode(true, NextNodeOrder);
        NextNodeOrder++;
    }

    internal void InsertItem(SpatialIndexItem item)
    {
        lock (MutationLock)
        {
            if (item == null)
            {
                throw new SpatialIndexException("R-tree item is null.");
            }
            if (ItemIds.Contains(item.Id))
            {
                throw new SpatialIndexException(
                    "R-tree item identifiers must be unique.");
            }
            if (ItemCount >= GeometryConstants.DefaultMaximumComponents)
            {
                throw new SpatialIndexException(
                    "R-tree item count exceeds the supported maximum.");
            }
            PackedItem packed = CreateDynamicItemSnapshot(
                item, NextItemOrder);
            NextItemOrder++;
            InsertPackedItem(packed);
            ItemIds.Add(item.Id);
            ItemCount++;
        }
    }

    internal bool RemoveItem(int itemId)
    {
        lock (MutationLock)
        {
            RTreeNode? leaf = FindLeafByItemId(Root, itemId);
            if (leaf == null) return false;
            RemoveItemFromLeaf(leaf, itemId);
            ItemIds.Remove(itemId);
            ItemCount--;
            CondenseTree(leaf);
            return true;
        }
    }

    internal bool UpdateItem(SpatialIndexItem item)
    {
        lock (MutationLock)
        {
            if (item == null)
            {
                throw new SpatialIndexException("R-tree item is null.");
            }
            RTreeNode? leaf = FindLeafByItemId(Root, item.Id);
            if (leaf == null) return false;
            PackedItem existing = GetItemFromLeaf(leaf, item.Id);
            PackedItem replacement = CreateDynamicItemSnapshot(
                item, existing.Order);
            RemoveItemFromLeaf(leaf, item.Id);
            ItemCount--;
            CondenseTree(leaf);
            InsertPackedItem(replacement);
            ItemCount++;
            return true;
        }
    }

    internal void ClearItems()
    {
        lock (MutationLock)
        {
            ItemIds.Clear();
            ItemCount = 0;
            NextItemOrder = 0;
            NextNodeOrder = 0;
            Root = new RTreeNode(true, NextNodeOrder);
            NextNodeOrder++;
        }
    }

    internal SpatialIndexItem[] QueryEnvelope(SpatialIndexQuery query)
    {
        lock (MutationLock)
        {
            StrTreeBuilder.ValidateSpatialIndexQuery(query, false);
            if (ItemCount == 0 ||
                EnvelopeAlgorithms.ValidateAndCheckEmpty(query.Envelope))
            {
                return [];
            }
            List<PackedItem> matches = new List<PackedItem>();
            AddEnvelopeMatches(Root, query.Envelope, matches);
            StrTreeBuilder.SortPackedItemsByInsertionOrder(matches);
            return StrTreeBuilder.CopyPackedItemResults(
                matches, query.MaximumResults);
        }
    }

    internal SpatialIndexItem[] QueryGeometry(SpatialIndexQuery query)
    {
        lock (MutationLock)
        {
            StrTreeBuilder.ValidateSpatialIndexQuery(
                query, query.RefineWithGeometry);
            if (ItemCount == 0 ||
                EnvelopeAlgorithms.ValidateAndCheckEmpty(query.Envelope))
            {
                return [];
            }
            List<PackedItem> candidates = new List<PackedItem>();
            AddEnvelopeMatches(Root, query.Envelope, candidates);
            StrTreeBuilder.SortPackedItemsByInsertionOrder(candidates);
            if (!query.RefineWithGeometry)
            {
                return StrTreeBuilder.CopyPackedItemResults(
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
            return StrTreeBuilder.CopyPackedItemResults(
                matches, query.MaximumResults);
        }
    }

    internal SpatialIndexItem? QueryNearest(SpatialIndexQuery query)
    {
        lock (MutationLock)
        {
            StrTreeBuilder.ValidateSpatialIndexDistanceQuery(query);
            PackedItem[] matches =
                BranchAndBoundNearest.FindNearestDynamicItems(
                    Root, query.Envelope, query.Geometry,
                    query.RefineWithGeometry, 1,
                    query.MaximumDistance);
            if (matches.Length == 0) return null;
            return StrTreeBuilder.CopySpatialIndexItemValue(
                matches[0].Item);
        }
    }

    internal SpatialIndexItem[] QueryKNearest(SpatialIndexQuery query)
    {
        lock (MutationLock)
        {
            StrTreeBuilder.ValidateSpatialIndexDistanceQuery(query);
            PackedItem[] matches =
                BranchAndBoundNearest.FindNearestDynamicItems(
                    Root, query.Envelope, query.Geometry,
                    query.RefineWithGeometry, query.MaximumResults,
                    query.MaximumDistance);
            return StrTreeBuilder.CopyPackedItemArrayResults(matches);
        }
    }

    internal SpatialIndexItem[] QueryWithinDistance(
        SpatialIndexQuery query)
    {
        lock (MutationLock)
        {
            StrTreeBuilder.ValidateSpatialIndexDistanceQuery(query);
            PackedItem[] matches =
                BranchAndBoundNearest.FindNearestDynamicItems(
                    Root, query.Envelope, query.Geometry,
                    query.RefineWithGeometry, query.MaximumResults,
                    query.MaximumDistance);
            return StrTreeBuilder.CopyPackedItemArrayResults(matches);
        }
    }

    internal SpatialIndexItem[] EnumerateItems()
    {
        lock (MutationLock)
        {
            List<PackedItem> items = new List<PackedItem>();
            CollectLeafItems(Root, items);
            StrTreeBuilder.SortPackedItemsByInsertionOrder(items);
            return StrTreeBuilder.CopyPackedItemResults(items, 0);
        }
    }

    internal int CountItems()
    {
        lock (MutationLock)
        {
            return ItemCount;
        }
    }

    internal static void AddEnvelopeMatches(
        RTreeNode node, Envelope queryBounds,
        List<PackedItem> matches)
    {
        if (!EnvelopeAlgorithms.IntersectsEnvelopeValue(
            node.Bounds, queryBounds))
        {
            return;
        }
        if (node.Leaf)
        {
            int itemIndex = 0;
            while (itemIndex < node.Items.Count)
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
        while (childIndex < node.Children.Count)
        {
            AddEnvelopeMatches(
                node.Children[childIndex], queryBounds, matches);
            childIndex++;
        }
    }

    internal PackedItem CreateDynamicItemSnapshot(
        SpatialIndexItem item, int order)
    {
        bool empty = EnvelopeAlgorithms.ValidateAndCheckEmpty(item.Envelope);
        if (empty)
        {
            throw new SpatialIndexException(
                "R-tree items require non-empty envelopes.");
        }
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
                    "R-tree item geometry is invalid: " +
                    validation.Message);
            }
            Envelope geometryBounds =
                EnvelopeAlgorithms.CreateGeometryEnvelope(item.Geometry);
            if (!EnvelopeAlgorithms.ValidateAndCheckEmpty(geometryBounds) &&
                !EnvelopeAlgorithms.CoversEnvelopeValue(
                    snapshot.Envelope, geometryBounds))
            {
                throw new SpatialIndexException(
                    "R-tree item envelope must cover its geometry.");
            }
            snapshot.Geometry = GeometryCopy.CopyGeometry(item.Geometry);
        }
        snapshot.Payload = item.Payload;
        Coordinate? center = EnvelopeAlgorithms.GetEnvelopeCenter(
            snapshot.Envelope);
        if (center == null)
        {
            throw new SpatialIndexException(
                "R-tree item envelope cannot be empty.");
        }
        PackedItem packed = new PackedItem();
        packed.Item = snapshot;
        packed.CenterX = center.X;
        packed.CenterY = center.Y;
        packed.Order = order;
        return packed;
    }

    internal void InsertPackedItem(PackedItem item)
    {
        RTreeNode leaf = ChooseLeafNode(Root, item.Item.Envelope);
        leaf.Items.Add(item);
        RecalculateBoundsToRoot(leaf);
        SplitOverflowingNodes(leaf);
    }

    internal RTreeNode ChooseLeafNode(
        RTreeNode start, Envelope itemBounds)
    {
        RTreeNode node = start;
        while (!node.Leaf)
        {
            if (node.Children.Count == 0)
            {
                throw new SpatialIndexException(
                    "R-tree branch node has no children.");
            }
            RTreeNode selected = node.Children[0];
            double selectedExpansion =
                RTreeSplitter.GetEnvelopeExpansionCost(
                    selected.Bounds, itemBounds);
            double selectedArea =
                EnvelopeAlgorithms.GetEnvelopeArea(selected.Bounds);
            int index = 1;
            while (index < node.Children.Count)
            {
                RTreeNode candidate = node.Children[index];
                double candidateExpansion =
                    RTreeSplitter.GetEnvelopeExpansionCost(
                        candidate.Bounds, itemBounds);
                double candidateArea =
                    EnvelopeAlgorithms.GetEnvelopeArea(candidate.Bounds);
                if (ShouldChooseCandidateNode(
                    candidate, candidateExpansion, candidateArea,
                    selected, selectedExpansion, selectedArea))
                {
                    selected = candidate;
                    selectedExpansion = candidateExpansion;
                    selectedArea = candidateArea;
                }
                index++;
            }
            node = selected;
        }
        return node;
    }

    internal static bool ShouldChooseCandidateNode(
        RTreeNode candidate, double candidateExpansion,
        double candidateArea, RTreeNode selected,
        double selectedExpansion, double selectedArea)
    {
        if (candidateExpansion < selectedExpansion) return true;
        if (candidateExpansion > selectedExpansion) return false;
        if (candidateArea < selectedArea) return true;
        if (candidateArea > selectedArea) return false;
        if (candidate.GetEntryCount() < selected.GetEntryCount()) return true;
        if (candidate.GetEntryCount() > selected.GetEntryCount()) return false;
        return candidate.Order < selected.Order;
    }

    internal void SplitOverflowingNodes(RTreeNode start)
    {
        RTreeNode node = start;
        while (node.GetEntryCount() > NodeCapacity)
        {
            RTreeNode sibling = RTreeSplitter.SplitNode(
                node, MinimumNodeSize, NextNodeOrder);
            NextNodeOrder++;
            RTreeNode? parent = node.Parent;
            if (parent == null)
            {
                RTreeNode root = new RTreeNode(false, NextNodeOrder);
                NextNodeOrder++;
                node.Parent = root;
                sibling.Parent = root;
                root.Children.Add(node);
                root.Children.Add(sibling);
                root.RecalculateBounds();
                Root = root;
                return;
            }
            sibling.Parent = parent;
            parent.Children.Add(sibling);
            parent.RecalculateBounds();
            node = parent;
        }
        RecalculateBoundsToRoot(node);
    }

    internal static void RecalculateBoundsToRoot(RTreeNode start)
    {
        RTreeNode? node = start;
        while (node != null)
        {
            node.RecalculateBounds();
            node = node.Parent;
        }
    }

    internal static RTreeNode? FindLeafByItemId(
        RTreeNode node, int itemId)
    {
        if (node.Leaf)
        {
            int itemIndex = 0;
            while (itemIndex < node.Items.Count)
            {
                if (node.Items[itemIndex].Item.Id == itemId) return node;
                itemIndex++;
            }
            return null;
        }
        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            RTreeNode? found = FindLeafByItemId(
                node.Children[childIndex], itemId);
            if (found != null) return found;
            childIndex++;
        }
        return null;
    }

    internal static PackedItem GetItemFromLeaf(
        RTreeNode leaf, int itemId)
    {
        int index = 0;
        while (index < leaf.Items.Count)
        {
            if (leaf.Items[index].Item.Id == itemId)
            {
                return leaf.Items[index];
            }
            index++;
        }
        throw new SpatialIndexException(
            "R-tree item was not found in its leaf.");
    }

    internal static void RemoveItemFromLeaf(
        RTreeNode leaf, int itemId)
    {
        int index = 0;
        while (index < leaf.Items.Count)
        {
            if (leaf.Items[index].Item.Id == itemId)
            {
                leaf.Items.RemoveAt(index);
                return;
            }
            index++;
        }
        throw new SpatialIndexException(
            "R-tree item was not found for removal.");
    }

    internal void CondenseTree(RTreeNode start)
    {
        List<PackedItem> reinserts = new List<PackedItem>();
        RTreeNode node = start;
        while (node != Root)
        {
            RTreeNode? parent = node.Parent;
            if (parent == null)
            {
                throw new SpatialIndexException(
                    "R-tree non-root node has no parent.");
            }
            if (node.GetEntryCount() < MinimumNodeSize)
            {
                parent.Children.Remove(node);
                CollectLeafItems(node, reinserts);
            }
            else
            {
                node.RecalculateBounds();
            }
            parent.RecalculateBounds();
            node = parent;
        }
        Root.RecalculateBounds();
        CollapseRoot();
        reinserts.Sort((first, second) =>
            first.Order.CompareTo(second.Order));
        int index = 0;
        while (index < reinserts.Count)
        {
            InsertPackedItem(reinserts[index]);
            index++;
        }
    }

    internal static void CollectLeafItems(
        RTreeNode node, List<PackedItem> destination)
    {
        if (node.Leaf)
        {
            int itemIndex = 0;
            while (itemIndex < node.Items.Count)
            {
                destination.Add(node.Items[itemIndex]);
                itemIndex++;
            }
            return;
        }
        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            CollectLeafItems(node.Children[childIndex], destination);
            childIndex++;
        }
    }

    internal void CollapseRoot()
    {
        while (!Root.Leaf && Root.Children.Count == 1)
        {
            Root = Root.Children[0];
            Root.Parent = null;
        }
        if (!Root.Leaf && Root.Children.Count == 0)
        {
            Root = new RTreeNode(true, NextNodeOrder);
            NextNodeOrder++;
        }
        Root.RecalculateBounds();
    }
}
