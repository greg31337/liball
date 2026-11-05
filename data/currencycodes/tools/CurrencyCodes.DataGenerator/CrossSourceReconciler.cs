using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CurrencyCodes.DataGenerator;

internal sealed class CrossSourceReconciler
{
    private static readonly string[] ConflictHeaders =
    {
        "alphabetic_code",
        "iso_numeric_code",
        "cldr_numeric_code",
        "resolution",
        "review_basis"
    };

    internal CrossSourceCurrencyData Reconcile(
        IReadOnlyList<IsoListOneRow> listOneRows,
        IReadOnlyList<IsoListThreeRow> listThreeRows,
        IReadOnlyList<CldrCurrencyCodeMapping> cldrMappings,
        CldrFractionData fractionData,
        string conflictPath)
    {
        Dictionary<string, CldrCurrencyCodeMapping> mappings = IndexMappings(cldrMappings);
        List<SourceConflictEntry> conflictEntries = ReadConflicts(conflictPath);
        Dictionary<string, SourceConflictEntry> conflicts = IndexConflicts(conflictEntries);
        HashSet<string> usedConflictKeys = new HashSet<string>(StringComparer.Ordinal);
        List<string> unreviewedConflicts = new List<string>();
        List<CrossSourceCurrencyRow> rows = new List<CrossSourceCurrencyRow>();

        AddListOneRows(
            listOneRows,
            mappings,
            fractionData,
            conflicts,
            usedConflictKeys,
            unreviewedConflicts,
            rows);
        AddListThreeRows(
            listThreeRows,
            mappings,
            fractionData,
            conflicts,
            usedConflictKeys,
            unreviewedConflicts,
            rows);

        List<string> staleConflicts = new List<string>();
        foreach (SourceConflictEntry entry in conflictEntries)
        {
            string key = CreateConflictKey(
                entry.AlphabeticCode,
                entry.IsoNumericCode,
                entry.CldrNumericCode);
            if (!usedConflictKeys.Contains(key))
            {
                staleConflicts.Add(
                    $"{entry.AlphabeticCode}:{entry.IsoNumericCode}:{entry.CldrNumericCode}");
            }
        }

        if (unreviewedConflicts.Count != 0 || staleConflicts.Count != 0)
        {
            throw new InvalidDataException(CreateConflictError(unreviewedConflicts, staleConflicts));
        }

        return new CrossSourceCurrencyData(rows.AsReadOnly());
    }

    private static void AddListOneRows(
        IReadOnlyList<IsoListOneRow> sourceRows,
        Dictionary<string, CldrCurrencyCodeMapping> mappings,
        CldrFractionData fractionData,
        Dictionary<string, SourceConflictEntry> conflicts,
        HashSet<string> usedConflictKeys,
        List<string> unreviewedConflicts,
        List<CrossSourceCurrencyRow> rows)
    {
        foreach (IsoListOneRow sourceRow in sourceRows)
        {
            rows.Add(CreateRow(
                "list-one",
                sourceRow.EntityName,
                sourceRow.OfficialName,
                sourceRow.AlphabeticCode,
                sourceRow.NumericCode,
                sourceRow.HasMinorUnit,
                sourceRow.MinorUnitDigits,
                mappings,
                fractionData,
                conflicts,
                usedConflictKeys,
                unreviewedConflicts));
        }
    }

    private static void AddListThreeRows(
        IReadOnlyList<IsoListThreeRow> sourceRows,
        Dictionary<string, CldrCurrencyCodeMapping> mappings,
        CldrFractionData fractionData,
        Dictionary<string, SourceConflictEntry> conflicts,
        HashSet<string> usedConflictKeys,
        List<string> unreviewedConflicts,
        List<CrossSourceCurrencyRow> rows)
    {
        foreach (IsoListThreeRow sourceRow in sourceRows)
        {
            rows.Add(CreateRow(
                "list-three",
                sourceRow.EntityName,
                sourceRow.OfficialName,
                sourceRow.AlphabeticCode,
                sourceRow.NumericCode,
                sourceRow.HasMinorUnit,
                sourceRow.MinorUnitDigits,
                mappings,
                fractionData,
                conflicts,
                usedConflictKeys,
                unreviewedConflicts));
        }
    }

