using System;
using System.Collections.Generic;
using CountryCodes.DataGenerator.Overrides;
using CountryCodes.DataGenerator.Staging;

namespace CountryCodes.DataGenerator.Reconciliation;

internal static class SourceReconciler
{
    internal static ReconciliationResult Reconcile(
        GeneratorStagingData staging,
        OverrideCatalog overrides)
    {
        ReconciliationIssues issues = new ReconciliationIssues();
        Dictionary<string, StagedCountry> countriesByAlpha2 = ValidateCountries(staging, issues);
        Dictionary<string, StagedCountry> countriesByAlpha3 = IndexCountriesByAlpha3(staging);
        Dictionary<string, StagedCountry> countriesByNumeric = IndexCountriesByNumeric(staging);
        Dictionary<string, StagedSubdivision> subdivisions = ValidateSubdivisions(
            staging,
            countriesByAlpha2,
            issues);

        ValidateHistoricalCountries(staging, countriesByAlpha2, issues);
        ValidateSupplementalSubdivisionPrefixes(staging, countriesByAlpha2, issues);
        Dictionary<string, bool> registeredLanguages = ReadRegisteredLanguages(staging, issues);
        int localeCount = ValidateLocalesAndDates(staging, registeredLanguages, issues);
        List<ExplainedSourceDelta> sourceDeltas = new List<ExplainedSourceDelta>();
        HashSet<string> usedDeltaOverrides = new HashSet<string>(StringComparer.Ordinal);
        int countryCrossCheckCount = ValidateM49Agreement(
            staging,
            countriesByAlpha2,
            countriesByAlpha3,
            countriesByNumeric,
            overrides,
            usedDeltaOverrides,
            sourceDeltas,
            issues);
        int regionNodeCount = ValidateRegionHierarchy(staging, issues);
        int subdivisionParentCrossCheckCount = ValidateCldrContainment(
            staging,
            countriesByAlpha2,
            subdivisions,
            overrides,
            usedDeltaOverrides,
            sourceDeltas,
            issues);
        int transitionCount = ValidateTransitions(
            staging,
            countriesByAlpha2,
            countriesByAlpha3,
            countriesByNumeric,
            subdivisions,
            overrides,
            usedDeltaOverrides,
            sourceDeltas,
            issues);
        ValidateAllDeltaOverridesUsed(overrides, usedDeltaOverrides, issues);

        issues.ThrowIfAny();
        return new ReconciliationResult(
            staging,
            countryCrossCheckCount,
            subdivisionParentCrossCheckCount,
            regionNodeCount,
            transitionCount,
            localeCount,
            sourceDeltas);
    }

    private static Dictionary<string, StagedCountry> ValidateCountries(
        GeneratorStagingData staging,
        ReconciliationIssues issues)
    {
        Dictionary<string, StagedCountry> alpha2 = new Dictionary<string, StagedCountry>(
            StringComparer.Ordinal);
        Dictionary<string, StagedCountry> alpha3 = new Dictionary<string, StagedCountry>(
            StringComparer.Ordinal);
        Dictionary<string, StagedCountry> numeric = new Dictionary<string, StagedCountry>(
            StringComparer.Ordinal);
        HashSet<string> triples = new HashSet<string>(StringComparer.Ordinal);

        foreach (StagedCountry country in staging.Countries)
        {
            string alpha2Value = country.Alpha2.Value;
            string alpha3Value = country.Alpha3.Value;
            string numericValue = country.Numeric.Value;
            RequireShape(
                ReconciliationSyntax.IsAlpha2(alpha2Value),
                $"Country alpha-2 '{alpha2Value}' is not two uppercase ASCII letters.",
                issues);
            RequireShape(
                ReconciliationSyntax.IsAlpha3(alpha3Value),
                $"Country alpha-3 '{alpha3Value}' is not three uppercase ASCII letters.",
                issues);
            RequireShape(
                ReconciliationSyntax.IsNumeric3(numericValue, false),
                $"Country numeric code '{numericValue}' is not in 001-899.",
                issues);
            AddUnique(alpha2, alpha2Value, country, "country alpha-2", issues);
            AddUnique(alpha3, alpha3Value, country, "country alpha-3", issues);
            AddUnique(numeric, numericValue, country, "country numeric code", issues);
            string triple = alpha2Value + ":" + alpha3Value + ":" + numericValue;
            if (!triples.Add(triple))
            {
                issues.Add($"Duplicate country code triple '{triple}'.");
            }
        }

        return alpha2;
    }

    private static Dictionary<string, StagedCountry> IndexCountriesByAlpha3(
        GeneratorStagingData staging)
    {
        Dictionary<string, StagedCountry> result = new Dictionary<string, StagedCountry>(
            StringComparer.Ordinal);
        foreach (StagedCountry country in staging.Countries)
        {
            result.TryAdd(country.Alpha3.Value, country);
        }

        return result;
    }

    private static Dictionary<string, StagedCountry> IndexCountriesByNumeric(
        GeneratorStagingData staging)
    {
        Dictionary<string, StagedCountry> result = new Dictionary<string, StagedCountry>(
            StringComparer.Ordinal);
        foreach (StagedCountry country in staging.Countries)
        {
            result.TryAdd(country.Numeric.Value, country);
        }

        return result;
    }

