using System;
using System.Collections.Generic;

namespace avif;

internal class ParsedItemDecodePlan
{
    List<ParsedItem> items;

    internal ParsedItemDecodePlan(List<ParsedItemDependencyNode> nodes)
    {
        items = new List<ParsedItem>();
        int index = 0;
        while (index < nodes.Count)
        {
            items.Add(nodes[index].GetItem());
            index++;
        }
    }

    internal int GetItemCount()
    {
        return items.Count;
    }

    internal ParsedItem? GetItem(int index)
    {
        if (index < 0 || index >= items.Count)
        {
            return null;
        }

        return items[index];
    }
}

internal class ParsedItemDependencyNode
{
    ParsedItem item;
    List<ParsedItemDependencyNode> dependencies;
    int depth;
    bool selfContained;
    ParsedItemDecodePlan? decodePlan;

    internal ParsedItemDependencyNode(ParsedItem item)
    {
        this.item = item;
        dependencies = new List<ParsedItemDependencyNode>();
        depth = 0;
        selfContained = false;
        decodePlan = null;
    }

    internal ParsedItem GetItem()
    {
        return item;
    }

    internal int GetDependencyCount()
    {
        return dependencies.Count;
    }

    internal ParsedItemDependencyNode? GetDependency(int index)
    {
        if (index < 0 || index >= dependencies.Count)
        {
            return null;
        }

        return dependencies[index];
    }

    internal int GetDepth()
    {
        return depth;
    }

    internal bool IsSelfContained()
    {
        return selfContained;
    }

    internal ParsedItemDecodePlan GetDecodePlan()
    {
        if (decodePlan != null)
        {
            return decodePlan;
        }

        List<ParsedItemDependencyNode> ordered = new List<ParsedItemDependencyNode>();
        HashSet<ParsedItemDependencyNode> visited = new HashSet<ParsedItemDependencyNode>();
        List<ParsedItemDependencyNode> stack = new List<ParsedItemDependencyNode>();
        List<int> positions = new List<int>();
        visited.Add(this);
        stack.Add(this);
        positions.Add(0);
        while (stack.Count > 0)
        {
            int stackIndex = stack.Count - 1;
            ParsedItemDependencyNode current = stack[stackIndex];
            int dependencyIndex = positions[stackIndex];
            if (dependencyIndex < current.GetDependencyCount())
            {
                positions[stackIndex] = dependencyIndex + 1;
                ParsedItemDependencyNode? dependency = current.GetDependency(dependencyIndex);
                if (visited.Add(dependency
                    ?? throw new InvalidOperationException("Invalid internal state.")))
                {
                    stack.Add(dependency);
                    positions.Add(0);
                }

                continue;
            }

            ordered.Add(current);
            stack.RemoveAt(stackIndex);
            positions.RemoveAt(stackIndex);
        }

        decodePlan = new ParsedItemDecodePlan(ordered);
        return decodePlan;
    }

    internal bool AddDependency(ParsedItemDependencyNode dependency)
    {
        int index = 0;
        while (index < dependencies.Count)
        {
            if (dependencies[index] == dependency)
            {
                return false;
            }

            index++;
        }

        dependencies.Add(dependency);
        return true;
    }

    internal void SetValidation(int validatedDepth, bool validatedSelfContained)
    {
        depth = validatedDepth;
        selfContained = validatedSelfContained;
    }
}

internal class ItemDependencyGraphCreateResult
{
    internal AvifResultCode Code;
    internal ParsedItemDependencyGraph? Graph;

    internal ItemDependencyGraphCreateResult(
        AvifResultCode code,
        ParsedItemDependencyGraph? graph)
    {
        Code = code;
        Graph = graph;
    }
}

internal class ParsedItemDependencyGraph
{
    List<ParsedItemDependencyNode> nodes;
    Dictionary<uint, ParsedItemDependencyNode> nodesById;

    internal ParsedItemDependencyGraph(
        List<ParsedItemDependencyNode> nodes,
        Dictionary<uint, ParsedItemDependencyNode> nodesById)
    {
        this.nodes = new List<ParsedItemDependencyNode>(nodes);
        this.nodesById = new Dictionary<uint, ParsedItemDependencyNode>(nodesById);
    }

