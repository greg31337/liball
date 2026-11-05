using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using LanguageCodes.DataGenerator.Model;

namespace LanguageCodes.DataGenerator.SourceReaders;

internal static class IanaLanguageSubtagRegistryReader
{
    private const string RecordSeparator = "%%";
    private const string FileDatePrefix = "File-Date: ";

    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    private static readonly HashSet<string> KnownFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "Type",
        "Subtag",
        "Tag",
        "Description",
        "Added",
        "Deprecated",
        "Preferred-Value",
        "Prefix",
        "Suppress-Script",
        "Macrolanguage",
        "Scope",
        "Comments"
    };

    internal static IanaLanguageSubtagRegistry Read(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        try
        {
            using FileStream stream = File.OpenRead(sourcePath);
            using StreamReader reader = new StreamReader(stream, StrictUtf8, false);
            return Read(sourcePath, reader);
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException($"{sourcePath}: source is not valid UTF-8.", exception);
        }
    }

    private static IanaLanguageSubtagRegistry Read(string sourcePath, StreamReader reader)
    {
        int lineNumber = 1;
        string? firstLine = reader.ReadLine();
        if (firstLine == null || !firstLine.StartsWith(FileDatePrefix, StringComparison.Ordinal))
        {
            throw LineError(sourcePath, lineNumber, "expected the File-Date header.");
        }

        DateOnly fileDate = ParseDate(
            sourcePath,
            lineNumber,
            firstLine.Substring(FileDatePrefix.Length),
            "file date");

        lineNumber++;
        if (reader.ReadLine() != RecordSeparator)
        {
            throw LineError(sourcePath, lineNumber, "expected a record separator after the header.");
        }

        List<IanaRegistryRecord> records = new List<IanaRegistryRecord>();
        HashSet<string> recordKeys = new HashSet<string>(StringComparer.Ordinal);
        RecordBuilder? builder = null;
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;
            if (line == RecordSeparator)
            {
                if (builder == null)
                {
                    throw LineError(sourcePath, lineNumber, "empty registry records are not allowed.");
                }

                AddRecord(sourcePath, builder, records, recordKeys);
                builder = null;
                continue;
            }

            if (line.Length == 0)
            {
                throw LineError(sourcePath, lineNumber, "empty lines are not allowed.");
            }

            if (builder == null)
            {
                builder = new RecordBuilder(sourcePath, lineNumber);
            }

            if (line.StartsWith("  ", StringComparison.Ordinal))
            {
                builder.AddContinuation(lineNumber, line.Substring(2));
            }
            else
            {
                builder.AddField(lineNumber, line);
            }
        }

        if (builder != null)
        {
            AddRecord(sourcePath, builder, records, recordKeys);
        }

        if (records.Count == 0)
        {
            throw new InvalidDataException($"{sourcePath}: expected at least one IANA registry record.");
        }

        return new IanaLanguageSubtagRegistry(fileDate, records.ToArray());
    }

    private static void AddRecord(
        string sourcePath,
        RecordBuilder builder,
        List<IanaRegistryRecord> records,
        HashSet<string> recordKeys)
    {
        IanaRegistryRecord record = ParseRecord(sourcePath, builder);
        string value = record.Subtag ?? record.Tag ?? "";
        string key = record.RecordType.ToString() + "\0" + value;
        if (!recordKeys.Add(key))
        {
            throw LineError(
                sourcePath,
                builder.StartLineNumber,
                $"duplicate {FormatType(record.RecordType)} record '{value}'.");
        }

        records.Add(record);
    }

    private static IanaRegistryRecord ParseRecord(string sourcePath, RecordBuilder builder)
    {
        string typeText = builder.GetRequiredScalar("Type");
        IanaRegistryRecordType recordType = ParseRecordType(sourcePath, builder.StartLineNumber, typeText);
        string? subtag = builder.GetOptionalScalar("Subtag");
        string? tag = builder.GetOptionalScalar("Tag");
        string[] descriptions = builder.GetRepeatedRequired("Description");
        DateOnly addedDate = ParseDate(
            sourcePath,
            builder.GetFirstLineNumber("Added"),
            builder.GetRequiredScalar("Added"),
            "added date");
        string? deprecatedText = builder.GetOptionalScalar("Deprecated");
        DateOnly? deprecatedDate = null;
        if (deprecatedText != null)
        {
            deprecatedDate = ParseDate(
                sourcePath,
                builder.GetFirstLineNumber("Deprecated"),
                deprecatedText,
                "deprecated date");
        }

        string? preferredValue = builder.GetOptionalScalar("Preferred-Value");
        string[] prefixes = builder.GetRepeated("Prefix");
        string? suppressScript = builder.GetOptionalScalar("Suppress-Script");
        string? macrolanguage = builder.GetOptionalScalar("Macrolanguage");
        string? scope = builder.GetOptionalScalar("Scope");
        string[] comments = builder.GetRepeated("Comments");

        ValidateRecordFields(sourcePath, builder.StartLineNumber, recordType, subtag, tag, prefixes);
        ValidateRecordValues(
            sourcePath,
            builder.StartLineNumber,
            recordType,
            subtag,
            tag,
            preferredValue,
            prefixes,
            suppressScript,
            macrolanguage,
            scope);

        return new IanaRegistryRecord(
            recordType,
            subtag,
            tag,
            descriptions,
            addedDate,
            deprecatedDate,
            preferredValue,
            prefixes,
            suppressScript,
            macrolanguage,
            scope,
            comments);
    }

    private static IanaRegistryRecordType ParseRecordType(
        string sourcePath,
        int lineNumber,
        string value)
    {
        switch (value)
        {
            case "language":
                return IanaRegistryRecordType.Language;
            case "extlang":
                return IanaRegistryRecordType.Extlang;
            case "script":
                return IanaRegistryRecordType.Script;
            case "region":
                return IanaRegistryRecordType.Region;
            case "variant":
                return IanaRegistryRecordType.Variant;
            case "grandfathered":
                return IanaRegistryRecordType.Grandfathered;
            case "redundant":
                return IanaRegistryRecordType.Redundant;
            default:
                throw LineError(sourcePath, lineNumber, $"unknown registry record type '{value}'.");
        }
    }

    private static void ValidateRecordFields(
        string sourcePath,
        int lineNumber,
        IanaRegistryRecordType recordType,
        string? subtag,
        string? tag,
        string[] prefixes)
    {
        bool isTagRecord = recordType == IanaRegistryRecordType.Grandfathered ||
            recordType == IanaRegistryRecordType.Redundant;
        if (isTagRecord)
        {
            if (tag == null || subtag != null)
            {
                throw LineError(sourcePath, lineNumber, $"{FormatType(recordType)} records require Tag, not Subtag.");
            }
        }
        else if (subtag == null || tag != null)
        {
            throw LineError(sourcePath, lineNumber, $"{FormatType(recordType)} records require Subtag, not Tag.");
        }

        if (recordType == IanaRegistryRecordType.Extlang && prefixes.Length == 0)
        {
            throw LineError(sourcePath, lineNumber, "extlang records require at least one Prefix.");
        }

        if (prefixes.Length > 0 && recordType != IanaRegistryRecordType.Extlang &&
            recordType != IanaRegistryRecordType.Variant)
        {
            throw LineError(sourcePath, lineNumber, $"{FormatType(recordType)} records cannot contain Prefix.");
        }
    }

    private static void ValidateRecordValues(
        string sourcePath,
        int lineNumber,
        IanaRegistryRecordType recordType,
        string? subtag,
        string? tag,
        string? preferredValue,
        string[] prefixes,
        string? suppressScript,
        string? macrolanguage,
        string? scope)
    {
        bool validSubtag = true;
        if (subtag != null)
        {
            switch (recordType)
            {
                case IanaRegistryRecordType.Language:
                    validSubtag = IsLowerAlpha(subtag, 2, 8) || IsRange(subtag, IsLanguageRangeEnd);
                    break;
                case IanaRegistryRecordType.Extlang:
                    validSubtag = IsLowerAlpha(subtag, 3, 3);
                    break;
                case IanaRegistryRecordType.Script:
                    validSubtag = IsScript(subtag) || IsRange(subtag, IsScript);
                    break;
                case IanaRegistryRecordType.Region:
                    validSubtag = IsRegion(subtag) || IsRange(subtag, IsAlphaRegion);
                    break;
                case IanaRegistryRecordType.Variant:
                    validSubtag = IsVariant(subtag);
                    break;
            }
        }

        if (!validSubtag)
        {
            throw LineError(sourcePath, lineNumber, $"invalid {FormatType(recordType)} subtag '{subtag}'.");
        }

        if (tag != null && !IsTag(tag))
        {
            throw LineError(sourcePath, lineNumber, $"invalid {FormatType(recordType)} tag '{tag}'.");
        }

        if (preferredValue != null && !IsTag(preferredValue))
        {
            throw LineError(sourcePath, lineNumber, $"invalid Preferred-Value '{preferredValue}'.");
        }

        foreach (string prefix in prefixes)
        {
            if (!IsTag(prefix))
            {
                throw LineError(sourcePath, lineNumber, $"invalid Prefix '{prefix}'.");
            }
        }

        if (suppressScript != null && !IsScript(suppressScript))
        {
            throw LineError(sourcePath, lineNumber, $"invalid Suppress-Script '{suppressScript}'.");
        }

        if (macrolanguage != null && !IsLowerAlpha(macrolanguage, 2, 3))
        {
            throw LineError(sourcePath, lineNumber, $"invalid Macrolanguage '{macrolanguage}'.");
        }

        if (scope != null && scope != "collection" && scope != "macrolanguage" &&
            scope != "private-use" && scope != "special")
        {
            throw LineError(sourcePath, lineNumber, $"unknown Scope '{scope}'.");
        }

    }

    private static bool IsRange(string value, Func<string, bool> validateEnd)
    {
        int separator = value.IndexOf("..", StringComparison.Ordinal);
        if (separator <= 0 || separator != value.LastIndexOf("..", StringComparison.Ordinal))
        {
            return false;
        }

        string first = value.Substring(0, separator);
        string last = value.Substring(separator + 2);
        return first.Length == last.Length && validateEnd(first) && validateEnd(last) &&
            string.CompareOrdinal(first, last) < 0;
    }

    private static bool IsLanguageRangeEnd(string value)
    {
        return IsLowerAlpha(value, 3, 3);
    }

    private static bool IsScript(string value)
    {
        if (value.Length != 4 || value[0] < 'A' || value[0] > 'Z')
        {
            return false;
        }

        for (int index = 1; index < value.Length; index++)
        {
            if (value[index] < 'a' || value[index] > 'z')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsRegion(string value)
    {
        if (IsAlphaRegion(value))
        {
            return true;
        }

        if (value.Length != 3)
        {
            return false;
        }

        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAlphaRegion(string value)
    {
        return value.Length == 2 && value[0] >= 'A' && value[0] <= 'Z' &&
            value[1] >= 'A' && value[1] <= 'Z';
    }

    private static bool IsVariant(string value)
    {
        if (!IsAsciiAlphaNumeric(value))
        {
            return false;
        }

        if (value.Length >= 5 && value.Length <= 8)
        {
            return value == value.ToLowerInvariant();
        }

        return value.Length == 4 && value[0] >= '0' && value[0] <= '9' &&
            value == value.ToLowerInvariant();
    }

    private static bool IsTag(string value)
    {
        if (value.Length == 0 || value[0] == '-' || value[value.Length - 1] == '-')
        {
            return false;
        }

        string[] parts = value.Split('-');
        foreach (string part in parts)
        {
            if (part.Length == 0 || part.Length > 8 || !IsAsciiAlphaNumeric(part))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsLowerAlpha(string value, int minimumLength, int maximumLength)
    {
        if (value.Length < minimumLength || value.Length > maximumLength)
        {
            return false;
        }

        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] < 'a' || value[index] > 'z')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAsciiAlphaNumeric(string value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            bool isAsciiLetter = character >= 'a' && character <= 'z' ||
                character >= 'A' && character <= 'Z';
            bool isDigit = character >= '0' && character <= '9';
            if (!isAsciiLetter && !isDigit)
            {
                return false;
            }
        }

        return value.Length > 0;
    }

    private static DateOnly ParseDate(
        string sourcePath,
        int lineNumber,
        string value,
        string fieldName)
    {
        if (!DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly date))
        {
            throw LineError(sourcePath, lineNumber, $"{fieldName} '{value}' is not a valid ISO date.");
        }

        return date;
    }

    private static string FormatType(IanaRegistryRecordType recordType)
    {
        return recordType.ToString().ToLowerInvariant();
    }

    private static InvalidDataException LineError(string sourcePath, int lineNumber, string message)
    {
        return new InvalidDataException($"{sourcePath}({lineNumber}): {message}");
    }

    private sealed class RecordBuilder
    {
        private readonly string sourcePath;
        private readonly List<FieldValue> fields = new List<FieldValue>();

        internal RecordBuilder(string sourcePath, int startLineNumber)
        {
            this.sourcePath = sourcePath;
            StartLineNumber = startLineNumber;
        }

        internal int StartLineNumber { get; }

        internal void AddField(int lineNumber, string line)
        {
            int separator = line.IndexOf(": ", StringComparison.Ordinal);
            if (separator <= 0)
            {
                throw LineError(sourcePath, lineNumber, "expected a registry field in 'Name: value' form.");
            }

            string name = line.Substring(0, separator);
            string value = line.Substring(separator + 2);
            if (!KnownFields.Contains(name))
            {
                throw LineError(sourcePath, lineNumber, $"unknown registry field '{name}'.");
            }

            ValidateText(lineNumber, value);
            fields.Add(new FieldValue(name, value, lineNumber));
        }

        internal void AddContinuation(int lineNumber, string value)
        {
            if (fields.Count == 0)
            {
                throw LineError(sourcePath, lineNumber, "a folded continuation has no preceding field.");
            }

            ValidateText(lineNumber, value);
            FieldValue previous = fields[fields.Count - 1];
            if (previous.Name != "Description" && previous.Name != "Comments")
            {
                throw LineError(
                    sourcePath,
                    lineNumber,
                    $"field '{previous.Name}' cannot have a folded continuation.");
            }

            previous.Value += " " + value;
        }

        internal string GetRequiredScalar(string name)
        {
            string? value = GetOptionalScalar(name);
            if (value == null)
            {
                throw LineError(sourcePath, StartLineNumber, $"required field '{name}' is missing.");
            }

            return value;
        }

        internal string? GetOptionalScalar(string name)
        {
            string? value = null;
            foreach (FieldValue field in fields)
            {
                if (field.Name != name)
                {
                    continue;
                }

                if (value != null)
                {
                    throw LineError(sourcePath, field.LineNumber, $"field '{name}' cannot be repeated.");
                }

                value = field.Value;
            }

            return value;
        }

        internal string[] GetRepeatedRequired(string name)
        {
            string[] values = GetRepeated(name);
            if (values.Length == 0)
            {
                throw LineError(sourcePath, StartLineNumber, $"required field '{name}' is missing.");
            }

            return values;
        }

        internal string[] GetRepeated(string name)
        {
            List<string> values = new List<string>();
            foreach (FieldValue field in fields)
            {
                if (field.Name == name)
                {
                    values.Add(field.Value);
                }
            }

            return values.ToArray();
        }

        internal int GetFirstLineNumber(string name)
        {
            foreach (FieldValue field in fields)
            {
                if (field.Name == name)
                {
                    return field.LineNumber;
                }
            }

            return StartLineNumber;
        }

        private void ValidateText(int lineNumber, string value)
        {
            if (value.Length == 0 || value != value.Trim())
            {
                throw LineError(sourcePath, lineNumber, "registry field values cannot be empty or padded.");
            }

            for (int index = 0; index < value.Length; index++)
            {
                if (char.IsControl(value[index]))
                {
                    throw LineError(sourcePath, lineNumber, "registry field values cannot contain control characters.");
                }
            }
        }
    }

    private sealed class FieldValue
    {
        internal FieldValue(string name, string value, int lineNumber)
        {
            Name = name;
            Value = value;
            LineNumber = lineNumber;
        }

        internal string Name { get; }

        internal string Value { get; set; }

        internal int LineNumber { get; }
    }
}
