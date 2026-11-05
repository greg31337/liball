using System;
using System.Collections.Generic;

namespace cbor;

internal class CddlGenericExpander
{
    internal CddlRuleTable Table;
    internal CddlResolutionResult Resolution;
    internal CddlCompileOptions Options;
    internal CddlGenericExpansionResult Result =
        new CddlGenericExpansionResult();
    internal bool ReportedInstantiationLimit;
    internal bool ReportedDepthLimit;

    internal CddlGenericExpander(
        CddlRuleTable table,
        CddlResolutionResult resolution,
        CddlCompileOptions options)
    {
        Table = table;
        Resolution = resolution;
        Options = options;
    }

    internal CddlGenericExpansionResult ExpandDocument()
    {
        ValidateArities();
        int index = 0;
        while (index < Table.Entries.Count)
        {
            CddlRule? rule = Table.Entries[index].MergedRule;
            if (rule != null && rule.GenericParameters.Count == 0)
            {
                ExpandRule(rule);
            }
            index = index + 1;
        }
        index = 0;
        while (index < Resolution.ExternalRules.Count)
        {
            CddlRule rule = Resolution.ExternalRules[index];
            if (rule.GenericParameters.Count == 0)
            {
                ExpandRule(rule);
            }
            index = index + 1;
        }
        return Result;
    }

    internal void ValidateArities()
    {
        int index = 0;
        while (index < Resolution.References.Count)
        {
            CddlResolvedReference reference = Resolution.References[index];
            if (reference.IsGenericParameter)
            {
                if (reference.Node.Children.Count != 0)
                {
                    ReportArityDiagnostic(
                        reference,
                        0,
                        reference.Node.Children.Count
                    );
                }
            }
            else if (!reference.IsEmptySocket && reference.Rule != null)
            {
                int expected = reference.Rule.GenericParameters.Count;
                int actual = reference.Node.Children.Count;
                if (expected != actual)
                {
                    ReportArityDiagnostic(reference, expected, actual);
                }
            }
            index = index + 1;
        }
    }

    internal void ExpandRule(CddlRule rule)
    {
        if (rule.Value != null)
        {
            ExpandNode(rule.Value, rule.Name, 1);
        }
    }

    internal void ExpandNode(
        CddlNode node,
        string containingRule,
        int depth)
    {
        int index = 0;
        while (index < node.Children.Count)
        {
            ExpandNode(node.Children[index], containingRule, depth);
            index = index + 1;
        }
        if (node.Kind != CddlNodeKind.RuleReference
            && node.Kind != CddlNodeKind.Unwrap
            && node.Kind != CddlNodeKind.GroupEnumeration)
        {
            return;
        }
        CddlRule? target = FindTargetRule(node);
        if (target == null || target.GenericParameters.Count == 0)
        {
            return;
        }
        if (target.GenericParameters.Count != node.Children.Count)
        {
            return;
        }
        Instantiate(target, node, containingRule, depth);
    }

