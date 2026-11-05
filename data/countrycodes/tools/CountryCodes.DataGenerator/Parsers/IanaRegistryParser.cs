using System;
using System.Collections.Generic;
using System.IO;

namespace CountryCodes.DataGenerator.Parsers;

internal static class IanaRegistryParser
{
    internal static IanaRegistryData Parse(TextReader input, string sourceName)
    {
        string? firstLine = input.ReadLine();
        if (firstLine == null || !firstLine.StartsWith("File-Date: ", StringComparison.Ordinal))
        {
            throw new SourceParseException(sourceName, "first line must declare 'File-Date'");
        }

        string fileDate = firstLine.Substring("File-Date: ".Length);
        if (fileDate.Length == 0)
        {
            throw new SourceParseException(sourceName, "File-Date must not be empty");
        }

        string? separator = input.ReadLine();
        if (!string.Equals(separator, "%%", StringComparison.Ordinal))
        {
            throw new SourceParseException(sourceName, "File-Date must be followed by '%%'");
        }

        List<IanaRegistryRecord> records = new List<IanaRegistryRecord>();
        List<MutableField> fields = new List<MutableField>();
        int lineNumber = 2;
        string? line;
        while ((line = input.ReadLine()) != null)
        {
            lineNumber += 1;
            if (string.Equals(line, "%%", StringComparison.Ordinal))
            {
                AddRecord(records, fields, sourceName);
                fields.Clear();
                continue;
            }

            if (line.Length == 0)
            {
                throw new SourceParseException(sourceName, $"unexpected empty line at {lineNumber}");
            }

            if (line[0] == ' ' || line[0] == '\t')
            {
                if (fields.Count == 0)
                {
                    throw new SourceParseException(
                        sourceName,
                        $"continuation line without a field at {lineNumber}");
                }

                MutableField previousField = fields[fields.Count - 1];
                previousField.Value = previousField.Value + "\n" + line.TrimStart(' ', '\t');
                continue;
            }

            int separatorIndex = line.IndexOf(": ", StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                throw new SourceParseException(
                    sourceName,
                    $"malformed field line at {lineNumber}");
            }

            string name = line.Substring(0, separatorIndex);
            string value = line.Substring(separatorIndex + 2);
            if (value.Length == 0)
            {
                throw new SourceParseException(
                    sourceName,
                    $"field '{name}' has an empty value at {lineNumber}");
            }

            fields.Add(new MutableField(name, value));
        }

        AddRecord(records, fields, sourceName);
        if (records.Count == 0)
        {
            throw new SourceParseException(sourceName, "registry contains no records");
        }

        return new IanaRegistryData(fileDate, records);
    }

    private static void AddRecord(
        List<IanaRegistryRecord> records,
        List<MutableField> fields,
        string sourceName)
    {
        if (fields.Count == 0)
        {
            if (records.Count == 0)
            {
                return;
            }

            throw new SourceParseException(sourceName, "empty registry record");
        }

        List<IanaRegistryField> immutableFields = new List<IanaRegistryField>(fields.Count);
        foreach (MutableField field in fields)
        {
            immutableFields.Add(new IanaRegistryField(field.Name, field.Value));
        }

        records.Add(new IanaRegistryRecord(records.Count, immutableFields));
    }

    private sealed class MutableField
    {
        internal MutableField(string name, string value)
        {
            Name = name;
            Value = value;
        }

        internal string Name { get; }

        internal string Value { get; set; }
    }
}
