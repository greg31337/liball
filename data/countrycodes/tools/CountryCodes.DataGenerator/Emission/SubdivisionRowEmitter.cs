using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CountryCodes.DataGenerator.Overrides;
using CountryCodes.DataGenerator.Staging;

namespace CountryCodes.DataGenerator.Emission;

internal static class SubdivisionRowEmitter
{
    private const string SubdivisionFileName = "SubdivisionTable.g.cs";
    private const string SubdivisionNameFileName = "SubdivisionNameTable.g.cs";

    internal static SubdivisionEmissionResult Emit(
        OverrideApplicationResult input,
        string outputDirectory,
        GeneratedIndexCollector indexes)
    {
        GeneratorStagingData staging = input.Reconciliation.Staging;
        Dictionary<string, StagedCountry> countries = IndexCountries(staging.Countries);
        Dictionary<string, StagedSubdivision> subdivisions = IndexSubdivisions(staging.Subdivisions);
        Dictionary<string, EffectiveParent> parents = BuildEffectiveParents(
            staging.Subdivisions,
            input.AppliedOverrides);

        List<StagedSubdivision> rows = new List<StagedSubdivision>(staging.Subdivisions);
        rows.Sort(CompareSubdivisions);
        ValidateSubdivisionRows(rows, countries, subdivisions, parents);

        List<SubdivisionAliasEmissionRow> aliases = BuildAliases(staging, subdivisions);
        List<SubdivisionReplacementEmissionRow> replacements = BuildReplacements(
            staging,
            subdivisions,
            countries);
        ValidateAliases(aliases, replacements, subdivisions);

        List<SubdivisionNameEmissionRow> names = BuildNames(
            staging,
            input.AppliedOverrides,
            subdivisions,
            aliases);
        ValidateNames(names, subdivisions, aliases);
        AddIndexes(rows, aliases, names, indexes);

        string subdivisionPath = Path.Combine(outputDirectory, SubdivisionFileName);
        string namePath = Path.Combine(outputDirectory, SubdivisionNameFileName);
        Directory.CreateDirectory(outputDirectory);
        WriteDeterministic(
            subdivisionPath,
            RenderSubdivisionTable(rows, parents, aliases, replacements));
        WriteDeterministic(namePath, RenderNameTable(names));
        return new SubdivisionEmissionResult(
            rows.Count,
            names.Count,
            aliases.Count,
            replacements.Count,
            subdivisionPath,
            namePath);
    }

    private static void AddIndexes(
        IReadOnlyList<StagedSubdivision> rows,
        IReadOnlyList<SubdivisionAliasEmissionRow> aliases,
        IReadOnlyList<SubdivisionNameEmissionRow> names,
        GeneratedIndexCollector indexes)
    {
        for (int index = 0; index < rows.Count; index += 1)
        {
            indexes.AddExact("SubdivisionCode", rows[index].Code.Value, index);
        }

        for (int index = 0; index < aliases.Count; index += 1)
        {
            indexes.AddExact("SubdivisionAlias", aliases[index].Code, index);
        }

        for (int index = 0; index < names.Count; index += 1)
        {
            indexes.AddSearch("SubdivisionName", names[index].SearchKey, index);
        }
    }

    private static Dictionary<string, StagedCountry> IndexCountries(
        IReadOnlyList<StagedCountry> rows)
    {
        Dictionary<string, StagedCountry> result = new Dictionary<string, StagedCountry>(
            StringComparer.Ordinal);
        foreach (StagedCountry row in rows)
        {
            result.Add(row.Alpha2.Value, row);
        }

        return result;
    }

    private static Dictionary<string, StagedSubdivision> IndexSubdivisions(
        IReadOnlyList<StagedSubdivision> rows)
    {
        Dictionary<string, StagedSubdivision> result = new Dictionary<string, StagedSubdivision>(
            StringComparer.Ordinal);
        foreach (StagedSubdivision row in rows)
        {
            result.Add(row.Code.Value, row);
        }

        return result;
    }

    private static Dictionary<string, EffectiveParent> BuildEffectiveParents(
        IReadOnlyList<StagedSubdivision> subdivisions,
        IReadOnlyList<AppliedOverride> overrides)
    {
        Dictionary<string, EffectiveParent> result = new Dictionary<string, EffectiveParent>(
            StringComparer.Ordinal);
        foreach (StagedSubdivision subdivision in subdivisions)
        {
            if (subdivision.Parent != null)
            {
                StagingOrigin origin = subdivision.Parent.Provenance.Origin;
                result.Add(
                    subdivision.Code.Value,
                    new EffectiveParent(
                        subdivision.Parent.Value,
                        origin.SourceId,
                        origin.SourceRevision,
                        origin.UpstreamKey,
                        origin.EffectiveDate,
                        subdivision.Parent.Provenance.SourceField));
            }
        }

        foreach (AppliedOverride applied in overrides)
        {
            OverrideEntry entry = applied.Definition;
            if (!string.Equals(entry.Kind, "supplement", StringComparison.Ordinal) ||
                !string.Equals(entry.TargetKind, "subdivision", StringComparison.Ordinal) ||
                !string.Equals(entry.Field, "parent", StringComparison.Ordinal))
            {
                continue;
            }

            if (entry.Values.Count != 1 || result.ContainsKey(entry.TargetKey))
            {
                throw new EmissionException(
                    $"Subdivision parent override '{entry.Id}' cannot be emitted.");
            }

            result.Add(
                entry.TargetKey,
                new EffectiveParent(
                    entry.Values[0],
                    entry.Provenance.SourceId,
                    entry.ExpiryCondition.Revision,
                    entry.Provenance.UpstreamKey,
                    entry.EffectiveDate,
                    entry.Field));
        }

        return result;
    }

