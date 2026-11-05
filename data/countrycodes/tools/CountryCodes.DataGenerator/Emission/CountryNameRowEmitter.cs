using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CountryCodes.DataGenerator.Overrides;
using CountryCodes.DataGenerator.Staging;

namespace CountryCodes.DataGenerator.Emission;

internal static class CountryNameRowEmitter
{
    private const string CountryNameFileName = "CountryNameTable.g.cs";

    internal static CountryNameEmissionResult Emit(
        OverrideApplicationResult input,
        string outputDirectory,
        GeneratedIndexCollector indexes)
    {
        GeneratorStagingData staging = input.Reconciliation.Staging;
        Dictionary<string, StagedCountry> countriesByAlpha2 = IndexCountriesByAlpha2(staging);
        Dictionary<string, StagedCountry> countriesByAlpha3 = IndexCountriesByAlpha3(staging);
        Dictionary<string, StagedCountry> countriesByNumeric = IndexCountriesByNumeric(staging);
        List<CountryNameEmissionRow> rows = new List<CountryNameEmissionRow>();

        AddIsoNames(rows, staging.Countries);
        AddLocalizedAndAliasNames(rows, staging.Names, countriesByAlpha2, countriesByAlpha3);
        AddDeprecatedNames(rows, staging.M49History, countriesByNumeric);
        AddHistoricalNames(rows, staging.HistoricalCountries);
        AddOverrideNames(rows, input.AppliedOverrides, countriesByAlpha2, staging);

        rows.Sort(CompareRows);
        ValidateRows(rows, staging.Countries, staging.HistoricalCountries);
        AddSearchIndexes(rows, indexes);

        string path = Path.Combine(outputDirectory, CountryNameFileName);
        Directory.CreateDirectory(outputDirectory);
        WriteDeterministic(path, Render(rows));
        return new CountryNameEmissionResult(rows.Count, path);
    }

    private static void AddSearchIndexes(
        IReadOnlyList<CountryNameEmissionRow> rows,
        GeneratedIndexCollector indexes)
    {
        for (int index = 0; index < rows.Count; index += 1)
        {
            CountryNameEmissionRow row = rows[index];
            string kind = string.Equals(row.TargetKind, "Country", StringComparison.Ordinal)
                ? "CountryName"
                : "HistoricalCountryName";
            indexes.AddSearch(kind, row.SearchKey, index);
        }
    }

    private static void AddIsoNames(
        List<CountryNameEmissionRow> rows,
        IReadOnlyList<StagedCountry> countries)
    {
        foreach (StagedCountry country in countries)
        {
            AddStagedRow(
                rows,
                "Country",
                country.Alpha2.Value,
                "IsoShort",
                "en",
                country.Name,
                null);
            if (country.OfficialName != null)
            {
                AddStagedRow(
                    rows,
                    "Country",
                    country.Alpha2.Value,
                    "IsoOfficial",
                    "en",
                    country.OfficialName,
                    null);
            }

            if (country.CommonName != null)
            {
                AddStagedRow(
                    rows,
                    "Country",
                    country.Alpha2.Value,
                    "Common",
                    "en",
                    country.CommonName,
                    null);
            }
        }
    }

    private static void AddLocalizedAndAliasNames(
        List<CountryNameEmissionRow> rows,
        IReadOnlyList<StagedName> names,
        IReadOnlyDictionary<string, StagedCountry> countriesByAlpha2,
        IReadOnlyDictionary<string, StagedCountry> countriesByAlpha3)
    {
        foreach (StagedName name in names)
        {
            if (name.Target == StagedNameTarget.UnM49Country)
            {
                if (name.Alpha3 == null ||
                    !countriesByAlpha3.TryGetValue(name.Alpha3.Value, out StagedCountry? country))
                {
                    throw new EmissionException(
                        $"UN M49 name '{name.Origin.UpstreamKey}' does not map to a current country.");
                }

                AddStagedRow(
                    rows,
                    "Country",
                    country.Alpha2.Value,
                    "Localized",
                    RequireLocale(name.Origin),
                    name.Value,
                    null);
                continue;
            }

            if (name.Target != StagedNameTarget.CldrTerritory ||
                !countriesByAlpha2.ContainsKey(name.EntityKey.Value))
            {
                continue;
            }

            string kind = name.Alternate == null ? "Localized" : "Alias";
            string? qualifier = name.Alternate == null ? null : name.Alternate.Value;
            AddStagedRow(
                rows,
                "Country",
                name.EntityKey.Value,
                kind,
                RequireLocale(name.Origin),
                name.Value,
                qualifier);
        }
    }