    private static CrossSourceCurrencyRow CreateRow(
        string sourceList,
        string entityName,
        string officialName,
        string? alphabeticCode,
        string? isoNumericCode,
        bool hasIsoMinorUnit,
        int isoMinorUnitDigits,
        Dictionary<string, CldrCurrencyCodeMapping> mappings,
        CldrFractionData fractionData,
        Dictionary<string, SourceConflictEntry> conflicts,
        HashSet<string> usedConflictKeys,
        List<string> unreviewedConflicts)
    {
        if (alphabeticCode == null)
        {
            return new CrossSourceCurrencyRow(
                sourceList,
                entityName,
                officialName,
                null,
                isoNumericCode,
                hasIsoMinorUnit,
                isoMinorUnitDigits,
                false,
                null,
                null,
                0,
                false,
                0,
                0,
                0,
                0,
                false,
                false,
                false,
                false,
                false,
                false);
        }

        mappings.TryGetValue(alphabeticCode, out CldrCurrencyCodeMapping? mapping);
        bool identityConflict = mapping != null
            && isoNumericCode != null
            && isoNumericCode != mapping.NumericCodeText;
        bool identityConflictReviewed = false;
        if (identityConflict)
        {
            string key = CreateConflictKey(alphabeticCode, isoNumericCode!, mapping!.NumericCodeText);
            identityConflictReviewed = conflicts.ContainsKey(key);
            if (identityConflictReviewed)
            {
                usedConflictKeys.Add(key);
            }
            else if (!unreviewedConflicts.Contains(key))
            {
                unreviewedConflicts.Add(key);
            }
        }

        CldrFractionRule fractionRule = fractionData.ResolveRule(alphabeticCode);
        return new CrossSourceCurrencyRow(
            sourceList,
            entityName,
            officialName,
            alphabeticCode,
            isoNumericCode,
            hasIsoMinorUnit,
            isoMinorUnitDigits,
            mapping != null,
            mapping?.SourceNumericCodeText,
            mapping?.NumericCodeText,
            mapping?.NumericCodeValue ?? 0,
            true,
            fractionRule.Digits,
            fractionRule.Rounding,
            fractionRule.CashDigits,
            fractionRule.CashRounding,
            fractionRule.DigitsExplicit,
            fractionRule.RoundingExplicit,
            fractionRule.CashDigitsExplicit,
            fractionRule.CashRoundingExplicit,
            identityConflict,
            identityConflictReviewed);
    }

    private static Dictionary<string, CldrCurrencyCodeMapping> IndexMappings(
        IReadOnlyList<CldrCurrencyCodeMapping> mappings)
    {
        Dictionary<string, CldrCurrencyCodeMapping> result =
            new Dictionary<string, CldrCurrencyCodeMapping>(StringComparer.Ordinal);
        foreach (CldrCurrencyCodeMapping mapping in mappings)
        {
            if (!result.TryAdd(mapping.AlphabeticCode, mapping))
            {
                throw new InvalidDataException(
                    $"Duplicate CLDR currency mapping '{mapping.AlphabeticCode}'.");
            }
        }

        return result;
    }

    private static Dictionary<string, SourceConflictEntry> IndexConflicts(
        List<SourceConflictEntry> entries)
    {
        Dictionary<string, SourceConflictEntry> result =
            new Dictionary<string, SourceConflictEntry>(StringComparer.Ordinal);
        foreach (SourceConflictEntry entry in entries)
        {
            string key = CreateConflictKey(
                entry.AlphabeticCode,
                entry.IsoNumericCode,
                entry.CldrNumericCode);
            if (!result.TryAdd(key, entry))
            {
                throw new InvalidDataException(
                    $"Duplicate source-conflicts.csv identity at line {entry.LineNumber}.");
            }
        }

        return result;
    }

