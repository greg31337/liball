using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CountryCodes.DataGenerator.Overrides;
using CountryCodes.DataGenerator.Staging;

namespace CountryCodes.DataGenerator.Emission;

internal static class HistoricalCountryRowEmitter
{
    private const string HistoricalFileName = "HistoricalCountryTable.g.cs";
    private const string ChangeFileName = "CountryChangeTable.g.cs";

    internal static HistoricalCountryEmissionResult Emit(
        OverrideApplicationResult input,
        string outputDirectory,
        GeneratedIndexCollector indexes)
    {
        GeneratorStagingData staging = input.Reconciliation.Staging;
        List<StagedHistoricalCountry> historical =
            new List<StagedHistoricalCountry>(staging.HistoricalCountries);
        historical.Sort(CompareHistorical);

        Dictionary<string, StagedCountry> countriesByAlpha2 = IndexCountriesByAlpha2(staging);
        Dictionary<string, StagedCountry> countriesByNumeric = IndexCountriesByNumeric(staging);
        Dictionary<string, StagedCountry> countriesByName = IndexCountriesByName(staging);
        Dictionary<string, List<StagedHistoricalCountry>> historicalByAlpha2 =
            IndexHistoricalByAlpha2(historical);
        Dictionary<string, StagedHistoricalCountry> historicalByAlpha4 =
            IndexHistoricalByAlpha4(historical);
        Dictionary<string, StagedAlias> territoryAliases = IndexTerritoryAliases(staging);

        List<CountryChangeEmissionRow> changes = new List<CountryChangeEmissionRow>();
        List<CountryChangeLinkEmissionRow> predecessors =
            new List<CountryChangeLinkEmissionRow>();
        List<CountryChangeLinkEmissionRow> successors =
            new List<CountryChangeLinkEmissionRow>();
        foreach (StagedHistoricalCountry row in historical)
        {
            predecessors.Add(CreatePredecessor(row));
            List<CountryChangeLinkEmissionRow> rowSuccessors = ResolveSuccessors(
                row,
                historicalByAlpha2,
                countriesByAlpha2,
                countriesByNumeric,
                countriesByName,
                territoryAliases,
                staging.M49History,
                input.AppliedOverrides);
            successors.AddRange(rowSuccessors);
            changes.Add(CreateChange(row, rowSuccessors, countriesByAlpha2, historicalByAlpha4));
        }

        changes.Sort(CompareChanges);
        predecessors.Sort(CompareLinks);
        successors.Sort(CompareLinks);
        Validate(
            historical,
            changes,
            predecessors,
            successors,
            countriesByAlpha2,
            historicalByAlpha4);
        AddIndexes(historical, changes, indexes);

        string historicalPath = Path.Combine(outputDirectory, HistoricalFileName);
        string changePath = Path.Combine(outputDirectory, ChangeFileName);
        Directory.CreateDirectory(outputDirectory);
        WriteDeterministic(historicalPath, RenderHistoricalTable(historical));
        WriteDeterministic(changePath, RenderChangeTable(changes, predecessors, successors));
        return new HistoricalCountryEmissionResult(
            historical.Count,
            changes.Count,
            predecessors.Count,
            successors.Count,
            historicalPath,
            changePath);
    }

    private static void AddIndexes(
        IReadOnlyList<StagedHistoricalCountry> historical,
        IReadOnlyList<CountryChangeEmissionRow> changes,
        GeneratedIndexCollector indexes)
    {
        for (int index = 0; index < historical.Count; index += 1)
        {
            StagedHistoricalCountry row = historical[index];
            indexes.AddExact("HistoricalAlpha2", row.Alpha2.Value, index);
            indexes.AddExact("HistoricalAlpha3", row.Alpha3.Value, index);
            indexes.AddExact("HistoricalAlpha4", row.Alpha4.Value, index);
            if (row.Numeric != null)
            {
                indexes.AddExact("HistoricalNumeric", row.Numeric.Value, index);
            }
        }

        for (int index = 0; index < changes.Count; index += 1)
        {
            indexes.AddExact("CountryChangeAlpha4", changes[index].Alpha4, index);
        }
    }

    private static CountryChangeLinkEmissionRow CreatePredecessor(StagedHistoricalCountry row)
    {
        StagingOrigin origin = row.Alpha4.Provenance.Origin;
        return new CountryChangeLinkEmissionRow(
            row.Alpha4.Value,
            "HistoricalCountry",
            row.Alpha4.Value,
            origin.SourceId,
            origin.SourceRevision,
            origin.UpstreamKey,
            origin.EffectiveDate,
            row.Alpha4.Provenance.SourceField);
    }

