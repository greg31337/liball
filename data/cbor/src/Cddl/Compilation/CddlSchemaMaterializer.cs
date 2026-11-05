using System.Collections.Generic;

namespace cbor;

internal class CddlSchemaMaterializer
{
    internal CddlDocument Document;
    internal CddlNormalizationResult Normalization;
    internal CddlCompileOptions Options;

    internal CddlSchemaMaterializer(
        CddlDocument document,
        CddlNormalizationResult normalization,
        CddlCompileOptions options)
    {
        Document = document;
        Normalization = normalization;
        Options = options;
    }

    internal CddlSchema MaterializeSchema()
    {
        CddlSchema schema = new CddlSchema();
        schema.Document = CloneDocument(Document);
        CddlNode[] nodes = CreateNodeShells();
        ConnectNodeChildren(nodes);
        schema.NormalizedNodes = nodes;
        schema.SourceMap = CreateSourceMap();
        schema.Rules = CreateRules(nodes);
        if (Normalization.RootRule != null)
        {
            schema.RootRule = schema.Rules[Normalization.RootRule.Index];
        }
        schema.ControlRegistrations = CloneControlRegistrations();
        schema.Features = FindFeatures();
        return schema;
    }

    internal CddlNode[] CreateNodeShells()
    {
        CddlNode[] nodes = new CddlNode[Normalization.Nodes.Count];
        int index = 0;
        while (index < Normalization.Nodes.Count)
        {
            nodes[index] = CloneNormalizedNodeFields(
                Normalization.Nodes[index]
            );
            index = index + 1;
        }
        return nodes;
    }

    internal void ConnectNodeChildren(CddlNode[] nodes)
    {
        int nodeIndex = 0;
        while (nodeIndex < Normalization.Nodes.Count)
        {
            CddlNormalizedNode source = Normalization.Nodes[nodeIndex];
            CddlNode destination = nodes[nodeIndex];
            int childIndex = 0;
            while (childIndex < source.Children.Count)
            {
                destination.Children.Add(nodes[
                    source.Children[childIndex].Index
                ]);
                childIndex = childIndex + 1;
            }
            nodeIndex = nodeIndex + 1;
        }
    }

    internal CddlSourceSpan[] CreateSourceMap()
    {
        CddlSourceSpan[] sourceMap =
            new CddlSourceSpan[Normalization.SourceMap.Count];
        int index = 0;
        while (index < Normalization.SourceMap.Count)
        {
            sourceMap[index] = CloneSpan(Normalization.SourceMap[index]);
            index = index + 1;
        }
        return sourceMap;
    }

    internal CddlRule[] CreateRules(CddlNode[] nodes)
    {
        CddlRule[] rules = new CddlRule[Normalization.Rules.Count];
        int index = 0;
        while (index < Normalization.Rules.Count)
        {
            CddlNormalizedRule source = Normalization.Rules[index];
            CddlRule rule = CloneRuleFields(source.SourceRule);
            rule.Name = source.Name;
            rule.Kind = source.Kind;
            rule.GenericParameters.Clear();
            if (source.Value != null)
            {
                rule.Value = nodes[source.Value.Index];
            }
            else
            {
                rule.Value = null;
            }
            rules[index] = rule;
            index = index + 1;
        }
        return rules;
    }

    internal CddlControlRegistration[] CloneControlRegistrations()
    {
        CddlControlRegistration[] registrations =
            new CddlControlRegistration[Options.ControlRegistrations.Count];
        int index = 0;
        while (index < Options.ControlRegistrations.Count)
        {
            CddlControlRegistration source =
                Options.ControlRegistrations[index];
            CddlControlRegistration registration =
                new CddlControlRegistration();
            registration.Name = source.Name;
            registration.Handler = source.Handler;
            registration.IsEnabled = source.IsEnabled;
            registrations[index] = registration;
            index = index + 1;
        }
        return registrations;
    }

