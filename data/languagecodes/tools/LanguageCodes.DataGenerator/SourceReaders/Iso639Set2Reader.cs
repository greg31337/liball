using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LanguageCodes.DataGenerator.Model;

namespace LanguageCodes.DataGenerator.SourceReaders;

internal static class Iso639Set2Reader
{
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    internal static Iso639Set2Data Read(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        byte[] bytes = File.ReadAllBytes(sourcePath);
        if (bytes.Length < 3 || bytes[0] != 0xef || bytes[1] != 0xbb || bytes[2] != 0xbf)
        {
            throw new InvalidDataException($"{sourcePath}: expected a UTF-8 byte-order mark.");
        }

        string content;
        try
        {
            content = StrictUtf8.GetString(bytes, 3, bytes.Length - 3);
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

        List<Iso639Set2Record> records = new List<Iso639Set2Record>(lineCount);
        List<Iso639Set2ReservedRange> reservedRanges = new List<Iso639Set2ReservedRange>();
        HashSet<string> bibliographicCodes = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> terminologyCodes = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> alpha2Codes = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> rangeKeys = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < lineCount; index++)
        {
            int lineNumber = index + 1;
            string line = lines[index];
            if (line.EndsWith('\r'))
            {
                line = line.Substring(0, line.Length - 1);
            }

            if (line.Length == 0)
            {
                throw LineError(sourcePath, lineNumber, "empty rows are not allowed.");
            }

            string[] fields = line.Split('|');
            if (fields.Length != 5)
            {
                throw LineError(sourcePath, lineNumber, "expected exactly five pipe-delimited fields.");
            }

            string[] englishNames = ParseNames(sourcePath, lineNumber, fields[3], "English names");
            string[] frenchNames = ParseNames(sourcePath, lineNumber, fields[4], "French names");

            if (fields[0].Contains('-'))
            {
                AddReservedRange(
                    sourcePath,
                    lineNumber,
                    fields,
                    englishNames,
                    frenchNames,
                    rangeKeys,
                    reservedRanges);
                continue;
            }

            string bibliographicCode = ParseRequiredCode(sourcePath, lineNumber, fields[0], 3, "bibliographic code");
            string? terminologyCode = ParseOptionalCode(sourcePath, lineNumber, fields[1], 3, "terminology code");
            string? alpha2Code = ParseOptionalCode(sourcePath, lineNumber, fields[2], 2, "alpha-2 code");

            AddUniqueCode(sourcePath, lineNumber, bibliographicCode, "bibliographic code", bibliographicCodes);
            if (terminologyCode != null)
            {
                if (terminologyCode == bibliographicCode)
                {
                    throw LineError(sourcePath, lineNumber, "terminology code duplicates the bibliographic code.");
                }

                AddUniqueCode(sourcePath, lineNumber, terminologyCode, "terminology code", terminologyCodes);
            }

            if (alpha2Code != null)
            {
                AddUniqueCode(sourcePath, lineNumber, alpha2Code, "alpha-2 code", alpha2Codes);
            }

            records.Add(new Iso639Set2Record(
                bibliographicCode,
                terminologyCode,
                alpha2Code,
                englishNames,
                frenchNames));
        }

        if (records.Count == 0)
        {
            throw new InvalidDataException($"{sourcePath}: expected at least one ISO 639 record.");
        }

        return new Iso639Set2Data(records.ToArray(), reservedRanges.ToArray());
    }

    private static void AddReservedRange(
        string sourcePath,
        int lineNumber,
        string[] fields,
        string[] englishNames,
        string[] frenchNames,
        HashSet<string> rangeKeys,
        List<Iso639Set2ReservedRange> reservedRanges)
    {
        if (fields[1].Length != 0 || fields[2].Length != 0)
        {
            throw LineError(sourcePath, lineNumber, "a reserved range cannot have terminology or alpha-2 mappings.");
        }

        string value = fields[0];
        if (value.Length != 7 || value[3] != '-')
        {
            throw LineError(sourcePath, lineNumber, "reserved range must have the form 'aaa-zzz'.");
        }

        string firstCode = ParseRequiredCode(sourcePath, lineNumber, value.Substring(0, 3), 3, "reserved range start");
        string lastCode = ParseRequiredCode(sourcePath, lineNumber, value.Substring(4, 3), 3, "reserved range end");
        if (string.CompareOrdinal(firstCode, lastCode) > 0)
        {
            throw LineError(sourcePath, lineNumber, "reserved range start must not follow its end.");
        }

        if (!rangeKeys.Add(value))
        {
            throw LineError(sourcePath, lineNumber, $"duplicate reserved range '{value}'.");
        }

        reservedRanges.Add(new Iso639Set2ReservedRange(firstCode, lastCode, englishNames, frenchNames));
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

    private static string[] ParseNames(
        string sourcePath,
        int lineNumber,
        string value,
        string fieldName)
    {
        if (value.Length == 0)
        {
            throw LineError(sourcePath, lineNumber, $"{fieldName} are required.");
        }

        string[] names = value.Split(';');
        HashSet<string> uniqueNames = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < names.Length; index++)
        {
            string name = names[index].Trim();
            if (name.Length == 0)
            {
                throw LineError(sourcePath, lineNumber, $"{fieldName} contain an empty name.");
            }

            if (name.IndexOfAny(['\r', '\n', '\t']) >= 0)
            {
                throw LineError(sourcePath, lineNumber, $"{fieldName} contain a control character.");
            }

            if (!uniqueNames.Add(name))
            {
                throw LineError(sourcePath, lineNumber, $"{fieldName} contain the duplicate name '{name}'.");
            }

            names[index] = name;
        }

        return names;
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
