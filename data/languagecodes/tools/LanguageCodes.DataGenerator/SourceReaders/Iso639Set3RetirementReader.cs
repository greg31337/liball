using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using LanguageCodes.DataGenerator.Model;

namespace LanguageCodes.DataGenerator.SourceReaders;

internal static class Iso639Set3RetirementReader
{
    private const string ExpectedHeader = "Id\tRef_Name\tRet_Reason\tChange_To\tRet_Remedy\tEffective";

    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    internal static Iso639Set3RetirementRecord[] Read(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        string content = ReadUtf8(sourcePath);
        string[] lines = content.Split('\n');
        int lineCount = lines.Length;
        if (lineCount > 0 && lines[lineCount - 1].Length == 0)
        {
            lineCount--;
        }

        if (lineCount == 0 || RemoveCarriageReturn(lines[0]) != ExpectedHeader)
        {
            throw new InvalidDataException($"{sourcePath}(1): unexpected ISO 639-3 retirements header.");
        }

        List<Iso639Set3RetirementRecord> records =
            new List<Iso639Set3RetirementRecord>(lineCount - 1);
        HashSet<string> identifiers = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 1; index < lineCount; index++)
        {
            int lineNumber = index + 1;
            string line = RemoveCarriageReturn(lines[index]);
            if (line.Length == 0)
            {
                throw LineError(sourcePath, lineNumber, "empty rows are not allowed.");
            }

            string[] fields = line.Split('\t');
            if (fields.Length != 6)
            {
                throw LineError(sourcePath, lineNumber, "expected exactly six tab-delimited fields.");
            }

            string id = ParseCode(sourcePath, lineNumber, fields[0], "retired identifier");
            string referenceName = ParseRequiredText(sourcePath, lineNumber, fields[1], "reference name");
            Iso639Set3RetirementReason reason = ParseReason(sourcePath, lineNumber, fields[2]);
            string? changeToCode = ParseOptionalCode(sourcePath, lineNumber, fields[3], "change-to code");
            string? remedy = ParseOptionalText(sourcePath, lineNumber, fields[4], "retirement remedy");
            DateOnly effectiveDate = ParseDate(sourcePath, lineNumber, fields[5]);

            if (!identifiers.Add(id))
            {
                throw LineError(sourcePath, lineNumber, $"duplicate retired identifier '{id}'.");
            }

            string[] replacementCodes = GetReplacementCodes(
                sourcePath,
                lineNumber,
                reason,
                changeToCode,
                remedy);

            records.Add(new Iso639Set3RetirementRecord(
                id,
                referenceName,
                reason,
                changeToCode,
                remedy,
                replacementCodes,
                effectiveDate));
        }

        if (records.Count == 0)
        {
            throw new InvalidDataException($"{sourcePath}: expected at least one ISO 639-3 retirement record.");
        }

