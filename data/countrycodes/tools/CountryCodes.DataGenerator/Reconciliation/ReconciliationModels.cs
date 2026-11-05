using System;
using System.Collections.Generic;
using CountryCodes.DataGenerator.Staging;

namespace CountryCodes.DataGenerator.Reconciliation;

internal sealed class ReconciliationResult
{
    internal ReconciliationResult(
        GeneratorStagingData staging,
        int countryCrossCheckCount,
        int subdivisionParentCrossCheckCount,
        int regionNodeCount,
        int transitionCount,
        int localeCount,
        IReadOnlyList<ExplainedSourceDelta> sourceDeltas)
    {
        Staging = staging;
        CountryCrossCheckCount = countryCrossCheckCount;
        SubdivisionParentCrossCheckCount = subdivisionParentCrossCheckCount;
        RegionNodeCount = regionNodeCount;
        TransitionCount = transitionCount;
        LocaleCount = localeCount;
        SourceDeltas = sourceDeltas;
    }

    internal GeneratorStagingData Staging { get; }

    internal int CountryCrossCheckCount { get; }

    internal int SubdivisionParentCrossCheckCount { get; }

    internal int RegionNodeCount { get; }

    internal int TransitionCount { get; }

    internal int LocaleCount { get; }

    internal IReadOnlyList<ExplainedSourceDelta> SourceDeltas { get; }
}

internal sealed class ExplainedSourceDelta
{
    internal ExplainedSourceDelta(
        string overrideId,
        string sourceId,
        string upstreamKey,
        string fieldGroup,
        string explanation)
    {
        OverrideId = overrideId;
        SourceId = sourceId;
        UpstreamKey = upstreamKey;
        FieldGroup = fieldGroup;
        Explanation = explanation;
    }

    internal string OverrideId { get; }

    internal string SourceId { get; }

    internal string UpstreamKey { get; }

    internal string FieldGroup { get; }

    internal string Explanation { get; }
}

internal sealed class ReconciliationException : Exception
{
    internal ReconciliationException(IReadOnlyList<string> issues)
        : base($"Reconciliation found {issues.Count} invariant violation(s).")
    {
        Issues = issues;
    }

    internal IReadOnlyList<string> Issues { get; }
}

internal sealed class ReconciliationIssues
{
    private readonly List<string> issues = new List<string>();

    internal int Count => issues.Count;

    internal void Add(string issue)
    {
        issues.Add(issue);
    }

    internal void ThrowIfAny()
    {
        if (issues.Count != 0)
        {
            throw new ReconciliationException(issues);
        }
    }
}
