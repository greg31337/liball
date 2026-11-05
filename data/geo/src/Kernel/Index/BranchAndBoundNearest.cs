using System.Collections.Generic;

namespace geo;

internal class NearestSearchEntry
{
    internal PackedNode? Node;
    internal RTreeNode? DynamicNode;
    internal PackedItem? Item;
    internal double LowerBound;
    internal int Kind;
    internal int Order;
}

internal class NearestItemCandidate
{
    internal PackedItem Item = new PackedItem();
    internal double Distance;
}

internal class NearestSearchQueue
{
    internal List<NearestSearchEntry> Entries =
        new List<NearestSearchEntry>();

    internal void AddEntry(NearestSearchEntry entry)
    {
        Entries.Add(entry);
        int index = Entries.Count - 1;
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (CompareEntries(Entries[parent], Entries[index]) <= 0)
            {
                return;
            }
            NearestSearchEntry value = Entries[parent];
            Entries[parent] = Entries[index];
            Entries[index] = value;
            index = parent;
        }
    }

    internal NearestSearchEntry RemoveMinimumEntry()
    {
        if (Entries.Count == 0)
        {
            throw new SpatialIndexException(
                "Nearest search queue is empty.");
        }
        NearestSearchEntry result = Entries[0];
        int lastIndex = Entries.Count - 1;
        NearestSearchEntry last = Entries[lastIndex];
        Entries.RemoveAt(lastIndex);
        if (Entries.Count == 0) return result;
        Entries[0] = last;
        int index = 0;
        while (true)
        {
            int left = index * 2 + 1;
            if (left >= Entries.Count) break;
            int right = left + 1;
            int smaller = left;
            if (right < Entries.Count &&
                CompareEntries(Entries[right], Entries[left]) < 0)
            {
                smaller = right;
            }
            if (CompareEntries(Entries[index], Entries[smaller]) <= 0)
            {
                break;
            }
            NearestSearchEntry value = Entries[index];
            Entries[index] = Entries[smaller];
            Entries[smaller] = value;
            index = smaller;
        }
        return result;
    }

    internal bool HasEntries()
    {
        return Entries.Count > 0;
    }

    internal static int CompareEntries(
        NearestSearchEntry first, NearestSearchEntry second)
    {
        if (first.LowerBound < second.LowerBound) return -1;
        if (first.LowerBound > second.LowerBound) return 1;
        if (first.Kind < second.Kind) return -1;
        if (first.Kind > second.Kind) return 1;
        return first.Order.CompareTo(second.Order);
    }
}

internal static class BranchAndBoundNearest
{
    internal static PackedItem[] FindNearestItems(
        PackedNode? root, Envelope queryBounds, Geometry? queryGeometry,
        bool refineWithGeometry, int maximumResults,
        double maximumDistance)
    {
        if (root == null ||
            EnvelopeAlgorithms.ValidateAndCheckEmpty(queryBounds))
        {
            return [];
        }
        NearestSearchQueue queue = new NearestSearchQueue();
        AddNodeSearchEntry(root, queryBounds, queue);
        List<NearestItemCandidate> candidates =
            new List<NearestItemCandidate>();
        double threshold = maximumDistance;
        while (queue.HasEntries())
        {
            NearestSearchEntry entry = queue.RemoveMinimumEntry();
            if (entry.LowerBound > threshold) break;
            if (entry.Node != null)
            {
                AddNodeContentsToQueue(
                    entry.Node, queryBounds, threshold, queue);
            }
            else if (entry.Item != null)
            {
                double distance = GetExactItemDistance(
                    entry.Item, queryBounds, queryGeometry,
                    refineWithGeometry);
                if (DistanceAlgorithm.IsDistanceWithinLimit(
                    distance, maximumDistance))
                {
                    AddNearestCandidate(
                        candidates, entry.Item, distance);
                    if (maximumResults > 0 &&
                        candidates.Count > maximumResults)
                    {
                        candidates.RemoveAt(candidates.Count - 1);
                    }
                    if (maximumResults > 0 &&
                        candidates.Count == maximumResults)
                    {
                        double candidateThreshold =
                            candidates[candidates.Count - 1].Distance;
                        if (candidateThreshold < threshold)
                        {
                            threshold = candidateThreshold;
                        }
                    }
                }
            }
        }
        PackedItem[] results = new PackedItem[candidates.Count];
        int index = 0;
        while (index < candidates.Count)
        {
            results[index] = candidates[index].Item;
            index++;
        }
        return results;
    }

    internal static PackedItem[] FindNearestDynamicItems(
        RTreeNode root, Envelope queryBounds, Geometry? queryGeometry,
        bool refineWithGeometry, int maximumResults,
        double maximumDistance)
    {
        if (root.GetEntryCount() == 0 ||
            EnvelopeAlgorithms.ValidateAndCheckEmpty(queryBounds))
        {
            return [];
        }
        NearestSearchQueue queue = new NearestSearchQueue();
        AddDynamicNodeSearchEntry(root, queryBounds, queue);
        List<NearestItemCandidate> candidates =
            new List<NearestItemCandidate>();
        double threshold = maximumDistance;
        while (queue.HasEntries())
        {
            NearestSearchEntry entry = queue.RemoveMinimumEntry();
            if (entry.LowerBound > threshold) break;
            if (entry.DynamicNode != null)
            {
                AddDynamicNodeContentsToQueue(
                    entry.DynamicNode, queryBounds, threshold, queue);
            }
            else if (entry.Item != null)
            {
                double distance = GetExactItemDistance(
                    entry.Item, queryBounds, queryGeometry,
                    refineWithGeometry);
                if (DistanceAlgorithm.IsDistanceWithinLimit(
                    distance, maximumDistance))
                {
                    AddNearestCandidate(
                        candidates, entry.Item, distance);
                    if (maximumResults > 0 &&
                        candidates.Count > maximumResults)
                    {
                        candidates.RemoveAt(candidates.Count - 1);
                    }
                    if (maximumResults > 0 &&
                        candidates.Count == maximumResults)
                    {
                        double candidateThreshold =
                            candidates[candidates.Count - 1].Distance;
                        if (candidateThreshold < threshold)
                        {
                            threshold = candidateThreshold;
                        }
                    }
                }
            }
        }
        PackedItem[] results = new PackedItem[candidates.Count];
        int index = 0;
        while (index < candidates.Count)
        {
            results[index] = candidates[index].Item;
            index++;
        }
        return results;
    }

