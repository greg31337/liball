using System.Collections.Generic;

namespace cbor;

internal class CddlReferenceGraphBuilder
{
    internal CddlNormalizationResult Normalization;
    internal CddlCompileOptions Options;
    internal CddlReferenceGraphResult Result = new CddlReferenceGraphResult();

    internal CddlReferenceGraphBuilder(
        CddlNormalizationResult normalization,
        CddlCompileOptions options)
    {
        Normalization = normalization;
        Options = options;
    }

    internal CddlReferenceGraphResult BuildGraph()
    {
        CreateGraphNodes();
        CreateGraphEdges();
        ClassifyComponents();
        CalculateProductivity();
        CalculateEmptyMatching();
        CalculateMinimumItems();
        UpdateComponents();
        ReportNonProductiveCycles();
        return Result;
    }

    internal void CreateGraphNodes()
    {
        int index = 0;
        while (index < Normalization.Rules.Count)
        {
            CddlReferenceGraphNode node = new CddlReferenceGraphNode();
            node.Rule = Normalization.Rules[index];
            Result.Nodes.Add(node);
            index = index + 1;
        }
    }

    internal void CreateGraphEdges()
    {
        int index = 0;
        while (index < Result.Nodes.Count)
        {
            CddlReferenceGraphNode source = Result.Nodes[index];
            if (source.Rule.Value != null)
            {
                CollectReferences(source, source.Rule.Value, true);
            }
            index = index + 1;
        }
    }

    internal void CollectReferences(
        CddlReferenceGraphNode source,
        CddlNormalizedNode node,
        bool required)
    {
        if (node.ReferencedRule != null)
        {
            CddlReferenceGraphNode? target = FindGraphNode(
                node.ReferencedRule
            );
            if (target != null)
            {
                CddlReferenceGraphEdge edge = new CddlReferenceGraphEdge();
                edge.Source = source;
                edge.Target = target;
                edge.ReferenceNode = node;
                edge.IsRequired = required;
                source.Outgoing.Add(edge);
                target.Incoming.Add(edge);
                Result.Edges.Add(edge);
            }
        }
        bool childrenRequired = required;
        if (node.Kind == CddlNodeKind.GroupEntry
            && node.MinimumOccurrence == 0)
        {
            childrenRequired = false;
        }
        if ((node.Kind == CddlNodeKind.TypeChoice
                || node.Kind == CddlNodeKind.GroupChoice)
            && node.Children.Count > 1)
        {
            childrenRequired = false;
        }
        int index = 0;
        while (index < node.Children.Count)
        {
            CollectReferences(
                source,
                node.Children[index],
                childrenRequired
            );
            index = index + 1;
        }
    }