    private static void AddDeprecatedNames(
        List<CountryNameEmissionRow> rows,
        IReadOnlyList<StagedM49History> history,
        IReadOnlyDictionary<string, StagedCountry> countriesByNumeric)
    {
        foreach (StagedM49History entry in history)
        {
            if (entry.Kind != UnM49HistoryKind.NameChanged)
            {
                continue;
            }

            string? previousName = ReadPreviousName(entry.Note.Value);
            if (previousName == null)
            {
                if (!entry.Note.Value.StartsWith("See footnote", StringComparison.Ordinal))
                {
                    throw new EmissionException(
                        $"M49 name-change row '{entry.Origin.UpstreamKey}' has no recognized previous name.");
                }

                continue;
            }

            if (!countriesByNumeric.TryGetValue(entry.M49Code.Value, out StagedCountry? country))
            {
                throw new EmissionException(
                    $"M49 previous name '{entry.Origin.UpstreamKey}' does not map to a current country.");
            }

            AddRow(
                rows,
                "Country",
                country.Alpha2.Value,
                "Deprecated",
                RequireLocale(entry.Origin),
                previousName,
                "previously-listed",
                entry.Origin.SourceId,
                entry.Origin.SourceRevision,
                entry.Origin.UpstreamKey,
                entry.Origin.EffectiveDate,
                entry.Note.Provenance.SourceField);
        }
    }

    private static void AddHistoricalNames(
        List<CountryNameEmissionRow> rows,
        IReadOnlyList<StagedHistoricalCountry> historicalCountries)
    {
        foreach (StagedHistoricalCountry historical in historicalCountries)
        {
            AddStagedRow(
                rows,
                "HistoricalCountry",
                historical.Alpha4.Value,
                "Historical",
                "en",
                historical.Name,
                null);
        }
    }

    private static void AddOverrideNames(
        List<CountryNameEmissionRow> rows,
        IReadOnlyList<AppliedOverride> overrides,
        IReadOnlyDictionary<string, StagedCountry> countriesByAlpha2,
        GeneratorStagingData staging)
    {
        HashSet<string> historicalKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (StagedHistoricalCountry historical in staging.HistoricalCountries)
        {
            historicalKeys.Add(historical.Alpha4.Value);
        }

        foreach (AppliedOverride applied in overrides)
        {
            OverrideEntry entry = applied.Definition;
            if (string.Equals(entry.Kind, "alias", StringComparison.Ordinal) &&
                (string.Equals(entry.TargetKind, "country", StringComparison.Ordinal) ||
                 string.Equals(entry.TargetKind, "historical-country", StringComparison.Ordinal)))
            {
                string targetKind = string.Equals(entry.TargetKind, "country", StringComparison.Ordinal)
                    ? "Country"
                    : "HistoricalCountry";
                ValidateOverrideTarget(
                    entry,
                    countriesByAlpha2.ContainsKey(entry.TargetKey),
                    historicalKeys.Contains(entry.TargetKey));
                foreach (string value in entry.Values)
                {
                    AddOverrideRow(rows, entry, targetKind, "Alias", value);
                }
            }
            else if (string.Equals(entry.Kind, "supplement", StringComparison.Ordinal) &&
                     string.Equals(entry.TargetKind, "country", StringComparison.Ordinal) &&
                     (string.Equals(entry.Field, "official-name", StringComparison.Ordinal) ||
                      string.Equals(entry.Field, "common-name", StringComparison.Ordinal)))
            {
                string kind = string.Equals(entry.Field, "official-name", StringComparison.Ordinal)
                    ? "IsoOfficial"
                    : "Common";
                foreach (string value in entry.Values)
                {
                    AddOverrideRow(rows, entry, "Country", kind, value);
                }
            }
        }
    }

    private static void AddOverrideRow(
        List<CountryNameEmissionRow> rows,
        OverrideEntry entry,
        string targetKind,
        string nameKind,
        string value)
    {
        AddRow(
            rows,
            targetKind,
            entry.TargetKey,
            nameKind,
            "en",
            value,
            entry.Id,
            entry.Provenance.SourceId,
            entry.ExpiryCondition.Revision,
            entry.Provenance.UpstreamKey,
            entry.EffectiveDate,
            entry.Field);
    }

    private static void ValidateOverrideTarget(
        OverrideEntry entry,
        bool currentCountryExists,
        bool historicalCountryExists)
    {
        bool exists = string.Equals(entry.TargetKind, "country", StringComparison.Ordinal)
            ? currentCountryExists
            : historicalCountryExists;
        if (!exists)
        {
            throw new EmissionException(
                $"Name override '{entry.Id}' targets an unknown {entry.TargetKind}.");
        }
    }

    private static void AddStagedRow(
        List<CountryNameEmissionRow> rows,
        string targetKind,
        string targetCode,
        string nameKind,
        string locale,
        StagedText text,
        string? qualifier)
    {
        StagingOrigin origin = text.Provenance.Origin;
        AddRow(
            rows,
            targetKind,
            targetCode,
            nameKind,
            locale,
            text.Value,
            qualifier,
            origin.SourceId,
            origin.SourceRevision,
            origin.UpstreamKey,
            origin.EffectiveDate,
            text.Provenance.SourceField);
    }

