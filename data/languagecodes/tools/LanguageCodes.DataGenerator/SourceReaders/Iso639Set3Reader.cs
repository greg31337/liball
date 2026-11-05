using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LanguageCodes.DataGenerator.Model;

namespace LanguageCodes.DataGenerator.SourceReaders;

internal static class Iso639Set3Reader
{
    private const string ExpectedHeader =
        "Id\tPart2b\tPart2t\tPart1\tScope\tLanguage_Type\tRef_Name\tComment";

    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    internal static Iso639Set3Record[] Read(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        byte[] bytes = File.ReadAllBytes(sourcePath);
        string content;
        try
        {
            content = StrictUtf8.GetString(bytes);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException($"{sourcePath}: source is not valid UTF-8.", exception);
        }

        string[] lines = content.Split('\n');
        int lineCount = lines.Length;
        if (lineCount > 0 && lines[lineCount - 1].Length == 0)
        {
            lineCount--;
        }

        if (lineCount == 0 || RemoveCarriageReturn(lines[0]) != ExpectedHeader)
        {
            throw new InvalidDataException($"{sourcePath}(1): unexpected ISO 639-3 table header.");
        }

        List<Iso639Set3Record> records = new List<Iso639Set3Record>(lineCount - 1);
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> part2BibliographicCodes = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> part2TerminologyCodes = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> alpha2Codes = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 1; index < lineCount; index++)
        {
            int lineNumber = index + 1;
            string line = RemoveCarriageReturn(lines[index]);
            if (line.Length == 0)
            {
                throw LineError(sourcePath, lineNumber, "empty rows are not allowed.");
            }

            string[] fields = line.Split('\t');
            if (fields.Length != 8)
            {
                throw LineError(sourcePath, lineNumber, "expected exactly eight tab-delimited fields.");
            }

            string id = ParseRequiredCode(sourcePath, lineNumber, fields[0], 3, "identifier");
            string? part2BibliographicCode = ParseOptionalCode(
                sourcePath,
                lineNumber,
                fields[1],
                3,
                "Set 2 bibliographic code");
            string? part2TerminologyCode = ParseOptionalCode(
                sourcePath,
                lineNumber,
                fields[2],
                3,
                "Set 2 terminology code");
            string? alpha2Code = ParseOptionalCode(sourcePath, lineNumber, fields[3], 2, "alpha-2 code");

            if ((part2BibliographicCode == null) != (part2TerminologyCode == null))
            {
                throw LineError(
                    sourcePath,
                    lineNumber,
                    "Set 2 bibliographic and terminology codes must either both be present or both be absent.");
            }

            Iso639Set3Scope scope = ParseScope(sourcePath, lineNumber, fields[4]);
            Iso639Set3LanguageType languageType = ParseLanguageType(sourcePath, lineNumber, fields[5]);
            string referenceName = ParseRequiredText(sourcePath, lineNumber, fields[6], "reference name");
            string? comment = ParseOptionalText(sourcePath, lineNumber, fields[7], "comment");

            AddUniqueCode(sourcePath, lineNumber, id, "identifier", ids);
            if (part2BibliographicCode != null)
            {
                AddUniqueCode(
                    sourcePath,
                    lineNumber,
                    part2BibliographicCode,
                    "Set 2 bibliographic code",
                    part2BibliographicCodes);
            }

            if (part2TerminologyCode != null)
            {
                AddUniqueCode(
                    sourcePath,
                    lineNumber,
                    part2TerminologyCode,
                    "Set 2 terminology code",
                    part2TerminologyCodes);
            }

            if (alpha2Code != null)
            {
                AddUniqueCode(sourcePath, lineNumber, alpha2Code, "alpha-2 code", alpha2Codes);
            }

            records.Add(new Iso639Set3Record(
                id,
                part2BibliographicCode,
                part2TerminologyCode,
                alpha2Code,
                scope,
                languageType,
                referenceName,
                comment));
        }

        if (records.Count == 0)
        {
            throw new InvalidDataException($"{sourcePath}: expected at least one ISO 639-3 record.");
        }

        return records.ToArray();
    }

    private static string RemoveCarriageReturn(string value)
    {
        if (value.EndsWith('\r'))
        {
            return value.Substring(0, value.Length - 1);
        }

        return value;
    }

    private static Iso639Set3Scope ParseScope(string sourcePath, int lineNumber, string value)
    {
        switch (value)
        {
            case "I":
                return Iso639Set3Scope.Individual;
            case "M":
                return Iso639Set3Scope.Macrolanguage;
            case "S":
                return Iso639Set3Scope.Special;
            default:
                throw LineError(sourcePath, lineNumber, $"unknown scope marker '{value}'.");
        }
    }

    private static Iso639Set3LanguageType ParseLanguageType(string sourcePath, int lineNumber, string value)
    {
        switch (value)
        {
            case "A":
                return Iso639Set3LanguageType.Ancient;
            case "C":
                return Iso639Set3LanguageType.Constructed;
            case "E":
                return Iso639Set3LanguageType.Extinct;
            case "H":
                return Iso639Set3LanguageType.Historical;
            case "L":
                return Iso639Set3LanguageType.Living;
            case "S":
                return Iso639Set3LanguageType.Special;
            default:
                throw LineError(sourcePath, lineNumber, $"unknown language type marker '{value}'.");
        }
    }

    private static string ParseRequiredCode(
        string sourcePath,
        int lineNumber,
        string value,
        int length,
        string fieldName)
    {
        string? code = ParseOptionalCode(sourcePath, lineNumber, value, length, fieldName);
        if (code == null)
        {
            throw LineError(sourcePath, lineNumber, $"{fieldName} is required.");
        }

        return code;
    }

    private static string? ParseOptionalCode(
        string sourcePath,
        int lineNumber,
        string value,
        int length,
        string fieldName)
    {
        if (value.Length == 0)
        {
            return null;
        }

        if (value.Length != length)
        {
            throw LineError(sourcePath, lineNumber, $"{fieldName} must contain exactly {length} letters.");
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

    private static void AddUniqueCode(
        string sourcePath,
        int lineNumber,
        string code,
        string fieldName,
        HashSet<string> codes)
    {
        if (!codes.Add(code))
        {
            throw LineError(sourcePath, lineNumber, $"duplicate {fieldName} '{code}'.");
        }
    }

    private static InvalidDataException LineError(string sourcePath, int lineNumber, string message)
    {
        return new InvalidDataException($"{sourcePath}({lineNumber}): {message}");
    }
}
