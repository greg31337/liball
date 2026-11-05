using System.Collections.Generic;

namespace cbor;

internal class CddlSchemaEmissionState
{
    internal CddlSchema Schema;
    internal ICddlSchemaSink Sink;
    internal CddlEmissionOptions Options;
    internal bool[] IncludedRules;
    internal Dictionary<CddlNode, int> NodeIndices =
        new Dictionary<CddlNode, int>();
    internal HashSet<CddlNode> EmittedNodes = new HashSet<CddlNode>();

    internal CddlSchemaEmissionState(
        CddlSchema schema,
        ICddlSchemaSink sink,
        CddlEmissionOptions options)
    {
        Schema = schema;
        Sink = sink;
        Options = CloneOptions(options);
        IncludedRules = new bool[schema.Rules.Length];
    }

    internal void EmitTraversal()
    {
        InitializeNodeIndices();
        InitializeRuleFilter();
        InvokeBeginCallback(CreateContext(null, null, -1, -1, 0));
        int index = 0;
        while (index < Schema.Rules.Length)
        {
            CddlRule rule = Schema.Rules[index];
            if (IncludedRules[index]
                && (Options.IncludePreludeRules || !rule.IsPrelude))
            {
                InvokeRuleCallback(CreateContext(
                    rule,
                    null,
                    index,
                    -1,
                    0
                ));
                if (Options.IncludeNormalizedNodes && rule.Value != null)
                {
                    EmitRuleNodes(rule, index);
                }
            }
            index = index + 1;
        }
        InvokeEndCallback(CreateContext(null, null, -1, -1, 0));
    }

    internal void InitializeNodeIndices()
    {
        int index = 0;
        while (index < Schema.NormalizedNodes.Length)
        {
            CddlNode node = Schema.NormalizedNodes[index];
            if (!NodeIndices.ContainsKey(node))
            {
                NodeIndices.Add(node, index);
            }
            index = index + 1;
        }
    }

    internal void InitializeRuleFilter()
    {
        if (Options.RootRuleName == null)
        {
            int index = 0;
            while (index < IncludedRules.Length)
            {
                IncludedRules[index] = true;
                index = index + 1;
            }
            return;
        }
        int rootIndex = FindRuleIndex(Options.RootRuleName);
        if (rootIndex < 0)
        {
            throw new System.ArgumentException(
                "The selected CDDL emission root rule does not exist."
            );
        }
        List<int> pending = new List<int>();
        IncludedRules[rootIndex] = true;
        pending.Add(rootIndex);
        int pendingIndex = 0;
        while (pendingIndex < pending.Count)
        {
            CddlRule rule = Schema.Rules[pending[pendingIndex]];
            if (rule.Value != null)
            {
                AddReferencedRules(rule.Value, pending);
            }
            pendingIndex = pendingIndex + 1;
        }
    }

    internal void AddReferencedRules(
        CddlNode root,
        List<int> pending)
    {
        List<CddlNode> nodes = new List<CddlNode>();
        HashSet<CddlNode> visited = new HashSet<CddlNode>();
        nodes.Add(root);
        while (nodes.Count > 0)
        {
            int last = nodes.Count - 1;
            CddlNode node = nodes[last];
            nodes.RemoveAt(last);
            if (!visited.Add(node))
            {
                continue;
            }
            if (node.Kind == CddlNodeKind.RuleReference)
            {
                int ruleIndex = ResolveReferencedRuleIndex(node);
                if (ruleIndex >= 0 && !IncludedRules[ruleIndex])
                {
                    IncludedRules[ruleIndex] = true;
                    pending.Add(ruleIndex);
                }
            }
            int childIndex = node.Children.Count - 1;
            while (childIndex >= 0)
            {
                nodes.Add(node.Children[childIndex]);
                childIndex = childIndex - 1;
            }
        }
    }

    internal int ResolveReferencedRuleIndex(CddlNode node)
    {
        if (node.ReferencedRuleIndex >= 0
            && node.ReferencedRuleIndex < Schema.Rules.Length)
        {
            return node.ReferencedRuleIndex;
        }
        if (node.Name != null)
        {
            return FindRuleIndex(node.Name);
        }
        return -1;
    }

