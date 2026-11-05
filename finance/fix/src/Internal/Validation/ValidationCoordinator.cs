using System.Collections.Generic;

namespace fix;

internal static class ValidationCoordinator
{
    internal static readonly ValidationLayer[] OrderedLayers =
    [
        ValidationLayer.Framing,
        ValidationLayer.Envelope,
        ValidationLayer.SessionHeader,
        ValidationLayer.DictionaryGrammar,
        ValidationLayer.Datatype,
        ValidationLayer.CodeSet,
        ValidationLayer.Group,
        ValidationLayer.ConditionalRule,
        ValidationLayer.ApplicationSemantic
    ];

    internal static ValidationResult CoordinateValidation(List<ValidationIssue> candidates, ValidationOptions options)
    {
        ValidationResult result = new ValidationResult();
        int issueLimit = ResolveIssueLimit(options);
        bool hasFailure = false;
        for (int layerIndex = 0; layerIndex < OrderedLayers.Length; layerIndex++)
        {
            ValidationLayer layer = OrderedLayers[layerIndex];
            if (!IsLayerEnabled(options, layer))
            {
                continue;
            }
            for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
            {
                ValidationIssue candidate = candidates[candidateIndex];
                if (candidate.Layer != layer)
                {
                    continue;
                }
                if (candidate.Severity == ValidationSeverity.Error || candidate.Severity == ValidationSeverity.Fatal)
                {
                    hasFailure = true;
                }
                if (!IsSeverityRetained(options, candidate.Severity))
                {
                    continue;
                }
                if (result.Issues.Count < issueLimit)
                {
                    result.Issues.Add(candidate);
                }
                else
                {
                    result.IssueLimitReached = true;
                }
            }
        }
        result.IsValid = !hasFailure;
        return result;
    }

    internal static bool IsLayerEnabled(ValidationOptions options, ValidationLayer layer)
    {
        if (layer == ValidationLayer.None || (options.Layers & layer) != layer)
        {
            return false;
        }
        if (layer == ValidationLayer.Group && !options.CheckGroups)
        {
            return false;
        }
        if (layer == ValidationLayer.ConditionalRule && !options.CheckConditionalRules)
        {
            return false;
        }
        return true;
    }

    internal static bool IsSeverityRetained(ValidationOptions options, ValidationSeverity severity)
    {
        return (int)severity >= (int)options.MinimumSeverity;
    }

    internal static int ResolveIssueLimit(ValidationOptions options)
    {
        if (options.MaxIssues <= 0)
        {
            return 0;
        }
        if (options.MaxIssues > FixDefaults.MaxDiagnostics)
        {
            return FixDefaults.MaxDiagnostics;
        }
        return options.MaxIssues;
    }
}