    private static List<SubdivisionAliasEmissionRow> BuildAliases(
        GeneratorStagingData staging,
        IReadOnlyDictionary<string, StagedSubdivision> subdivisions)
    {
        List<SubdivisionAliasEmissionRow> result = new List<SubdivisionAliasEmissionRow>();
        foreach (StagedAlias alias in staging.Aliases)
        {
            if (alias.Target != StagedAliasTarget.Subdivision)
            {
                continue;
            }

            string reason = NormalizeAliasReason(alias.Reason.Value, alias.Code.Value);
            bool isCurrentSubdivision = subdivisions.ContainsKey(alias.Code.Value);
            if (isCurrentSubdivision && string.Equals(reason, "Deprecated", StringComparison.Ordinal))
            {
                throw new EmissionException(
                    $"Deprecated subdivision alias '{alias.Code.Value}' masks a current subdivision.");
            }

            result.Add(new SubdivisionAliasEmissionRow(
                alias.Code.Value,
                ReadCountryCode(alias.Code.Value),
                reason,
                isCurrentSubdivision,
                alias.Origin.SourceId,
                alias.Origin.SourceRevision,
                alias.Origin.UpstreamKey,
                alias.Origin.EffectiveDate,
                alias.Reason.Provenance.SourceField));
        }

        result.Sort(CompareAliases);
        return result;
    }

    private static List<SubdivisionReplacementEmissionRow> BuildReplacements(
        GeneratorStagingData staging,
        IReadOnlyDictionary<string, StagedSubdivision> subdivisions,
        IReadOnlyDictionary<string, StagedCountry> countries)
    {
        List<SubdivisionReplacementEmissionRow> result =
            new List<SubdivisionReplacementEmissionRow>();
        foreach (StagedAlias alias in staging.Aliases)
        {
            if (alias.Target != StagedAliasTarget.Subdivision)
            {
                continue;
            }

            foreach (StagedText replacement in alias.Replacements)
            {
                bool isSubdivision = replacement.Value.IndexOf('-') >= 0;
                bool isCurrent = isSubdivision
                    ? subdivisions.ContainsKey(replacement.Value)
                    : countries.ContainsKey(replacement.Value);
                result.Add(new SubdivisionReplacementEmissionRow(
                    alias.Code.Value,
                    replacement.Value,
                    isSubdivision ? "Subdivision" : "CountryOrArea",
                    isCurrent,
                    alias.Origin.SourceId,
                    alias.Origin.SourceRevision,
                    alias.Origin.UpstreamKey,
                    alias.Origin.EffectiveDate,
                    replacement.Provenance.SourceField));
            }
        }

        result.Sort(CompareReplacements);
        return result;
    }

