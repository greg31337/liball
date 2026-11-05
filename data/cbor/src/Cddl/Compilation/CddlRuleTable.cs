using System.Collections.Generic;

namespace cbor;

internal class CddlRuleTable
{
    internal List<CddlRuleTableEntry> Entries = new List<CddlRuleTableEntry>();
    internal List<CddlDiagnostic> Diagnostics = new List<CddlDiagnostic>();
    internal int MaxDiagnostics;
    internal bool HasErrors;

    internal CddlRuleTable(CddlRule[] rules, CddlCompileOptions options)
    {
        MaxDiagnostics = options.MaxDiagnostics;
        int index = 0;
        while (index < rules.Length)
        {
            GatherRule(rules[index]);
            index = index + 1;
        }
        FinalizeEntries();
    }

    internal void GatherRule(CddlRule rule)
    {
        CddlRuleTableEntry? entry = FindMatchingEntry(rule);
        if (entry == null)
        {
            entry = new CddlRuleTableEntry();
            entry.Name = rule.Name;
            if (rule.Kind == CddlRuleKind.TypeSocket
                || rule.Kind == CddlRuleKind.GroupSocket)
            {
                entry.SocketKind = rule.Kind;
            }
            Entries.Add(entry);
        }
        entry.Definitions.Add(rule);
        if (RuleFamily(rule.Kind) == 0)
        {
            entry.HasConflict = true;
            AddDiagnostic(
                "CDDL_COMPILE_RULE_KIND_INVALID",
                "The rule has an unsupported or unassigned definition kind.",
                rule
            );
            return;
        }
        if (rule.Value == null)
        {
            entry.HasConflict = true;
            AddDiagnostic(
                "CDDL_COMPILE_RULE_VALUE_MISSING",
                "The rule has no parsed definition value.",
                rule
            );
        }
        if (IsBaseKind(rule.Kind))
        {
            if (entry.BaseRule == null)
            {
                entry.BaseRule = rule;
            }
            else
            {
                entry.HasConflict = true;
                AddDiagnostic(
                    "CDDL_COMPILE_DUPLICATE_BASE_RULE",
                    "A rule name can have only one base definition.",
                    rule
                );
            }
        }
    }

    internal CddlRuleTableEntry? FindEntry(string name)
    {
        int index = 0;
        while (index < Entries.Count)
        {
            if (Entries[index].Name == name
                && Entries[index].SocketKind == CddlRuleKind.None)
            {
                return Entries[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal CddlRuleTableEntry? FindMatchingEntry(CddlRule rule)
    {
        CddlRuleKind socketKind = CddlRuleKind.None;
        if (rule.Kind == CddlRuleKind.TypeSocket
            || rule.Kind == CddlRuleKind.GroupSocket)
        {
            socketKind = rule.Kind;
        }
        int index = 0;
        while (index < Entries.Count)
        {
            if (Entries[index].Name == rule.Name
                && Entries[index].SocketKind == socketKind)
            {
                return Entries[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal CddlRuleTableEntry? FindSocketEntry(
        string name,
        CddlRuleKind socketKind)
    {
        int index = 0;
        while (index < Entries.Count)
        {
            if (Entries[index].Name == name
                && Entries[index].SocketKind == socketKind)
            {
                return Entries[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal void FinalizeEntries()
    {
        CddlExtensionMerger merger = new CddlExtensionMerger();
        int index = 0;
        while (index < Entries.Count)
        {
            CddlRuleTableEntry entry = Entries[index];
            ValidateEntryKind(entry);
            if (!entry.HasConflict && entry.Definitions.Count > 0)
            {
                entry.MergedRule = merger.MergeEntry(entry);
            }
            index = index + 1;
        }
    }

    internal void ValidateEntryKind(CddlRuleTableEntry entry)
    {
        int expectedFamily = 0;
        if (entry.BaseRule != null)
        {
            expectedFamily = RuleFamily(entry.BaseRule.Kind);
        }
        if (expectedFamily == 0)
        {
            int familyIndex = 0;
            while (familyIndex < entry.Definitions.Count && expectedFamily == 0)
            {
                expectedFamily = RuleFamily(entry.Definitions[familyIndex].Kind);
                familyIndex = familyIndex + 1;
            }
        }
        entry.Kind = expectedFamily == 1
            ? CddlRuleKind.Type
            : CddlRuleKind.Group;

        int index = 0;
        while (index < entry.Definitions.Count)
        {
            CddlRule rule = entry.Definitions[index];
            int family = RuleFamily(rule.Kind);
            bool duplicateBase = IsBaseKind(rule.Kind)
                && entry.BaseRule != null
                && rule != entry.BaseRule;
            if (family != 0 && family != expectedFamily && !duplicateBase)
            {
                entry.HasConflict = true;
                AddDiagnostic(
                    "CDDL_COMPILE_RULE_KIND_MISMATCH",
                    "A rule cannot mix type and group definitions or extensions.",
                    rule
                );
            }
            index = index + 1;
        }
    }

    internal int RuleFamily(CddlRuleKind kind)
    {
        switch (kind)
        {
            case CddlRuleKind.Type:
            case CddlRuleKind.TypeExtension:
            case CddlRuleKind.TypeSocket:
                return 1;
            case CddlRuleKind.Group:
            case CddlRuleKind.GroupExtension:
            case CddlRuleKind.GroupSocket:
                return 2;
            default:
                return 0;
        }
    }

    internal bool IsBaseKind(CddlRuleKind kind)
    {
        return kind == CddlRuleKind.Type || kind == CddlRuleKind.Group;
    }

    internal void AddDiagnostic(
        string code,
        string message,
        CddlRule rule)
    {
        HasErrors = true;
        if (MaxDiagnostics <= 0 || Diagnostics.Count >= MaxDiagnostics)
        {
            return;
        }
        CddlDiagnostic diagnostic = new CddlDiagnostic();
        diagnostic.Severity = CddlDiagnosticSeverity.Error;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Span = rule.Span;
        diagnostic.RuleName = rule.Name;
        Diagnostics.Add(diagnostic);
    }
}
