using System.Collections.Generic;

namespace cbor;

internal class CddlSchemaNormalizer
{
    internal CddlRuleTable Table;
    internal CddlResolutionResult Resolution;
    internal CddlGenericExpansionResult Expansion;
    internal CddlCompileOptions Options;
    internal CddlNormalizationResult Result = new CddlNormalizationResult();
    internal bool ReportedNodeLimit;

    internal CddlSchemaNormalizer(
        CddlRuleTable table,
        CddlResolutionResult resolution,
        CddlGenericExpansionResult expansion,
        CddlCompileOptions options)
    {
        Table = table;
        Resolution = resolution;
        Expansion = expansion;
        Options = options;
    }

    internal CddlNormalizationResult NormalizeDocument()
    {
        CreateRuleShells();
        int index = 0;
        while (index < Result.Rules.Count)
        {
            NormalizeRule(Result.Rules[index]);
            index = index + 1;
        }
        if (Resolution.RootRule != null)
        {
            Result.RootRule = FindNormalizedRule(Resolution.RootRule);
        }
        return Result;
    }

    internal void CreateRuleShells()
    {
        int index = 0;
        while (index < Table.Entries.Count)
        {
            CddlRule? rule = Table.Entries[index].MergedRule;
            if (rule != null && rule.GenericParameters.Count == 0)
            {
                AddRuleShell(rule, false);
            }
            index = index + 1;
        }
        index = 0;
        while (index < Resolution.ExternalRules.Count)
        {
            CddlRule rule = Resolution.ExternalRules[index];
            if (rule.GenericParameters.Count == 0)
            {
                AddRuleShell(rule, false);
            }
            index = index + 1;
        }
        index = 0;
        while (index < Expansion.Instantiations.Count)
        {
            AddRuleShell(
                Expansion.Instantiations[index].InstantiatedRule,
                true
            );
            index = index + 1;
        }
    }

    internal void AddRuleShell(CddlRule rule, bool genericInstantiation)
    {
        if (FindNormalizedRule(rule) != null)
        {
            return;
        }
        CddlNormalizedRule normalized = new CddlNormalizedRule();
        normalized.Index = Result.Rules.Count;
        normalized.Name = rule.Name;
        normalized.Kind = rule.Kind;
        normalized.SourceRule = rule;
        normalized.IsGenericInstantiation = genericInstantiation;
        Result.Rules.Add(normalized);
    }

    internal void NormalizeRule(CddlNormalizedRule rule)
    {
        if (rule.SourceRule.Value != null)
        {
            rule.Value = NormalizeNode(
                rule.SourceRule.Value,
                rule.SourceRule.Name
            );
        }
    }

    internal CddlNormalizedNode? NormalizeNode(
        CddlNode source,
        string containingRule)
    {
        if ((source.Kind == CddlNodeKind.ParenthesizedType
                || source.Kind == CddlNodeKind.ParenthesizedGroup)
            && source.Children.Count == 1)
        {
            return NormalizeNode(source.Children[0], containingRule);
        }
        CddlNormalizedNode? normalized = ReserveNode(source, containingRule);
        if (normalized == null)
        {
            return null;
        }
        CopyFields(source, normalized);
        ResolveReference(source, normalized);
        if (source.Kind == CddlNodeKind.TypeChoice
            || source.Kind == CddlNodeKind.GroupChoice)
        {
            AppendChoiceChildren(
                source,
                normalized,
                containingRule,
                source.Kind
            );
            return normalized;
        }
        if (normalized.ReferencedRule != null
            && FindInstantiation(source) != null)
        {
            return normalized;
        }
        int index = 0;
        while (index < source.Children.Count)
        {
            CddlNormalizedNode? child = NormalizeNode(
                source.Children[index],
                containingRule
            );
            if (child != null)
            {
                normalized.Children.Add(child);
            }
            index = index + 1;
        }
        return normalized;
    }

