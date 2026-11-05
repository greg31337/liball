using System.Collections.Generic;

namespace cbor;

internal class CddlReferenceResolver
{
    internal CddlRuleTable Table;
    internal CddlDocument Document;
    internal CddlCompileOptions Options;
    internal CddlResolutionResult Result = new CddlResolutionResult();
    internal List<CddlExternalResolution> ExternalResolutions =
        new List<CddlExternalResolution>();
    internal List<string> ActiveGenericParameters = new List<string>();

    internal CddlReferenceResolver(
        CddlRuleTable table,
        CddlDocument document,
        CddlCompileOptions options)
    {
        Table = table;
        Document = document;
        Options = options;
    }

    internal CddlResolutionResult ResolveDocument()
    {
        SelectRootRule();
        int index = 0;
        while (index < Table.Entries.Count)
        {
            CddlRuleTableEntry entry = Table.Entries[index];
            if (entry.MergedRule != null)
            {
                VisitRule(entry.MergedRule, DisplayRuleName(entry));
            }
            index = index + 1;
        }
        ProcessExternalRules();
        return Result;
    }

    internal void SelectRootRule()
    {
        string? name = Options.RootRuleName;
        if (name == null)
        {
            name = Document.RootRuleName;
        }
        if (name == null)
        {
            return;
        }
        CddlRuleTableEntry? entry = Table.FindEntry(name);
        if (entry != null)
        {
            if (entry.MergedRule == null)
            {
                return;
            }
            if (entry.Kind != CddlRuleKind.Type)
            {
                ReportDiagnostic(
                    "CDDL_COMPILE_ROOT_KIND_INVALID",
                    "The selected root rule must define a type.",
                    entry.MergedRule.Span,
                    name
                );
                return;
            }
            Result.RootRule = entry.MergedRule;
            return;
        }

        CddlSourceSpan span = CreateRootSpan();
        CddlRule? externalType = RequestExternalRule(
            name,
            CddlRuleKind.Type,
            span,
            name
        );
        if (externalType != null)
        {
            Result.RootRule = externalType;
            return;
        }
        CddlRule? externalGroup = RequestExternalRule(
            name,
            CddlRuleKind.Group,
            span,
            name
        );
        if (externalGroup != null)
        {
            ReportDiagnostic(
                "CDDL_COMPILE_ROOT_KIND_INVALID",
                "The selected external root rule defines a group, not a type.",
                span,
                name
            );
            return;
        }
        if (!HasInvalidExternalResult(name))
        {
            ReportDiagnostic(
                "CDDL_COMPILE_ROOT_UNDEFINED",
                "The selected root rule is not defined.",
                span,
                name
            );
        }
    }

    internal void VisitRule(CddlRule rule, string displayName)
    {
        if (rule.Value == null)
        {
            return;
        }
        ActiveGenericParameters = rule.GenericParameters;
        VisitNode(rule.Value, displayName, rule.Kind);
        ActiveGenericParameters = new List<string>();
    }

    internal void VisitNode(
        CddlNode node,
        string containingRule,
        CddlRuleKind expectedKind)
    {
        switch (node.Kind)
        {
            case CddlNodeKind.RuleReference:
                ResolveReferenceNode(node, containingRule, expectedKind);
                VisitTypeChildren(node, containingRule);
                break;
            case CddlNodeKind.TypeChoice:
            case CddlNodeKind.Range:
            case CddlNodeKind.Control:
            case CddlNodeKind.ParenthesizedType:
            case CddlNodeKind.Tag:
            case CddlNodeKind.Representation:
                VisitTypeChildren(node, containingRule);
                break;
            case CddlNodeKind.Array:
            case CddlNodeKind.Map:
            case CddlNodeKind.ParenthesizedGroup:
            case CddlNodeKind.GroupChoice:
            case CddlNodeKind.Group:
                VisitGroupChildren(node, containingRule);
                break;
            case CddlNodeKind.GroupEntry:
                VisitGroupEntry(node, containingRule);
                break;
            case CddlNodeKind.Unwrap:
                ResolveNamedNode(
                    node,
                    containingRule,
                    CddlRuleKind.Type
                );
                VisitTypeChildren(node, containingRule);
                break;
            case CddlNodeKind.GroupEnumeration:
                if (node.Name != null)
                {
                    ResolveNamedNode(
                        node,
                        containingRule,
                        CddlRuleKind.Group
                    );
                    VisitTypeChildren(node, containingRule);
                }
                else
                {
                    VisitGroupChildren(node, containingRule);
                }
                break;
            default:
                break;
        }
    }

