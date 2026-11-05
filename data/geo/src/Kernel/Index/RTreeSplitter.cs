using System;
using System.Collections.Generic;

namespace geo;

internal class RTreeSplitEntry
{
    internal PackedItem? Item;
    internal RTreeNode? Node;
    internal Envelope Bounds = new Envelope();
    internal int Order;
}

internal class RTreeSplitResult
{
    internal List<RTreeSplitEntry> First =
        new List<RTreeSplitEntry>();
    internal List<RTreeSplitEntry> Second =
        new List<RTreeSplitEntry>();
}

internal class RTreeSeedPair
{
    internal int First;
    internal int Second;
}

internal static class RTreeSplitter
{
    internal static RTreeNode SplitNode(
        RTreeNode node, int minimumNodeSize, int siblingOrder)
    {
        RTreeSplitEntry[] entries = CreateSplitEntries(node);
        RTreeSplitResult groups = QuadraticPartition(
            entries, minimumNodeSize);
        RTreeNode sibling = new RTreeNode(node.Leaf, siblingOrder);
        sibling.Parent = node.Parent;
        AssignFirstSplitGroup(node, groups.First);
        AssignSecondSplitGroup(sibling, groups.Second);
        node.RecalculateBounds();
        sibling.RecalculateBounds();
        return sibling;
    }

    internal static RTreeSplitEntry[] CreateSplitEntries(RTreeNode node)
    {
        RTreeSplitEntry[] entries =
            new RTreeSplitEntry[node.GetEntryCount()];
        int index = 0;
        if (node.Leaf)
        {
            while (index < node.Items.Count)
            {
                PackedItem item = node.Items[index];
                RTreeSplitEntry entry = new RTreeSplitEntry();
                entry.Item = item;
                entry.Bounds = item.Item.Envelope;
                entry.Order = item.Order;
                entries[index] = entry;
                index++;
            }
        }
        else
        {
            while (index < node.Children.Count)
            {
                RTreeNode child = node.Children[index];
                RTreeSplitEntry entry = new RTreeSplitEntry();
                entry.Node = child;
                entry.Bounds = child.Bounds;
                entry.Order = child.Order;
                entries[index] = entry;
                index++;
            }
        }
        return entries;
    }

    internal static RTreeSplitResult QuadraticPartition(
        RTreeSplitEntry[] entries, int minimumNodeSize)
    {
        if (entries.Length < 2)
        {
            throw new SpatialIndexException(
                "An R-tree split requires at least two entries.");
        }
        RTreeSeedPair seeds = SelectQuadraticSeeds(entries);
        int firstSeed = seeds.First;
        int secondSeed = seeds.Second;
        bool[] assigned = new bool[entries.Length];
        assigned[firstSeed] = true;
        assigned[secondSeed] = true;
        int remaining = entries.Length - 2;
        RTreeSplitResult result = new RTreeSplitResult();
        result.First.Add(entries[firstSeed]);
        result.Second.Add(entries[secondSeed]);
        Envelope firstBounds = EnvelopeAlgorithms.CopyEnvelopeValue(
            entries[firstSeed].Bounds);
        Envelope secondBounds = EnvelopeAlgorithms.CopyEnvelopeValue(
            entries[secondSeed].Bounds);

        while (remaining > 0)
        {
            if (result.First.Count + remaining == minimumNodeSize)
            {
                AssignRemainingEntries(
                    entries, assigned, result.First);
                break;
            }
            if (result.Second.Count + remaining == minimumNodeSize)
            {
                AssignRemainingEntries(
                    entries, assigned, result.Second);
                break;
            }

            int selected = SelectNextSplitEntry(
                entries, assigned, firstBounds, secondBounds);
            RTreeSplitEntry entry = entries[selected];
            double firstExpansion = GetEnvelopeExpansionCost(
                firstBounds, entry.Bounds);
            double secondExpansion = GetEnvelopeExpansionCost(
                secondBounds, entry.Bounds);
            bool assignFirst = SelectFirstSplitGroup(
                firstExpansion, secondExpansion,
                firstBounds, secondBounds,
                result.First.Count, result.Second.Count);
            if (assignFirst)
            {
                result.First.Add(entry);
                firstBounds = EnvelopeAlgorithms.ExpandEnvelopeByEnvelope(
                    firstBounds, entry.Bounds);
            }
            else
            {
                result.Second.Add(entry);
                secondBounds = EnvelopeAlgorithms.ExpandEnvelopeByEnvelope(
                    secondBounds, entry.Bounds);
            }
            assigned[selected] = true;
            remaining--;
        }
        return result;
    }

    internal static RTreeSeedPair SelectQuadraticSeeds(
        RTreeSplitEntry[] entries)
    {
        RTreeSeedPair seeds = new RTreeSeedPair();
        seeds.First = 0;
        seeds.Second = 1;
        double bestWaste = GetPairWaste(entries[0], entries[1]);
        int firstIndex = 0;
        while (firstIndex + 1 < entries.Length)
        {
            int secondIndex = firstIndex + 1;
            while (secondIndex < entries.Length)
            {
                double waste = GetPairWaste(
                    entries[firstIndex], entries[secondIndex]);
                if (waste > bestWaste ||
                    (waste == bestWaste && IsEarlierSeedPair(
                        entries, firstIndex, secondIndex,
                        seeds.First, seeds.Second)))
                {
                    bestWaste = waste;
                    seeds.First = firstIndex;
                    seeds.Second = secondIndex;
                }
                secondIndex++;
            }
            firstIndex++;
        }
        return seeds;
    }