    internal CddlReferenceGraphNode? FindGraphNode(CddlNormalizedRule rule)
    {
        int index = 0;
        while (index < Result.Nodes.Count)
        {
            if (Result.Nodes[index].Rule == rule)
            {
                return Result.Nodes[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal void ClassifyComponents()
    {
        List<CddlReferenceGraphNode> finishOrder =
            CalculateFinishOrder();
        bool[] assigned = new bool[Result.Nodes.Count];
        int orderIndex = finishOrder.Count - 1;
        while (orderIndex >= 0)
        {
            CddlReferenceGraphNode start = finishOrder[orderIndex];
            int startIndex = start.Rule.Index;
            if (!assigned[startIndex])
            {
                CddlReferenceComponent component =
                    new CddlReferenceComponent();
                component.Index = Result.Components.Count;
                AssignReverseComponent(start, component, assigned);
                Result.Components.Add(component);
            }
            orderIndex = orderIndex - 1;
        }
        MarkRecursiveComponents();
    }

    internal List<CddlReferenceGraphNode> CalculateFinishOrder()
    {
        List<CddlReferenceGraphNode> order =
            new List<CddlReferenceGraphNode>();
        bool[] visited = new bool[Result.Nodes.Count];
        int startIndex = 0;
        while (startIndex < Result.Nodes.Count)
        {
            if (!visited[startIndex])
            {
                TraverseForFinish(
                    Result.Nodes[startIndex],
                    visited,
                    order
                );
            }
            startIndex = startIndex + 1;
        }
        return order;
    }

    internal void TraverseForFinish(
        CddlReferenceGraphNode start,
        bool[] visited,
        List<CddlReferenceGraphNode> order)
    {
        List<CddlReferenceGraphNode> nodes =
            new List<CddlReferenceGraphNode>();
        List<int> edgeIndices = new List<int>();
        nodes.Add(start);
        edgeIndices.Add(0);
        visited[start.Rule.Index] = true;
        while (nodes.Count > 0)
        {
            int top = nodes.Count - 1;
            CddlReferenceGraphNode current = nodes[top];
            int edgeIndex = edgeIndices[top];
            if (edgeIndex < current.Outgoing.Count)
            {
                edgeIndices[top] = edgeIndex + 1;
                CddlReferenceGraphNode target =
                    current.Outgoing[edgeIndex].Target;
                if (!visited[target.Rule.Index])
                {
                    visited[target.Rule.Index] = true;
                    nodes.Add(target);
                    edgeIndices.Add(0);
                }
            }
            else
            {
                order.Add(current);
                nodes.RemoveAt(top);
                edgeIndices.RemoveAt(top);
            }
        }
    }

    internal void AssignReverseComponent(
        CddlReferenceGraphNode start,
        CddlReferenceComponent component,
        bool[] assigned)
    {
        List<CddlReferenceGraphNode> pending =
            new List<CddlReferenceGraphNode>();
        pending.Add(start);
        assigned[start.Rule.Index] = true;
        while (pending.Count > 0)
        {
            int last = pending.Count - 1;
            CddlReferenceGraphNode current = pending[last];
            pending.RemoveAt(last);
            current.ComponentIndex = component.Index;
            component.Nodes.Add(current);
            int edgeIndex = 0;
            while (edgeIndex < current.Incoming.Count)
            {
                CddlReferenceGraphNode source =
                    current.Incoming[edgeIndex].Source;
                if (!assigned[source.Rule.Index])
                {
                    assigned[source.Rule.Index] = true;
                    pending.Add(source);
                }
                edgeIndex = edgeIndex + 1;
            }
        }
    }

    internal void MarkRecursiveComponents()
    {
        int componentIndex = 0;
        while (componentIndex < Result.Components.Count)
        {
            CddlReferenceComponent component =
                Result.Components[componentIndex];
            bool recursive = component.Nodes.Count > 1;
            if (!recursive && component.Nodes.Count == 1)
            {
                CddlReferenceGraphNode node = component.Nodes[0];
                int edgeIndex = 0;
                while (edgeIndex < node.Outgoing.Count && !recursive)
                {
                    if (node.Outgoing[edgeIndex].Target == node)
                    {
                        recursive = true;
                    }
                    edgeIndex = edgeIndex + 1;
                }
            }
            component.IsRecursive = recursive;
            int nodeIndex = 0;
            while (nodeIndex < component.Nodes.Count)
            {
                component.Nodes[nodeIndex].IsRecursive = recursive;
                nodeIndex = nodeIndex + 1;
            }
            componentIndex = componentIndex + 1;
        }
    }

    internal void CalculateProductivity()
    {
        List<CddlReferenceGraphNode> pending =
            new List<CddlReferenceGraphNode>();
        bool[] queued = new bool[Result.Nodes.Count];
        int index = 0;
        while (index < Result.Nodes.Count)
        {
            pending.Add(Result.Nodes[index]);
            queued[index] = true;
            index = index + 1;
        }
        int cursor = 0;
        while (cursor < pending.Count)
        {
            CddlReferenceGraphNode node = pending[cursor];
            cursor = cursor + 1;
            queued[node.Rule.Index] = false;
            if (!node.IsProductive
                && node.Rule.Value != null
                && IsNodeProductive(node.Rule.Value))
            {
                node.IsProductive = true;
                EnqueueIncoming(node, pending, queued);
            }
        }
    }

    internal bool IsNodeProductive(CddlNormalizedNode node)
    {
        if (node.ReferencedRule != null)
        {
            CddlReferenceGraphNode? target = FindGraphNode(node.ReferencedRule);
            return target != null && target.IsProductive;
        }
        if (node.IsEmptySocket)
        {
            return node.ReferenceKind == CddlRuleKind.Group;
        }
        switch (node.Kind)
        {
            case CddlNodeKind.Literal:
                return true;
            case CddlNodeKind.Representation:
                if (node.Children.Count == 0)
                {
                    return true;
                }
                return AreAllChildrenProductive(node.Children);
            case CddlNodeKind.TypeChoice:
            case CddlNodeKind.GroupChoice:
                return IsAnyChildProductive(node.Children);
            case CddlNodeKind.Group:
                return AreAllChildrenProductive(node.Children);
            case CddlNodeKind.GroupEntry:
                if (node.MinimumOccurrence == 0)
                {
                    return true;
                }
                return AreAllChildrenProductive(node.Children);
            case CddlNodeKind.Array:
            case CddlNodeKind.Map:
            case CddlNodeKind.Control:
            case CddlNodeKind.Range:
            case CddlNodeKind.Tag:
                return node.Children.Count > 0
                    && AreAllChildrenProductive(node.Children);
            default:
                return false;
        }
    }

    internal bool AreAllChildrenProductive(
        List<CddlNormalizedNode> children)
    {
        int index = 0;
        while (index < children.Count)
        {
            if (!IsNodeProductive(children[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal bool IsAnyChildProductive(
        List<CddlNormalizedNode> children)
    {
        int index = 0;
        while (index < children.Count)
        {
            if (IsNodeProductive(children[index]))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal void CalculateEmptyMatching()
    {
        List<CddlReferenceGraphNode> pending =
            new List<CddlReferenceGraphNode>();
        bool[] queued = new bool[Result.Nodes.Count];
        int index = 0;
        while (index < Result.Nodes.Count)
        {
            pending.Add(Result.Nodes[index]);
            queued[index] = true;
            index = index + 1;
        }
        int cursor = 0;
        while (cursor < pending.Count)
        {
            CddlReferenceGraphNode node = pending[cursor];
            cursor = cursor + 1;
            queued[node.Rule.Index] = false;
            if (!node.CanMatchEmpty
                && node.Rule.Value != null
                && CanNodeMatchEmpty(node.Rule.Value))
            {
                node.CanMatchEmpty = true;
                EnqueueIncoming(node, pending, queued);
            }
        }
    }

    internal bool CanNodeMatchEmpty(CddlNormalizedNode node)
    {
        if (node.ReferencedRule != null)
        {
            if (node.ReferenceKind != CddlRuleKind.Group)
            {
                return false;
            }
            CddlReferenceGraphNode? target = FindGraphNode(node.ReferencedRule);
            return target != null && target.CanMatchEmpty;
        }
        if (node.IsEmptySocket)
        {
            return node.ReferenceKind == CddlRuleKind.Group;
        }
        switch (node.Kind)
        {
            case CddlNodeKind.Group:
                return CanAllChildrenMatchEmpty(node.Children);
            case CddlNodeKind.GroupChoice:
                return CanAnyChildMatchEmpty(node.Children);
            case CddlNodeKind.GroupEntry:
                if (node.MinimumOccurrence == 0)
                {
                    return true;
                }
                if (node.Children.Count == 1
                    && NodeProducesGroup(node.Children[0]))
                {
                    return CanNodeMatchEmpty(node.Children[0]);
                }
                return false;
            default:
                return false;
        }
    }

    internal bool CanAllChildrenMatchEmpty(
        List<CddlNormalizedNode> children)
    {
        int index = 0;
        while (index < children.Count)
        {
            if (!CanNodeMatchEmpty(children[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal bool CanAnyChildMatchEmpty(
        List<CddlNormalizedNode> children)
    {
        int index = 0;
        while (index < children.Count)
        {
            if (CanNodeMatchEmpty(children[index]))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal bool NodeProducesGroup(CddlNormalizedNode node)
    {
        if (node.ReferenceKind == CddlRuleKind.Group)
        {
            return true;
        }
        return node.Kind == CddlNodeKind.Group
            || node.Kind == CddlNodeKind.GroupChoice;
    }

    internal void CalculateMinimumItems()
    {
        List<CddlReferenceGraphNode> pending =
            new List<CddlReferenceGraphNode>();
        bool[] queued = new bool[Result.Nodes.Count];
        int index = 0;
        while (index < Result.Nodes.Count)
        {
            CddlReferenceGraphNode node = Result.Nodes[index];
            if (node.IsProductive)
            {
                if (node.Rule.Kind == CddlRuleKind.Type)
                {
                    node.MinimumItems = 1;
                }
                else
                {
                    pending.Add(node);
                    queued[index] = true;
                }
            }
            index = index + 1;
        }
        int cursor = 0;
        while (cursor < pending.Count)
        {
            CddlReferenceGraphNode node = pending[cursor];
            cursor = cursor + 1;
            queued[node.Rule.Index] = false;
            if (node.Rule.Value != null)
            {
                int minimum = CalculateNodeMinimumItems(node.Rule.Value);
                if (minimum >= 0 && (node.MinimumItems < 0
                        || minimum < node.MinimumItems))
                {
                    node.MinimumItems = minimum;
                    EnqueueIncoming(node, pending, queued);
                }
            }
        }
    }

    internal int CalculateNodeMinimumItems(CddlNormalizedNode node)
    {
        if (!IsNodeProductive(node))
        {
            return -1;
        }
        if (node.ReferencedRule != null)
        {
            if (node.ReferenceKind != CddlRuleKind.Group)
            {
                return 1;
            }
            CddlReferenceGraphNode? target = FindGraphNode(node.ReferencedRule);
            if (target == null)
            {
                return -1;
            }
            return target.MinimumItems;
        }
        if (node.IsEmptySocket)
        {
            return node.ReferenceKind == CddlRuleKind.Group ? 0 : -1;
        }
        switch (node.Kind)
        {
            case CddlNodeKind.Group:
                return SumChildMinimumItems(node.Children);
            case CddlNodeKind.GroupChoice:
                return SelectChildMinimumItems(node.Children);
            case CddlNodeKind.GroupEntry:
                return CalculateEntryMinimumItems(node);
            default:
                return 1;
        }
    }

    internal int CalculateEntryMinimumItems(CddlNormalizedNode node)
    {
        if (node.MinimumOccurrence == 0)
        {
            return 0;
        }
        int perOccurrence = 1;
        if (node.Children.Count == 1
            && NodeProducesGroup(node.Children[0]))
        {
            perOccurrence = CalculateNodeMinimumItems(node.Children[0]);
            if (perOccurrence < 0)
            {
                return -1;
            }
        }
        if (perOccurrence > 0
            && node.MinimumOccurrence > int.MaxValue / perOccurrence)
        {
            return int.MaxValue;
        }
        return perOccurrence * node.MinimumOccurrence;
    }

    internal int SumChildMinimumItems(
        List<CddlNormalizedNode> children)
    {
        int total = 0;
        int index = 0;
        while (index < children.Count)
        {
            int value = CalculateNodeMinimumItems(children[index]);
            if (value < 0)
            {
                return -1;
            }
            if (value > int.MaxValue - total)
            {
                return int.MaxValue;
            }
            total = total + value;
            index = index + 1;
        }
        return total;
    }

    internal int SelectChildMinimumItems(
        List<CddlNormalizedNode> children)
    {
        int minimum = -1;
        int index = 0;
        while (index < children.Count)
        {
            int value = CalculateNodeMinimumItems(children[index]);
            if (value >= 0 && (minimum < 0 || value < minimum))
            {
                minimum = value;
            }
            index = index + 1;
        }
        return minimum;
    }

    internal void EnqueueIncoming(
        CddlReferenceGraphNode target,
        List<CddlReferenceGraphNode> pending,
        bool[] queued)
    {
        int index = 0;
        while (index < target.Incoming.Count)
        {
            CddlReferenceGraphNode source = target.Incoming[index].Source;
            if (!queued[source.Rule.Index])
            {
                queued[source.Rule.Index] = true;
                pending.Add(source);
            }
            index = index + 1;
        }
    }

    internal void UpdateComponents()
    {
        int componentIndex = 0;
        while (componentIndex < Result.Components.Count)
        {
            CddlReferenceComponent component =
                Result.Components[componentIndex];
            int nodeIndex = 0;
            while (nodeIndex < component.Nodes.Count)
            {
                if (component.Nodes[nodeIndex].IsProductive)
                {
                    component.IsProductive = true;
                }
                nodeIndex = nodeIndex + 1;
            }
            componentIndex = componentIndex + 1;
        }
    }

    internal void ReportNonProductiveCycles()
    {
        bool[] reported = new bool[Result.Components.Count];
        int index = 0;
        while (index < Result.Nodes.Count)
        {
            CddlReferenceGraphNode node = Result.Nodes[index];
            CddlReferenceComponent component =
                Result.Components[node.ComponentIndex];
            if (component.IsRecursive
                && !component.IsProductive
                && !reported[component.Index])
            {
                reported[component.Index] = true;
                ReportCycleDiagnostic(node);
            }
            index = index + 1;
        }
    }

    internal void ReportCycleDiagnostic(CddlReferenceGraphNode node)
    {
        Result.HasErrors = true;
        if (Options.MaxDiagnostics <= 0
            || Result.Diagnostics.Count >= Options.MaxDiagnostics)
        {
            return;
        }
        CddlDiagnostic diagnostic = new CddlDiagnostic();
        diagnostic.Severity = CddlDiagnosticSeverity.Error;
        diagnostic.Code = "CDDL_COMPILE_NON_PRODUCTIVE_CYCLE";
        diagnostic.Message =
            "The recursive rule component has no finite derivation.";
        diagnostic.Span = node.Rule.SourceRule.Span;
        diagnostic.RuleName = node.Rule.Name;
        Result.Diagnostics.Add(diagnostic);
    }
}
