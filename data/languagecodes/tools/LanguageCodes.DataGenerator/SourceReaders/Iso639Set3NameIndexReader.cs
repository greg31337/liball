using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LanguageCodes.DataGenerator.Model;

namespace LanguageCodes.DataGenerator.SourceReaders;

internal static class Iso639Set3NameIndexReader
{
    private const string ExpectedHeader = "Id\tPrint_Name\tInverted_Name";

    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    internal static Iso639Set3NameRecord[] Read(string sourcePath)
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
            throw new InvalidDataException($"{sourcePath}(1): unexpected ISO 639-3 name-index header.");
        }

        List<Iso639Set3NameRecord> records = new List<Iso639Set3NameRecord>(lineCount - 1);
        HashSet<string> exactRows = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 1; index < lineCount; index++)
        {
            int lineNumber = index + 1;
            string line = RemoveCarriageReturn(lines[index]);
            if (line.Length == 0)
            {
                throw LineError(sourcePath, lineNumber, "empty rows are not allowed.");
            }

            string[] fields = line.Split('\t');
            if (fields.Length != 3)
            {
                throw LineError(sourcePath, lineNumber, "expected exactly three tab-delimited fields.");
            }

            string id = ParseIdentifier(sourcePath, lineNumber, fields[0]);
            string printName = ParseName(sourcePath, lineNumber, fields[1], "print name");
            string invertedName = ParseName(sourcePath, lineNumber, fields[2], "inverted name");
            string exactRowKey = id + "\0" + printName + "\0" + invertedName;

            if (!exactRows.Add(exactRowKey))
            {
                continue;
            }

            records.Add(new Iso639Set3NameRecord(id, printName, invertedName));
        }

        if (records.Count == 0)
        {
            throw new InvalidDataException($"{sourcePath}: expected at least one ISO 639-3 name record.");
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

    private static string ParseIdentifier(string sourcePath, int lineNumber, string value)
    {
        if (value.Length != 3)
        {
            throw LineError(sourcePath, lineNumber, "identifier must contain exactly three letters.");
        }

        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (character < 'a' || character > 'z')
            {
                throw LineError(sourcePath, lineNumber, "identifier must contain lowercase ASCII letters only.");
            }
        }

        return value;
    }

    private static string ParseName(string sourcePath, int lineNumber, string value, string fieldName)
    {
        if (value.Length == 0)
        {
            throw LineError(sourcePath, lineNumber, $"{fieldName} is required.");
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

    private static InvalidDataException LineError(string sourcePath, int lineNumber, string message)
    {
        return new InvalidDataException($"{sourcePath}({lineNumber}): {message}");
    }
}