    private static CountryChangeEmissionRow CreateChange(
        StagedHistoricalCountry row,
        IReadOnlyList<CountryChangeLinkEmissionRow> successors,
        IReadOnlyDictionary<string, StagedCountry> countries,
        IReadOnlyDictionary<string, StagedHistoricalCountry> historical)
    {
        string kind;
        if (successors.Count > 1)
        {
            kind = "Split";
        }
        else if (successors.Count == 0)
        {
            kind = "OtherOrUnknown";
        }
        else if (HasSameNumericIdentity(row, successors[0], countries, historical))
        {
            kind = "SignificantChange";
        }
        else
        {
            kind = "Merger";
        }

        StagingOrigin origin = row.Origin;
        return new CountryChangeEmissionRow(
            row.Alpha4.Value,
            kind,
            row.WithdrawalDate != null,
            row.WithdrawalDate?.Value ?? string.Empty,
            origin.SourceId,
            origin.SourceRevision,
            origin.UpstreamKey,
            origin.EffectiveDate);
    }

    private static bool HasSameNumericIdentity(
        StagedHistoricalCountry row,
        CountryChangeLinkEmissionRow successor,
        IReadOnlyDictionary<string, StagedCountry> countries,
        IReadOnlyDictionary<string, StagedHistoricalCountry> historical)
    {
        if (row.Numeric == null)
        {
            return false;
        }

        string? successorNumeric = null;
        if (string.Equals(successor.TargetKind, "CurrentCountry", StringComparison.Ordinal) &&
            countries.TryGetValue(successor.TargetCode, out StagedCountry? country))
        {
            successorNumeric = country.Numeric.Value;
        }
        else if (string.Equals(
                     successor.TargetKind,
                     "HistoricalCountry",
                     StringComparison.Ordinal) &&
                 historical.TryGetValue(
                     successor.TargetCode,
                     out StagedHistoricalCountry? historicalCountry) &&
                 historicalCountry.Numeric != null)
        {
            successorNumeric = historicalCountry.Numeric.Value;
        }

        return successorNumeric != null &&
               string.Equals(row.Numeric.Value, successorNumeric, StringComparison.Ordinal);
    }

    private static List<CountryChangeLinkEmissionRow> ResolveSuccessors(
        StagedHistoricalCountry row,
        IReadOnlyDictionary<string, List<StagedHistoricalCountry>> historicalByAlpha2,
        IReadOnlyDictionary<string, StagedCountry> countriesByAlpha2,
        IReadOnlyDictionary<string, StagedCountry> countriesByNumeric,
        IReadOnlyDictionary<string, StagedCountry> countriesByName,
        IReadOnlyDictionary<string, StagedAlias> territoryAliases,
        IReadOnlyList<StagedM49History> m49History,
        IReadOnlyList<AppliedOverride> overrides)
    {
        List<CountryChangeLinkEmissionRow> result =
            ResolveOverrideSuccessors(row, overrides, countriesByAlpha2);
        if (result.Count != 0)
        {
            return SortAndValidateSuccessors(row, result);
        }

        string suffix = row.Alpha4.Value.Substring(2, 2);
        if (string.Equals(suffix, "AA", StringComparison.Ordinal))
        {
            result.Add(CreateHistoricalSourceLink(row, "CurrentCountry", row.Alpha2.Value));
            return result;
        }

        if (!string.Equals(suffix, "HH", StringComparison.Ordinal) &&
            !string.Equals(suffix, "XX", StringComparison.Ordinal))
        {
            result.Add(ResolveDirectSuffix(
                row,
                suffix,
                historicalByAlpha2,
                countriesByAlpha2));
            return result;
        }

        bool isLatestForAlpha2 = IsLatestHistoricalAlpha2(row, historicalByAlpha2);
        if (isLatestForAlpha2 &&
            territoryAliases.TryGetValue(row.Alpha2.Value, out StagedAlias? alias))
        {
            foreach (StagedText replacement in alias.Replacements)
            {
                if (!countriesByAlpha2.ContainsKey(replacement.Value))
                {
                    throw new EmissionException(
                        $"Historical country '{row.Alpha4.Value}' has unknown CLDR successor " +
                        $"'{replacement.Value}'.");
                }

                result.Add(CreateStagedLink(
                    row.Alpha4.Value,
                    "CurrentCountry",
                    replacement.Value,
                    replacement));
            }
        }

        if (result.Count == 0 && row.Numeric != null)
        {
            result.AddRange(ResolveM49Successors(row, m49History, countriesByNumeric));
        }

        if (result.Count == 0 && row.Comment != null)
        {
            result.AddRange(ResolveCommentSuccessors(row, countriesByName));
        }

        if (result.Count == 0)
        {
            throw new EmissionException(
                $"Historical country '{row.Alpha4.Value}' has no resolvable successor evidence.");
        }

        return SortAndValidateSuccessors(row, result);
    }