    private static void AddRow(
        List<CountryNameEmissionRow> rows,
        string targetKind,
        string targetCode,
        string nameKind,
        string locale,
        string displayName,
        string? qualifier,
        string sourceId,
        string sourceRevision,
        string upstreamKey,
        string effectiveDate,
        string sourceField)
    {
        string normalizedDisplay = NameEmissionUtility.NormalizeDisplay(displayName);
        string searchKey = NameEmissionUtility.CreateSearchKey(normalizedDisplay);
        if (normalizedDisplay.Length == 0 || searchKey.Length == 0)
        {
            throw new EmissionException(
                $"Name row '{sourceId}:{upstreamKey}' has no searchable display value.");
        }

        rows.Add(new CountryNameEmissionRow(
            targetKind,
            targetCode,
            nameKind,
            locale,
            normalizedDisplay,
            searchKey,
            qualifier,
            sourceId,
            sourceRevision,
            upstreamKey,
            effectiveDate,
            sourceField));
    }

    private static string? ReadPreviousName(string note)
    {
        const string ListedPrefix = "Previously listed as ";
        const string PreviousPrefix = "Previously ";
        if (note.StartsWith(ListedPrefix, StringComparison.Ordinal))
        {
            return note.Substring(ListedPrefix.Length);
        }

        if (note.StartsWith(PreviousPrefix, StringComparison.Ordinal))
        {
            return note.Substring(PreviousPrefix.Length);
        }

        return null;
    }

    private static string RequireLocale(StagingOrigin origin)
    {
        if (string.IsNullOrEmpty(origin.Locale))
        {
            throw new EmissionException(
                $"Localized name '{origin.SourceId}:{origin.UpstreamKey}' has no locale.");
        }

        return origin.Locale;
    }

    private static Dictionary<string, StagedCountry> IndexCountriesByAlpha2(
        GeneratorStagingData staging)
    {
        Dictionary<string, StagedCountry> result = new Dictionary<string, StagedCountry>(
            StringComparer.Ordinal);
        foreach (StagedCountry country in staging.Countries)
        {
            result.Add(country.Alpha2.Value, country);
        }

        return result;
    }

    private static Dictionary<string, StagedCountry> IndexCountriesByAlpha3(
        GeneratorStagingData staging)
    {
        Dictionary<string, StagedCountry> result = new Dictionary<string, StagedCountry>(
            StringComparer.Ordinal);
        foreach (StagedCountry country in staging.Countries)
        {
            result.Add(country.Alpha3.Value, country);
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
            result.Add(country.Numeric.Value, country);
        }

        return result;
    }

    private static void ValidateRows(
        IReadOnlyList<CountryNameEmissionRow> rows,
        IReadOnlyList<StagedCountry> countries,
        IReadOnlyList<StagedHistoricalCountry> historicalCountries)
    {
        HashSet<string> canonicalCountries = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> historicalNames = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> categories = new HashSet<string>(StringComparer.Ordinal);
        CountryNameEmissionRow? previous = null;
        foreach (CountryNameEmissionRow row in rows)
        {
            if (previous != null && CompareRows(previous, row) >= 0)
            {
                throw new EmissionException(
                    "Country-name rows are not unique stable ordinal order.");
            }

            previous = row;
            categories.Add(row.NameKind);
            if (string.Equals(row.TargetKind, "Country", StringComparison.Ordinal) &&
                string.Equals(row.NameKind, "IsoShort", StringComparison.Ordinal))
            {
                canonicalCountries.Add(row.TargetCode);
            }

            if (string.Equals(row.TargetKind, "HistoricalCountry", StringComparison.Ordinal) &&
                string.Equals(row.NameKind, "Historical", StringComparison.Ordinal))
            {
                historicalNames.Add(row.TargetCode);
            }
        }

        if (canonicalCountries.Count != countries.Count)
        {
            throw new EmissionException("Not every current country has one canonical name row.");
        }

        if (historicalNames.Count != historicalCountries.Count)
        {
            throw new EmissionException("Not every historical country has one historical name row.");
        }

        string[] requiredCategories =
        {
            "IsoShort", "IsoOfficial", "Common", "Localized", "Alias", "Deprecated", "Historical"
        };
        foreach (string category in requiredCategories)
        {
            if (!categories.Contains(category))
            {
                throw new EmissionException($"Country-name rows have no '{category}' values.");
            }
        }
    }

