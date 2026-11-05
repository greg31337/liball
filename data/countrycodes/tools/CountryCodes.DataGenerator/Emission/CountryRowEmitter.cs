using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CountryCodes.DataGenerator.Overrides;
using CountryCodes.DataGenerator.Staging;

namespace CountryCodes.DataGenerator.Emission;

internal static class CountryRowEmitter
{
    private const string CountryFileName = "CountryTable.g.cs";
    private const string ReservedFileName = "ReservedCodeTable.g.cs";
    private const string IsoGlossarySource = "iso-3166-glossary-review";

    internal static CountryRowEmissionSummary Emit(
        OverrideApplicationResult input,
        SourceManifestInfo manifest,
        string outputDirectory)
    {
        GeneratorStagingData staging = input.Reconciliation.Staging;
        GeneratedIndexCollector indexes = new GeneratedIndexCollector();
        List<StagedCountry> countries = new List<StagedCountry>(staging.Countries);
        countries.Sort(CompareCountries);
        ValidateCountryOrder(countries);

        List<ReservedEmissionRow> reserved = BuildReservedRows(staging);
        List<UserAssignedRangeInput> ranges = BuildUserAssignedRanges();
        string countryPath = Path.Combine(outputDirectory, CountryFileName);
        string reservedPath = Path.Combine(outputDirectory, ReservedFileName);

        Directory.CreateDirectory(outputDirectory);
        WriteDeterministic(countryPath, RenderCountryTable(countries));
        WriteDeterministic(reservedPath, RenderReservedTable(reserved, ranges));
        AddCountryIndexes(countries, reserved, indexes);
        CountryNameEmissionResult names = CountryNameRowEmitter.Emit(input, outputDirectory, indexes);
        SubdivisionEmissionResult subdivisions = SubdivisionRowEmitter.Emit(
            input,
            outputDirectory,
            indexes);
        HistoricalCountryEmissionResult historical = HistoricalCountryRowEmitter.Emit(
            input,
            outputDirectory,
            indexes);
        GeographicRegionEmissionResult regions = GeographicRegionRowEmitter.Emit(
            staging,
            outputDirectory,
            indexes);
        GeneratedIndexEmissionResult generatedIndexes = GeneratedIndexEmitter.Emit(
            indexes,
            outputDirectory);
        List<string> contentPaths = new List<string>
        {
            countryPath,
            reservedPath,
            names.Path,
            subdivisions.TablePath,
            subdivisions.NameTablePath,
            historical.TablePath,
            historical.ChangeTablePath,
            regions.TablePath,
            generatedIndexes.Path
        };
        GeneratedMetadataEmissionResult metadata = GeneratedMetadataEmitter.Emit(
            manifest,
            contentPaths,
            outputDirectory);
        return new CountryRowEmissionSummary(
            countries.Count,
            reserved.Count,
            ranges.Count,
            names.RowCount,
            subdivisions.RowCount,
            subdivisions.NameCount,
            subdivisions.AliasCount,
            subdivisions.ReplacementCount,
            historical.RowCount,
            historical.ChangeCount,
            historical.PredecessorCount,
            historical.SuccessorCount,
            regions.RegionCount,
            regions.NameCount,
            regions.MappingCount,
            generatedIndexes.ExactCount,
            generatedIndexes.SearchCount,
            metadata.SourceRevisionCount,
            metadata.ContentHashCount,
            metadata.AggregateContentChecksum,
            countryPath,
            reservedPath,
            names.Path,
            subdivisions.TablePath,
            subdivisions.NameTablePath,
            historical.TablePath,
            historical.ChangeTablePath,
            regions.TablePath,
            generatedIndexes.Path,
            metadata.Path);
    }