    private static List<CountryChangeLinkEmissionRow> ResolveOverrideSuccessors(
        StagedHistoricalCountry row,
        IReadOnlyList<AppliedOverride> overrides,
        IReadOnlyDictionary<string, StagedCountry> countries)
    {
        List<CountryChangeLinkEmissionRow> result = new List<CountryChangeLinkEmissionRow>();
        foreach (AppliedOverride applied in overrides)
        {
            OverrideEntry entry = applied.Definition;
            if (!string.Equals(entry.Kind, "supplement", StringComparison.Ordinal) ||
                !string.Equals(entry.TargetKind, "historical-country", StringComparison.Ordinal) ||
                !string.Equals(entry.TargetKey, row.Alpha4.Value, StringComparison.Ordinal) ||
                !string.Equals(entry.Field, "transition-replacement", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (string replacement in entry.Values)
            {
                if (!countries.ContainsKey(replacement))
                {
                    throw new EmissionException(
                        $"Historical override '{entry.Id}' has unknown successor '{replacement}'.");
                }

                result.Add(new CountryChangeLinkEmissionRow(
                    row.Alpha4.Value,
                    "CurrentCountry",
                    replacement,
                    entry.Provenance.SourceId,
                    entry.ExpiryCondition.Revision,
                    entry.Provenance.UpstreamKey,
                    entry.EffectiveDate,
                    entry.Field));
            }
        }

        return result;
    }

    private static CountryChangeLinkEmissionRow ResolveDirectSuffix(
        StagedHistoricalCountry row,
        string suffix,
        IReadOnlyDictionary<string, List<StagedHistoricalCountry>> historicalByAlpha2,
        IReadOnlyDictionary<string, StagedCountry> countries)
    {
        if (countries.ContainsKey(suffix))
        {
            return CreateHistoricalSourceLink(row, "CurrentCountry", suffix);
        }

        if (historicalByAlpha2.TryGetValue(
                suffix,
                out List<StagedHistoricalCountry>? candidates))
        {
            StagedHistoricalCountry? successor = FindLaterHistorical(row, candidates);
            if (successor != null)
            {
                return CreateHistoricalSourceLink(
                    row,
                    "HistoricalCountry",
                    successor.Alpha4.Value);
            }
        }

        throw new EmissionException(
            $"Historical country '{row.Alpha4.Value}' has unknown alpha-4 suffix '{suffix}'.");
    }

    private static StagedHistoricalCountry? FindLaterHistorical(
        StagedHistoricalCountry source,
        IReadOnlyList<StagedHistoricalCountry> candidates)
    {
        StagedHistoricalCountry? result = null;
        foreach (StagedHistoricalCountry candidate in candidates)
        {
            if (candidate.WithdrawalDate == null || source.WithdrawalDate == null ||
                CompareCanonicalDates(
                    candidate.WithdrawalDate.Value,
                    source.WithdrawalDate.Value) <= 0)
            {
                continue;
            }

            if (result == null ||
                CompareCanonicalDates(
                    candidate.WithdrawalDate.Value,
                    result.WithdrawalDate!.Value) < 0)
            {
                result = candidate;
            }
        }

        return result;
    }

    private static List<CountryChangeLinkEmissionRow> ResolveM49Successors(
        StagedHistoricalCountry row,
        IReadOnlyList<StagedM49History> history,
        IReadOnlyDictionary<string, StagedCountry> countriesByNumeric)
    {
        List<CountryChangeLinkEmissionRow> result = new List<CountryChangeLinkEmissionRow>();
        foreach (StagedM49History entry in history)
        {
            if (entry.Kind != UnM49HistoryKind.NotInCurrentUse ||
                !string.Equals(entry.M49Code.Value, row.Numeric!.Value, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (string numeric in ReadThreeDigitTokens(entry.Note.Value))
            {
                if (!countriesByNumeric.TryGetValue(numeric, out StagedCountry? successor))
                {
                    continue;
                }

                result.Add(CreateStagedLink(
                    row.Alpha4.Value,
                    "CurrentCountry",
                    successor.Alpha2.Value,
                    entry.Note));
            }
        }

        return result;
    }

    private static List<CountryChangeLinkEmissionRow> ResolveCommentSuccessors(
        StagedHistoricalCountry row,
        IReadOnlyDictionary<string, StagedCountry> countriesByName)
    {
        const string Prefix = "now split into ";
        List<CountryChangeLinkEmissionRow> result = new List<CountryChangeLinkEmissionRow>();
        string comment = row.Comment!.Value;
        if (!comment.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return result;
        }

        string[] names = comment.Substring(Prefix.Length).Split(
            new[] { " and " },
            StringSplitOptions.None);
        foreach (string name in names)
        {
            if (!countriesByName.TryGetValue(name, out StagedCountry? country))
            {
                throw new EmissionException(
                    $"Historical comment for '{row.Alpha4.Value}' names unknown successor " +
                    $"'{name}'.");
            }

            result.Add(CreateStagedLink(
                row.Alpha4.Value,
                "CurrentCountry",
                country.Alpha2.Value,
                row.Comment));
        }

        return result;
    }

    private static IReadOnlyList<string> ReadThreeDigitTokens(string value)
    {
        List<string> result = new List<string>();
        for (int index = 0; index <= value.Length - 3; index += 1)
        {
            bool digits = IsAsciiDigit(value[index]) &&
                          IsAsciiDigit(value[index + 1]) &&
                          IsAsciiDigit(value[index + 2]);
            bool startsToken = index == 0 || !IsAsciiDigit(value[index - 1]);
            bool endsToken = index + 3 == value.Length || !IsAsciiDigit(value[index + 3]);
            if (digits && startsToken && endsToken)
            {
                result.Add(value.Substring(index, 3));
                index += 2;
            }
        }

        return result;
    }

    private static bool IsAsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    private static CountryChangeLinkEmissionRow CreateHistoricalSourceLink(
        StagedHistoricalCountry row,
        string targetKind,
        string targetCode)
    {
        return CreateStagedLink(row.Alpha4.Value, targetKind, targetCode, row.Alpha4);
    }

    private static CountryChangeLinkEmissionRow CreateStagedLink(
        string changeAlpha4,
        string targetKind,
        string targetCode,
        StagedText evidence)
    {
        StagingOrigin origin = evidence.Provenance.Origin;
        return new CountryChangeLinkEmissionRow(
            changeAlpha4,
            targetKind,
            targetCode,
            origin.SourceId,
            origin.SourceRevision,
            origin.UpstreamKey,
            origin.EffectiveDate,
            evidence.Provenance.SourceField);
    }

    private static List<CountryChangeLinkEmissionRow> SortAndValidateSuccessors(
        StagedHistoricalCountry row,
        List<CountryChangeLinkEmissionRow> successors)
    {
        successors.Sort(CompareLinks);
        CountryChangeLinkEmissionRow? previous = null;
        foreach (CountryChangeLinkEmissionRow successor in successors)
        {
            if (previous != null && CompareLinks(previous, successor) == 0)
            {
                throw new EmissionException(
                    $"Historical country '{row.Alpha4.Value}' repeats successor " +
                    $"'{successor.TargetCode}'.");
            }

            previous = successor;
        }

        return successors;
    }

    private static bool IsLatestHistoricalAlpha2(
        StagedHistoricalCountry row,
        IReadOnlyDictionary<string, List<StagedHistoricalCountry>> historicalByAlpha2)
    {
        IReadOnlyList<StagedHistoricalCountry> candidates = historicalByAlpha2[row.Alpha2.Value];
        foreach (StagedHistoricalCountry candidate in candidates)
        {
            if (candidate.WithdrawalDate != null && row.WithdrawalDate != null &&
                CompareCanonicalDates(
                    candidate.WithdrawalDate.Value,
                    row.WithdrawalDate.Value) > 0)
            {
                return false;
            }
        }

        return true;
    }

    private static int CompareCanonicalDates(string left, string right)
    {
        int leftYear = int.Parse(left.Substring(0, 4), CultureInfo.InvariantCulture);
        int rightYear = int.Parse(right.Substring(0, 4), CultureInfo.InvariantCulture);
        if (leftYear != rightYear)
        {
            return leftYear.CompareTo(rightYear);
        }

        return string.CompareOrdinal(left.PadRight(10, '-'), right.PadRight(10, '-'));
    }

    private static Dictionary<string, StagedCountry> IndexCountriesByAlpha2(
        GeneratorStagingData staging)
    {
        Dictionary<string, StagedCountry> result = new Dictionary<string, StagedCountry>(
            StringComparer.Ordinal);
        foreach (StagedCountry row in staging.Countries)
        {
            result.Add(row.Alpha2.Value, row);
        }

        return result;
    }

    private static Dictionary<string, StagedCountry> IndexCountriesByNumeric(
        GeneratorStagingData staging)
    {
        Dictionary<string, StagedCountry> result = new Dictionary<string, StagedCountry>(
            StringComparer.Ordinal);
        foreach (StagedCountry row in staging.Countries)
        {
            result.Add(row.Numeric.Value, row);
        }

        return result;
    }

    private static Dictionary<string, StagedCountry> IndexCountriesByName(
        GeneratorStagingData staging)
    {
        Dictionary<string, StagedCountry> result = new Dictionary<string, StagedCountry>(
            StringComparer.Ordinal);
        foreach (StagedCountry row in staging.Countries)
        {
            result.TryAdd(row.Name.Value, row);
            if (row.CommonName != null)
            {
                result.TryAdd(row.CommonName.Value, row);
            }
        }

        return result;
    }

    private static Dictionary<string, List<StagedHistoricalCountry>> IndexHistoricalByAlpha2(
        IReadOnlyList<StagedHistoricalCountry> historical)
    {
        Dictionary<string, List<StagedHistoricalCountry>> result =
            new Dictionary<string, List<StagedHistoricalCountry>>(StringComparer.Ordinal);
        foreach (StagedHistoricalCountry row in historical)
        {
            if (!result.TryGetValue(row.Alpha2.Value, out List<StagedHistoricalCountry>? values))
            {
                values = new List<StagedHistoricalCountry>();
                result.Add(row.Alpha2.Value, values);
            }

            values.Add(row);
        }

        return result;
    }

    private static Dictionary<string, StagedHistoricalCountry> IndexHistoricalByAlpha4(
        IReadOnlyList<StagedHistoricalCountry> historical)
    {
        Dictionary<string, StagedHistoricalCountry> result =
            new Dictionary<string, StagedHistoricalCountry>(StringComparer.Ordinal);
        foreach (StagedHistoricalCountry row in historical)
        {
            result.Add(row.Alpha4.Value, row);
        }

        return result;
    }

    private static Dictionary<string, StagedAlias> IndexTerritoryAliases(
        GeneratorStagingData staging)
    {
        Dictionary<string, StagedAlias> result = new Dictionary<string, StagedAlias>(
            StringComparer.Ordinal);
        foreach (StagedAlias alias in staging.Aliases)
        {
            if (alias.Target == StagedAliasTarget.Territory &&
                alias.Code.Value.Length == 2)
            {
                result.Add(alias.Code.Value, alias);
            }
        }

        return result;
    }

    private static void Validate(
        IReadOnlyList<StagedHistoricalCountry> historical,
        IReadOnlyList<CountryChangeEmissionRow> changes,
        IReadOnlyList<CountryChangeLinkEmissionRow> predecessors,
        IReadOnlyList<CountryChangeLinkEmissionRow> successors,
        IReadOnlyDictionary<string, StagedCountry> countries,
        IReadOnlyDictionary<string, StagedHistoricalCountry> historicalByAlpha4)
    {
        if (historical.Count != changes.Count || historical.Count != predecessors.Count)
        {
            throw new EmissionException(
                "Historical rows, changes, and predecessor links do not have equal coverage.");
        }

        string? previousAlpha4 = null;
        foreach (StagedHistoricalCountry row in historical)
        {
            if (previousAlpha4 != null &&
                string.CompareOrdinal(previousAlpha4, row.Alpha4.Value) >= 0)
            {
                throw new EmissionException(
                    "Historical-country rows are not unique ordinal alpha-4 order.");
            }

            previousAlpha4 = row.Alpha4.Value;
        }

        HashSet<string> changesWithPredecessors = new HashSet<string>(StringComparer.Ordinal);
        foreach (CountryChangeLinkEmissionRow predecessor in predecessors)
        {
            if (!historicalByAlpha4.ContainsKey(predecessor.TargetCode))
            {
                throw new EmissionException(
                    $"Change '{predecessor.ChangeAlpha4}' has unknown predecessor " +
                    $"'{predecessor.TargetCode}'.");
            }

            changesWithPredecessors.Add(predecessor.ChangeAlpha4);
        }

        HashSet<string> changesWithSuccessors = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, string> historicalEdges = new Dictionary<string, string>(
            StringComparer.Ordinal);
        foreach (CountryChangeLinkEmissionRow successor in successors)
        {
            if (string.Equals(successor.TargetKind, "CurrentCountry", StringComparison.Ordinal))
            {
                if (!countries.ContainsKey(successor.TargetCode))
                {
                    throw new EmissionException(
                        $"Change '{successor.ChangeAlpha4}' has unknown current successor " +
                        $"'{successor.TargetCode}'.");
                }
            }
            else if (!historicalByAlpha4.ContainsKey(successor.TargetCode))
            {
                throw new EmissionException(
                    $"Change '{successor.ChangeAlpha4}' has unknown historical successor " +
                    $"'{successor.TargetCode}'.");
            }
            else
            {
                historicalEdges.Add(successor.ChangeAlpha4, successor.TargetCode);
            }

            changesWithSuccessors.Add(successor.ChangeAlpha4);
        }

        if (changesWithPredecessors.Count != changes.Count ||
            changesWithSuccessors.Count != changes.Count)
        {
            throw new EmissionException("A country change has incomplete relationship links.");
        }

        ValidateHistoricalCycles(historicalEdges);
    }

    private static void ValidateHistoricalCycles(IReadOnlyDictionary<string, string> edges)
    {
        foreach (string start in edges.Keys)
        {
            HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
            string current = start;
            while (edges.TryGetValue(current, out string? next))
            {
                if (!visited.Add(current))
                {
                    throw new EmissionException(
                        $"Historical transition graph contains a cycle at '{current}'.");
                }

                current = next;
            }
        }
    }

    private static int CompareHistorical(StagedHistoricalCountry left, StagedHistoricalCountry right)
    {
        return string.CompareOrdinal(left.Alpha4.Value, right.Alpha4.Value);
    }

    private static int CompareChanges(CountryChangeEmissionRow left, CountryChangeEmissionRow right)
    {
        return string.CompareOrdinal(left.Alpha4, right.Alpha4);
    }

    private static int CompareLinks(
        CountryChangeLinkEmissionRow left,
        CountryChangeLinkEmissionRow right)
    {
        int comparison = string.CompareOrdinal(left.ChangeAlpha4, right.ChangeAlpha4);
        if (comparison == 0)
        {
            comparison = TargetKindOrder(left.TargetKind).CompareTo(TargetKindOrder(right.TargetKind));
        }

        return comparison == 0
            ? string.CompareOrdinal(left.TargetCode, right.TargetCode)
            : comparison;
    }

    private static int TargetKindOrder(string kind)
    {
        return string.Equals(kind, "CurrentCountry", StringComparison.Ordinal) ? 0 : 1;
    }

    private static string RenderHistoricalTable(IReadOnlyList<StagedHistoricalCountry> rows)
    {
        StringBuilder builder = new StringBuilder(48_000);
        Line(builder, "// <auto-generated />");
        Line(builder, "namespace countrycodes;");
        Line(builder, string.Empty);
        Line(builder, "internal class GeneratedHistoricalCountryRow");
        Line(builder, "{");
        Line(builder, "    internal string Alpha2;");
        Line(builder, "    internal string Alpha3;");
        Line(builder, "    internal string Alpha4;");
        Line(builder, "    internal bool HasNumeric;");
        Line(builder, "    internal string NumericText;");
        Line(builder, "    internal int NumericValue;");
        Line(builder, "    internal bool HasWithdrawalDate;");
        Line(builder, "    internal string WithdrawalDate;");
        Line(builder, "    internal bool HasComment;");
        Line(builder, "    internal string Comment;");
        Line(builder, "    internal GeneratedCodeAssignmentState AssignmentState;");
        Line(builder, "    internal string SourceId;");
        Line(builder, "    internal string SourceRevision;");
        Line(builder, "    internal string UpstreamKey;");
        Line(builder, "    internal string EffectiveDate;");
        Line(builder, string.Empty);
        Line(builder, "    internal GeneratedHistoricalCountryRow(");
        Line(builder, "        string alpha2,");
        Line(builder, "        string alpha3,");
        Line(builder, "        string alpha4,");
        Line(builder, "        bool hasNumeric,");
        Line(builder, "        string numericText,");
        Line(builder, "        int numericValue,");
        Line(builder, "        bool hasWithdrawalDate,");
        Line(builder, "        string withdrawalDate,");
        Line(builder, "        bool hasComment,");
        Line(builder, "        string comment,");
        Line(builder, "        GeneratedCodeAssignmentState assignmentState,");
        Line(builder, "        string sourceId,");
        Line(builder, "        string sourceRevision,");
        Line(builder, "        string upstreamKey,");
        Line(builder, "        string effectiveDate)");
        Line(builder, "    {");
        Line(builder, "        Alpha2 = alpha2;");
        Line(builder, "        Alpha3 = alpha3;");
        Line(builder, "        Alpha4 = alpha4;");
        Line(builder, "        HasNumeric = hasNumeric;");
        Line(builder, "        NumericText = numericText;");
        Line(builder, "        NumericValue = numericValue;");
        Line(builder, "        HasWithdrawalDate = hasWithdrawalDate;");
        Line(builder, "        WithdrawalDate = withdrawalDate;");
        Line(builder, "        HasComment = hasComment;");
        Line(builder, "        Comment = comment;");
        Line(builder, "        AssignmentState = assignmentState;");
        Line(builder, "        SourceId = sourceId;");
        Line(builder, "        SourceRevision = sourceRevision;");
        Line(builder, "        UpstreamKey = upstreamKey;");
        Line(builder, "        EffectiveDate = effectiveDate;");
        Line(builder, "    }");
        Line(builder, "}");
        Line(builder, string.Empty);
        Line(builder, "internal static class HistoricalCountryTable");
        Line(builder, "{");
        Line(builder, "    internal static readonly GeneratedHistoricalCountryRow[] Rows =");
        Line(builder, "    {");
        foreach (StagedHistoricalCountry row in rows)
        {
            bool hasNumeric = row.Numeric != null;
            int numericValue = hasNumeric
                ? int.Parse(row.Numeric!.Value, CultureInfo.InvariantCulture)
                : 0;
            Line(builder, "        new GeneratedHistoricalCountryRow(");
            Line(builder, $"            {Literal(row.Alpha2.Value)},");
            Line(builder, $"            {Literal(row.Alpha3.Value)},");
            Line(builder, $"            {Literal(row.Alpha4.Value)},");
            Line(builder, $"            {(hasNumeric ? "true" : "false")},");
            Line(builder, $"            {Literal(row.Numeric?.Value ?? string.Empty)},");
            Line(builder, $"            {numericValue.ToString(CultureInfo.InvariantCulture)},");
            Line(builder, $"            {(row.WithdrawalDate != null ? "true" : "false")},");
            Line(builder, $"            {Literal(row.WithdrawalDate?.Value ?? string.Empty)},");
            Line(builder, $"            {(row.Comment != null ? "true" : "false")},");
            Line(builder, $"            {Literal(row.Comment?.Value ?? string.Empty)},");
            Line(builder, "            GeneratedCodeAssignmentState.FormerlyAssigned,");
            Line(builder, $"            {Literal(row.Origin.SourceId)},");
            Line(builder, $"            {Literal(row.Origin.SourceRevision)},");
            Line(builder, $"            {Literal(row.Origin.UpstreamKey)},");
            Line(builder, $"            {Literal(row.Origin.EffectiveDate)}),");
        }

        Line(builder, "    };");
        Line(builder, "}");
        return builder.ToString();
    }

    private static string RenderChangeTable(
        IReadOnlyList<CountryChangeEmissionRow> changes,
        IReadOnlyList<CountryChangeLinkEmissionRow> predecessors,
        IReadOnlyList<CountryChangeLinkEmissionRow> successors)
    {
        StringBuilder builder = new StringBuilder(96_000);
        Line(builder, "// <auto-generated />");
        Line(builder, "namespace countrycodes;");
        Line(builder, string.Empty);
        Line(builder, "internal enum GeneratedCountryChangeKind");
        Line(builder, "{");
        Line(builder, "    SignificantChange = 0,");
        Line(builder, "    Merger = 1,");
        Line(builder, "    Split = 2,");
        Line(builder, "    OtherOrUnknown = 3");
        Line(builder, "}");
        Line(builder, string.Empty);
        Line(builder, "internal enum GeneratedCountryChangeTargetKind");
        Line(builder, "{");
        Line(builder, "    CurrentCountry = 0,");
        Line(builder, "    HistoricalCountry = 1");
        Line(builder, "}");
        Line(builder, string.Empty);
        RenderChangeTypes(builder);
        Line(builder, string.Empty);
        Line(builder, "internal static class CountryChangeTable");
        Line(builder, "{");
        Line(builder, "    internal static readonly GeneratedCountryChangeRow[] Rows =");
        Line(builder, "    {");
        foreach (CountryChangeEmissionRow change in changes)
        {
            Line(builder, "        new GeneratedCountryChangeRow(");
            Line(builder, $"            {Literal(change.Alpha4)},");
            Line(builder, $"            GeneratedCountryChangeKind.{change.Kind},");
            Line(builder, $"            {(change.HasEffectiveDate ? "true" : "false")},");
            Line(builder, $"            {Literal(change.EffectiveDate)},");
            Line(builder, $"            {Literal(change.SourceId)},");
            Line(builder, $"            {Literal(change.SourceRevision)},");
            Line(builder, $"            {Literal(change.UpstreamKey)},");
            Line(builder, $"            {Literal(change.SourceEffectiveDate)}),");
        }

        Line(builder, "    };");
        RenderLinks(builder, "Predecessors", predecessors);
        RenderLinks(builder, "Successors", successors);
        Line(builder, "}");
        return builder.ToString();
    }

    private static void RenderChangeTypes(StringBuilder builder)
    {
        Line(builder, "internal class GeneratedCountryChangeRow");
        Line(builder, "{");
        Line(builder, "    internal string Alpha4;");
        Line(builder, "    internal GeneratedCountryChangeKind Kind;");
        Line(builder, "    internal bool HasEffectiveDate;");
        Line(builder, "    internal string EffectiveDate;");
        Line(builder, "    internal string SourceId;");
        Line(builder, "    internal string SourceRevision;");
        Line(builder, "    internal string UpstreamKey;");
        Line(builder, "    internal string SourceEffectiveDate;");
        Line(builder, string.Empty);
        Line(builder, "    internal GeneratedCountryChangeRow(");
        Line(builder, "        string alpha4,");
        Line(builder, "        GeneratedCountryChangeKind kind,");
        Line(builder, "        bool hasEffectiveDate,");
        Line(builder, "        string effectiveDate,");
        Line(builder, "        string sourceId,");
        Line(builder, "        string sourceRevision,");
        Line(builder, "        string upstreamKey,");
        Line(builder, "        string sourceEffectiveDate)");
        Line(builder, "    {");
        Line(builder, "        Alpha4 = alpha4;");
        Line(builder, "        Kind = kind;");
        Line(builder, "        HasEffectiveDate = hasEffectiveDate;");
        Line(builder, "        EffectiveDate = effectiveDate;");
        Line(builder, "        SourceId = sourceId;");
        Line(builder, "        SourceRevision = sourceRevision;");
        Line(builder, "        UpstreamKey = upstreamKey;");
        Line(builder, "        SourceEffectiveDate = sourceEffectiveDate;");
        Line(builder, "    }");
        Line(builder, "}");
        Line(builder, string.Empty);
        Line(builder, "internal class GeneratedCountryChangeLinkRow");
        Line(builder, "{");
        Line(builder, "    internal string ChangeAlpha4;");
        Line(builder, "    internal GeneratedCountryChangeTargetKind TargetKind;");
        Line(builder, "    internal string TargetCode;");
        Line(builder, "    internal string SourceId;");
        Line(builder, "    internal string SourceRevision;");
        Line(builder, "    internal string UpstreamKey;");
        Line(builder, "    internal string EffectiveDate;");
        Line(builder, "    internal string SourceField;");
        Line(builder, string.Empty);
        Line(builder, "    internal GeneratedCountryChangeLinkRow(");
        Line(builder, "        string changeAlpha4,");
        Line(builder, "        GeneratedCountryChangeTargetKind targetKind,");
        Line(builder, "        string targetCode,");
        Line(builder, "        string sourceId,");
        Line(builder, "        string sourceRevision,");
        Line(builder, "        string upstreamKey,");
        Line(builder, "        string effectiveDate,");
        Line(builder, "        string sourceField)");
        Line(builder, "    {");
        Line(builder, "        ChangeAlpha4 = changeAlpha4;");
        Line(builder, "        TargetKind = targetKind;");
        Line(builder, "        TargetCode = targetCode;");
        Line(builder, "        SourceId = sourceId;");
        Line(builder, "        SourceRevision = sourceRevision;");
        Line(builder, "        UpstreamKey = upstreamKey;");
        Line(builder, "        EffectiveDate = effectiveDate;");
        Line(builder, "        SourceField = sourceField;");
        Line(builder, "    }");
        Line(builder, "}");
    }

    private static void RenderLinks(
        StringBuilder builder,
        string fieldName,
        IReadOnlyList<CountryChangeLinkEmissionRow> links)
    {
        Line(builder, string.Empty);
        Line(builder, $"    internal static readonly GeneratedCountryChangeLinkRow[] {fieldName} =");
        Line(builder, "    {");
        foreach (CountryChangeLinkEmissionRow link in links)
        {
            Line(builder, "        new GeneratedCountryChangeLinkRow(");
            Line(builder, $"            {Literal(link.ChangeAlpha4)},");
            Line(builder, $"            GeneratedCountryChangeTargetKind.{link.TargetKind},");
            Line(builder, $"            {Literal(link.TargetCode)},");
            Line(builder, $"            {Literal(link.SourceId)},");
            Line(builder, $"            {Literal(link.SourceRevision)},");
            Line(builder, $"            {Literal(link.UpstreamKey)},");
            Line(builder, $"            {Literal(link.EffectiveDate)},");
            Line(builder, $"            {Literal(link.SourceField)}),");
        }

        Line(builder, "    };");
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

    private sealed class CountryChangeEmissionRow
    {
        internal CountryChangeEmissionRow(
            string alpha4,
            string kind,
            bool hasEffectiveDate,
            string effectiveDate,
            string sourceId,
            string sourceRevision,
            string upstreamKey,
            string sourceEffectiveDate)
        {
            Alpha4 = alpha4;
            Kind = kind;
            HasEffectiveDate = hasEffectiveDate;
            EffectiveDate = effectiveDate;
            SourceId = sourceId;
            SourceRevision = sourceRevision;
            UpstreamKey = upstreamKey;
            SourceEffectiveDate = sourceEffectiveDate;
        }

        internal string Alpha4 { get; }
        internal string Kind { get; }
        internal bool HasEffectiveDate { get; }
        internal string EffectiveDate { get; }
        internal string SourceId { get; }
        internal string SourceRevision { get; }
        internal string UpstreamKey { get; }
        internal string SourceEffectiveDate { get; }
    }

    private sealed class CountryChangeLinkEmissionRow
    {
        internal CountryChangeLinkEmissionRow(
            string changeAlpha4,
            string targetKind,
            string targetCode,
            string sourceId,
            string sourceRevision,
            string upstreamKey,
            string effectiveDate,
            string sourceField)
        {
            ChangeAlpha4 = changeAlpha4;
            TargetKind = targetKind;
            TargetCode = targetCode;
            SourceId = sourceId;
            SourceRevision = sourceRevision;
            UpstreamKey = upstreamKey;
            EffectiveDate = effectiveDate;
            SourceField = sourceField;
        }

        internal string ChangeAlpha4 { get; }
        internal string TargetKind { get; }
        internal string TargetCode { get; }
        internal string SourceId { get; }
        internal string SourceRevision { get; }
        internal string UpstreamKey { get; }
        internal string EffectiveDate { get; }
        internal string SourceField { get; }
    }
}