    internal static ItemDependencyGraphCreateResult Create(
        List<ParsedItem> items,
        AvifSecurityLimits limits)
    {
        if (items == null || limits == null)
        {
            return new ItemDependencyGraphCreateResult(
                AvifResultCode.InvalidArgument,
                null);
        }

        if (ResourceGuard.ValidateLimits(limits) != AvifResultCode.Success)
        {
            return new ItemDependencyGraphCreateResult(
                AvifResultCode.InvalidArgument,
                null);
        }

        if (items.Count > limits.MaxItemCount)
        {
            return new ItemDependencyGraphCreateResult(
                AvifResultCode.LimitExceeded,
                null);
        }

        List<ParsedItemDependencyNode> nodes = new List<ParsedItemDependencyNode>();
        Dictionary<uint, ParsedItemDependencyNode> nodesById =
            new Dictionary<uint, ParsedItemDependencyNode>();
        int itemIndex = 0;
        while (itemIndex < items.Count)
        {
            ParsedItem item = items[itemIndex];
            uint itemId = item.GetItemId();
            if (itemId == 0 || nodesById.ContainsKey(itemId))
            {
                return new ItemDependencyGraphCreateResult(
                    AvifResultCode.InvalidItem,
                    null);
            }

            ParsedItemDependencyNode node = new ParsedItemDependencyNode(item);
            nodes.Add(node);
            nodesById.Add(itemId, node);
            itemIndex++;
        }

        long dependencyCount = 0;
        int nodeIndex = 0;
        while (nodeIndex < nodes.Count)
        {
            ParsedItemDependencyNode node = nodes[nodeIndex];
            ParsedItem item = node.GetItem();
            int referenceIndex = 0;
            while (referenceIndex < item.GetOutgoingReferenceCount())
            {
                ParsedItemReference? reference = item.GetOutgoingReference(referenceIndex);
                if ((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetKind() == ParsedItemReferenceKind.DerivedImage)
                {
                    int targetIndex = 0;
                    while (targetIndex < reference.GetTargetCount())
                    {
                        uint targetId = reference.GetTargetItemId(targetIndex);
                        int countBefore = node.GetDependencyCount();
                        AvifResultCode addCode = AddDependency(
                            node,
                            targetId,
                            nodesById,
                            limits.MaxReferenceCount,
                            dependencyCount);
                        if (addCode != AvifResultCode.Success)
                        {
                            return new ItemDependencyGraphCreateResult(
                                addCode,
                                null);
                        }

                        if (node.GetDependencyCount() > countBefore)
                        {
                            dependencyCount++;
                        }

                        targetIndex++;
                    }
                }

                referenceIndex++;
            }

            if (item.HasLocation()
                && (item.GetLocation() ?? throw new InvalidOperationException("Invalid internal state.")).GetConstructionMethod() == 2)
            {
                ParsedItemReference? locationReference = FindLocationReference(item);
                if (locationReference == null)
                {
                    nodeIndex++;
                    continue;
                }

                ParsedItemLocation? location = item.GetLocation();
                int extentIndex = 0;
                while (extentIndex < (location ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentCount())
                {
                    ulong referenceIndexValue = (location
                        .GetExtent(extentIndex) ?? throw new InvalidOperationException("Invalid internal state."))
                        .GetExtentIndex();
                    if (referenceIndexValue == 0
                        || referenceIndexValue > (ulong)locationReference.GetTargetCount())
                    {
                        return new ItemDependencyGraphCreateResult(
                            AvifResultCode.InvalidItem,
                            null);
                    }

                    uint targetId = locationReference.GetTargetItemId(
                        (int)referenceIndexValue - 1);
                    int countBefore = node.GetDependencyCount();
                    AvifResultCode addCode = AddDependency(
                        node,
                        targetId,
                        nodesById,
                        limits.MaxReferenceCount,
                        dependencyCount);
                    if (addCode != AvifResultCode.Success)
                    {
                        return new ItemDependencyGraphCreateResult(addCode, null);
                    }

                    if (node.GetDependencyCount() > countBefore)
                    {
                        dependencyCount++;
                    }

                    extentIndex++;
                }
            }

            nodeIndex++;
        }

        AvifResultCode validationCode = Validate(
            nodes,
            limits.MaxDependencyDepth);
        if (validationCode != AvifResultCode.Success)
        {
            return new ItemDependencyGraphCreateResult(validationCode, null);
        }

        return new ItemDependencyGraphCreateResult(
            AvifResultCode.Success,
            new ParsedItemDependencyGraph(nodes, nodesById));
    }

    internal static AvifResultCode AddDependency(
        ParsedItemDependencyNode node,
        uint targetId,
        Dictionary<uint, ParsedItemDependencyNode> nodesById,
        int maximumCount,
        long currentCount)
    {
        if (!nodesById.ContainsKey(targetId))
        {
            return AvifResultCode.InvalidItem;
        }

        ParsedItemDependencyNode dependency = nodesById[targetId];
        if (node.AddDependency(dependency)
            && currentCount >= maximumCount)
        {
            return AvifResultCode.LimitExceeded;
        }

        return AvifResultCode.Success;
    }

    internal static ParsedItemReference? FindLocationReference(ParsedItem item)
    {
        int index = 0;
        while (index < item.GetOutgoingReferenceCount())
        {
            ParsedItemReference? reference = item.GetOutgoingReference(index);
            if ((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetBoxType() == "iloc")
            {
                return reference;
            }

            index++;
        }

        return null;
    }

    internal static AvifResultCode Validate(
        List<ParsedItemDependencyNode> nodes,
        int maximumDepth)
    {
        Dictionary<ParsedItemDependencyNode, int> states =
            new Dictionary<ParsedItemDependencyNode, int>();
        int index = 0;
        while (index < nodes.Count)
        {
            states.Add(nodes[index], 0);
            index++;
        }

        index = 0;
        while (index < nodes.Count)
        {
            ParsedItemDependencyNode root = nodes[index];
            if (states[root] != 0)
            {
                index++;
                continue;
            }

            List<ParsedItemDependencyNode> stack = new List<ParsedItemDependencyNode>();
            List<int> positions = new List<int>();
            states[root] = 1;
            stack.Add(root);
            positions.Add(0);
            while (stack.Count > 0)
            {
                int stackIndex = stack.Count - 1;
                ParsedItemDependencyNode current = stack[stackIndex];
                int dependencyIndex = positions[stackIndex];
                if (dependencyIndex < current.GetDependencyCount())
                {
                    positions[stackIndex] = dependencyIndex + 1;
                    ParsedItemDependencyNode? dependency = current.GetDependency(dependencyIndex);
                    int dependencyState = states[dependency
                        ?? throw new InvalidOperationException("Invalid internal state.")];
                    if (dependencyState == 1)
                    {
                        return AvifResultCode.InvalidItem;
                    }

                    if (dependencyState == 0)
                    {
                        states[dependency] = 1;
                        stack.Add(dependency);
                        positions.Add(0);
                    }

                    continue;
                }

                int depth = 0;
                bool selfContained = IsDirectlySelfContained(current.GetItem());
                int completedIndex = 0;
                while (completedIndex < current.GetDependencyCount())
                {
                    ParsedItemDependencyNode? dependency = current.GetDependency(completedIndex);
                    int candidateDepth = (dependency ?? throw new InvalidOperationException("Invalid internal state.")).GetDepth() + 1;
                    if (candidateDepth > depth)
                    {
                        depth = candidateDepth;
                    }

                    if (!dependency.IsSelfContained())
                    {
                        selfContained = false;
                    }

                    completedIndex++;
                }

                if (depth > maximumDepth)
                {
                    return AvifResultCode.LimitExceeded;
                }

                current.SetValidation(depth, selfContained);
                states[current] = 2;
                stack.RemoveAt(stackIndex);
                positions.RemoveAt(stackIndex);
            }

            index++;
        }

        return AvifResultCode.Success;
    }

    internal static bool IsDirectlySelfContained(ParsedItem item)
    {
        if (!item.HasLocation())
        {
            return false;
        }

        ParsedItemLocation? location = item.GetLocation();
        if ((location ?? throw new InvalidOperationException("Invalid internal state.")).GetConstructionMethod() == 2
            && FindLocationReference(item) == null)
        {
            return false;
        }

        if (location.GetDataReferenceIndex() == 0)
        {
            return true;
        }

        return location.HasDataReference()
            && (location.GetDataReference() ?? throw new InvalidOperationException("Invalid internal state.")).IsSelfContained();
    }

    internal int GetNodeCount()
    {
        return nodes.Count;
    }

    internal ParsedItemDependencyNode? GetNode(int index)
    {
        if (index < 0 || index >= nodes.Count)
        {
            return null;
        }

        return nodes[index];
    }

    internal ParsedItemDependencyNode? FindNode(uint itemId)
    {
        if (!nodesById.ContainsKey(itemId))
        {
            return null;
        }

        return nodesById[itemId];
    }

    internal AvifResultCode ValidateSelfContainment(uint itemId)
    {
        ParsedItemDependencyNode? node = FindNode(itemId);
        if (node == null)
        {
            return AvifResultCode.NotFound;
        }

        return node.IsSelfContained()
            ? AvifResultCode.Success
            : AvifResultCode.NonConformingData;
    }
}