    private static void AddCountryIndexes(
        IReadOnlyList<StagedCountry> countries,
        IReadOnlyList<ReservedEmissionRow> reserved,
        GeneratedIndexCollector indexes)
    {
        for (int index = 0; index < countries.Count; index += 1)
        {
            StagedCountry row = countries[index];
            indexes.AddExact("CountryAlpha2", row.Alpha2.Value, index);
            indexes.AddExact("CountryAlpha3", row.Alpha3.Value, index);
            indexes.AddExact("CountryNumeric", row.Numeric.Value, index);
        }

        for (int index = 0; index < reserved.Count; index += 1)
        {
            ReservedEmissionRow row = reserved[index];
            string kind;
            if (string.Equals(row.Kind, "Alpha2", StringComparison.Ordinal))
            {
                kind = "ReservedAlpha2";
            }
            else if (string.Equals(row.Kind, "Alpha3", StringComparison.Ordinal))
            {
                kind = "ReservedAlpha3";
            }
            else if (string.Equals(row.Kind, "Numeric3", StringComparison.Ordinal))
            {
                kind = "ReservedNumeric";
            }
            else
            {
                kind = "ReservedAlpha4";
            }

            indexes.AddExact(kind, row.Code, index);
        }
    }

    private static List<ReservedEmissionRow> BuildReservedRows(GeneratorStagingData staging)
    {
        HashSet<string> assigned = BuildAssignedKeys(staging.Countries);
        Dictionary<string, MutableReservedRow> rows = new Dictionary<string, MutableReservedRow>(
            StringComparer.Ordinal);
        foreach (StagedHistoricalCountry historical in staging.HistoricalCountries)
        {
            AddFormerIdentifier(rows, assigned, "Alpha2", historical.Alpha2.Value, historical);
            AddFormerIdentifier(rows, assigned, "Alpha3", historical.Alpha3.Value, historical);
            AddFormerIdentifier(rows, assigned, "Alpha4", historical.Alpha4.Value, historical);
            if (historical.Numeric != null)
            {
                AddFormerIdentifier(rows, assigned, "Numeric3", historical.Numeric.Value, historical);
            }
        }

        AddReviewedReservation(
            rows,
            assigned,
            "Alpha2",
            "UK",
            "ExceptionallyReserved",
            "UK");
        AddReviewedReservation(
            rows,
            assigned,
            "Alpha2",
            "YU",
            "TransitionallyReserved",
            "YU");

        List<ReservedEmissionRow> result = new List<ReservedEmissionRow>(rows.Count);
        foreach (MutableReservedRow row in rows.Values)
        {
            result.Add(row.Freeze());
        }

        result.Sort(CompareReservedRows);
        ValidateReservedOrder(result);
        return result;
    }

    private static HashSet<string> BuildAssignedKeys(IReadOnlyList<StagedCountry> countries)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
        foreach (StagedCountry country in countries)
        {
            result.Add("Alpha2:" + country.Alpha2.Value);
            result.Add("Alpha3:" + country.Alpha3.Value);
            result.Add("Numeric3:" + country.Numeric.Value);
        }

