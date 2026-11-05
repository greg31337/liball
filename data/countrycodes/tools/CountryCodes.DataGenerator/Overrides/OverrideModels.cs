using System;
using System.Collections.Generic;
using CountryCodes.DataGenerator.Reconciliation;

namespace CountryCodes.DataGenerator.Overrides;

internal sealed class OverrideCatalog
{
    internal OverrideCatalog(int policyVersion, IReadOnlyList<OverrideEntry> entries)
    {
        PolicyVersion = policyVersion;
        Entries = entries;
    }

    internal int PolicyVersion { get; }

    internal IReadOnlyList<OverrideEntry> Entries { get; }
}

internal sealed class OverrideEntry
{
    internal OverrideEntry(
        string id,
        string kind,
        string targetKind,
        string targetKey,
        string field,
        IReadOnlyList<string> values,
        OverrideProvenance provenance,
        string reason,
        string reviewer,
        string effectiveDate,
        OverrideExpiryCondition expiryCondition)
    {
        Id = id;
        Kind = kind;
        TargetKind = targetKind;
        TargetKey = targetKey;
        Field = field;
        Values = values;
        Provenance = provenance;
        Reason = reason;
        Reviewer = reviewer;
        EffectiveDate = effectiveDate;
        ExpiryCondition = expiryCondition;
    }

    internal string Id { get; }

    internal string Kind { get; }

    internal string TargetKind { get; }

    internal string TargetKey { get; }

    internal string Field { get; }

    internal IReadOnlyList<string> Values { get; }

    internal OverrideProvenance Provenance { get; }

    internal string Reason { get; }

    internal string Reviewer { get; }

    internal string EffectiveDate { get; }

    internal OverrideExpiryCondition ExpiryCondition { get; }
}

internal sealed class OverrideProvenance
{
    internal OverrideProvenance(string sourceId, string upstreamKey, string evidence)
    {
        SourceId = sourceId;
        UpstreamKey = upstreamKey;
        Evidence = evidence;
    }

    internal string SourceId { get; }

    internal string UpstreamKey { get; }

    internal string Evidence { get; }
}

internal sealed class OverrideExpiryCondition
{
    internal OverrideExpiryCondition(string sourceId, string revision, string condition)
    {
        SourceId = sourceId;
        Revision = revision;
        Condition = condition;
    }

    internal string SourceId { get; }

    internal string Revision { get; }

    internal string Condition { get; }
}

internal sealed class OverrideApplicationResult
{
    internal OverrideApplicationResult(
        ReconciliationResult reconciliation,
        IReadOnlyList<AppliedOverride> appliedOverrides)
    {
        Reconciliation = reconciliation;
        AppliedOverrides = appliedOverrides;
    }

    internal ReconciliationResult Reconciliation { get; }

    internal IReadOnlyList<AppliedOverride> AppliedOverrides { get; }
}

internal sealed class AppliedOverride
{
    internal AppliedOverride(OverrideEntry definition)
    {
        Definition = definition;
    }

    internal OverrideEntry Definition { get; }
}

internal sealed class OverrideException : Exception
{
    internal OverrideException(IReadOnlyList<string> issues)
        : base($"Override processing found {issues.Count} issue(s).")
    {
        Issues = issues;
    }

    internal IReadOnlyList<string> Issues { get; }
}