    internal void AppendChoiceChildren(
        CddlNode source,
        CddlNormalizedNode destination,
        string containingRule,
        CddlNodeKind choiceKind)
    {
        int index = 0;
        while (index < source.Children.Count)
        {
            CddlNode child = UnwrapChoiceParentheses(
                source.Children[index],
                choiceKind
            );
            if (child.Kind == choiceKind)
            {
                AppendChoiceChildren(
                    child,
                    destination,
                    containingRule,
                    choiceKind
                );
            }
            else
            {
                CddlNormalizedNode? normalized = NormalizeNode(
                    child,
                    containingRule
                );
                if (normalized != null)
                {
                    destination.Children.Add(normalized);
                }
            }
            index = index + 1;
        }
    }

    internal CddlNode UnwrapChoiceParentheses(
        CddlNode source,
        CddlNodeKind choiceKind)
    {
        CddlNode current = source;
        bool unwrap = true;
        while (unwrap && current.Children.Count == 1)
        {
            unwrap = false;
            if (choiceKind == CddlNodeKind.TypeChoice
                && current.Kind == CddlNodeKind.ParenthesizedType)
            {
                current = current.Children[0];
                unwrap = true;
            }
            else if (choiceKind == CddlNodeKind.GroupChoice
                && current.Kind == CddlNodeKind.ParenthesizedGroup)
            {
                current = current.Children[0];
                unwrap = true;
            }
        }
        return current;
    }

    internal CddlNormalizedNode? ReserveNode(
        CddlNode source,
        string containingRule)
    {
        if (Options.MaxNodes < 0 || Result.Nodes.Count >= Options.MaxNodes)
        {
            ReportNodeLimit(source, containingRule);
            return null;
        }
        CddlNormalizedNode normalized = new CddlNormalizedNode();
        normalized.Index = Result.Nodes.Count;
        normalized.SourceNode = source;
        normalized.Span = CloneSpan(source.Span);
        Result.Nodes.Add(normalized);
        Result.SourceMap.Add(normalized.Span);
        return normalized;
    }

    internal void CopyFields(
        CddlNode source,
        CddlNormalizedNode destination)
    {
        destination.Kind = source.Kind;
        destination.LiteralKind = source.LiteralKind;
        destination.Occurrence = source.Occurrence;
        destination.Name = source.Name;
        destination.TextValue = source.TextValue;
        if (source.ByteStringValue != null)
        {
            destination.ByteStringValue =
                (byte[])source.ByteStringValue.Clone();
        }
        destination.IntegerValue = source.IntegerValue;
        destination.HasIntegerValue = source.HasIntegerValue;
        destination.FloatValue = source.FloatValue;
        destination.HasFloatValue = source.HasFloatValue;
        destination.ControlName = source.ControlName;
        destination.MinimumOccurrence = source.MinimumOccurrence;
        destination.MaximumOccurrence = source.MaximumOccurrence;
        destination.MajorType = source.MajorType;
        destination.ArgumentValue = source.ArgumentValue;
        destination.HasArgumentValue = source.HasArgumentValue;
        destination.IsRangeUpperExclusive = source.IsRangeUpperExclusive;
        destination.IsCut = source.IsCut;
    }

    internal void ResolveReference(
        CddlNode source,
        CddlNormalizedNode destination)
    {
        CddlGenericInstantiation? instantiation = FindInstantiation(source);
        if (instantiation != null)
        {
            destination.ReferencedRule = FindNormalizedRule(
                instantiation.InstantiatedRule
            );
            destination.ReferenceKind = instantiation.InstantiatedRule.Kind;
            destination.Name = instantiation.InstantiatedRule.Name;
            return;
        }
        CddlResolvedReference? reference = FindResolvedReference(source);
        if (reference == null)
        {
            reference = FindEquivalentResolvedReference(source);
        }
        if (reference != null)
        {
            destination.ReferenceKind = reference.Kind;
            destination.IsEmptySocket = reference.IsEmptySocket;
            if (reference.Rule != null)
            {
                destination.ReferencedRule = FindNormalizedRule(reference.Rule);
            }
            return;
        }
        if (source.Name == null)
        {
            return;
        }
        CddlRule? target = FindNamedTarget(source.Name);
        if (target != null)
        {
            destination.ReferencedRule = FindNormalizedRule(target);
            destination.ReferenceKind = target.Kind;
        }
        else if (source.Name.Length > 0 && source.Name[0] == '$')
        {
            destination.IsEmptySocket = true;
        }
    }