    internal CddlRule? FindTargetRule(CddlNode node)
    {
        int index = 0;
        while (index < Resolution.References.Count)
        {
            CddlResolvedReference reference = Resolution.References[index];
            if (reference.Node == node)
            {
                return reference.Rule;
            }
            index = index + 1;
        }
        string name = node.Name ?? "";
        CddlRuleTableEntry? entry = Table.FindEntry(name);
        if (entry != null)
        {
            return entry.MergedRule;
        }
        index = 0;
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

    internal CddlRule? Instantiate(
        CddlRule genericRule,
        CddlNode application,
        string containingRule,
        int depth)
    {
        CddlGenericInstantiation? existing = FindInstantiation(
            genericRule,
            application.Children
        );
        if (existing != null)
        {
            existing.Applications.Add(application);
            return existing.InstantiatedRule;
        }
        if (depth > Options.MaxReferenceDepth)
        {
            ReportDepthLimit(application, containingRule);
            return null;
        }
        if (Options.MaxGenericInstantiations <= 0
            || Result.Instantiations.Count >= Options.MaxGenericInstantiations)
        {
            ReportInstantiationLimit(application, containingRule);
            return null;
        }

        CddlRule instantiatedRule = new CddlRule();
        instantiatedRule.Name = genericRule.Name
            + "<" + Result.Instantiations.Count + ">";
        instantiatedRule.Kind = genericRule.Kind;
        instantiatedRule.IsPrelude = genericRule.IsPrelude;
        instantiatedRule.SourceOrder = genericRule.SourceOrder;
        instantiatedRule.Span = CloneSpan(application.Span);
        if (genericRule.Value != null)
        {
            instantiatedRule.Value = CloneAndSubstitute(
                genericRule.Value,
                genericRule.GenericParameters,
                application.Children
            );
        }

        CddlGenericInstantiation instantiation =
            new CddlGenericInstantiation();
        instantiation.GenericRule = genericRule;
        instantiation.InstantiatedRule = instantiatedRule;
        IndexArguments(instantiation.Arguments, application.Children);
        instantiation.Applications.Add(application);
        Result.Instantiations.Add(instantiation);

        if (instantiatedRule.Value != null)
        {
            ExpandNode(
                instantiatedRule.Value,
                containingRule,
                depth + 1
            );
        }
        return instantiatedRule;
    }

    internal void IndexArguments(
        List<CddlNode> destination,
        List<CddlNode> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(CloneNode(source[index]));
            index = index + 1;
        }
    }

    internal CddlGenericInstantiation? FindInstantiation(
        CddlRule genericRule,
        List<CddlNode> arguments)
    {
        int index = 0;
        while (index < Result.Instantiations.Count)
        {
            CddlGenericInstantiation candidate = Result.Instantiations[index];
            if (candidate.GenericRule == genericRule
                && NodeListsEqual(candidate.Arguments, arguments))
            {
                return candidate;
            }
            index = index + 1;
        }
        return null;
    }

    internal CddlNode CloneAndSubstitute(
        CddlNode node,
        List<string> parameterNames,
        List<CddlNode> arguments)
    {
        if (node.Kind == CddlNodeKind.RuleReference
            && node.Children.Count == 0
            && node.Name != null)
        {
            int parameterIndex = FindParameter(parameterNames, node.Name);
            if (parameterIndex >= 0 && parameterIndex < arguments.Count)
            {
                return CloneNode(arguments[parameterIndex]);
            }
        }
        CddlNode clone = CloneNodeFields(node);
        int index = 0;
        while (index < node.Children.Count)
        {
            clone.Children.Add(CloneAndSubstitute(
                node.Children[index],
                parameterNames,
                arguments
            ));
            index = index + 1;
        }
        return clone;
    }

    internal int FindParameter(List<string> names, string name)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (names[index] == name)
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal CddlNode CloneNode(CddlNode node)
    {
        CddlNode clone = CloneNodeFields(node);
        int index = 0;
        while (index < node.Children.Count)
        {
            clone.Children.Add(CloneNode(node.Children[index]));
            index = index + 1;
        }
        return clone;
    }

    internal CddlNode CloneNodeFields(CddlNode node)
    {
        CddlNode clone = new CddlNode();
        clone.Kind = node.Kind;
        clone.LiteralKind = node.LiteralKind;
        clone.Occurrence = node.Occurrence;
        clone.Name = node.Name;
        clone.TextValue = node.TextValue;
        if (node.ByteStringValue != null)
        {
            clone.ByteStringValue = (byte[])node.ByteStringValue.Clone();
        }
        clone.IntegerValue = node.IntegerValue;
        clone.HasIntegerValue = node.HasIntegerValue;
        clone.FloatValue = node.FloatValue;
        clone.HasFloatValue = node.HasFloatValue;
        clone.ControlName = node.ControlName;
        clone.MinimumOccurrence = node.MinimumOccurrence;
        clone.MaximumOccurrence = node.MaximumOccurrence;
        clone.MajorType = node.MajorType;
        clone.ArgumentValue = node.ArgumentValue;
        clone.HasArgumentValue = node.HasArgumentValue;
        clone.IsRangeUpperExclusive = node.IsRangeUpperExclusive;
        clone.IsCut = node.IsCut;
        clone.Span = CloneSpan(node.Span);
        return clone;
    }