    internal string[] FindFeatures()
    {
        List<string> features = new List<string>();
        int index = 0;
        while (index < Normalization.Nodes.Count)
        {
            CddlNormalizedNode node = Normalization.Nodes[index];
            if (node.Kind == CddlNodeKind.Control
                && node.ControlName == CddlControlNames.Feature
                && node.Children.Count == 2)
            {
                string? feature = FindFeatureName(
                    node.Children[1],
                    new List<CddlNormalizedRule>()
                );
                if (feature != null && !ContainsString(features, feature))
                {
                    features.Add(feature);
                }
            }
            index = index + 1;
        }
        return features.ToArray();
    }

    internal string? FindFeatureName(
        CddlNormalizedNode node,
        List<CddlNormalizedRule> visited)
    {
        string? text = FindTextSingleton(node, visited);
        if (text != null)
        {
            return text;
        }
        if (node.ReferencedRule != null)
        {
            if (node.ReferencedRule.Value == null
                || ContainsRule(visited, node.ReferencedRule))
            {
                return null;
            }
            visited.Add(node.ReferencedRule);
            string? referenced = FindFeatureName(
                node.ReferencedRule.Value,
                visited
            );
            visited.RemoveAt(visited.Count - 1);
            return referenced;
        }
        if ((node.Kind == CddlNodeKind.ParenthesizedType
                || node.Kind == CddlNodeKind.TypeChoice)
            && node.Children.Count == 1)
        {
            return FindFeatureName(node.Children[0], visited);
        }
        if (node.Kind != CddlNodeKind.Array || node.Children.Count != 1)
        {
            return null;
        }
        CddlNormalizedNode? group = FindFeatureArrayGroup(
            node.Children[0]
        );
        if (group == null || group.Children.Count != 2)
        {
            return null;
        }
        CddlNormalizedNode entry = group.Children[0];
        if (entry.Kind != CddlNodeKind.GroupEntry
            || entry.Children.Count == 0)
        {
            return null;
        }
        return FindTextSingleton(
            entry.Children[entry.Children.Count - 1],
            visited
        );
    }

    internal CddlNormalizedNode? FindFeatureArrayGroup(
        CddlNormalizedNode node)
    {
        CddlNormalizedNode current = node;
        while ((current.Kind == CddlNodeKind.ParenthesizedGroup
                || current.Kind == CddlNodeKind.GroupChoice)
            && current.Children.Count == 1)
        {
            current = current.Children[0];
        }
        return current.Kind == CddlNodeKind.Group ? current : null;
    }

    internal string? FindTextSingleton(
        CddlNormalizedNode node,
        List<CddlNormalizedRule> visited)
    {
        if (node.Kind == CddlNodeKind.Literal
            && node.LiteralKind == CddlLiteralKind.TextString)
        {
            return node.TextValue;
        }
        if (node.ReferencedRule != null
            && node.ReferencedRule.Value != null
            && !ContainsRule(visited, node.ReferencedRule))
        {
            visited.Add(node.ReferencedRule);
            string? referenced = FindTextSingleton(
                node.ReferencedRule.Value,
                visited
            );
            visited.RemoveAt(visited.Count - 1);
            return referenced;
        }
        if (node.Kind == CddlNodeKind.TypeChoice
            && node.Children.Count == 1)
        {
            return FindTextSingleton(node.Children[0], visited);
        }
        return null;
    }