    internal int FindRuleIndex(string name)
    {
        int index = 0;
        while (index < Schema.Rules.Length)
        {
            if (Schema.Rules[index].Name == name)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal void EmitRuleNodes(CddlRule rule, int ruleIndex)
    {
        if (rule.Value == null)
        {
            return;
        }
        List<CddlEmissionNodeFrame> pending =
            new List<CddlEmissionNodeFrame>();
        pending.Add(new CddlEmissionNodeFrame(rule.Value, 0));
        while (pending.Count > 0)
        {
            int last = pending.Count - 1;
            CddlEmissionNodeFrame frame = pending[last];
            pending.RemoveAt(last);
            if (!EmittedNodes.Add(frame.Node))
            {
                continue;
            }
            int nodeIndex = FindNodeIndex(frame.Node);
            InvokeNodeCallback(CreateContext(
                rule,
                frame.Node,
                ruleIndex,
                nodeIndex,
                frame.Depth
            ));
            int childIndex = frame.Node.Children.Count - 1;
            while (childIndex >= 0)
            {
                pending.Add(new CddlEmissionNodeFrame(
                    frame.Node.Children[childIndex],
                    frame.Depth + 1
                ));
                childIndex = childIndex - 1;
            }
        }
    }

    internal int FindNodeIndex(CddlNode node)
    {
        if (NodeIndices.ContainsKey(node))
        {
            return NodeIndices[node];
        }
        return -1;
    }

    internal void InvokeBeginCallback(CddlEmissionContext context)
    {
        using (CddlSchemaSinkScope scope = new CddlSchemaSinkScope(
            "begin schema",
            context
        ))
        {
            Sink.BeginSchema(context);
            scope.CompleteCallback();
        }
    }

    internal void InvokeRuleCallback(CddlEmissionContext context)
    {
        using (CddlSchemaSinkScope scope = new CddlSchemaSinkScope(
            "rule emission",
            context
        ))
        {
            Sink.AcceptRule(context);
            scope.CompleteCallback();
        }
    }

    internal void InvokeNodeCallback(CddlEmissionContext context)
    {
        using (CddlSchemaSinkScope scope = new CddlSchemaSinkScope(
            "node emission",
            context
        ))
        {
            Sink.AcceptNode(context);
            scope.CompleteCallback();
        }
    }

    internal void InvokeEndCallback(CddlEmissionContext context)
    {
        using (CddlSchemaSinkScope scope = new CddlSchemaSinkScope(
            "end schema",
            context
        ))
        {
            Sink.EndSchema(context);
            scope.CompleteCallback();
        }
    }

    internal CddlEmissionContext CreateContext(
        CddlRule? rule,
        CddlNode? node,
        int ruleIndex,
        int nodeIndex,
        int depth)
    {
        CddlEmissionContext context = new CddlEmissionContext();
        context.Schema = Schema;
        context.Rule = rule;
        context.Node = node;
        context.Options = CloneOptions(Options);
        context.RuleIndex = ruleIndex;
        context.NodeIndex = nodeIndex;
        context.Depth = depth;
        context.OutputName = CreateOutputName(rule, node);
        context.Span = CreateEventSpan(rule, node, nodeIndex);
        return context;
    }

    internal string? CreateOutputName(CddlRule? rule, CddlNode? node)
    {
        string prefix = Options.NamePrefix ?? "";
        if (node != null && node.Name != null)
        {
            return prefix + node.Name;
        }
        if (rule != null)
        {
            return prefix + rule.Name;
        }
        return null;
    }

    internal CddlSourceSpan CreateEventSpan(
        CddlRule? rule,
        CddlNode? node,
        int nodeIndex)
    {
        if (!Options.IncludeSourceContext)
        {
            return new CddlSourceSpan();
        }
        if (node != null)
        {
            if (nodeIndex >= 0 && nodeIndex < Schema.SourceMap.Length)
            {
                return CloneSpan(Schema.SourceMap[nodeIndex]);
            }
            return CloneSpan(node.Span);
        }
        if (rule != null)
        {
            return CloneSpan(rule.Span);
        }
        return new CddlSourceSpan();
    }

    internal CddlEmissionOptions CloneOptions(CddlEmissionOptions source)
    {
        CddlEmissionOptions options = new CddlEmissionOptions();
        options.IncludePreludeRules = source.IncludePreludeRules;
        options.IncludeNormalizedNodes = source.IncludeNormalizedNodes;
        options.IncludeSourceContext = source.IncludeSourceContext;
        options.RootRuleName = source.RootRuleName;
        options.NamePrefix = source.NamePrefix ?? "";
        return options;
    }

    internal CddlSourceSpan CloneSpan(CddlSourceSpan source)
    {
        CddlSourceSpan span = new CddlSourceSpan();
        span.SourceName = source.SourceName;
        span.Start = source.Start;
        span.Length = source.Length;
        span.StartLine = source.StartLine;
        span.StartColumn = source.StartColumn;
        span.EndLine = source.EndLine;
        span.EndColumn = source.EndColumn;
        return span;
    }
}
