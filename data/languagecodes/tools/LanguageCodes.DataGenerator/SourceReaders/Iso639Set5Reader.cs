using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LanguageCodes.DataGenerator.Model;

namespace LanguageCodes.DataGenerator.SourceReaders;

internal static class Iso639Set5Reader
{
    private const string ExpectedHeader = "URI\tcode\tLabel (English)\tLabel (French)";
    private const string ExpectedUriPrefix = "http://id.loc.gov/vocabulary/iso639-5/";

    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    internal static Iso639Set5Record[] Read(string sourcePath)
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
            throw new InvalidDataException($"{sourcePath}(1): unexpected ISO 639-5 table header.");
        }

        List<Iso639Set5Record> records = new List<Iso639Set5Record>(lineCount - 1);
        HashSet<string> codes = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 1; index < lineCount; index++)
        {
            int lineNumber = index + 1;
            string line = RemoveCarriageReturn(lines[index]);
            if (line.Length == 0)
            {
                throw LineError(sourcePath, lineNumber, "empty rows are not allowed.");
            }

            string[] fields = line.Split('\t');
            if (fields.Length != 4)
            {
                throw LineError(sourcePath, lineNumber, "expected exactly four tab-delimited fields.");
            }

            string code = ParseCode(sourcePath, lineNumber, fields[1]);
            string expectedUri = ExpectedUriPrefix + code;
            if (fields[0] != expectedUri)
            {
                throw LineError(
                    sourcePath,
                    lineNumber,
                    $"URI must be '{expectedUri}' for code '{code}'.");
            }

            if (!codes.Add(code))
            {
                throw LineError(sourcePath, lineNumber, $"duplicate ISO 639-5 code '{code}'.");
            }

            string[] englishNames = ParseNames(sourcePath, lineNumber, fields[2], "English names");
            string[] frenchNames = ParseNames(sourcePath, lineNumber, fields[3], "French names");
            records.Add(new Iso639Set5Record(fields[0], code, englishNames, frenchNames));
        }

        if (records.Count == 0)
        {
            throw new InvalidDataException($"{sourcePath}: expected at least one ISO 639-5 record.");
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

    private static string ParseCode(string sourcePath, int lineNumber, string value)
    {
        if (value.Length != 3)
        {
            throw LineError(sourcePath, lineNumber, "code must contain exactly three letters.");
        }

        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (character < 'a' || character > 'z')
            {
                throw LineError(sourcePath, lineNumber, "code must contain lowercase ASCII letters only.");
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

            for (int characterIndex = 0; characterIndex < name.Length; characterIndex++)
            {
                if (char.IsControl(name[characterIndex]))
                {
                    throw LineError(sourcePath, lineNumber, $"{fieldName} contain a control character.");
                }
            }

            if (!uniqueNames.Add(name))
            {
                throw LineError(sourcePath, lineNumber, $"{fieldName} contain duplicate name '{name}'.");
            }

            names[index] = name;
        }

        return names;
    }

    private static InvalidDataException LineError(string sourcePath, int lineNumber, string message)
    {
        return new InvalidDataException($"{sourcePath}({lineNumber}): {message}");
    }
}