    private static int CompareRows(CountryNameEmissionRow left, CountryNameEmissionRow right)
    {
        int comparison = TargetKindOrder(left.TargetKind).CompareTo(TargetKindOrder(right.TargetKind));
        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(left.TargetCode, right.TargetCode);
        }

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
            comparison = NameKindOrder(left.NameKind).CompareTo(NameKindOrder(right.NameKind));
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

        if (comparison == 0)
        {
            comparison = string.CompareOrdinal(left.SourceField, right.SourceField);
        }

        return comparison == 0
            ? string.CompareOrdinal(left.Qualifier ?? string.Empty, right.Qualifier ?? string.Empty)
            : comparison;
    }

    private static int TargetKindOrder(string targetKind)
    {
        return string.Equals(targetKind, "Country", StringComparison.Ordinal) ? 0 : 1;
    }

    private static int NameKindOrder(string nameKind)
    {
        if (string.Equals(nameKind, "IsoShort", StringComparison.Ordinal))
        {
            return 0;
        }

        if (string.Equals(nameKind, "IsoOfficial", StringComparison.Ordinal))
        {
            return 1;
        }

        if (string.Equals(nameKind, "Common", StringComparison.Ordinal))
        {
            return 2;
        }

        if (string.Equals(nameKind, "Localized", StringComparison.Ordinal))
        {
            return 3;
        }

        if (string.Equals(nameKind, "Alias", StringComparison.Ordinal))
        {
            return 4;
        }

        if (string.Equals(nameKind, "Deprecated", StringComparison.Ordinal))
        {
            return 5;
        }

        return 6;
    }

    private static string Render(IReadOnlyList<CountryNameEmissionRow> rows)
    {
        StringBuilder builder = new StringBuilder(1_000_000);
        Line(builder, "// <auto-generated />");
        Line(builder, "namespace countrycodes;");
        Line(builder, string.Empty);
        Line(builder, "internal enum GeneratedCountryNameTargetKind");
        Line(builder, "{");
        Line(builder, "    Country = 0,");
        Line(builder, "    HistoricalCountry = 1");
        Line(builder, "}");
        Line(builder, string.Empty);
        Line(builder, "internal enum GeneratedCountryNameKind");
        Line(builder, "{");
        Line(builder, "    IsoShort = 0,");
        Line(builder, "    IsoOfficial = 1,");
        Line(builder, "    Common = 2,");
        Line(builder, "    Localized = 3,");
        Line(builder, "    Alias = 4,");
        Line(builder, "    Deprecated = 5,");
        Line(builder, "    Historical = 6");
        Line(builder, "}");
        Line(builder, string.Empty);
        Line(builder, "internal class GeneratedCountryNameRow");
        Line(builder, "{");
        Line(builder, "    internal GeneratedCountryNameTargetKind TargetKind;");
        Line(builder, "    internal string TargetCode;");
        Line(builder, "    internal GeneratedCountryNameKind NameKind;");
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
        Line(builder, "    internal GeneratedCountryNameRow(");
        Line(builder, "        GeneratedCountryNameTargetKind targetKind,");
        Line(builder, "        string targetCode,");
        Line(builder, "        GeneratedCountryNameKind nameKind,");
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
        Line(builder, "        TargetKind = targetKind;");
        Line(builder, "        TargetCode = targetCode;");
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
        Line(builder, "internal static class CountryNameTable");
        Line(builder, "{");
        Line(builder, "    internal static readonly GeneratedCountryNameRow[] Rows =");
        Line(builder, "    {");
        foreach (CountryNameEmissionRow row in rows)
        {
            Line(builder, "        new GeneratedCountryNameRow(");
            Line(builder, $"            GeneratedCountryNameTargetKind.{row.TargetKind},");
            Line(builder, $"            {Literal(row.TargetCode)},");
            Line(builder, $"            GeneratedCountryNameKind.{row.NameKind},");
            Line(builder, $"            {Literal(row.Locale)},");
            Line(builder, $"            {Literal(row.DisplayName)},");
            Line(builder, $"            {Literal(row.SearchKey)},");
            Line(builder, $"            {(row.Qualifier == null ? "false" : "true")},");
            Line(builder, $"            {Literal(row.Qualifier ?? string.Empty)},");
            Line(builder, $"            {Literal(row.SourceId)},");
            Line(builder, $"            {Literal(row.SourceRevision)},");
            Line(builder, $"            {Literal(row.UpstreamKey)},");
            Line(builder, $"            {Literal(row.EffectiveDate)},");
            Line(builder, $"            {Literal(row.SourceField)}),");
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

    private sealed class CountryNameEmissionRow
    {
        internal CountryNameEmissionRow(
            string targetKind,
            string targetCode,
            string nameKind,
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
            TargetKind = targetKind;
            TargetCode = targetCode;
            NameKind = nameKind;
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

        internal string TargetKind { get; }

        internal string TargetCode { get; }

        internal string NameKind { get; }

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