    internal void VisitTypeChildren(CddlNode node, string containingRule)
    {
        int index = 0;
        while (index < node.Children.Count)
        {
            VisitNode(
                node.Children[index],
                containingRule,
                CddlRuleKind.Type
            );
            index = index + 1;
        }
    }

    internal void VisitGroupChildren(CddlNode node, string containingRule)
    {
        int index = 0;
        while (index < node.Children.Count)
        {
            VisitNode(
                node.Children[index],
                containingRule,
                CddlRuleKind.Group
            );
            index = index + 1;
        }
    }

    internal void VisitGroupEntry(CddlNode entry, string containingRule)
    {
        if (entry.Children.Count == 1)
        {
            CddlNode value = entry.Children[0];
            CddlRuleKind expectedKind = CddlRuleKind.Type;
            if (value.Kind == CddlNodeKind.ParenthesizedGroup)
            {
                expectedKind = CddlRuleKind.Group;
            }
            else if (value.Kind == CddlNodeKind.RuleReference)
            {
                expectedKind = CddlRuleKind.None;
            }
            VisitNode(value, containingRule, expectedKind);
            return;
        }
        int index = 0;
        while (index < entry.Children.Count)
        {
            VisitNode(
                entry.Children[index],
                containingRule,
                CddlRuleKind.Type
            );
            index = index + 1;
        }
    }

    internal void ResolveReferenceNode(
        CddlNode node,
        string containingRule,
        CddlRuleKind expectedKind)
    {
        if (node.Name == null)
        {
            return;
        }
        int socketPrefix = SocketPrefixLength(node.Name);
        if (socketPrefix > 0)
        {
            ResolveSocketNode(node, containingRule, expectedKind, socketPrefix);
            return;
        }
        if (IsActiveGenericParameter(node.Name))
        {
            AddGenericParameterReference(node, containingRule);
            return;
        }
        ResolveOrdinaryNode(node, containingRule, expectedKind);
    }

    internal void ResolveNamedNode(
        CddlNode node,
        string containingRule,
        CddlRuleKind expectedKind)
    {
        if (node.Name == null)
        {
            return;
        }
        if (IsActiveGenericParameter(node.Name))
        {
            if (expectedKind == CddlRuleKind.Type)
            {
                AddGenericParameterReference(node, containingRule);
            }
            else
            {
                ReportDiagnostic(
                    "CDDL_COMPILE_REFERENCE_KIND_MISMATCH",
                    "A generic type parameter cannot resolve as a group.",
                    node.Span,
                    containingRule
                );
            }
            return;
        }
        ResolveOrdinaryNode(node, containingRule, expectedKind);
    }

    internal void ResolveOrdinaryNode(
        CddlNode node,
        string containingRule,
        CddlRuleKind expectedKind)
    {
        string name = node.Name ?? "";
        CddlRuleTableEntry? entry = Table.FindEntry(name);
        if (entry != null)
        {
            if (entry.MergedRule == null)
            {
                return;
            }
            if (expectedKind != CddlRuleKind.None && entry.Kind != expectedKind)
            {
                ReportDiagnostic(
                    "CDDL_COMPILE_REFERENCE_KIND_MISMATCH",
                    "The referenced rule has the wrong type or group kind for this context.",
                    node.Span,
                    containingRule
                );
                return;
            }
            AddResolvedReference(
                node,
                entry.MergedRule,
                entry.Kind,
                false,
                containingRule
            );
            return;
        }
        if (expectedKind == CddlRuleKind.None)
        {
            ResolveAmbiguousExternalNode(node, containingRule);
            return;
        }
        ResolveExpectedExternalNode(node, containingRule, expectedKind);
    }