    private static Dictionary<string, StagedSubdivision> ValidateSubdivisions(
        GeneratorStagingData staging,
        IReadOnlyDictionary<string, StagedCountry> countries,
        ReconciliationIssues issues)
    {
        Dictionary<string, StagedSubdivision> subdivisions =
            new Dictionary<string, StagedSubdivision>(StringComparer.Ordinal);
        foreach (StagedSubdivision subdivision in staging.Subdivisions)
        {
            string code = subdivision.Code.Value;
            if (!ReconciliationSyntax.IsSubdivision(code))
            {
                issues.Add($"Subdivision code '{code}' has an illegal ISO 3166-2 shape.");
            }

            if (!countries.ContainsKey(subdivision.CountryCode.Value))
            {
                issues.Add(
                    $"Subdivision '{code}' has unknown country prefix " +
                    $"'{subdivision.CountryCode.Value}'.");
            }

            AddUnique(subdivisions, code, subdivision, "subdivision code", issues);
        }

        Dictionary<string, string> parents = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (StagedSubdivision subdivision in staging.Subdivisions)
        {
            if (subdivision.Parent == null)
            {
                continue;
            }

            string code = subdivision.Code.Value;
            string parent = subdivision.Parent.Value;
            if (!subdivisions.ContainsKey(parent))
            {
                issues.Add($"Subdivision '{code}' has unknown parent '{parent}'.");
                continue;
            }

            if (parent.Length < 2 ||
                !string.Equals(
                    subdivision.CountryCode.Value,
                    parent.Substring(0, 2),
                    StringComparison.Ordinal))
            {
                issues.Add($"Subdivision '{code}' has cross-country parent '{parent}'.");
            }

            if (string.Equals(code, parent, StringComparison.Ordinal))
            {
                issues.Add($"Subdivision '{code}' is its own parent.");
            }

            parents[code] = parent;
        }

        ValidateAcyclic(parents, "subdivision", issues);
        return subdivisions;
    }

    private static void ValidateHistoricalCountries(
        GeneratorStagingData staging,
        IReadOnlyDictionary<string, StagedCountry> countries,
        ReconciliationIssues issues)
    {
        HashSet<string> alpha4Codes = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> historicalAlpha2 = new HashSet<string>(StringComparer.Ordinal);
        foreach (StagedHistoricalCountry historical in staging.HistoricalCountries)
        {
            historicalAlpha2.Add(historical.Alpha2.Value);
        }

        foreach (StagedHistoricalCountry historical in staging.HistoricalCountries)
        {
            string alpha2 = historical.Alpha2.Value;
            string alpha3 = historical.Alpha3.Value;
            string alpha4 = historical.Alpha4.Value;
            RequireShape(
                ReconciliationSyntax.IsAlpha2(alpha2),
                $"Historical alpha-2 '{alpha2}' has an illegal shape.",
                issues);
            RequireShape(
                ReconciliationSyntax.IsAlpha3(alpha3),
                $"Historical alpha-3 '{alpha3}' has an illegal shape.",
                issues);
            RequireShape(
                ReconciliationSyntax.IsAlpha4(alpha4),
                $"Historical alpha-4 '{alpha4}' has an illegal shape.",
                issues);
            if (alpha4.Length >= 2 &&
                !string.Equals(alpha4.Substring(0, 2), alpha2, StringComparison.Ordinal))
            {
                issues.Add($"Historical alpha-4 '{alpha4}' does not start with '{alpha2}'.");
            }

            if (!alpha4Codes.Add(alpha4))
            {
                issues.Add($"Duplicate historical alpha-4 '{alpha4}'.");
            }

            if (alpha4.Length == 4)
            {
                string successor = alpha4.Substring(2, 2);
                bool specialTransition = string.Equals(successor, "AA", StringComparison.Ordinal) ||
                                         string.Equals(successor, "HH", StringComparison.Ordinal) ||
                                         string.Equals(successor, "XX", StringComparison.Ordinal);
                if (!specialTransition &&
                    !countries.ContainsKey(successor) &&
                    !historicalAlpha2.Contains(successor))
                {
                    issues.Add(
                        $"Historical alpha-4 '{alpha4}' has unknown successor code " +
                        $"'{successor}'.");
                }
            }

            if (historical.Numeric != null &&
                !ReconciliationSyntax.IsNumeric3(historical.Numeric.Value, false))
            {
                issues.Add($"Historical numeric code '{historical.Numeric.Value}' is not in 001-899.");
            }

            if (historical.WithdrawalDate != null &&
                !ReconciliationSyntax.IsCanonicalDate(historical.WithdrawalDate.Value))
            {
                issues.Add(
                    $"Historical record '{alpha4}' has invalid withdrawal date " +
                    $"'{historical.WithdrawalDate.Value}'.");
            }
        }
    }

    private static Dictionary<string, bool> ReadRegisteredLanguages(
        GeneratorStagingData staging,
        ReconciliationIssues issues)
    {
        Dictionary<string, bool> languages = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (StagedIanaRecord record in staging.IanaRecords)
        {
            string? type = FindIanaField(record, "Type");
            if (!string.Equals(type, "language", StringComparison.Ordinal))
            {
                continue;
            }

            string? subtag = FindIanaField(record, "Subtag");
            if (subtag != null && TryAddLanguageRange(subtag, languages))
            {
                continue;
            }

            if (subtag == null || subtag.Length < 2 || subtag.Length > 8 || !IsAsciiLetters(subtag))
            {
                issues.Add($"IANA language record '{record.Origin.UpstreamKey}' has an invalid subtag.");
                continue;
            }

            string normalized = IdentifierStagingNormalizer.LowerAscii(subtag);
            if (!languages.TryAdd(normalized, true))
            {
                issues.Add($"IANA registry contains duplicate language subtag '{normalized}'.");
            }
        }

        if (languages.Count == 0)
        {
            issues.Add("IANA registry contains no language subtags for locale validation.");
        }

        return languages;
    }