    private static List<SourceConflictEntry> ReadConflicts(string path)
    {
        string[] lines = File.ReadAllLines(path, new UTF8Encoding(false, true));
        if (lines.Length == 0)
        {
            throw new InvalidDataException("source-conflicts.csv is empty.");
        }

        List<string> headers = ParseCsvLine(lines[0], 1);
        if (headers.Count != ConflictHeaders.Length)
        {
            throw new InvalidDataException("source-conflicts.csv has an invalid header.");
        }

        for (int index = 0; index < ConflictHeaders.Length; index++)
        {
            if (headers[index] != ConflictHeaders[index])
            {
                throw new InvalidDataException("source-conflicts.csv has an invalid header.");
            }
        }

        List<SourceConflictEntry> entries = new List<SourceConflictEntry>();
        for (int index = 1; index < lines.Length; index++)
        {
            int lineNumber = index + 1;
            if (lines[index].Length == 0)
            {
                throw new InvalidDataException($"source-conflicts.csv contains a blank line at line {lineNumber}.");
            }

            List<string> fields = ParseCsvLine(lines[index], lineNumber);
            if (fields.Count != ConflictHeaders.Length)
            {
                throw new InvalidDataException(
                    $"source-conflicts.csv line {lineNumber} has {fields.Count} fields; expected {ConflictHeaders.Length}.");
            }

            if (!IsAlphabeticCode(fields[0])
                || !IsNumericCode(fields[1])
                || !IsNumericCode(fields[2])
                || fields[3] != "retain-source-qualified"
                || fields[4].Length == 0)
            {
                throw new InvalidDataException($"source-conflicts.csv has an invalid row at line {lineNumber}.");
            }

            if (fields[1] == fields[2])
            {
                throw new InvalidDataException(
                    $"source-conflicts.csv line {lineNumber} does not describe a conflict.");
            }

            entries.Add(new SourceConflictEntry(
                lineNumber,
                fields[0],
                fields[1],
                fields[2],
                fields[3],
                fields[4]));
        }

        return entries;
    }

    private static List<string> ParseCsvLine(string line, int lineNumber)
    {
        List<string> fields = new List<string>();
        StringBuilder field = new StringBuilder();
        bool quoted = false;
        bool quoteClosed = false;
        for (int index = 0; index < line.Length; index++)
        {
            char character = line[index];
            if (quoted)
            {
                if (character == '"')
                {
                    if (index + 1 < line.Length && line[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else
                    {
                        quoted = false;
                        quoteClosed = true;
                    }
                }
                else
                {
                    field.Append(character);
                }
            }
            else if (character == ',')
            {
                fields.Add(field.ToString());
                field.Clear();
                quoteClosed = false;
            }
            else if (character == '"' && field.Length == 0 && !quoteClosed)
            {
                quoted = true;
            }
            else if (character == '"' || quoteClosed)
            {
                throw new InvalidDataException($"Invalid CSV quoting at line {lineNumber}.");
            }
            else
            {
                field.Append(character);
            }
        }

        if (quoted)
        {
            throw new InvalidDataException($"Unclosed CSV quote at line {lineNumber}.");
        }

        fields.Add(field.ToString());
        return fields;
    }

    private static bool IsAlphabeticCode(string value)
    {
        if (value.Length != 3)
        {
            return false;
        }

        foreach (char character in value)
        {
            if (character < 'A' || character > 'Z')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsNumericCode(string value)
    {
        if (value.Length != 3)
        {
            return false;
        }

        foreach (char character in value)
        {
            if (character < '0' || character > '9')
            {
                return false;
            }
        }

        return true;
    }

    private static string CreateConflictKey(
        string alphabeticCode,
        string isoNumericCode,
        string cldrNumericCode)
    {
        return alphabeticCode + ":" + isoNumericCode + ":" + cldrNumericCode;
    }

    private static string CreateConflictError(
        List<string> unreviewedConflicts,
        List<string> staleConflicts)
    {
        StringBuilder message = new StringBuilder("Cross-source identity reconciliation failed.");
        if (unreviewedConflicts.Count != 0)
        {
            message.Append(" Unreviewed identity conflicts: ");
            message.Append(string.Join(" | ", unreviewedConflicts));
            message.Append('.');
        }

        if (staleConflicts.Count != 0)
        {
            message.Append(" Stale source-conflicts.csv rows: ");
            message.Append(string.Join(" | ", staleConflicts));
            message.Append('.');
        }

        return message.ToString();
    }

    private sealed class SourceConflictEntry
    {
        internal SourceConflictEntry(
            int lineNumber,
            string alphabeticCode,
            string isoNumericCode,
            string cldrNumericCode,
            string resolution,
            string reviewBasis)
        {
            LineNumber = lineNumber;
            AlphabeticCode = alphabeticCode;
            IsoNumericCode = isoNumericCode;
            CldrNumericCode = cldrNumericCode;
            Resolution = resolution;
            ReviewBasis = reviewBasis;
        }

        internal int LineNumber { get; }

        internal string AlphabeticCode { get; }

        internal string IsoNumericCode { get; }

        internal string CldrNumericCode { get; }

        internal string Resolution { get; }

        internal string ReviewBasis { get; }
    }
}
