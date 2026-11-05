using System;
using System.Collections.Generic;
using CountryCodes.DataGenerator.Reconciliation;
using CountryCodes.DataGenerator.Staging;

namespace CountryCodes.DataGenerator.Overrides;

internal static class OverrideProcessor
{
    private static readonly HashSet<string> IdentifierFields = new HashSet<string>(
        new[]
        {
            "alpha-2", "alpha-3", "alpha-4", "numeric-3", "code", "country-prefix",
            "identifier"
        },
        StringComparer.Ordinal);

    internal static OverrideApplicationResult Apply(
        OverrideCatalog catalog,
        GeneratorStagingData staging,
        ReconciliationResult reconciliation)
    {
        List<string> issues = new List<string>();
        List<OverrideEntry> sorted = new List<OverrideEntry>(catalog.Entries);
        sorted.Sort(CompareEntries);
        ValidateConflicts(sorted, issues);

        Dictionary<string, StagedCountry> countries = IndexCountries(staging);
        Dictionary<string, StagedSubdivision> subdivisions = IndexSubdivisions(staging);
        Dictionary<string, StagedHistoricalCountry> historical = IndexHistorical(staging);
        List<AppliedOverride> applied = new List<AppliedOverride>();
        foreach (OverrideEntry entry in sorted)
        {
            if (string.Equals(entry.Kind, "source-delta", StringComparison.Ordinal))
            {
                ValidateSourceDelta(entry, reconciliation, issues);
            }
            else if (IdentifierFields.Contains(entry.Field))
            {
                issues.Add(
                    $"Override '{entry.Id}' attempts to mutate authoritative identifier " +
                    $"field '{entry.Field}'.");
            }
            else if (string.Equals(entry.Kind, "alias", StringComparison.Ordinal))
            {
                ValidateAlias(entry, countries, subdivisions, historical, issues);
            }
            else if (string.Equals(entry.Kind, "supplement", StringComparison.Ordinal))
            {
                ValidateSupplement(entry, countries, subdivisions, historical, issues);
            }

            applied.Add(new AppliedOverride(entry));
        }

        if (issues.Count != 0)
        {
            throw new OverrideException(issues);
        }

        return new OverrideApplicationResult(reconciliation, applied);
    }

    private static void ValidateSourceDelta(
        OverrideEntry entry,
        ReconciliationResult reconciliation,
        List<string> issues)
    {
        if (!string.Equals(entry.TargetKind, "source-delta", StringComparison.Ordinal))
        {
            issues.Add($"Source-delta override '{entry.Id}' must target kind 'source-delta'.");
        }

        if (!string.Equals(
                entry.TargetKey,
                entry.Provenance.UpstreamKey,
                StringComparison.Ordinal))
        {
            issues.Add(
                $"Source-delta override '{entry.Id}' target key must match its provenance " +
                "upstream key.");
        }

        foreach (ExplainedSourceDelta delta in reconciliation.SourceDeltas)
        {
            if (string.Equals(delta.OverrideId, entry.Id, StringComparison.Ordinal))
            {
                return;
            }
        }

        issues.Add($"Source-delta override '{entry.Id}' did not explain an observed source delta.");
    }

    private static void ValidateAlias(
        OverrideEntry entry,
        IReadOnlyDictionary<string, StagedCountry> countries,
        IReadOnlyDictionary<string, StagedSubdivision> subdivisions,
        IReadOnlyDictionary<string, StagedHistoricalCountry> historical,
        List<string> issues)
    {
        if (!string.Equals(entry.Field, "alias", StringComparison.Ordinal))
        {
            issues.Add($"Alias override '{entry.Id}' must use field 'alias'.");
        }

        if (!TargetExists(entry, countries, subdivisions, historical))
        {
            issues.Add(
                $"Alias override '{entry.Id}' targets unknown {entry.TargetKind} " +
                $"'{entry.TargetKey}'.");
        }
    }

    private static void ValidateSupplement(
        OverrideEntry entry,
        IReadOnlyDictionary<string, StagedCountry> countries,
        IReadOnlyDictionary<string, StagedSubdivision> subdivisions,
        IReadOnlyDictionary<string, StagedHistoricalCountry> historical,
        List<string> issues)
    {
        if (string.Equals(entry.TargetKind, "country", StringComparison.Ordinal))
        {
            ValidateCountrySupplement(entry, countries, issues);
            return;
        }

        if (string.Equals(entry.TargetKind, "subdivision", StringComparison.Ordinal))
        {
            ValidateSubdivisionSupplement(entry, subdivisions, issues);
            return;
        }

        if (string.Equals(entry.TargetKind, "historical-country", StringComparison.Ordinal))
        {
            ValidateHistoricalSupplement(entry, historical, countries, issues);
            return;
        }

        issues.Add(
            $"Supplement override '{entry.Id}' has unsupported target kind " +
            $"'{entry.TargetKind}'.");
    }