    internal void ResolveAmbiguousExternalNode(
        CddlNode node,
        string containingRule)
    {
        string name = node.Name ?? "";
        CddlRule? type = RequestExternalRule(
            name,
            CddlRuleKind.Type,
            node.Span,
            containingRule
        );
        CddlRule? group = RequestExternalRule(
            name,
            CddlRuleKind.Group,
            node.Span,
            containingRule
        );
        if (type != null && group != null)
        {
            ReportDiagnostic(
                "CDDL_COMPILE_REFERENCE_AMBIGUOUS",
                "The reference can resolve as both a type and a group.",
                node.Span,
                containingRule
            );
        }
        else if (type != null)
        {
            AddResolvedReference(
                node,
                type,
                CddlRuleKind.Type,
                false,
                containingRule
            );
        }
        else if (group != null)
        {
            AddResolvedReference(
                node,
                group,
                CddlRuleKind.Group,
                false,
                containingRule
            );
        }
        else if (!HasInvalidExternalResult(name))
        {
            ReportDiagnostic(
                "CDDL_COMPILE_REFERENCE_UNDEFINED",
                "The referenced rule is not defined.",
                node.Span,
                containingRule
            );
        }
    }

    internal void ResolveExpectedExternalNode(
        CddlNode node,
        string containingRule,
        CddlRuleKind expectedKind)
    {
        string name = node.Name ?? "";
        CddlRule? expected = RequestExternalRule(
            name,
            expectedKind,
            node.Span,
            containingRule
        );
        if (expected != null)
        {
            AddResolvedReference(
                node,
                expected,
                expectedKind,
                false,
                containingRule
            );
            return;
        }
        CddlRuleKind oppositeKind = expectedKind == CddlRuleKind.Type
            ? CddlRuleKind.Group
            : CddlRuleKind.Type;
        CddlRule? opposite = RequestExternalRule(
            name,
            oppositeKind,
            node.Span,
            containingRule
        );
        if (opposite != null)
        {
            ReportDiagnostic(
                "CDDL_COMPILE_REFERENCE_KIND_MISMATCH",
                "The external rule has the wrong type or group kind for this context.",
                node.Span,
                containingRule
            );
        }
        else if (!HasInvalidExternalResult(name))
        {
            ReportDiagnostic(
                "CDDL_COMPILE_REFERENCE_UNDEFINED",
                "The referenced rule is not defined.",
                node.Span,
                containingRule
            );
        }
    }

    internal void ResolveSocketNode(
        CddlNode node,
        string containingRule,
        CddlRuleKind expectedKind,
        int prefixLength)
    {
        string originalName = node.Name ?? "";
        if (originalName.Length == prefixLength)
        {
            ReportDiagnostic(
                "CDDL_COMPILE_SOCKET_NAME_INVALID",
                "A socket reference requires a name after its prefix.",
                node.Span,
                containingRule
            );
            return;
        }
        CddlRuleKind kind = prefixLength == 2
            ? CddlRuleKind.Group
            : CddlRuleKind.Type;
        if (expectedKind != CddlRuleKind.None && expectedKind != kind)
        {
            ReportDiagnostic(
                "CDDL_COMPILE_REFERENCE_KIND_MISMATCH",
                "The socket kind is not valid in this context.",
                node.Span,
                containingRule
            );
            return;
        }
        string name = originalName.Substring(prefixLength);
        CddlRuleKind socketKind = prefixLength == 2
            ? CddlRuleKind.GroupSocket
            : CddlRuleKind.TypeSocket;
        CddlRuleTableEntry? entry = Table.FindSocketEntry(name, socketKind);
        if (entry != null && entry.MergedRule != null)
        {
            AddResolvedReference(
                node,
                entry.MergedRule,
                kind,
                false,
                containingRule
            );
        }
        else
        {
            AddResolvedReference(
                node,
                null,
                kind,
                true,
                containingRule
            );
        }
    }