        return records.ToArray();
    }

    private static string ReadUtf8(string sourcePath)
    {
        byte[] bytes = File.ReadAllBytes(sourcePath);
        try
        {
            return StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException($"{sourcePath}: source is not valid UTF-8.", exception);
        }
    }

    private static string RemoveCarriageReturn(string value)
    {
        if (value.EndsWith('\r'))
        {
            return value.Substring(0, value.Length - 1);
        }

        return value;
    }

    private static string ParseCode(string sourcePath, int lineNumber, string value, string fieldName)
    {
        if (value.Length != 3)
        {
            throw LineError(sourcePath, lineNumber, $"{fieldName} must contain exactly three letters.");
        }

        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (character < 'a' || character > 'z')
            {
                throw LineError(sourcePath, lineNumber, $"{fieldName} must contain lowercase ASCII letters only.");
            }
        }

        return value;
    }

    private static string? ParseOptionalCode(
        string sourcePath,
        int lineNumber,
        string value,
        string fieldName)
    {
        if (value.Length == 0)
        {
            return null;
        }

        return ParseCode(sourcePath, lineNumber, value, fieldName);
    }

    private static Iso639Set3RetirementReason ParseReason(string sourcePath, int lineNumber, string value)
    {
        switch (value)
        {
            case "C":
                return Iso639Set3RetirementReason.Change;
            case "D":
                return Iso639Set3RetirementReason.Duplicate;
            case "N":
                return Iso639Set3RetirementReason.Nonexistent;
            case "S":
                return Iso639Set3RetirementReason.Split;
            case "M":
                return Iso639Set3RetirementReason.Merge;
            default:
                throw LineError(sourcePath, lineNumber, $"unknown retirement reason marker '{value}'.");
        }
    }

    private static string ParseRequiredText(
        string sourcePath,
        int lineNumber,
        string value,
        string fieldName)
    {
        string? text = ParseOptionalText(sourcePath, lineNumber, value, fieldName);
        if (text == null)
        {
            throw LineError(sourcePath, lineNumber, $"{fieldName} is required.");
        }

        return text;
    }

    private static string? ParseOptionalText(
        string sourcePath,
        int lineNumber,
        string value,
        string fieldName)
    {
        if (value.Length == 0)
        {
            return null;
        }

        if (value != value.Trim())
        {
            throw LineError(sourcePath, lineNumber, $"{fieldName} contains leading or trailing whitespace.");
        }

        for (int index = 0; index < value.Length; index++)
        {
            if (char.IsControl(value[index]))
            {
                throw LineError(sourcePath, lineNumber, $"{fieldName} contains a control character.");
            }
        }

        return value;
    }

    private static DateOnly ParseDate(string sourcePath, int lineNumber, string value)
    {
        if (!DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly effectiveDate))
        {
            throw LineError(sourcePath, lineNumber, $"effective date '{value}' is not a valid ISO date.");
        }

        return effectiveDate;
    }

    private static string[] GetReplacementCodes(
        string sourcePath,
        int lineNumber,
        Iso639Set3RetirementReason reason,
        string? changeToCode,
        string? remedy)
    {
        if (reason == Iso639Set3RetirementReason.Split)
        {
            if (changeToCode != null)
            {
                throw LineError(sourcePath, lineNumber, "a split retirement cannot have a singular change-to code.");
            }

            if (remedy == null)
            {
                throw LineError(sourcePath, lineNumber, "a split retirement must include a remedy.");
            }

            string[] splitCodes = ExtractSplitCodes(sourcePath, lineNumber, remedy);
            if (splitCodes.Length < 2)
            {
                throw LineError(sourcePath, lineNumber, "a split retirement remedy must identify at least two codes.");
            }

            return splitCodes;
        }

        if (reason == Iso639Set3RetirementReason.Nonexistent)
        {
            if (changeToCode != null)
            {
                throw LineError(sourcePath, lineNumber, "a nonexistent retirement cannot have a change-to code.");
            }

            return Array.Empty<string>();
        }

        if (changeToCode == null)
        {
            throw LineError(sourcePath, lineNumber, "this retirement reason requires a singular change-to code.");
        }

        return [changeToCode];
    }

    private static string[] ExtractSplitCodes(string sourcePath, int lineNumber, string remedy)
    {
        List<string> codes = new List<string>();
        HashSet<string> uniqueCodes = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;
        while (index < remedy.Length)
        {
            int openingBracket = remedy.IndexOf('[', index);
            int closingBracket = remedy.IndexOf(']', index);
            if (openingBracket < 0)
            {
                if (closingBracket >= 0)
                {
                    throw LineError(sourcePath, lineNumber, "split remedy contains an unmatched closing bracket.");
                }

                break;
            }

            if (closingBracket < openingBracket)
            {
                throw LineError(sourcePath, lineNumber, "split remedy contains an unmatched closing bracket.");
            }

            closingBracket = remedy.IndexOf(']', openingBracket + 1);
            if (closingBracket < 0)
            {
                throw LineError(sourcePath, lineNumber, "split remedy contains an unmatched opening bracket.");
            }

            string code = ParseCode(
                sourcePath,
                lineNumber,
                remedy.Substring(openingBracket + 1, closingBracket - openingBracket - 1),
                "split replacement code");
            if (!uniqueCodes.Add(code))
            {
                throw LineError(sourcePath, lineNumber, $"split remedy repeats replacement code '{code}'.");
            }

            codes.Add(code);
            index = closingBracket + 1;
        }

        return codes.ToArray();
    }

    private static InvalidDataException LineError(string sourcePath, int lineNumber, string message)
    {
        return new InvalidDataException($"{sourcePath}({lineNumber}): {message}");
    }
}