    internal CddlGenericInstantiation? FindInstantiation(CddlNode application)
    {
        int index = 0;
        while (index < Expansion.Instantiations.Count)
        {
            CddlGenericInstantiation instantiation =
                Expansion.Instantiations[index];
            int applicationIndex = 0;
            while (applicationIndex < instantiation.Applications.Count)
            {
                if (instantiation.Applications[applicationIndex] == application)
                {
                    return instantiation;
                }
                applicationIndex = applicationIndex + 1;
            }
            index = index + 1;
        }
        return null;
    }

    internal CddlResolvedReference? FindResolvedReference(CddlNode source)
    {
        int index = 0;
        while (index < Resolution.References.Count)
        {
            if (Resolution.References[index].Node == source)
            {
                return Resolution.References[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal CddlResolvedReference? FindEquivalentResolvedReference(
        CddlNode source)
    {
        int index = 0;
        while (index < Resolution.References.Count)
        {
            CddlResolvedReference reference = Resolution.References[index];
            CddlSourceSpan span = reference.Node.Span;
            if (reference.Node.Kind == source.Kind
                && reference.Node.Name == source.Name
                && span.SourceName == source.Span.SourceName
                && span.Start == source.Span.Start
                && span.Length == source.Span.Length)
            {
                return reference;
            }
            index = index + 1;
        }
        return null;
    }

    internal CddlRule? FindNamedTarget(string name)
    {
        int prefixLength = SocketPrefixLength(name);
        CddlRuleTableEntry? entry;
        if (prefixLength == 0)
        {
            entry = Table.FindEntry(name);
        }
        else
        {
            string socketName = name.Substring(prefixLength);
            CddlRuleKind socketKind = prefixLength == 2
                ? CddlRuleKind.GroupSocket
                : CddlRuleKind.TypeSocket;
            entry = Table.FindSocketEntry(socketName, socketKind);
        }
        if (entry != null)
        {
            return entry.MergedRule;
        }
        int index = 0;
        while (index < Resolution.ExternalRules.Count)
        {
            if (Resolution.ExternalRules[index].Name == name)
            {
                return Resolution.ExternalRules[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal int SocketPrefixLength(string name)
    {
        if (name.Length >= 2 && name[0] == '$' && name[1] == '$')
        {
            return 2;
        }
        if (name.Length >= 1 && name[0] == '$')
        {
            return 1;
        }
        return 0;
    }

    internal CddlNormalizedRule? FindNormalizedRule(CddlRule source)
    {
        int index = 0;
        while (index < Result.Rules.Count)
        {
            if (Result.Rules[index].SourceRule == source)
            {
                return Result.Rules[index];
            }
            index = index + 1;
        }
        return null;
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

    internal void ReportNodeLimit(
        CddlNode source,
        string containingRule)
    {
        Result.HasErrors = true;
        if (ReportedNodeLimit)
        {
            return;
        }
        ReportedNodeLimit = true;
        if (Options.MaxDiagnostics <= 0
            || Result.Diagnostics.Count >= Options.MaxDiagnostics)
        {
            return;
        }
        CddlDiagnostic diagnostic = new CddlDiagnostic();
        diagnostic.Severity = CddlDiagnosticSeverity.Error;
        diagnostic.Code = "CDDL_COMPILE_NODE_LIMIT";
        diagnostic.Message = "The normalized schema node limit was exceeded.";
        diagnostic.Span = source.Span;
        diagnostic.RuleName = containingRule;
        Result.Diagnostics.Add(diagnostic);
    }
}