    private static int ValidateLocalesAndDates(
        GeneratorStagingData staging,
        IReadOnlyDictionary<string, bool> registeredLanguages,
        ReconciliationIssues issues)
    {
        HashSet<string> locales = new HashSet<string>(StringComparer.Ordinal);
        foreach (StagingOrigin origin in EnumerateOrigins(staging))
        {
            if (!ReconciliationSyntax.IsCanonicalDate(origin.EffectiveDate))
            {
                issues.Add(
                    $"Origin '{origin.SourceId}:{origin.UpstreamKey}' has invalid effective date " +
                    $"'{origin.EffectiveDate}'.");
            }

            if (origin.Locale == null)
            {
                continue;
            }

            if (!ReconciliationSyntax.IsBcp47Tag(origin.Locale, registeredLanguages))
            {
                issues.Add(
                    $"Origin '{origin.SourceId}:{origin.UpstreamKey}' has invalid or " +
                    $"unregistered BCP 47 locale '{origin.Locale}'.");
            }

            locales.Add(origin.Locale);
        }

        foreach (StagedM49History history in staging.M49History)
        {
            if (!IsM49HistoryCodeSet(history.M49Code.Value))
            {
                issues.Add(
                    $"M49 history '{history.Origin.UpstreamKey}' has invalid code " +
                    $"'{history.M49Code.Value}'.");
            }

            if (history.ChangeDate != null &&
                !ReconciliationSyntax.IsCanonicalDate(history.ChangeDate.Value))
            {
                issues.Add(
                    $"M49 history '{history.Origin.UpstreamKey}' has invalid change date " +
                    $"'{history.ChangeDate.Value}'.");
            }
        }

        return locales.Count;
    }

    private static int ValidateM49Agreement(
        GeneratorStagingData staging,
        IReadOnlyDictionary<string, StagedCountry> countriesByAlpha2,
        IReadOnlyDictionary<string, StagedCountry> countriesByAlpha3,
        IReadOnlyDictionary<string, StagedCountry> countriesByNumeric,
        OverrideCatalog overrides,
        HashSet<string> usedDeltaOverrides,
        List<ExplainedSourceDelta> sourceDeltas,
        ReconciliationIssues issues)
    {
        Dictionary<string, HashSet<string>> namesByLocale =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        Dictionary<string, HashSet<string>> hierarchyByLocale =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        HashSet<string> crossChecked = new HashSet<string>(StringComparer.Ordinal);

        foreach (StagedName name in staging.Names)
        {
            if (name.Target != StagedNameTarget.UnM49Country || name.Alpha3 == null)
            {
                continue;
            }

            string alpha3 = name.Alpha3.Value;
            string numeric = name.EntityKey.Value;
            string locale = name.Origin.Locale ?? string.Empty;
            HashSet<string> localeKeys = GetLocaleKeys(namesByLocale, locale);
            if (!localeKeys.Add(alpha3))
            {
                issues.Add($"UN M49 locale '{locale}' duplicates country '{alpha3}'.");
            }

            ValidateM49Triple(alpha3, numeric, null, countriesByAlpha3, countriesByNumeric, issues);
            if (countriesByAlpha3.ContainsKey(alpha3))
            {
                crossChecked.Add(alpha3);
            }
        }

        foreach (StagedM49Hierarchy hierarchy in staging.M49Hierarchy)
        {
            string alpha2 = hierarchy.Alpha2.Value;
            string alpha3 = hierarchy.Alpha3.Value;
            string numeric = hierarchy.M49Code.Value;
            string locale = hierarchy.Origin.Locale ?? string.Empty;
            HashSet<string> localeKeys = GetLocaleKeys(hierarchyByLocale, locale);
            if (!localeKeys.Add(alpha3))
            {
                issues.Add($"UN M49 hierarchy locale '{locale}' duplicates country '{alpha3}'.");
            }

            ValidateM49Triple(
                alpha3,
                numeric,
                alpha2,
                countriesByAlpha3,
                countriesByNumeric,
                issues);
            if (!countriesByAlpha2.ContainsKey(alpha2))
            {
                issues.Add($"UN M49 hierarchy references unknown alpha-2 '{alpha2}'.");
            }
        }

        ValidateLocaleKeyAgreement(namesByLocale, hierarchyByLocale, issues);
        ValidateM49Completeness(
            countriesByAlpha3,
            namesByLocale,
            hierarchyByLocale,
            overrides,
            usedDeltaOverrides,
            sourceDeltas,
            issues);
        return crossChecked.Count;
    }

    private static int ValidateRegionHierarchy(
        GeneratorStagingData staging,
        ReconciliationIssues issues)
    {
        Dictionary<string, RegionNodeShape> nodes = new Dictionary<string, RegionNodeShape>(
            StringComparer.Ordinal);
        foreach (StagedM49Hierarchy row in staging.M49Hierarchy)
        {
            ValidateRegionCode(row.GlobalCode.Value, "global", row.Origin.UpstreamKey, issues);
            AddRegionNode(nodes, row.GlobalCode.Value, null, 0, issues);
            ValidateOptionalRegionPair(row.RegionCode, row.RegionName, "region", row, issues);
            ValidateOptionalRegionPair(row.SubregionCode, row.SubregionName, "subregion", row, issues);
            ValidateOptionalRegionPair(
                row.IntermediateRegionCode,
                row.IntermediateRegionName,
                "intermediate region",
                row,
                issues);

            if (row.SubregionCode != null && row.RegionCode == null)
            {
                issues.Add($"M49 country '{row.M49Code.Value}' has a subregion without a region.");
            }

            if (row.IntermediateRegionCode != null && row.SubregionCode == null)
            {
                issues.Add(
                    $"M49 country '{row.M49Code.Value}' has an intermediate region " +
                    "without a subregion.");
            }

            if (row.RegionCode != null)
            {
                AddRegionNode(nodes, row.RegionCode.Value, row.GlobalCode.Value, 1, issues);
            }

            if (row.SubregionCode != null && row.RegionCode != null)
            {
                AddRegionNode(nodes, row.SubregionCode.Value, row.RegionCode.Value, 2, issues);
            }

            if (row.IntermediateRegionCode != null && row.SubregionCode != null)
            {
                AddRegionNode(
                    nodes,
                    row.IntermediateRegionCode.Value,
                    row.SubregionCode.Value,
                    3,
                    issues);
            }
        }

        Dictionary<string, string> parents = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, RegionNodeShape> pair in nodes)
        {
            if (pair.Value.Parent != null)
            {
                parents[pair.Key] = pair.Value.Parent;
            }
        }