    private static void ValidateCountrySupplement(
        OverrideEntry entry,
        IReadOnlyDictionary<string, StagedCountry> countries,
        List<string> issues)
    {
        if (!countries.TryGetValue(entry.TargetKey, out StagedCountry? country))
        {
            issues.Add($"Override '{entry.Id}' targets unknown country '{entry.TargetKey}'.");
            return;
        }

        if (string.Equals(entry.Field, "official-name", StringComparison.Ordinal))
        {
            if (country.OfficialName != null)
            {
                issues.Add(
                    $"Override '{entry.Id}' cannot displace authoritative official name for " +
                    $"'{entry.TargetKey}'.");
            }
        }
        else if (string.Equals(entry.Field, "common-name", StringComparison.Ordinal))
        {
            if (country.CommonName != null)
            {
                issues.Add(
                    $"Override '{entry.Id}' cannot displace authoritative common name for " +
                    $"'{entry.TargetKey}'.");
            }
        }
        else
        {
            issues.Add(
                $"Override '{entry.Id}' cannot supplement country field '{entry.Field}'.");
        }

        RequireSingleValue(entry, issues);
    }

    private static void ValidateSubdivisionSupplement(
        OverrideEntry entry,
        IReadOnlyDictionary<string, StagedSubdivision> subdivisions,
        List<string> issues)
    {
        if (!subdivisions.TryGetValue(entry.TargetKey, out StagedSubdivision? subdivision))
        {
            issues.Add($"Override '{entry.Id}' targets unknown subdivision '{entry.TargetKey}'.");
            return;
        }

        if (!string.Equals(entry.Field, "parent", StringComparison.Ordinal))
        {
            issues.Add(
                $"Override '{entry.Id}' cannot supplement subdivision field '{entry.Field}'.");
            return;
        }

        RequireSingleValue(entry, issues);
        if (subdivision.Parent != null)
        {
            issues.Add(
                $"Override '{entry.Id}' cannot displace authoritative parent " +
                $"'{subdivision.Parent.Value}'.");
            return;
        }

        if (entry.Values.Count == 1)
        {
            string parent = entry.Values[0];
            if (!subdivisions.TryGetValue(parent, out StagedSubdivision? parentRow))
            {
                issues.Add($"Override '{entry.Id}' references unknown parent '{parent}'.");
            }
            else if (!string.Equals(
                         subdivision.CountryCode.Value,
                         parentRow.CountryCode.Value,
                         StringComparison.Ordinal))
            {
                issues.Add($"Override '{entry.Id}' references cross-country parent '{parent}'.");
            }
            else
            {
                ValidateParentCycle(entry, subdivision, parentRow, subdivisions, issues);
            }
        }
    }