    internal CddlRule? RequestExternalRule(
        string name,
        CddlRuleKind kind,
        CddlSourceSpan span,
        string containingRule)
    {
        CddlExternalResolution? existing = FindExternalResolution(name, kind);
        if (existing != null)
        {
            if (existing.IsInvalid)
            {
                ReportDiagnostic(
                    "CDDL_COMPILE_EXTERNAL_RULE_INVALID",
                    existing.ErrorMessage,
                    span,
                    containingRule
                );
            }
            return existing.Rule;
        }
        if (Options.RuleResolver == null)
        {
            return null;
        }
        CddlExternalResolution resolution = new CddlExternalResolution();
        resolution.Name = name;
        resolution.Kind = kind;
        ExternalResolutions.Add(resolution);
        CddlRule? rule = Options.RuleResolver.ResolveRule(name, kind);
        if (rule == null)
        {
            return null;
        }
        string? error = ValidateExternalRule(rule, name, kind);
        if (error != null)
        {
            resolution.IsInvalid = true;
            resolution.ErrorMessage = error;
            ReportDiagnostic(
                "CDDL_COMPILE_EXTERNAL_RULE_INVALID",
                error,
                span,
                containingRule
            );
            return null;
        }
        resolution.Rule = rule;
        Result.ExternalRules.Add(rule);
        return rule;
    }

    internal CddlExternalResolution? FindExternalResolution(
        string name,
        CddlRuleKind kind)
    {
        int index = 0;
        while (index < ExternalResolutions.Count)
        {
            CddlExternalResolution resolution = ExternalResolutions[index];
            if (resolution.Name == name && resolution.Kind == kind)
            {
                return resolution;
            }
            index = index + 1;
        }
        return null;
    }

    internal string? ValidateExternalRule(
        CddlRule rule,
        string name,
        CddlRuleKind kind)
    {
        if (rule.Name != name)
        {
            return "The external resolver returned a rule with a different name.";
        }
        if (rule.Kind != kind)
        {
            return "The external resolver returned a rule with a different kind.";
        }
        if (rule.Value == null)
        {
            return "The external resolver returned a rule without a value.";
        }
        return null;
    }

    internal bool HasInvalidExternalResult(string name)
    {
        int index = 0;
        while (index < ExternalResolutions.Count)
        {
            if (ExternalResolutions[index].Name == name
                && ExternalResolutions[index].IsInvalid)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal void ProcessExternalRules()
    {
        int index = 0;
        while (index < ExternalResolutions.Count)
        {
            CddlExternalResolution resolution = ExternalResolutions[index];
            if (resolution.Rule != null && !resolution.IsProcessed)
            {
                resolution.IsProcessed = true;
                VisitRule(resolution.Rule, resolution.Rule.Name);
            }
            index = index + 1;
        }
    }

    internal void AddResolvedReference(
        CddlNode node,
        CddlRule? rule,
        CddlRuleKind kind,
        bool emptySocket,
        string containingRule)
    {
        CddlResolvedReference reference = new CddlResolvedReference();
        reference.Node = node;
        reference.Rule = rule;
        reference.Kind = kind;
        reference.ContainingRule = containingRule;
        reference.IsEmptySocket = emptySocket;
        Result.References.Add(reference);
    }

    internal bool IsActiveGenericParameter(string name)
    {
        int index = 0;
        while (index < ActiveGenericParameters.Count)
        {
            if (ActiveGenericParameters[index] == name)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal void AddGenericParameterReference(
        CddlNode node,
        string containingRule)
    {
        CddlResolvedReference reference = new CddlResolvedReference();
        reference.Node = node;
        reference.Kind = CddlRuleKind.Type;
        reference.ContainingRule = containingRule;
        reference.IsGenericParameter = true;
        Result.References.Add(reference);
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

    internal string DisplayRuleName(CddlRuleTableEntry entry)
    {
        if (entry.SocketKind == CddlRuleKind.TypeSocket)
        {
            return "$" + entry.Name;
        }
        if (entry.SocketKind == CddlRuleKind.GroupSocket)
        {
            return "$$" + entry.Name;
        }
        return entry.Name;
    }

    internal CddlSourceSpan CreateRootSpan()
    {
        CddlSourceSpan span = new CddlSourceSpan();
        span.SourceName = Document.SourceName;
        span.StartLine = 1;
        span.StartColumn = 1;
        span.EndLine = 1;
        span.EndColumn = 1;
        return span;
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