        ValidateAcyclic(parents, "M49 region", issues);
        return nodes.Count;
    }

    private static int ValidateCldrContainment(
        GeneratorStagingData staging,
        IReadOnlyDictionary<string, StagedCountry> countries,
        IReadOnlyDictionary<string, StagedSubdivision> subdivisions,
        OverrideCatalog overrides,
        HashSet<string> usedDeltaOverrides,
        List<ExplainedSourceDelta> sourceDeltas,
        ReconciliationIssues issues)
    {
        HashSet<string> aliasCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (StagedAlias alias in staging.Aliases)
        {
            if (alias.Target == StagedAliasTarget.Subdivision)
            {
                aliasCodes.Add(alias.Code.Value);
            }
        }

        Dictionary<string, string> cldrParents = new Dictionary<string, string>(StringComparer.Ordinal);
        int crossChecks = 0;
        foreach (StagedContainment containment in staging.Containment)
        {
            string parent = containment.Parent.Value;
            bool parentIsCountry = countries.ContainsKey(parent);
            bool parentIsSubdivision = subdivisions.ContainsKey(parent);
            bool parentIsAlias = aliasCodes.Contains(parent);
            if (!parentIsCountry && !parentIsSubdivision && !parentIsAlias)
            {
                issues.Add($"CLDR containment has unknown parent '{parent}'.");
            }

            foreach (StagedText childField in containment.Children)
            {
                string child = childField.Value;
                bool childIsSubdivision = subdivisions.TryGetValue(child, out StagedSubdivision? row);
                if (!childIsSubdivision && !aliasCodes.Contains(child))
                {
                    issues.Add($"CLDR containment parent '{parent}' has unknown child '{child}'.");
                    continue;
                }

                if (cldrParents.TryGetValue(child, out string? existingParent) &&
                    !string.Equals(existingParent, parent, StringComparison.Ordinal))
                {
                    issues.Add(
                        $"CLDR containment child '{child}' has parents '{existingParent}' and '{parent}'.");
                }
                else
                {
                    cldrParents[child] = parent;
                }

                if (!childIsSubdivision || row == null)
                {
                    continue;
                }

                string? debianParent = row.Parent?.Value;
                if (parentIsCountry)
                {
                    if (debianParent != null)
                    {
                        issues.Add(
                            $"CLDR places subdivision '{child}' below country '{parent}', " +
                            $"but Debian declares parent '{debianParent}'.");
                    }
                    else if (!string.Equals(row.CountryCode.Value, parent, StringComparison.Ordinal))
                    {
                        issues.Add(
                            $"CLDR places subdivision '{child}' below wrong country '{parent}'.");
                    }
                    else
                    {
                        crossChecks += 1;
                    }
                }
                else if (parentIsSubdivision)
                {
                    if (debianParent != null &&
                        !string.Equals(debianParent, parent, StringComparison.Ordinal))
                    {
                        ExplainSourceDelta(
                            overrides,
                            usedDeltaOverrides,
                            "cldr-48-2",
                            child,
                            "subdivision-parent",
                            new[] { debianParent, parent },
                            sourceDeltas,
                            issues);
                    }
                    else
                    {
                        crossChecks += 1;
                    }
                }
            }
        }

        Dictionary<string, string> subdivisionParents =
            new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> pair in cldrParents)
        {
            if (ReconciliationSyntax.IsSubdivision(pair.Key) &&
                ReconciliationSyntax.IsSubdivision(pair.Value))
            {
                subdivisionParents[pair.Key] = pair.Value;
            }
        }

        ValidateAcyclic(subdivisionParents, "CLDR subdivision", issues);
        return crossChecks;
    }

    private static int ValidateTransitions(
        GeneratorStagingData staging,
        IReadOnlyDictionary<string, StagedCountry> countriesByAlpha2,
        IReadOnlyDictionary<string, StagedCountry> countriesByAlpha3,
        IReadOnlyDictionary<string, StagedCountry> countriesByNumeric,
        IReadOnlyDictionary<string, StagedSubdivision> subdivisions,
        OverrideCatalog overrides,
        HashSet<string> usedDeltaOverrides,
        List<ExplainedSourceDelta> sourceDeltas,
        ReconciliationIssues issues)
    {
        HashSet<string> territoryIdentifiers = new HashSet<string>(StringComparer.Ordinal);
        foreach (StagedName name in staging.Names)
        {
            if (name.Target == StagedNameTarget.CldrTerritory)
            {
                territoryIdentifiers.Add(name.EntityKey.Value);
            }
        }

        foreach (string code in countriesByAlpha2.Keys)
        {
            territoryIdentifiers.Add(code);
        }

        HashSet<string> subdivisionIdentifiers = new HashSet<string>(subdivisions.Keys, StringComparer.Ordinal);
        foreach (StagedAlias alias in staging.Aliases)
        {
            if (alias.Target == StagedAliasTarget.Territory)
            {
                territoryIdentifiers.Add(alias.Code.Value);
            }
            else
            {
                subdivisionIdentifiers.Add(alias.Code.Value);
            }
        }

        HashSet<string> aliasKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (StagedAlias alias in staging.Aliases)
        {
            string scope = alias.Target == StagedAliasTarget.Territory
                ? "territory"
                : "subdivision";
            string aliasKey = scope + ":" + alias.Code.Value;
            if (!aliasKeys.Add(aliasKey))
            {
                issues.Add($"Duplicate CLDR {scope} transition '{alias.Code.Value}'.");
            }
        }

        Dictionary<string, string> aliasEdges = new Dictionary<string, string>(StringComparer.Ordinal);
        int transitions = staging.HistoricalCountries.Count + staging.M49History.Count;
        foreach (StagedAlias alias in staging.Aliases)
        {
            string scope = alias.Target == StagedAliasTarget.Territory ? "territory" : "subdivision";
            string aliasKey = scope + ":" + alias.Code.Value;
            ValidateAliasSource(alias, issues);
            HashSet<string> replacements = new HashSet<string>(StringComparer.Ordinal);
            foreach (StagedText replacementField in alias.Replacements)
            {
                string replacement = replacementField.Value;
                transitions += 1;
                if (!replacements.Add(replacement))
                {
                    issues.Add(
                        $"CLDR {scope} transition '{alias.Code.Value}' repeats " +
                        $"replacement '{replacement}'.");
                }

                bool knownTarget = alias.Target == StagedAliasTarget.Territory
                    ? territoryIdentifiers.Contains(replacement) ||
                      IsUserAssignedAlpha2(replacement)
                    : subdivisionIdentifiers.Contains(replacement) ||
                      territoryIdentifiers.Contains(replacement);
                if (!knownTarget)
                {
                    if (alias.Target == StagedAliasTarget.Subdivision)
                    {
                        ExplainSourceDelta(
                            overrides,
                            usedDeltaOverrides,
                            "cldr-48-2",
                            alias.Code.Value,
                            "subdivision-transition",
                            new[] { replacement },
                            sourceDeltas,
                            issues);
                    }
                    else
                    {
                        issues.Add($"CLDR {scope} transition '{alias.Code.Value}' has unknown replacement '{replacement}'.");
                    }
                }

                if (string.Equals(alias.Code.Value, replacement, StringComparison.Ordinal))
                {
                    issues.Add($"CLDR {scope} transition '{alias.Code.Value}' is self-referential.");
                }

                string replacementKey = scope + ":" + replacement;
                if (alias.Replacements.Count == 1 && aliasKeys.Contains(replacementKey))
                {
                    aliasEdges[aliasKey] = replacementKey;
                }
            }
        }

        ValidateCurrentCountryAliases(
            staging,
            countriesByAlpha3,
            countriesByNumeric,
            issues);
        ValidateAcyclic(aliasEdges, "CLDR transition", issues);
        return transitions;
    }

    private static void ValidateCurrentCountryAliases(
        GeneratorStagingData staging,
        IReadOnlyDictionary<string, StagedCountry> countriesByAlpha3,
        IReadOnlyDictionary<string, StagedCountry> countriesByNumeric,
        ReconciliationIssues issues)
    {
        Dictionary<string, StagedAlias> aliases = new Dictionary<string, StagedAlias>(
            StringComparer.Ordinal);
        foreach (StagedAlias alias in staging.Aliases)
        {
            if (alias.Target == StagedAliasTarget.Territory)
            {
                aliases.TryAdd(alias.Code.Value, alias);
            }
        }

        foreach (KeyValuePair<string, StagedCountry> pair in countriesByAlpha3)
        {
            ValidateIdentityAlias(pair.Key, pair.Value.Alpha2.Value, aliases, issues);
        }

        foreach (KeyValuePair<string, StagedCountry> pair in countriesByNumeric)
        {
            ValidateIdentityAlias(pair.Key, pair.Value.Alpha2.Value, aliases, issues);
        }
    }

    private static void ValidateIdentityAlias(
        string source,
        string expectedAlpha2,
        IReadOnlyDictionary<string, StagedAlias> aliases,
        ReconciliationIssues issues)
    {
        if (!aliases.TryGetValue(source, out StagedAlias? alias))
        {
            issues.Add($"CLDR has no identity alias from '{source}' to '{expectedAlpha2}'.");
            return;
        }

        if (alias.Replacements.Count != 1 ||
            !string.Equals(alias.Replacements[0].Value, expectedAlpha2, StringComparison.Ordinal))
        {
            issues.Add(
                $"CLDR identity alias '{source}' does not resolve exactly to '{expectedAlpha2}'.");
        }
    }

    private static void ValidateAliasSource(StagedAlias alias, ReconciliationIssues issues)
    {
        string code = alias.Code.Value;
        if (alias.Target == StagedAliasTarget.Territory)
        {
            bool valid = ReconciliationSyntax.IsAlpha2(code) ||
                         ReconciliationSyntax.IsAlpha3(code) ||
                         ReconciliationSyntax.IsNumeric3(code, true);
            if (!valid)
            {
                issues.Add($"CLDR territory transition source '{code}' has an illegal shape.");
            }
        }
        else if (!ReconciliationSyntax.IsSubdivision(code))
        {
            issues.Add($"CLDR subdivision transition source '{code}' has an illegal shape.");
        }
    }

    private static void ValidateSupplementalSubdivisionPrefixes(
        GeneratorStagingData staging,
        IReadOnlyDictionary<string, StagedCountry> countries,
        ReconciliationIssues issues)
    {
        HashSet<string> knownPrefixes = new HashSet<string>(countries.Keys, StringComparer.Ordinal);
        foreach (StagedHistoricalCountry historical in staging.HistoricalCountries)
        {
            knownPrefixes.Add(historical.Alpha2.Value);
        }

        foreach (StagedName name in staging.Names)
        {
            if (name.Target == StagedNameTarget.CldrTerritory &&
                ReconciliationSyntax.IsAlpha2(name.EntityKey.Value))
            {
                knownPrefixes.Add(name.EntityKey.Value);
            }
        }

        foreach (StagedAlias alias in staging.Aliases)
        {
            if (alias.Target == StagedAliasTarget.Territory &&
                ReconciliationSyntax.IsAlpha2(alias.Code.Value))
            {
                knownPrefixes.Add(alias.Code.Value);
            }
        }

        foreach (StagedName name in staging.Names)
        {
            if (name.Target == StagedNameTarget.CldrSubdivision)
            {
                ValidateSupplementalSubdivisionCode(
                    name.EntityKey.Value,
                    "CLDR subdivision name",
                    knownPrefixes,
                    issues);
            }
        }

        foreach (StagedAlias alias in staging.Aliases)
        {
            if (alias.Target != StagedAliasTarget.Subdivision)
            {
                continue;
            }

            ValidateSupplementalSubdivisionCode(
                alias.Code.Value,
                "CLDR subdivision transition",
                knownPrefixes,
                issues);
            foreach (StagedText replacement in alias.Replacements)
            {
                if (ReconciliationSyntax.IsSubdivision(replacement.Value))
                {
                    ValidateSupplementalSubdivisionCode(
                        replacement.Value,
                        "CLDR subdivision replacement",
                        knownPrefixes,
                        issues);
                }
            }
        }

        foreach (StagedContainment containment in staging.Containment)
        {
            if (ReconciliationSyntax.IsSubdivision(containment.Parent.Value))
            {
                ValidateSupplementalSubdivisionCode(
                    containment.Parent.Value,
                    "CLDR containment parent",
                    knownPrefixes,
                    issues);
            }

            foreach (StagedText child in containment.Children)
            {
                ValidateSupplementalSubdivisionCode(
                    child.Value,
                    "CLDR containment child",
                    knownPrefixes,
                    issues);
            }
        }
    }

    private static void ValidateSupplementalSubdivisionCode(
        string code,
        string context,
        IReadOnlySet<string> knownPrefixes,
        ReconciliationIssues issues)
    {
        if (!ReconciliationSyntax.IsSubdivision(code))
        {
            issues.Add($"{context} '{code}' has an illegal ISO 3166-2 shape.");
            return;
        }

        string prefix = code.Substring(0, 2);
        if (!knownPrefixes.Contains(prefix))
        {
            issues.Add($"{context} '{code}' has unknown country prefix '{prefix}'.");
        }
    }

    private static void ValidateM49Triple(
        string alpha3,
        string numeric,
        string? alpha2,
        IReadOnlyDictionary<string, StagedCountry> countriesByAlpha3,
        IReadOnlyDictionary<string, StagedCountry> countriesByNumeric,
        ReconciliationIssues issues)
    {
        if (!ReconciliationSyntax.IsAlpha3(alpha3))
        {
            issues.Add($"UN M49 alpha-3 '{alpha3}' has an illegal shape.");
        }

        if (!ReconciliationSyntax.IsNumeric3(numeric, false))
        {
            issues.Add($"UN M49 country code '{numeric}' is not in 001-899.");
        }

        if (!countriesByAlpha3.TryGetValue(alpha3, out StagedCountry? byAlpha3))
        {
            issues.Add($"UN M49 references unknown current alpha-3 '{alpha3}'.");
            return;
        }

        if (!countriesByNumeric.TryGetValue(numeric, out StagedCountry? byNumeric) ||
            !ReferenceEquals(byAlpha3, byNumeric))
        {
            issues.Add(
                $"UN M49 pair '{alpha3}/{numeric}' conflicts with the Debian country triple.");
        }

        if (alpha2 != null &&
            !string.Equals(byAlpha3.Alpha2.Value, alpha2, StringComparison.Ordinal))
        {
            issues.Add(
                $"UN M49 triple '{alpha2}/{alpha3}/{numeric}' conflicts with Debian alpha-2 " +
                $"'{byAlpha3.Alpha2.Value}'.");
        }
    }

    private static void ValidateLocaleKeyAgreement(
        IReadOnlyDictionary<string, HashSet<string>> namesByLocale,
        IReadOnlyDictionary<string, HashSet<string>> hierarchyByLocale,
        ReconciliationIssues issues)
    {
        foreach (KeyValuePair<string, HashSet<string>> pair in namesByLocale)
        {
            if (!hierarchyByLocale.TryGetValue(pair.Key, out HashSet<string>? hierarchyKeys))
            {
                issues.Add($"UN M49 names locale '{pair.Key}' has no hierarchy table.");
                continue;
            }

            RequireSameKeys(pair.Value, hierarchyKeys, $"UN M49 locale '{pair.Key}'", issues);
        }

        foreach (string locale in hierarchyByLocale.Keys)
        {
            if (!namesByLocale.ContainsKey(locale))
            {
                issues.Add($"UN M49 hierarchy locale '{locale}' has no country-name table.");
            }
        }
    }

    private static void ValidateM49Completeness(
        IReadOnlyDictionary<string, StagedCountry> countries,
        IReadOnlyDictionary<string, HashSet<string>> namesByLocale,
        IReadOnlyDictionary<string, HashSet<string>> hierarchyByLocale,
        OverrideCatalog overrides,
        HashSet<string> usedDeltaOverrides,
        List<ExplainedSourceDelta> sourceDeltas,
        ReconciliationIssues issues)
    {
        foreach (string alpha3 in countries.Keys)
        {
            int presentCount = 0;
            foreach (string locale in namesByLocale.Keys)
            {
                bool hasName = namesByLocale[locale].Contains(alpha3);
                bool hasHierarchy = hierarchyByLocale.TryGetValue(locale, out HashSet<string>? keys) &&
                                    keys.Contains(alpha3);
                if (hasName && hasHierarchy)
                {
                    presentCount += 1;
                }
            }

            if (presentCount == namesByLocale.Count)
            {
                continue;
            }

            if (presentCount != 0)
            {
                issues.Add(
                    $"UN M49 supplies current country '{alpha3}' in only {presentCount} of " +
                    $"{namesByLocale.Count} locales.");
                continue;
            }

            ExplainSourceDelta(
                overrides,
                usedDeltaOverrides,
                "un-m49",
                alpha3,
                "current-country-numeric-identifier",
                new[]
                {
                    countries[alpha3].Alpha2.Value,
                    countries[alpha3].Alpha3.Value,
                    countries[alpha3].Numeric.Value,
                    "missing"
                },
                sourceDeltas,
                issues);
        }
    }

    private static void ValidateRegionCode(
        string code,
        string context,
        string upstreamKey,
        ReconciliationIssues issues)
    {
        if (!ReconciliationSyntax.IsNumeric3(code, true))
        {
            issues.Add(
                $"M49 {context} code '{code}' for country '{upstreamKey}' has an illegal shape.");
        }
    }

    private static void ValidateOptionalRegionPair(
        StagedText? code,
        StagedText? name,
        string context,
        StagedM49Hierarchy row,
        ReconciliationIssues issues)
    {
        if ((code == null) != (name == null))
        {
            issues.Add(
                $"M49 country '{row.M49Code.Value}' has incomplete {context} code/name data.");
        }

        if (code != null)
        {
            ValidateRegionCode(code.Value, context, row.Origin.UpstreamKey, issues);
        }
    }

    private static void AddRegionNode(
        Dictionary<string, RegionNodeShape> nodes,
        string code,
        string? parent,
        int level,
        ReconciliationIssues issues)
    {
        if (!nodes.TryGetValue(code, out RegionNodeShape? existing))
        {
            nodes.Add(code, new RegionNodeShape(parent, level));
            return;
        }

        if (!string.Equals(existing.Parent, parent, StringComparison.Ordinal) ||
            existing.Level != level)
        {
            issues.Add($"M49 aggregate '{code}' has conflicting hierarchy placement.");
        }
    }

    private static IEnumerable<StagingOrigin> EnumerateOrigins(GeneratorStagingData staging)
    {
        foreach (StagedCountry row in staging.Countries)
        {
            yield return row.Origin;
        }

        foreach (StagedSubdivision row in staging.Subdivisions)
        {
            yield return row.Origin;
        }

        foreach (StagedHistoricalCountry row in staging.HistoricalCountries)
        {
            yield return row.Origin;
        }

        foreach (StagedName row in staging.Names)
        {
            yield return row.Origin;
        }

        foreach (StagedAlias row in staging.Aliases)
        {
            yield return row.Origin;
        }

        foreach (StagedContainment row in staging.Containment)
        {
            yield return row.Origin;
        }

        foreach (StagedM49History row in staging.M49History)
        {
            yield return row.Origin;
        }

        foreach (StagedM49Hierarchy row in staging.M49Hierarchy)
        {
            yield return row.Origin;
        }

        foreach (StagedIanaRecord row in staging.IanaRecords)
        {
            yield return row.Origin;
        }
    }

    private static string? FindIanaField(StagedIanaRecord record, string fieldName)
    {
        foreach (StagedNamedField field in record.Fields)
        {
            if (string.Equals(field.Name, fieldName, StringComparison.Ordinal))
            {
                return field.Value.Value;
            }
        }

        return null;
    }

    private static HashSet<string> GetLocaleKeys(
        Dictionary<string, HashSet<string>> values,
        string locale)
    {
        if (!values.TryGetValue(locale, out HashSet<string>? keys))
        {
            keys = new HashSet<string>(StringComparer.Ordinal);
            values.Add(locale, keys);
        }

        return keys;
    }

    private static void RequireSameKeys(
        IReadOnlySet<string> left,
        IReadOnlySet<string> right,
        string context,
        ReconciliationIssues issues)
    {
        foreach (string value in left)
        {
            if (!right.Contains(value))
            {
                issues.Add($"{context} hierarchy is missing '{value}' from its names table.");
            }
        }

        foreach (string value in right)
        {
            if (!left.Contains(value))
            {
                issues.Add($"{context} names table is missing '{value}' from its hierarchy.");
            }
        }
    }

    private static void ValidateAcyclic(
        IReadOnlyDictionary<string, string> parents,
        string context,
        ReconciliationIssues issues)
    {
        Dictionary<string, int> states = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string node in parents.Keys)
        {
            string current = node;
            HashSet<string> path = new HashSet<string>(StringComparer.Ordinal);
            while (parents.TryGetValue(current, out string? parent))
            {
                if (!path.Add(current))
                {
                    issues.Add($"The {context} hierarchy contains a cycle at '{current}'.");
                    break;
                }

                if (states.TryGetValue(current, out int state) && state == 2)
                {
                    break;
                }

                current = parent;
            }

            foreach (string visited in path)
            {
                states[visited] = 2;
            }
        }
    }

    private static void AddUnique<T>(
        Dictionary<string, T> values,
        string key,
        T value,
        string context,
        ReconciliationIssues issues)
    {
        if (!values.TryAdd(key, value))
        {
            issues.Add($"Duplicate {context} '{key}'.");
        }
    }

    private static bool IsAsciiLetters(string value)
    {
        foreach (char character in value)
        {
            bool upper = character >= 'A' && character <= 'Z';
            bool lower = character >= 'a' && character <= 'z';
            if (!upper && !lower)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryAddLanguageRange(
        string value,
        Dictionary<string, bool> languages)
    {
        if (value.Length != 8 ||
            !string.Equals(value.Substring(3, 2), "..", StringComparison.Ordinal))
        {
            return false;
        }

        string start = IdentifierStagingNormalizer.LowerAscii(value.Substring(0, 3));
        string end = IdentifierStagingNormalizer.LowerAscii(value.Substring(5, 3));
        if (!IsAsciiLetters(start) || !IsAsciiLetters(end) ||
            string.CompareOrdinal(start, end) > 0)
        {
            return false;
        }

        char first = start[0];
        char second = start[1];
        char third = start[2];
        while (true)
        {
            string subtag = new string(new[] { first, second, third });
            languages.TryAdd(subtag, true);
            if (string.Equals(subtag, end, StringComparison.Ordinal))
            {
                return true;
            }

            third += (char)1;
            if (third <= 'z')
            {
                continue;
            }

            third = 'a';
            second += (char)1;
            if (second <= 'z')
            {
                continue;
            }

            second = 'a';
            first += (char)1;
            if (first > 'z')
            {
                return false;
            }
        }
    }

    private static bool IsUserAssignedAlpha2(string value)
    {
        if (!ReconciliationSyntax.IsAlpha2(value))
        {
            return false;
        }

        return string.Equals(value, "AA", StringComparison.Ordinal) ||
               string.Equals(value, "ZZ", StringComparison.Ordinal) ||
               (value[0] == 'Q' && value[1] >= 'M' && value[1] <= 'Z') ||
               (value[0] == 'X' && value[1] >= 'A' && value[1] <= 'Z');
    }

    private static bool IsM49HistoryCodeSet(string value)
    {
        string[] codes = value.Split(',', StringSplitOptions.TrimEntries);
        if (codes.Length == 0)
        {
            return false;
        }

        foreach (string code in codes)
        {
            if (!ReconciliationSyntax.IsNumeric3(code, true))
            {
                return false;
            }
        }

        return true;
    }

    private static void ExplainSourceDelta(
        OverrideCatalog overrides,
        HashSet<string> usedDeltaOverrides,
        string sourceId,
        string upstreamKey,
        string fieldGroup,
        IReadOnlyList<string> values,
        List<ExplainedSourceDelta> sourceDeltas,
        ReconciliationIssues issues)
    {
        OverrideEntry? match = null;
        foreach (OverrideEntry entry in overrides.Entries)
        {
            if (!string.Equals(entry.Kind, "source-delta", StringComparison.Ordinal) ||
                !string.Equals(entry.TargetKind, "source-delta", StringComparison.Ordinal) ||
                !string.Equals(entry.TargetKey, upstreamKey, StringComparison.Ordinal) ||
                !string.Equals(entry.Provenance.SourceId, sourceId, StringComparison.Ordinal) ||
                !string.Equals(entry.Provenance.UpstreamKey, upstreamKey, StringComparison.Ordinal) ||
                !string.Equals(entry.Field, fieldGroup, StringComparison.Ordinal) ||
                !SameValues(entry.Values, values))
            {
                continue;
            }

            match = entry;
            break;
        }

        if (match == null)
        {
            issues.Add(
                $"Unexplained source delta {sourceId}:{upstreamKey} ({fieldGroup}) with " +
                $"values [{string.Join(", ", values)}].");
            return;
        }

        if (!usedDeltaOverrides.Add(match.Id))
        {
            issues.Add($"Source-delta override '{match.Id}' matched more than one delta.");
            return;
        }

        sourceDeltas.Add(new ExplainedSourceDelta(
            match.Id,
            sourceId,
            upstreamKey,
            fieldGroup,
            match.Reason));
    }

    private static void ValidateAllDeltaOverridesUsed(
        OverrideCatalog overrides,
        IReadOnlySet<string> usedDeltaOverrides,
        ReconciliationIssues issues)
    {
        foreach (OverrideEntry entry in overrides.Entries)
        {
            if (string.Equals(entry.Kind, "source-delta", StringComparison.Ordinal) &&
                !usedDeltaOverrides.Contains(entry.Id))
            {
                issues.Add(
                    $"Source-delta override '{entry.Id}' is stale because its expected delta " +
                    "was not observed.");
            }
        }
    }

    private static bool SameValues(
        IReadOnlyList<string> left,
        IReadOnlyList<string> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (int index = 0; index < left.Count; index += 1)
        {
            if (!string.Equals(left[index], right[index], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static void RequireShape(
        bool condition,
        string issue,
        ReconciliationIssues issues)
    {
        if (!condition)
        {
            issues.Add(issue);
        }
    }

    private sealed class RegionNodeShape
    {
        internal RegionNodeShape(string? parent, int level)
        {
            Parent = parent;
            Level = level;
        }

        internal string? Parent { get; }

        internal int Level { get; }
    }
}