    internal static void AddNodeSearchEntry(
        PackedNode node, Envelope queryBounds, NearestSearchQueue queue)
    {
        NearestSearchEntry entry = new NearestSearchEntry();
        entry.Node = node;
        entry.LowerBound = EnvelopeAlgorithms.DistanceBetweenEnvelopes(
            node.Bounds, queryBounds);
        entry.Kind = 0;
        entry.Order = node.Order;
        queue.AddEntry(entry);
    }

    internal static void AddNodeContentsToQueue(
        PackedNode node, Envelope queryBounds, double threshold,
        NearestSearchQueue queue)
    {
        if (node.IsLeafNode())
        {
            int itemIndex = 0;
            while (itemIndex < node.Items.Length)
            {
                PackedItem item = node.Items[itemIndex];
                double lowerBound =
                    EnvelopeAlgorithms.DistanceBetweenEnvelopes(
                        item.Item.Envelope, queryBounds);
                if (lowerBound <= threshold)
                {
                    NearestSearchEntry entry =
                        new NearestSearchEntry();
                    entry.Item = item;
                    entry.LowerBound = lowerBound;
                    entry.Kind = 1;
                    entry.Order = item.Order;
                    queue.AddEntry(entry);
                }
                itemIndex++;
            }
            return;
        }
        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            PackedNode child = node.Children[childIndex];
            double lowerBound =
                EnvelopeAlgorithms.DistanceBetweenEnvelopes(
                    child.Bounds, queryBounds);
            if (lowerBound <= threshold)
            {
                AddNodeSearchEntry(child, queryBounds, queue);
            }
            childIndex++;
        }
    }

    internal static void AddDynamicNodeSearchEntry(
        RTreeNode node, Envelope queryBounds,
        NearestSearchQueue queue)
    {
        NearestSearchEntry entry = new NearestSearchEntry();
        entry.DynamicNode = node;
        entry.LowerBound = EnvelopeAlgorithms.DistanceBetweenEnvelopes(
            node.Bounds, queryBounds);
        entry.Kind = 0;
        entry.Order = node.Order;
        queue.AddEntry(entry);
    }

    internal static void AddDynamicNodeContentsToQueue(
        RTreeNode node, Envelope queryBounds, double threshold,
        NearestSearchQueue queue)
    {
        if (node.IsLeafNode())
        {
            int itemIndex = 0;
            while (itemIndex < node.Items.Count)
            {
                PackedItem item = node.Items[itemIndex];
                double lowerBound =
                    EnvelopeAlgorithms.DistanceBetweenEnvelopes(
                        item.Item.Envelope, queryBounds);
                if (lowerBound <= threshold)
                {
                    NearestSearchEntry entry =
                        new NearestSearchEntry();
                    entry.Item = item;
                    entry.LowerBound = lowerBound;
                    entry.Kind = 1;
                    entry.Order = item.Order;
                    queue.AddEntry(entry);
                }
                itemIndex++;
            }
            return;
        }
        int childIndex = 0;
        while (childIndex < node.Children.Count)
        {
            RTreeNode child = node.Children[childIndex];
            double lowerBound =
                EnvelopeAlgorithms.DistanceBetweenEnvelopes(
                    child.Bounds, queryBounds);
            if (lowerBound <= threshold)
            {
                AddDynamicNodeSearchEntry(
                    child, queryBounds, queue);
            }
            childIndex++;
        }
    }

    internal static double GetExactItemDistance(
        PackedItem item, Envelope queryBounds, Geometry? queryGeometry,
        bool refineWithGeometry)
    {
        if (!refineWithGeometry)
        {
            return EnvelopeAlgorithms.DistanceBetweenEnvelopes(
                item.Item.Envelope, queryBounds);
        }
        if (queryGeometry == null || item.Item.Geometry == null)
        {
            return double.NaN;
        }
        DistanceResult result =
            DistanceAlgorithm.GetIndexedMinimumDistance(
                item.Item.Geometry, queryGeometry);
        if (!result.HasResult) return double.NaN;
        return result.Distance;
    }

    internal static void AddNearestCandidate(
        List<NearestItemCandidate> candidates, PackedItem item,
        double distance)
    {
        NearestItemCandidate candidate = new NearestItemCandidate();
        candidate.Item = item;
        candidate.Distance = distance;
        int index = 0;
        while (index < candidates.Count &&
            CompareCandidates(candidates[index], candidate) <= 0)
        {
            index++;
        }
        candidates.Insert(index, candidate);
    }

    internal static int CompareCandidates(
        NearestItemCandidate first, NearestItemCandidate second)
    {
        if (first.Distance < second.Distance) return -1;
        if (first.Distance > second.Distance) return 1;
        return first.Item.Order.CompareTo(second.Item.Order);
    }
}