    internal CddlSourceSpan CloneSpan(CddlSourceSpan span)
    {
        CddlSourceSpan clone = new CddlSourceSpan();
        clone.SourceName = span.SourceName;
        clone.Start = span.Start;
        clone.Length = span.Length;
        clone.StartLine = span.StartLine;
        clone.StartColumn = span.StartColumn;
        clone.EndLine = span.EndLine;
        clone.EndColumn = span.EndColumn;
        return clone;
    }

    internal bool NodeListsEqual(
        List<CddlNode> left,
        List<CddlNode> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }
        int index = 0;
        while (index < left.Count)
        {
            if (!NodesEqual(left[index], right[index]))
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal bool NodesEqual(CddlNode left, CddlNode right)
    {
        if (left.Kind != right.Kind
            || left.LiteralKind != right.LiteralKind
            || left.Occurrence != right.Occurrence
            || left.Name != right.Name
            || left.TextValue != right.TextValue
            || left.IntegerValue != right.IntegerValue
            || left.HasIntegerValue != right.HasIntegerValue
            || BitConverter.DoubleToInt64Bits(left.FloatValue)
                != BitConverter.DoubleToInt64Bits(right.FloatValue)
            || left.HasFloatValue != right.HasFloatValue
            || left.ControlName != right.ControlName
            || left.MinimumOccurrence != right.MinimumOccurrence
            || left.MaximumOccurrence != right.MaximumOccurrence
            || left.MajorType != right.MajorType
            || left.ArgumentValue != right.ArgumentValue
            || left.HasArgumentValue != right.HasArgumentValue
            || left.IsRangeUpperExclusive != right.IsRangeUpperExclusive
            || left.IsCut != right.IsCut
            || !ByteArraysEqual(left.ByteStringValue, right.ByteStringValue))
        {
            return false;
        }
        return NodeListsEqual(left.Children, right.Children);
    }

    internal bool ByteArraysEqual(byte[]? left, byte[]? right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }
        if (left.Length != right.Length)
        {
            return false;
        }
        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    internal void ReportArityDiagnostic(
        CddlResolvedReference reference,
        int expected,
        int actual)
    {
        string name = reference.Node.Name ?? "";
        ReportDiagnostic(
            "CDDL_COMPILE_GENERIC_ARITY_MISMATCH",
            "Rule '" + name + "' expects " + expected
                + " generic argument(s), but received " + actual + ".",
            reference.Node.Span,
            reference.ContainingRule
        );
    }

    internal void ReportInstantiationLimit(
        CddlNode application,
        string containingRule)
    {
        if (ReportedInstantiationLimit)
        {
            return;
        }
        ReportedInstantiationLimit = true;
        ReportDiagnostic(
            "CDDL_COMPILE_GENERIC_INSTANTIATION_LIMIT",
            "The generic instantiation limit was exceeded.",
            application.Span,
            containingRule
        );
    }

    internal void ReportDepthLimit(
        CddlNode application,
        string containingRule)
    {
        if (ReportedDepthLimit)
        {
            return;
        }
        ReportedDepthLimit = true;
        ReportDiagnostic(
            "CDDL_COMPILE_GENERIC_DEPTH_LIMIT",
            "The generic expansion depth limit was exceeded.",
            application.Span,
            containingRule
        );
    }

    internal void ReportDiagnostic(
        string code,
        string message,
        CddlSourceSpan span,
        string containingRule)
    {
        Result.HasErrors = true;
        if (Options.MaxDiagnostics <= 0
            || Result.Diagnostics.Count >= Options.MaxDiagnostics)
        {
            return;
        }
        CddlDiagnostic diagnostic = new CddlDiagnostic();
        diagnostic.Severity = CddlDiagnosticSeverity.Error;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Span = span;
        diagnostic.RuleName = containingRule;
        Result.Diagnostics.Add(diagnostic);
    }
}