    internal static int SelectNextSplitEntry(
        RTreeSplitEntry[] entries, bool[] assigned,
        Envelope firstBounds, Envelope secondBounds)
    {
        int selected = -1;
        double bestDifference = -1.0;
        int index = 0;
        while (index < entries.Length)
        {
            if (!assigned[index])
            {
                double firstExpansion = GetEnvelopeExpansionCost(
                    firstBounds, entries[index].Bounds);
                double secondExpansion = GetEnvelopeExpansionCost(
                    secondBounds, entries[index].Bounds);
                double difference = GetExpansionDifference(
                    firstExpansion, secondExpansion);
                if (selected < 0 || difference > bestDifference ||
                    (difference == bestDifference &&
                        entries[index].Order < entries[selected].Order))
                {
                    selected = index;
                    bestDifference = difference;
                }
            }
            index++;
        }
        if (selected < 0)
        {
            throw new SpatialIndexException(
                "R-tree split could not select an unassigned entry.");
        }
        return selected;
    }

    internal static bool SelectFirstSplitGroup(
        double firstExpansion, double secondExpansion,
        Envelope firstBounds, Envelope secondBounds,
        int firstCount, int secondCount)
    {
        if (firstExpansion < secondExpansion) return true;
        if (firstExpansion > secondExpansion) return false;
        double firstArea = EnvelopeAlgorithms.GetEnvelopeArea(firstBounds);
        double secondArea = EnvelopeAlgorithms.GetEnvelopeArea(secondBounds);
        if (firstArea < secondArea) return true;
        if (firstArea > secondArea) return false;
        if (firstCount < secondCount) return true;
        if (firstCount > secondCount) return false;
        return true;
    }

    internal static double GetPairWaste(
        RTreeSplitEntry first, RTreeSplitEntry second)
    {
        Envelope union = EnvelopeAlgorithms.ExpandEnvelopeByEnvelope(
            first.Bounds, second.Bounds);
        double unionArea = EnvelopeAlgorithms.GetEnvelopeArea(union);
        double firstArea = EnvelopeAlgorithms.GetEnvelopeArea(first.Bounds);
        double secondArea = EnvelopeAlgorithms.GetEnvelopeArea(second.Bounds);
        double waste = unionArea - firstArea - secondArea;
        if (double.IsNaN(waste))
        {
            return EnvelopesHaveEqualBounds(first.Bounds, second.Bounds) ?
                0.0 : double.PositiveInfinity;
        }
        return waste;
    }

    internal static double GetEnvelopeExpansionCost(
        Envelope current, Envelope addition)
    {
        Envelope union = EnvelopeAlgorithms.ExpandEnvelopeByEnvelope(
            current, addition);
        if (EnvelopesHaveEqualBounds(current, union)) return 0.0;
        double currentArea = EnvelopeAlgorithms.GetEnvelopeArea(current);
        double unionArea = EnvelopeAlgorithms.GetEnvelopeArea(union);
        double expansion = unionArea - currentArea;
        return double.IsNaN(expansion) ?
            double.PositiveInfinity : expansion;
    }

    internal static double GetExpansionDifference(
        double first, double second)
    {
        if (first == second) return 0.0;
        if (double.IsPositiveInfinity(first) ||
            double.IsPositiveInfinity(second))
        {
            return double.PositiveInfinity;
        }
        return Math.Abs(first - second);
    }

    internal static bool EnvelopesHaveEqualBounds(
        Envelope first, Envelope second)
    {
        return first.MinX == second.MinX &&
            first.MinY == second.MinY &&
            first.MaxX == second.MaxX &&
            first.MaxY == second.MaxY;
    }

    internal static bool IsEarlierSeedPair(
        RTreeSplitEntry[] entries, int firstIndex, int secondIndex,
        int currentFirst, int currentSecond)
    {
        int firstOrder = entries[firstIndex].Order;
        int secondOrder = entries[secondIndex].Order;
        int currentFirstOrder = entries[currentFirst].Order;
        int currentSecondOrder = entries[currentSecond].Order;
        if (firstOrder < currentFirstOrder) return true;
        if (firstOrder > currentFirstOrder) return false;
        return secondOrder < currentSecondOrder;
    }

    internal static void AssignRemainingEntries(
        RTreeSplitEntry[] entries, bool[] assigned,
        List<RTreeSplitEntry> destination)
    {
        int index = 0;
        while (index < entries.Length)
        {
            if (!assigned[index])
            {
                destination.Add(entries[index]);
                assigned[index] = true;
            }
            index++;
        }
    }

    internal static void AssignFirstSplitGroup(
        RTreeNode node, List<RTreeSplitEntry> entries)
    {
        node.Items.Clear();
        node.Children.Clear();
        int index = 0;
        while (index < entries.Count)
        {
            if (node.Leaf)
            {
                PackedItem? item = entries[index].Item;
                if (item == null)
                {
                    throw new SpatialIndexException(
                        "Leaf split entry has no item.");
                }
                node.Items.Add(item);
            }
            else
            {
                RTreeNode? child = entries[index].Node;
                if (child == null)
                {
                    throw new SpatialIndexException(
                        "Branch split entry has no child node.");
                }
                child.Parent = node;
                node.Children.Add(child);
            }
            index++;
        }
    }

    internal static void AssignSecondSplitGroup(
        RTreeNode node, List<RTreeSplitEntry> entries)
    {
        int index = 0;
        while (index < entries.Count)
        {
            if (node.Leaf)
            {
                PackedItem? item = entries[index].Item;
                if (item == null)
                {
                    throw new SpatialIndexException(
                        "Leaf split entry has no item.");
                }
                node.Items.Add(item);
            }
            else
            {
                RTreeNode? child = entries[index].Node;
                if (child == null)
                {
                    throw new SpatialIndexException(
                        "Branch split entry has no child node.");
                }
                child.Parent = node;
                node.Children.Add(child);
            }
            index++;
        }
    }
}