    private static List<SubdivisionNameEmissionRow> BuildNames(
        GeneratorStagingData staging,
        IReadOnlyList<AppliedOverride> overrides,
        IReadOnlyDictionary<string, StagedSubdivision> subdivisions,
        IReadOnlyList<SubdivisionAliasEmissionRow> aliases)
    {
        HashSet<string> aliasCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (SubdivisionAliasEmissionRow alias in aliases)
        {
            aliasCodes.Add(alias.Code);
        }

        List<SubdivisionNameEmissionRow> result = new List<SubdivisionNameEmissionRow>();
        foreach (StagedSubdivision subdivision in staging.Subdivisions)
        {
            AddName(
                result,
                subdivision.Code.Value,
                true,
                "Canonical",
                "en",
                subdivision.Name.Value,
                null,
                subdivision.Name.Provenance.Origin,
                subdivision.Name.Provenance.SourceField);
        }

        foreach (StagedName name in staging.Names)
        {
            if (name.Target != StagedNameTarget.CldrSubdivision)
            {
                continue;
            }

            bool isCurrent = subdivisions.ContainsKey(name.EntityKey.Value);
            if (!isCurrent && !aliasCodes.Contains(name.EntityKey.Value))
            {
                throw new EmissionException(
                    $"CLDR subdivision name '{name.EntityKey.Value}' has no known target.");
            }

            string kind = name.Alternate == null ? "Localized" : "Alias";
            AddName(
                result,
                name.EntityKey.Value,
                isCurrent,
                kind,
                RequireLocale(name.Origin),
                name.Value.Value,
                name.Alternate?.Value,
                name.Value.Provenance.Origin,
                name.Value.Provenance.SourceField);
        }

        foreach (AppliedOverride applied in overrides)
        {
            OverrideEntry entry = applied.Definition;
            if (!string.Equals(entry.Kind, "alias", StringComparison.Ordinal) ||
                !string.Equals(entry.TargetKind, "subdivision", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (string value in entry.Values)
            {
                AddOverrideName(result, entry, value);
            }
        }

        result.Sort(CompareNames);
        return result;
    }

    private static void AddOverrideName(
        List<SubdivisionNameEmissionRow> rows,
        OverrideEntry entry,
        string value)
    {
        string display = NameEmissionUtility.NormalizeDisplay(value);
        string searchKey = NameEmissionUtility.CreateSearchKey(display);
        rows.Add(new SubdivisionNameEmissionRow(
            entry.TargetKey,
            true,
            "Alias",
            "en",
            display,
            searchKey,
            entry.Id,
            entry.Provenance.SourceId,
            entry.ExpiryCondition.Revision,
            entry.Provenance.UpstreamKey,
            entry.EffectiveDate,
            entry.Field));
    }

    private static void AddName(
        List<SubdivisionNameEmissionRow> rows,
        string code,
        bool isCurrent,
        string kind,
        string locale,
        string value,
        string? qualifier,
        StagingOrigin origin,
        string sourceField)
    {
        string display = NameEmissionUtility.NormalizeDisplay(value);
        string searchKey = NameEmissionUtility.CreateSearchKey(display);
        if (display.Length == 0 || searchKey.Length == 0)
        {
            throw new EmissionException(
                $"Subdivision name '{origin.SourceId}:{origin.UpstreamKey}' is not searchable.");
        }

        rows.Add(new SubdivisionNameEmissionRow(
            code,
            isCurrent,
            kind,
            locale,
            display,
            searchKey,
            qualifier,
            origin.SourceId,
            origin.SourceRevision,
            origin.UpstreamKey,
            origin.EffectiveDate,
            sourceField));
    }

    private static void ValidateSubdivisionRows(
        IReadOnlyList<StagedSubdivision> rows,
        IReadOnlyDictionary<string, StagedCountry> countries,
        IReadOnlyDictionary<string, StagedSubdivision> subdivisions,
        IReadOnlyDictionary<string, EffectiveParent> parents)
    {
        string? previous = null;
        foreach (StagedSubdivision row in rows)
        {
            string code = row.Code.Value;
            if (previous != null && string.CompareOrdinal(previous, code) >= 0)
            {
                throw new EmissionException(
                    "Subdivision rows are not unique ordinal full-code order.");
            }

            previous = code;
            if (!countries.ContainsKey(row.CountryCode.Value) ||
                !string.Equals(
                    code,
                    row.CountryCode.Value + "-" + row.Suffix.Value,
                    StringComparison.Ordinal))
            {
                throw new EmissionException($"Subdivision '{code}' has an invalid country link.");
            }

            if (parents.TryGetValue(code, out EffectiveParent? parent))
            {
                if (!subdivisions.TryGetValue(parent.Code, out StagedSubdivision? parentRow) ||
                    !string.Equals(
                        row.CountryCode.Value,
                        parentRow.CountryCode.Value,
                        StringComparison.Ordinal))
                {
                    throw new EmissionException(
                        $"Subdivision '{code}' has invalid parent '{parent.Code}'.");
                }
            }
        }

        ValidateParentCycles(rows, parents);
    }

    private static void ValidateParentCycles(
        IReadOnlyList<StagedSubdivision> rows,
        IReadOnlyDictionary<string, EffectiveParent> parents)
    {
        foreach (StagedSubdivision row in rows)
        {
            HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
            string current = row.Code.Value;
            while (parents.TryGetValue(current, out EffectiveParent? parent))
            {
                if (!visited.Add(current))
                {
                    throw new EmissionException(
                        $"Subdivision hierarchy contains a cycle at '{current}'.");
                }

                current = parent.Code;
            }
        }
    }

    private static void ValidateAliases(
        IReadOnlyList<SubdivisionAliasEmissionRow> aliases,
        IReadOnlyList<SubdivisionReplacementEmissionRow> replacements,
        IReadOnlyDictionary<string, StagedSubdivision> subdivisions)
    {
        HashSet<string> aliasCodes = new HashSet<string>(StringComparer.Ordinal);
        SubdivisionAliasEmissionRow? previousAlias = null;
        foreach (SubdivisionAliasEmissionRow alias in aliases)
        {
            if (previousAlias != null && CompareAliases(previousAlias, alias) >= 0)
            {
                throw new EmissionException(
                    "Subdivision aliases are not unique ordinal code order.");
            }

            previousAlias = alias;
            aliasCodes.Add(alias.Code);
            if (subdivisions.ContainsKey(alias.Code) != alias.IsCurrentSubdivision)
            {
                throw new EmissionException(
                    $"Subdivision alias '{alias.Code}' has an invalid current-status flag.");
            }

            if (alias.IsCurrentSubdivision &&
                alias.Reason == "Deprecated")
            {
                throw new EmissionException(
                    $"Deprecated subdivision alias '{alias.Code}' masks a current subdivision.");
            }
        }

        HashSet<string> aliasesWithReplacements = new HashSet<string>(StringComparer.Ordinal);
        SubdivisionReplacementEmissionRow? previousReplacement = null;
        foreach (SubdivisionReplacementEmissionRow replacement in replacements)
        {
            if (previousReplacement != null &&
                CompareReplacements(previousReplacement, replacement) >= 0)
            {
                throw new EmissionException(
                    "Subdivision replacement rows are not unique stable ordinal order.");
            }

            previousReplacement = replacement;
            if (!aliasCodes.Contains(replacement.AliasCode))
            {
                throw new EmissionException(
                    $"Replacement references unknown alias '{replacement.AliasCode}'.");
            }

            aliasesWithReplacements.Add(replacement.AliasCode);
        }

        if (aliasesWithReplacements.Count != aliases.Count)
        {
            throw new EmissionException("A subdivision alias has no replacement mapping.");
        }
    }

    private static void ValidateNames(
        IReadOnlyList<SubdivisionNameEmissionRow> names,
        IReadOnlyDictionary<string, StagedSubdivision> subdivisions,
        IReadOnlyList<SubdivisionAliasEmissionRow> aliases)
    {
        HashSet<string> knownAliases = new HashSet<string>(StringComparer.Ordinal);
        foreach (SubdivisionAliasEmissionRow alias in aliases)
        {
            knownAliases.Add(alias.Code);
        }

        HashSet<string> canonical = new HashSet<string>(StringComparer.Ordinal);
        SubdivisionNameEmissionRow? previous = null;
        foreach (SubdivisionNameEmissionRow name in names)
        {
            if (previous != null && CompareNames(previous, name) >= 0)
            {
                throw new EmissionException(
                    "Subdivision-name rows are not unique stable ordinal order.");
            }

            previous = name;
            bool current = subdivisions.ContainsKey(name.Code);
            if (current != name.IsCurrent || (!current && !knownAliases.Contains(name.Code)))
            {
                throw new EmissionException(
                    $"Subdivision name '{name.Code}' has an invalid target.");
            }

            if (string.Equals(name.Kind, "Canonical", StringComparison.Ordinal))
            {
                canonical.Add(name.Code);
            }
        }

        if (canonical.Count != subdivisions.Count)
        {
            throw new EmissionException("Not every current subdivision has one canonical name.");
        }
    }

    private static string NormalizeAliasReason(string reason, string code)
    {
        if (string.Equals(reason, "deprecated", StringComparison.Ordinal))
        {
            return "Deprecated";
        }

        if (string.Equals(reason, "overlong", StringComparison.Ordinal))
        {
            return "Overlong";
        }

        throw new EmissionException(
            $"Subdivision alias '{code}' has unsupported reason '{reason}'.");
    }

    private static string ReadCountryCode(string subdivisionCode)
    {
        int separator = subdivisionCode.IndexOf('-');
        if (separator != 2)
        {
            throw new EmissionException(
                $"Subdivision code '{subdivisionCode}' has no canonical country prefix.");
        }

        return subdivisionCode.Substring(0, 2);
    }

    private static string RequireLocale(StagingOrigin origin)
    {
        if (string.IsNullOrEmpty(origin.Locale))
        {
            throw new EmissionException(
                $"Subdivision name '{origin.SourceId}:{origin.UpstreamKey}' has no locale.");
        }

        return origin.Locale;
    }

    private static int CompareSubdivisions(StagedSubdivision left, StagedSubdivision right)
    {
        return string.CompareOrdinal(left.Code.Value, right.Code.Value);
    }

    private static int CompareAliases(
        SubdivisionAliasEmissionRow left,
        SubdivisionAliasEmissionRow right)
    {
        return string.CompareOrdinal(left.Code, right.Code);
    }

    private static int CompareReplacements(
        SubdivisionReplacementEmissionRow left,
        SubdivisionReplacementEmissionRow right)
    {
        int comparison = string.CompareOrdinal(left.AliasCode, right.AliasCode);
        return comparison == 0
            ? string.CompareOrdinal(left.ReplacementCode, right.ReplacementCode)
            : comparison;
    }

    private static int CompareNames(
        SubdivisionNameEmissionRow left,
        SubdivisionNameEmissionRow right)
    {
        int comparison = string.CompareOrdinal(left.Code, right.Code);
        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(left.Locale, right.Locale);
        }

        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(left.SearchKey, right.SearchKey);
        }

        if (comparison == 0)
        {
            comparison = NameKindOrder(left.Kind).CompareTo(NameKindOrder(right.Kind));
        }

        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(left.DisplayName, right.DisplayName);
        }

        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(left.SourceId, right.SourceId);
        }

        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(left.UpstreamKey, right.UpstreamKey);
        }

        return comparison == 0
            ? string.CompareOrdinal(left.Qualifier ?? string.Empty, right.Qualifier ?? string.Empty)
            : comparison;
    }

    private static int NameKindOrder(string kind)
    {
        if (string.Equals(kind, "Canonical", StringComparison.Ordinal))
        {
            return 0;
        }

        return string.Equals(kind, "Localized", StringComparison.Ordinal) ? 1 : 2;
    }

    private static string RenderSubdivisionTable(
        IReadOnlyList<StagedSubdivision> rows,
        IReadOnlyDictionary<string, EffectiveParent> parents,
        IReadOnlyList<SubdivisionAliasEmissionRow> aliases,
        IReadOnlyList<SubdivisionReplacementEmissionRow> replacements)
    {
        StringBuilder builder = new StringBuilder(2_000_000);
        Line(builder, "// <auto-generated />");
        Line(builder, "namespace countrycodes;");
        Line(builder, string.Empty);
        RenderSubdivisionTypes(builder);
        Line(builder, string.Empty);
        Line(builder, "internal static class SubdivisionTable");
        Line(builder, "{");
        Line(builder, "    internal static readonly GeneratedSubdivisionRow[] Rows =");
        Line(builder, "    {");
        foreach (StagedSubdivision row in rows)
        {
            bool hasParent = parents.TryGetValue(row.Code.Value, out EffectiveParent? parent);
            Line(builder, "        new GeneratedSubdivisionRow(");
            Line(builder, $"            {Literal(row.Code.Value)},");
            Line(builder, $"            {Literal(row.CountryCode.Value)},");
            Line(builder, $"            {Literal(row.Suffix.Value)},");
            Line(builder, $"            {Literal(row.Type.Value)},");
            Line(builder, "            GeneratedSubdivisionStatus.Active,");
            Line(builder, $"            {(hasParent ? "true" : "false")},");
            Line(builder, $"            {Literal(parent?.Code ?? string.Empty)},");
            Line(builder, $"            {Literal(row.Origin.SourceId)},");
            Line(builder, $"            {Literal(row.Origin.SourceRevision)},");
            Line(builder, $"            {Literal(row.Origin.UpstreamKey)},");
            Line(builder, $"            {Literal(row.Origin.EffectiveDate)},");
            Line(builder, $"            {Literal(parent?.SourceId ?? string.Empty)},");
            Line(builder, $"            {Literal(parent?.SourceRevision ?? string.Empty)},");
            Line(builder, $"            {Literal(parent?.UpstreamKey ?? string.Empty)},");
            Line(builder, $"            {Literal(parent?.EffectiveDate ?? string.Empty)},");
            Line(builder, $"            {Literal(parent?.SourceField ?? string.Empty)}),");
        }

        Line(builder, "    };");
        Line(builder, string.Empty);
        Line(builder, "    internal static readonly GeneratedSubdivisionAliasRow[] Aliases =");
        Line(builder, "    {");
        foreach (SubdivisionAliasEmissionRow alias in aliases)
        {
            Line(builder, "        new GeneratedSubdivisionAliasRow(");
            Line(builder, $"            {Literal(alias.Code)},");
            Line(builder, $"            {Literal(alias.CountryCode)},");
            Line(builder, $"            GeneratedSubdivisionAliasReason.{alias.Reason},");
            Line(builder, $"            {(alias.IsCurrentSubdivision ? "true" : "false")},");
            Line(builder, $"            {Literal(alias.SourceId)},");
            Line(builder, $"            {Literal(alias.SourceRevision)},");
            Line(builder, $"            {Literal(alias.UpstreamKey)},");
            Line(builder, $"            {Literal(alias.EffectiveDate)},");
            Line(builder, $"            {Literal(alias.SourceField)}),");
        }

        Line(builder, "    };");
        Line(builder, string.Empty);
        Line(builder, "    internal static readonly GeneratedSubdivisionReplacementRow[] Replacements =");
        Line(builder, "    {");
        foreach (SubdivisionReplacementEmissionRow replacement in replacements)
        {
            Line(builder, "        new GeneratedSubdivisionReplacementRow(");
            Line(builder, $"            {Literal(replacement.AliasCode)},");
            Line(builder, $"            {Literal(replacement.ReplacementCode)},");
            Line(builder, $"            GeneratedSubdivisionReplacementTargetKind.{replacement.TargetKind},");
            Line(builder, $"            {(replacement.IsCurrent ? "true" : "false")},");
            Line(builder, $"            {Literal(replacement.SourceId)},");
            Line(builder, $"            {Literal(replacement.SourceRevision)},");
            Line(builder, $"            {Literal(replacement.UpstreamKey)},");
            Line(builder, $"            {Literal(replacement.EffectiveDate)},");
            Line(builder, $"            {Literal(replacement.SourceField)}),");
        }

        Line(builder, "    };");
        Line(builder, "}");
        return builder.ToString();
    }

    private static void RenderSubdivisionTypes(StringBuilder builder)
    {
        Line(builder, "internal enum GeneratedSubdivisionAliasReason");
        Line(builder, "{");
        Line(builder, "    Deprecated = 0,");
        Line(builder, "    Overlong = 1");
        Line(builder, "}");
        Line(builder, string.Empty);
        Line(builder, "internal enum GeneratedSubdivisionReplacementTargetKind");
        Line(builder, "{");
        Line(builder, "    Subdivision = 0,");
        Line(builder, "    CountryOrArea = 1");
        Line(builder, "}");
        Line(builder, string.Empty);
        Line(builder, "internal enum GeneratedSubdivisionStatus");
        Line(builder, "{");
        Line(builder, "    Active = 0,");
        Line(builder, "    Deprecated = 1,");
        Line(builder, "    Withdrawn = 2,");
        Line(builder, "    Unknown = 3");
        Line(builder, "}");
        Line(builder, string.Empty);
        Line(builder, "internal class GeneratedSubdivisionRow");
        Line(builder, "{");
        Line(builder, "    internal string Code;");
        Line(builder, "    internal string CountryCode;");
        Line(builder, "    internal string Suffix;");
        Line(builder, "    internal string Type;");
        Line(builder, "    internal GeneratedSubdivisionStatus Status;");
        Line(builder, "    internal bool HasParent;");
        Line(builder, "    internal string ParentCode;");
        Line(builder, "    internal string SourceId;");
        Line(builder, "    internal string SourceRevision;");
        Line(builder, "    internal string UpstreamKey;");
        Line(builder, "    internal string EffectiveDate;");
        Line(builder, "    internal string ParentSourceId;");
        Line(builder, "    internal string ParentSourceRevision;");
        Line(builder, "    internal string ParentUpstreamKey;");
        Line(builder, "    internal string ParentEffectiveDate;");
        Line(builder, "    internal string ParentSourceField;");
        Line(builder, string.Empty);
        Line(builder, "    internal GeneratedSubdivisionRow(");
        Line(builder, "        string code,");
        Line(builder, "        string countryCode,");
        Line(builder, "        string suffix,");
        Line(builder, "        string type,");
        Line(builder, "        GeneratedSubdivisionStatus status,");
        Line(builder, "        bool hasParent,");
        Line(builder, "        string parentCode,");
        Line(builder, "        string sourceId,");
        Line(builder, "        string sourceRevision,");
        Line(builder, "        string upstreamKey,");
        Line(builder, "        string effectiveDate,");
        Line(builder, "        string parentSourceId,");
        Line(builder, "        string parentSourceRevision,");
        Line(builder, "        string parentUpstreamKey,");
        Line(builder, "        string parentEffectiveDate,");
        Line(builder, "        string parentSourceField)");
        Line(builder, "    {");
        Line(builder, "        Code = code;");
        Line(builder, "        CountryCode = countryCode;");
        Line(builder, "        Suffix = suffix;");
        Line(builder, "        Type = type;");
        Line(builder, "        Status = status;");
        Line(builder, "        HasParent = hasParent;");
        Line(builder, "        ParentCode = parentCode;");
        Line(builder, "        SourceId = sourceId;");
        Line(builder, "        SourceRevision = sourceRevision;");
        Line(builder, "        UpstreamKey = upstreamKey;");
        Line(builder, "        EffectiveDate = effectiveDate;");
        Line(builder, "        ParentSourceId = parentSourceId;");
        Line(builder, "        ParentSourceRevision = parentSourceRevision;");
        Line(builder, "        ParentUpstreamKey = parentUpstreamKey;");
        Line(builder, "        ParentEffectiveDate = parentEffectiveDate;");
        Line(builder, "        ParentSourceField = parentSourceField;");
        Line(builder, "    }");
        Line(builder, "}");
        Line(builder, string.Empty);
        Line(builder, "internal class GeneratedSubdivisionAliasRow");
        Line(builder, "{");
        Line(builder, "    internal string Code;");
        Line(builder, "    internal string CountryCode;");
        Line(builder, "    internal GeneratedSubdivisionAliasReason Reason;");
        Line(builder, "    internal bool IsCurrentSubdivision;");
        Line(builder, "    internal string SourceId;");
        Line(builder, "    internal string SourceRevision;");
        Line(builder, "    internal string UpstreamKey;");
        Line(builder, "    internal string EffectiveDate;");
        Line(builder, "    internal string SourceField;");
        Line(builder, string.Empty);
        Line(builder, "    internal GeneratedSubdivisionAliasRow(");
        Line(builder, "        string code,");
        Line(builder, "        string countryCode,");
        Line(builder, "        GeneratedSubdivisionAliasReason reason,");
        Line(builder, "        bool isCurrentSubdivision,");
        Line(builder, "        string sourceId,");
        Line(builder, "        string sourceRevision,");
        Line(builder, "        string upstreamKey,");
        Line(builder, "        string effectiveDate,");
        Line(builder, "        string sourceField)");
        Line(builder, "    {");
        Line(builder, "        Code = code;");
        Line(builder, "        CountryCode = countryCode;");
        Line(builder, "        Reason = reason;");
        Line(builder, "        IsCurrentSubdivision = isCurrentSubdivision;");
        Line(builder, "        SourceId = sourceId;");
        Line(builder, "        SourceRevision = sourceRevision;");
        Line(builder, "        UpstreamKey = upstreamKey;");
        Line(builder, "        EffectiveDate = effectiveDate;");
        Line(builder, "        SourceField = sourceField;");
        Line(builder, "    }");
        Line(builder, "}");
        Line(builder, string.Empty);
        Line(builder, "internal class GeneratedSubdivisionReplacementRow");
        Line(builder, "{");
        Line(builder, "    internal string AliasCode;");
        Line(builder, "    internal string ReplacementCode;");
        Line(builder, "    internal GeneratedSubdivisionReplacementTargetKind TargetKind;");
        Line(builder, "    internal bool IsCurrent;");
        Line(builder, "    internal string SourceId;");
        Line(builder, "    internal string SourceRevision;");
        Line(builder, "    internal string UpstreamKey;");
        Line(builder, "    internal string EffectiveDate;");
        Line(builder, "    internal string SourceField;");
        Line(builder, string.Empty);
        Line(builder, "    internal GeneratedSubdivisionReplacementRow(");
        Line(builder, "        string aliasCode,");
        Line(builder, "        string replacementCode,");
        Line(builder, "        GeneratedSubdivisionReplacementTargetKind targetKind,");
        Line(builder, "        bool isCurrent,");
        Line(builder, "        string sourceId,");
        Line(builder, "        string sourceRevision,");
        Line(builder, "        string upstreamKey,");
        Line(builder, "        string effectiveDate,");
        Line(builder, "        string sourceField)");
        Line(builder, "    {");
        Line(builder, "        AliasCode = aliasCode;");
        Line(builder, "        ReplacementCode = replacementCode;");
        Line(builder, "        TargetKind = targetKind;");
        Line(builder, "        IsCurrent = isCurrent;");
        Line(builder, "        SourceId = sourceId;");
        Line(builder, "        SourceRevision = sourceRevision;");
        Line(builder, "        UpstreamKey = upstreamKey;");
        Line(builder, "        EffectiveDate = effectiveDate;");
        Line(builder, "        SourceField = sourceField;");
        Line(builder, "    }");
        Line(builder, "}");
    }

    private static string RenderNameTable(IReadOnlyList<SubdivisionNameEmissionRow> names)
    {
        StringBuilder builder = new StringBuilder(1_000_000);
        Line(builder, "// <auto-generated />");
        Line(builder, "namespace countrycodes;");
        Line(builder, string.Empty);
        Line(builder, "internal enum GeneratedSubdivisionNameKind");
        Line(builder, "{");
        Line(builder, "    Canonical = 0,");
        Line(builder, "    Localized = 1,");
        Line(builder, "    Alias = 2");
        Line(builder, "}");
        Line(builder, string.Empty);
        Line(builder, "internal class GeneratedSubdivisionNameRow");
        Line(builder, "{");
        Line(builder, "    internal string SubdivisionCode;");
        Line(builder, "    internal bool IsCurrent;");
        Line(builder, "    internal GeneratedSubdivisionNameKind NameKind;");
        Line(builder, "    internal string Locale;");
        Line(builder, "    internal string DisplayName;");
        Line(builder, "    internal string SearchKey;");
        Line(builder, "    internal bool HasQualifier;");
        Line(builder, "    internal string Qualifier;");
        Line(builder, "    internal string SourceId;");
        Line(builder, "    internal string SourceRevision;");
        Line(builder, "    internal string UpstreamKey;");
        Line(builder, "    internal string EffectiveDate;");
        Line(builder, "    internal string SourceField;");
        Line(builder, string.Empty);
        Line(builder, "    internal GeneratedSubdivisionNameRow(");
        Line(builder, "        string subdivisionCode,");
        Line(builder, "        bool isCurrent,");
        Line(builder, "        GeneratedSubdivisionNameKind nameKind,");
        Line(builder, "        string locale,");
        Line(builder, "        string displayName,");
        Line(builder, "        string searchKey,");
        Line(builder, "        bool hasQualifier,");
        Line(builder, "        string qualifier,");
        Line(builder, "        string sourceId,");
        Line(builder, "        string sourceRevision,");
        Line(builder, "        string upstreamKey,");
        Line(builder, "        string effectiveDate,");
        Line(builder, "        string sourceField)");
        Line(builder, "    {");
        Line(builder, "        SubdivisionCode = subdivisionCode;");
        Line(builder, "        IsCurrent = isCurrent;");
        Line(builder, "        NameKind = nameKind;");
        Line(builder, "        Locale = locale;");
        Line(builder, "        DisplayName = displayName;");
        Line(builder, "        SearchKey = searchKey;");
        Line(builder, "        HasQualifier = hasQualifier;");
        Line(builder, "        Qualifier = qualifier;");
        Line(builder, "        SourceId = sourceId;");
        Line(builder, "        SourceRevision = sourceRevision;");
        Line(builder, "        UpstreamKey = upstreamKey;");
        Line(builder, "        EffectiveDate = effectiveDate;");
        Line(builder, "        SourceField = sourceField;");
        Line(builder, "    }");
        Line(builder, "}");
        Line(builder, string.Empty);
        Line(builder, "internal static class SubdivisionNameTable");
        Line(builder, "{");
        Line(builder, "    internal static readonly GeneratedSubdivisionNameRow[] Rows =");
        Line(builder, "    {");
        foreach (SubdivisionNameEmissionRow name in names)
        {
            Line(builder, "        new GeneratedSubdivisionNameRow(");
            Line(builder, $"            {Literal(name.Code)},");
            Line(builder, $"            {(name.IsCurrent ? "true" : "false")},");
            Line(builder, $"            GeneratedSubdivisionNameKind.{name.Kind},");
            Line(builder, $"            {Literal(name.Locale)},");
            Line(builder, $"            {Literal(name.DisplayName)},");
            Line(builder, $"            {Literal(name.SearchKey)},");
            Line(builder, $"            {(name.Qualifier == null ? "false" : "true")},");
            Line(builder, $"            {Literal(name.Qualifier ?? string.Empty)},");
            Line(builder, $"            {Literal(name.SourceId)},");
            Line(builder, $"            {Literal(name.SourceRevision)},");
            Line(builder, $"            {Literal(name.UpstreamKey)},");
            Line(builder, $"            {Literal(name.EffectiveDate)},");
            Line(builder, $"            {Literal(name.SourceField)}),");
        }

        Line(builder, "    };");
        Line(builder, "}");
        return builder.ToString();
    }

    private static string Literal(string value)
    {
        return "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal) + "\"";
    }

    private static void Line(StringBuilder builder, string value)
    {
        builder.Append(value);
        builder.Append('\n');
    }

    private static void WriteDeterministic(string path, string content)
    {
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }

    private sealed class EffectiveParent
    {
        internal EffectiveParent(
            string code,
            string sourceId,
            string sourceRevision,
            string upstreamKey,
            string effectiveDate,
            string sourceField)
        {
            Code = code;
            SourceId = sourceId;
            SourceRevision = sourceRevision;
            UpstreamKey = upstreamKey;
            EffectiveDate = effectiveDate;
            SourceField = sourceField;
        }

        internal string Code { get; }
        internal string SourceId { get; }
        internal string SourceRevision { get; }
        internal string UpstreamKey { get; }
        internal string EffectiveDate { get; }
        internal string SourceField { get; }
    }

    private sealed class SubdivisionAliasEmissionRow
    {
        internal SubdivisionAliasEmissionRow(
            string code,
            string countryCode,
            string reason,
            bool isCurrentSubdivision,
            string sourceId,
            string sourceRevision,
            string upstreamKey,
            string effectiveDate,
            string sourceField)
        {
            Code = code;
            CountryCode = countryCode;
            Reason = reason;
            IsCurrentSubdivision = isCurrentSubdivision;
            SourceId = sourceId;
            SourceRevision = sourceRevision;
            UpstreamKey = upstreamKey;
            EffectiveDate = effectiveDate;
            SourceField = sourceField;
        }

        internal string Code { get; }
        internal string CountryCode { get; }
        internal string Reason { get; }
        internal bool IsCurrentSubdivision { get; }
        internal string SourceId { get; }
        internal string SourceRevision { get; }
        internal string UpstreamKey { get; }
        internal string EffectiveDate { get; }
        internal string SourceField { get; }
    }

    private sealed class SubdivisionReplacementEmissionRow
    {
        internal SubdivisionReplacementEmissionRow(
            string aliasCode,
            string replacementCode,
            string targetKind,
            bool isCurrent,
            string sourceId,
            string sourceRevision,
            string upstreamKey,
            string effectiveDate,
            string sourceField)
        {
            AliasCode = aliasCode;
            ReplacementCode = replacementCode;
            TargetKind = targetKind;
            IsCurrent = isCurrent;
            SourceId = sourceId;
            SourceRevision = sourceRevision;
            UpstreamKey = upstreamKey;
            EffectiveDate = effectiveDate;
            SourceField = sourceField;
        }

        internal string AliasCode { get; }
        internal string ReplacementCode { get; }
        internal string TargetKind { get; }
        internal bool IsCurrent { get; }
        internal string SourceId { get; }
        internal string SourceRevision { get; }
        internal string UpstreamKey { get; }
        internal string EffectiveDate { get; }
        internal string SourceField { get; }
    }

    private sealed class SubdivisionNameEmissionRow
    {
        internal SubdivisionNameEmissionRow(
            string code,
            bool isCurrent,
            string kind,
            string locale,
            string displayName,
            string searchKey,
            string? qualifier,
            string sourceId,
            string sourceRevision,
            string upstreamKey,
            string effectiveDate,
            string sourceField)
        {
            Code = code;
            IsCurrent = isCurrent;
            Kind = kind;
            Locale = locale;
            DisplayName = displayName;
            SearchKey = searchKey;
            Qualifier = qualifier;
            SourceId = sourceId;
            SourceRevision = sourceRevision;
            UpstreamKey = upstreamKey;
            EffectiveDate = effectiveDate;
            SourceField = sourceField;
        }

        internal string Code { get; }
        internal bool IsCurrent { get; }
        internal string Kind { get; }
        internal string Locale { get; }
        internal string DisplayName { get; }
        internal string SearchKey { get; }
        internal string? Qualifier { get; }
        internal string SourceId { get; }
        internal string SourceRevision { get; }
        internal string UpstreamKey { get; }
        internal string EffectiveDate { get; }
        internal string SourceField { get; }
    }
}