        return result;
    }

    private static void AddFormerIdentifier(
        Dictionary<string, MutableReservedRow> rows,
        IReadOnlySet<string> assigned,
        string kind,
        string code,
        StagedHistoricalCountry historical)
    {
        string key = kind + ":" + code;
        if (assigned.Contains(key))
        {
            return;
        }

        if (!rows.TryGetValue(key, out MutableReservedRow? row))
        {
            row = new MutableReservedRow(
                kind,
                code,
                "FormerlyAssigned",
                historical.Origin.SourceId);
            rows.Add(key, row);
        }

        row.AddUpstreamKey(historical.Origin.UpstreamKey);
    }

    private static void AddReviewedReservation(
        Dictionary<string, MutableReservedRow> rows,
        IReadOnlySet<string> assigned,
        string kind,
        string code,
        string state,
        string upstreamKey)
    {
        string key = kind + ":" + code;
        if (assigned.Contains(key))
        {
            throw new EmissionException(
                $"Reviewed reservation '{key}' conflicts with a current assignment.");
        }

        MutableReservedRow row = new MutableReservedRow(kind, code, state, IsoGlossarySource);
        row.AddUpstreamKey(upstreamKey);
        rows[key] = row;
    }

    private static List<UserAssignedRangeInput> BuildUserAssignedRanges()
    {
        return new List<UserAssignedRangeInput>
        {
            new UserAssignedRangeInput("Alpha2", "AA", "AA", "alpha-2:AA"),
            new UserAssignedRangeInput("Alpha2", "QM", "QZ", "alpha-2:QM-QZ"),
            new UserAssignedRangeInput("Alpha2", "XA", "XZ", "alpha-2:XA-XZ"),
            new UserAssignedRangeInput("Alpha2", "ZZ", "ZZ", "alpha-2:ZZ"),
            new UserAssignedRangeInput("Alpha3", "AAA", "AAZ", "alpha-3:AAA-AAZ"),
            new UserAssignedRangeInput("Alpha3", "QMA", "QZZ", "alpha-3:QMA-QZZ"),
            new UserAssignedRangeInput("Alpha3", "XAA", "XZZ", "alpha-3:XAA-XZZ"),
            new UserAssignedRangeInput("Alpha3", "ZZA", "ZZZ", "alpha-3:ZZA-ZZZ"),
            new UserAssignedRangeInput("Numeric3", "900", "999", "numeric-3:900-999")
        };
    }

    private static string RenderCountryTable(IReadOnlyList<StagedCountry> countries)
    {
        StringBuilder builder = new StringBuilder(64_000);
        Line(builder, "// <auto-generated />");
        Line(builder, "namespace countrycodes;");
        Line(builder, string.Empty);
        RenderSharedTypes(builder);
        Line(builder, string.Empty);
        Line(builder, "internal class GeneratedCountryRow");
        Line(builder, "{");
        Line(builder, "    internal string Alpha2;");
        Line(builder, "    internal string Alpha3;");
        Line(builder, "    internal string NumericText;");
        Line(builder, "    internal int NumericValue;");
        Line(builder, "    internal GeneratedCodeAssignmentState AssignmentState;");
        Line(builder, "    internal string SourceId;");
        Line(builder, "    internal string UpstreamKey;");
        Line(builder, string.Empty);
        Line(builder, "    internal GeneratedCountryRow(");
        Line(builder, "        string alpha2,");
        Line(builder, "        string alpha3,");
        Line(builder, "        string numericText,");
        Line(builder, "        int numericValue,");
        Line(builder, "        GeneratedCodeAssignmentState assignmentState,");
        Line(builder, "        string sourceId,");
        Line(builder, "        string upstreamKey)");
        Line(builder, "    {");
        Line(builder, "        Alpha2 = alpha2;");
        Line(builder, "        Alpha3 = alpha3;");
        Line(builder, "        NumericText = numericText;");
        Line(builder, "        NumericValue = numericValue;");
        Line(builder, "        AssignmentState = assignmentState;");
        Line(builder, "        SourceId = sourceId;");
        Line(builder, "        UpstreamKey = upstreamKey;");
        Line(builder, "    }");
        Line(builder, "}");
        Line(builder, string.Empty);
        Line(builder, "internal static class CountryTable");
        Line(builder, "{");
        Line(builder, "    internal static readonly GeneratedCountryRow[] Rows =");
        Line(builder, "    {");
        foreach (StagedCountry country in countries)
        {
            int numericValue = int.Parse(country.Numeric.Value, CultureInfo.InvariantCulture);
            Line(builder, "        new GeneratedCountryRow(");
            Line(builder, $"            {Literal(country.Alpha2.Value)},");
            Line(builder, $"            {Literal(country.Alpha3.Value)},");
            Line(builder, $"            {Literal(country.Numeric.Value)},");
            Line(builder, $"            {numericValue.ToString(CultureInfo.InvariantCulture)},");
            Line(builder, "            GeneratedCodeAssignmentState.Assigned,");
            Line(builder, $"            {Literal(country.Origin.SourceId)},");
            Line(builder, $"            {Literal(country.Origin.UpstreamKey)}),");
        }

        Line(builder, "    };");
        Line(builder, "}");
        return builder.ToString();
    }

    private static string RenderReservedTable(
        IReadOnlyList<ReservedEmissionRow> rows,
        IReadOnlyList<UserAssignedRangeInput> ranges)
    {
        StringBuilder builder = new StringBuilder(48_000);
        Line(builder, "// <auto-generated />");
        Line(builder, "namespace countrycodes;");
        Line(builder, string.Empty);
        Line(builder, "internal class GeneratedReservedCodeRow");
        Line(builder, "{");
        Line(builder, "    internal GeneratedCodeKind Kind;");
        Line(builder, "    internal string Code;");
        Line(builder, "    internal GeneratedCodeAssignmentState AssignmentState;");
        Line(builder, "    internal string SourceId;");
        Line(builder, "    internal string UpstreamKey;");
        Line(builder, string.Empty);
        Line(builder, "    internal GeneratedReservedCodeRow(");
        Line(builder, "        GeneratedCodeKind kind,");
        Line(builder, "        string code,");
        Line(builder, "        GeneratedCodeAssignmentState assignmentState,");
        Line(builder, "        string sourceId,");
        Line(builder, "        string upstreamKey)");
        Line(builder, "    {");
        Line(builder, "        Kind = kind;");
        Line(builder, "        Code = code;");
        Line(builder, "        AssignmentState = assignmentState;");
        Line(builder, "        SourceId = sourceId;");
        Line(builder, "        UpstreamKey = upstreamKey;");
        Line(builder, "    }");
        Line(builder, "}");
        Line(builder, string.Empty);
        Line(builder, "internal class GeneratedUserAssignedRange");
        Line(builder, "{");
        Line(builder, "    internal GeneratedCodeKind Kind;");
        Line(builder, "    internal string Start;");
        Line(builder, "    internal string End;");
        Line(builder, "    internal GeneratedCodeAssignmentState AssignmentState;");
        Line(builder, "    internal string SourceId;");
        Line(builder, "    internal string UpstreamKey;");
        Line(builder, string.Empty);
        Line(builder, "    internal GeneratedUserAssignedRange(");
        Line(builder, "        GeneratedCodeKind kind,");
        Line(builder, "        string start,");
        Line(builder, "        string end,");
        Line(builder, "        GeneratedCodeAssignmentState assignmentState,");
        Line(builder, "        string sourceId,");
        Line(builder, "        string upstreamKey)");
        Line(builder, "    {");
        Line(builder, "        Kind = kind;");
        Line(builder, "        Start = start;");
        Line(builder, "        End = end;");
        Line(builder, "        AssignmentState = assignmentState;");
        Line(builder, "        SourceId = sourceId;");
        Line(builder, "        UpstreamKey = upstreamKey;");
        Line(builder, "    }");
        Line(builder, "}");
        Line(builder, string.Empty);
        Line(builder, "internal static class ReservedCodeTable");
        Line(builder, "{");
        Line(builder, "    internal static readonly GeneratedReservedCodeRow[] Rows =");
        Line(builder, "    {");
        foreach (ReservedEmissionRow row in rows)
        {
            Line(builder, "        new GeneratedReservedCodeRow(");
            Line(builder, $"            GeneratedCodeKind.{row.Kind},");
            Line(builder, $"            {Literal(row.Code)},");
            Line(builder, $"            GeneratedCodeAssignmentState.{row.State},");
            Line(builder, $"            {Literal(row.SourceId)},");
            Line(builder, $"            {Literal(row.UpstreamKey)}),");
        }

        Line(builder, "    };");
        Line(builder, string.Empty);
        Line(builder, "    internal static readonly GeneratedUserAssignedRange[] UserAssignedRanges =");
        Line(builder, "    {");
        foreach (UserAssignedRangeInput range in ranges)
        {
            Line(builder, "        new GeneratedUserAssignedRange(");
            Line(builder, $"            GeneratedCodeKind.{range.Kind},");
            Line(builder, $"            {Literal(range.Start)},");
            Line(builder, $"            {Literal(range.End)},");
            Line(builder, "            GeneratedCodeAssignmentState.UserAssigned,");
            Line(builder, $"            {Literal(IsoGlossarySource)},");
            Line(builder, $"            {Literal(range.UpstreamKey)}),");
        }

        Line(builder, "    };");
        Line(builder, "}");
        return builder.ToString();
    }

    private static void RenderSharedTypes(StringBuilder builder)
    {
        Line(builder, "internal enum GeneratedCodeAssignmentState");
        Line(builder, "{");
        Line(builder, "    Assigned = 0,");
        Line(builder, "    ExceptionallyReserved = 1,");
        Line(builder, "    TransitionallyReserved = 2,");
        Line(builder, "    IndeterminatelyReserved = 3,");
        Line(builder, "    FormerlyAssigned = 4,");
        Line(builder, "    UserAssigned = 5,");
        Line(builder, "    Unassigned = 6");
        Line(builder, "}");
        Line(builder, string.Empty);
        Line(builder, "internal enum GeneratedCodeKind");
        Line(builder, "{");
        Line(builder, "    Alpha2 = 0,");
        Line(builder, "    Alpha3 = 1,");
        Line(builder, "    Numeric3 = 2,");
        Line(builder, "    Alpha4 = 3");
        Line(builder, "}");
    }

    private static void ValidateCountryOrder(IReadOnlyList<StagedCountry> countries)
    {
        string? previous = null;
        foreach (StagedCountry country in countries)
        {
            if (previous != null && string.CompareOrdinal(previous, country.Alpha2.Value) >= 0)
            {
                throw new EmissionException("Current-country rows are not unique ordinal alpha-2 order.");
            }

            previous = country.Alpha2.Value;
        }
    }

    private static void ValidateReservedOrder(IReadOnlyList<ReservedEmissionRow> rows)
    {
        ReservedEmissionRow? previous = null;
        foreach (ReservedEmissionRow row in rows)
        {
            if (previous != null && CompareReservedRows(previous, row) >= 0)
            {
                throw new EmissionException("Reserved-code rows are not unique stable ordinal order.");
            }

            previous = row;
        }
    }

    private static int CompareCountries(StagedCountry left, StagedCountry right)
    {
        return string.CompareOrdinal(left.Alpha2.Value, right.Alpha2.Value);
    }

    private static int CompareReservedRows(ReservedEmissionRow left, ReservedEmissionRow right)
    {
        int comparison = KindOrder(left.Kind).CompareTo(KindOrder(right.Kind));
        return comparison != 0 ? comparison : string.CompareOrdinal(left.Code, right.Code);
    }

    private static int KindOrder(string kind)
    {
        if (string.Equals(kind, "Alpha2", StringComparison.Ordinal))
        {
            return 0;
        }

        if (string.Equals(kind, "Alpha3", StringComparison.Ordinal))
        {
            return 1;
        }

        if (string.Equals(kind, "Numeric3", StringComparison.Ordinal))
        {
            return 2;
        }

        return 3;
    }

    private static string Literal(string value)
    {
        return "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
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

    private sealed class MutableReservedRow
    {
        private readonly SortedSet<string> upstreamKeys = new SortedSet<string>(StringComparer.Ordinal);

        internal MutableReservedRow(string kind, string code, string state, string sourceId)
        {
            Kind = kind;
            Code = code;
            State = state;
            SourceId = sourceId;
        }

        internal string Kind { get; }

        internal string Code { get; }

        internal string State { get; }

        internal string SourceId { get; }

        internal void AddUpstreamKey(string value)
        {
            upstreamKeys.Add(value);
        }

        internal ReservedEmissionRow Freeze()
        {
            return new ReservedEmissionRow(
                Kind,
                Code,
                State,
                SourceId,
                string.Join("|", upstreamKeys));
        }
    }

    private sealed class ReservedEmissionRow
    {
        internal ReservedEmissionRow(
            string kind,
            string code,
            string state,
            string sourceId,
            string upstreamKey)
        {
            Kind = kind;
            Code = code;
            State = state;
            SourceId = sourceId;
            UpstreamKey = upstreamKey;
        }

        internal string Kind { get; }

        internal string Code { get; }

        internal string State { get; }

        internal string SourceId { get; }

        internal string UpstreamKey { get; }
    }

    private sealed class UserAssignedRangeInput
    {
        internal UserAssignedRangeInput(string kind, string start, string end, string upstreamKey)
        {
            Kind = kind;
            Start = start;
            End = end;
            UpstreamKey = upstreamKey;
        }

        internal string Kind { get; }

        internal string Start { get; }

        internal string End { get; }

        internal string UpstreamKey { get; }
    }
}