    private static void ValidateParentCycle(
        OverrideEntry entry,
        StagedSubdivision subdivision,
        StagedSubdivision parent,
        IReadOnlyDictionary<string, StagedSubdivision> subdivisions,
        List<string> issues)
    {
        HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal)
        {
            subdivision.Code.Value
        };
        StagedSubdivision current = parent;
        while (true)
        {
            if (!visited.Add(current.Code.Value))
            {
                issues.Add($"Override '{entry.Id}' would create a subdivision parent cycle.");
                return;
            }

            if (current.Parent == null ||
                !subdivisions.TryGetValue(current.Parent.Value, out StagedSubdivision? next))
            {
                return;
            }

            current = next;
        }
    }

    private static void ValidateHistoricalSupplement(
        OverrideEntry entry,
        IReadOnlyDictionary<string, StagedHistoricalCountry> historical,
        IReadOnlyDictionary<string, StagedCountry> countries,
        List<string> issues)
    {
        if (!historical.TryGetValue(entry.TargetKey, out StagedHistoricalCountry? row))
        {
            issues.Add(
                $"Override '{entry.Id}' targets unknown historical country '{entry.TargetKey}'.");
            return;
        }

        if (string.Equals(entry.Field, "withdrawal-date", StringComparison.Ordinal))
        {
            RequireSingleValue(entry, issues);
            if (row.WithdrawalDate != null)
            {
                issues.Add(
                    $"Override '{entry.Id}' cannot displace authoritative withdrawal date " +
                    $"'{row.WithdrawalDate.Value}'.");
            }
            else if (entry.Values.Count == 1 &&
                     !ReconciliationSyntax.IsCanonicalDate(entry.Values[0]))
            {
                issues.Add($"Override '{entry.Id}' has invalid withdrawal date '{entry.Values[0]}'.");
            }
        }
        else if (string.Equals(entry.Field, "transition-replacement", StringComparison.Ordinal))
        {
            foreach (string replacement in entry.Values)
            {
                if (!countries.ContainsKey(replacement))
                {
                    issues.Add(
                        $"Override '{entry.Id}' references unknown current successor " +
                        $"'{replacement}'.");
                }
            }
        }
        else
        {
            issues.Add(
                $"Override '{entry.Id}' cannot supplement historical field '{entry.Field}'.");
        }
    }

    private static bool TargetExists(
        OverrideEntry entry,
        IReadOnlyDictionary<string, StagedCountry> countries,
        IReadOnlyDictionary<string, StagedSubdivision> subdivisions,
        IReadOnlyDictionary<string, StagedHistoricalCountry> historical)
    {
        if (string.Equals(entry.TargetKind, "country", StringComparison.Ordinal))
        {
            return countries.ContainsKey(entry.TargetKey);
        }

        if (string.Equals(entry.TargetKind, "subdivision", StringComparison.Ordinal))
        {
            return subdivisions.ContainsKey(entry.TargetKey);
        }

        return string.Equals(entry.TargetKind, "historical-country", StringComparison.Ordinal) &&
               historical.ContainsKey(entry.TargetKey);
    }

    private static void ValidateConflicts(
        IReadOnlyList<OverrideEntry> entries,
        List<string> issues)
    {
        Dictionary<string, OverrideEntry> singletonFields = new Dictionary<string, OverrideEntry>(
            StringComparer.Ordinal);
        HashSet<string> aliases = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> sourceDeltas = new HashSet<string>(StringComparer.Ordinal);
        foreach (OverrideEntry entry in entries)
        {
            if (string.Equals(entry.Kind, "source-delta", StringComparison.Ordinal))
            {
                string key = entry.Provenance.SourceId + ":" + entry.Provenance.UpstreamKey + ":" +
                             entry.Field;
                if (!sourceDeltas.Add(key))
                {
                    issues.Add($"Conflicting source-delta overrides share key '{key}'.");
                }

                continue;
            }

            if (string.Equals(entry.Kind, "alias", StringComparison.Ordinal))
            {
                foreach (string value in entry.Values)
                {
                    string key = entry.TargetKind + ":" + entry.TargetKey + ":" + value;
                    if (!aliases.Add(key))
                    {
                        issues.Add($"Duplicate alias override value '{key}'.");
                    }
                }

                continue;
            }

            string fieldKey = entry.TargetKind + ":" + entry.TargetKey + ":" + entry.Field;
            if (singletonFields.TryGetValue(fieldKey, out OverrideEntry? existing))
            {
                issues.Add(
                    $"Overrides '{existing.Id}' and '{entry.Id}' conflict for '{fieldKey}'.");
            }
            else
            {
                singletonFields.Add(fieldKey, entry);
            }
        }
    }

    private static Dictionary<string, StagedCountry> IndexCountries(GeneratorStagingData staging)
    {
        Dictionary<string, StagedCountry> result = new Dictionary<string, StagedCountry>(
            StringComparer.Ordinal);
        foreach (StagedCountry country in staging.Countries)
        {
            result.Add(country.Alpha2.Value, country);
        }

        return result;
    }

    private static Dictionary<string, StagedSubdivision> IndexSubdivisions(
        GeneratorStagingData staging)
    {
        Dictionary<string, StagedSubdivision> result =
            new Dictionary<string, StagedSubdivision>(StringComparer.Ordinal);
        foreach (StagedSubdivision subdivision in staging.Subdivisions)
        {
            result.Add(subdivision.Code.Value, subdivision);
        }

        return result;
    }

    private static Dictionary<string, StagedHistoricalCountry> IndexHistorical(
        GeneratorStagingData staging)
    {
        Dictionary<string, StagedHistoricalCountry> result =
            new Dictionary<string, StagedHistoricalCountry>(StringComparer.Ordinal);
        foreach (StagedHistoricalCountry row in staging.HistoricalCountries)
        {
            result.Add(row.Alpha4.Value, row);
        }

        return result;
    }

    private static void RequireSingleValue(OverrideEntry entry, List<string> issues)
    {
        if (entry.Values.Count != 1)
        {
            issues.Add($"Override '{entry.Id}' field '{entry.Field}' requires exactly one value.");
        }
    }

    private static int CompareEntries(OverrideEntry left, OverrideEntry right)
    {
        int comparison = string.CompareOrdinal(left.TargetKind, right.TargetKind);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = string.CompareOrdinal(left.TargetKey, right.TargetKey);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = string.CompareOrdinal(left.Field, right.Field);
        return comparison != 0 ? comparison : string.CompareOrdinal(left.Id, right.Id);
    }
}