    internal bool ContainsString(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal bool ContainsRule(
        List<CddlNormalizedRule> rules,
        CddlNormalizedRule rule)
    {
        int index = 0;
        while (index < rules.Count)
        {
            if (rules[index] == rule)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal CddlDocument CloneDocument(CddlDocument source)
    {
        CddlDocument document = new CddlDocument();
        document.SourceName = source.SourceName;
        document.SourceText = source.SourceText;
        document.RootRuleName = source.RootRuleName;
        int index = 0;
        while (index < source.Rules.Count)
        {
            CddlRule rule = CloneRuleFields(source.Rules[index]);
            CddlNode? value = source.Rules[index].Value;
            if (value != null)
            {
                rule.Value = CloneSourceNode(value);
            }
            document.Rules.Add(rule);
            index = index + 1;
        }
        index = 0;
        while (index < source.Trivia.Count)
        {
            document.Trivia.Add(source.Trivia[index]);
            index = index + 1;
        }
        index = 0;
        while (index < source.TriviaSpans.Count)
        {
            document.TriviaSpans.Add(CloneSpan(source.TriviaSpans[index]));
            index = index + 1;
        }
        index = 0;
        while (index < source.Diagnostics.Count)
        {
            document.Diagnostics.Add(CloneDiagnostic(source.Diagnostics[index]));
            index = index + 1;
        }
        return document;
    }

    internal CddlRule CloneRuleFields(CddlRule source)
    {
        CddlRule rule = new CddlRule();
        rule.Name = source.Name;
        rule.Kind = source.Kind;
        rule.IsPrelude = source.IsPrelude;
        rule.SourceOrder = source.SourceOrder;
        rule.Span = CloneSpan(source.Span);
        int index = 0;
        while (index < source.GenericParameters.Count)
        {
            rule.GenericParameters.Add(source.GenericParameters[index]);
            index = index + 1;
        }
        return rule;
    }

    internal CddlNode CloneSourceNode(CddlNode source)
    {
        CddlNode node = CloneSourceNodeFields(source);
        int index = 0;
        while (index < source.Children.Count)
        {
            node.Children.Add(CloneSourceNode(source.Children[index]));
            index = index + 1;
        }
        return node;
    }

    internal CddlNode CloneSourceNodeFields(CddlNode source)
    {
        CddlNode node = new CddlNode();
        node.Kind = source.Kind;
        node.LiteralKind = source.LiteralKind;
        node.Occurrence = source.Occurrence;
        node.Name = source.Name;
        node.TextValue = source.TextValue;
        if (source.ByteStringValue != null)
        {
            node.ByteStringValue = (byte[])source.ByteStringValue.Clone();
        }
        node.IntegerValue = source.IntegerValue;
        node.HasIntegerValue = source.HasIntegerValue;
        node.FloatValue = source.FloatValue;
        node.HasFloatValue = source.HasFloatValue;
        node.ControlName = source.ControlName;
        node.MinimumOccurrence = source.MinimumOccurrence;
        node.MaximumOccurrence = source.MaximumOccurrence;
        node.MajorType = source.MajorType;
        node.ArgumentValue = source.ArgumentValue;
        node.HasArgumentValue = source.HasArgumentValue;
        node.IsRangeUpperExclusive = source.IsRangeUpperExclusive;
        node.IsCut = source.IsCut;
        node.Span = CloneSpan(source.Span);
        return node;
    }

    internal CddlNode CloneNormalizedNodeFields(CddlNormalizedNode source)
    {
        CddlNode node = new CddlNode();
        node.Kind = source.Kind;
        node.LiteralKind = source.LiteralKind;
        node.Occurrence = source.Occurrence;
        node.Name = source.Name;
        node.TextValue = source.TextValue;
        if (source.ByteStringValue != null)
        {
            node.ByteStringValue = (byte[])source.ByteStringValue.Clone();
        }
        node.IntegerValue = source.IntegerValue;
        node.HasIntegerValue = source.HasIntegerValue;
        node.FloatValue = source.FloatValue;
        node.HasFloatValue = source.HasFloatValue;
        node.ControlName = source.ControlName;
        node.MinimumOccurrence = source.MinimumOccurrence;
        node.MaximumOccurrence = source.MaximumOccurrence;
        node.MajorType = source.MajorType;
        node.ArgumentValue = source.ArgumentValue;
        node.HasArgumentValue = source.HasArgumentValue;
        node.IsRangeUpperExclusive = source.IsRangeUpperExclusive;
        node.IsCut = source.IsCut;
        if (source.ReferencedRule != null)
        {
            node.ReferencedRuleIndex = source.ReferencedRule.Index;
        }
        node.Span = CloneSpan(source.Span);
        return node;
    }

    internal CddlDiagnostic CloneDiagnostic(CddlDiagnostic source)
    {
        CddlDiagnostic diagnostic = new CddlDiagnostic();
        diagnostic.Severity = source.Severity;
        diagnostic.Code = source.Code;
        diagnostic.Message = source.Message;
        diagnostic.RuleName = source.RuleName;
        diagnostic.Span = CloneSpan(source.Span);
        return diagnostic;
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
