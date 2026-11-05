using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LanguageCodes.DataGenerator.Model;

namespace LanguageCodes.DataGenerator.SourceReaders;

internal static class Iso639Set3MacrolanguageReader
{
    private const string ExpectedHeader = "M_Id\tI_Id\tI_Status";

    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    internal static Iso639Set3MacrolanguageMapping[] Read(
        string sourcePath,
        Iso639Set3Record[] activeRecords,
        Iso639Set3RetirementRecord[] retiredRecords)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentNullException.ThrowIfNull(activeRecords);
        ArgumentNullException.ThrowIfNull(retiredRecords);

        Dictionary<string, Iso639Set3Record> activeById = CreateActiveIndex(activeRecords);
        HashSet<string> retiredIds = CreateRetiredIndex(retiredRecords, activeById);
        string content = ReadUtf8(sourcePath);
        string[] lines = content.Split('\n');
        int lineCount = lines.Length;
        if (lineCount > 0 && lines[lineCount - 1].Length == 0)
        {
            lineCount--;
        }

        if (lineCount == 0 || RemoveCarriageReturn(lines[0]) != ExpectedHeader)
        {
            throw new InvalidDataException($"{sourcePath}(1): unexpected ISO 639-3 macrolanguage header.");
        }

        List<Iso639Set3MacrolanguageMapping> mappings =
            new List<Iso639Set3MacrolanguageMapping>(lineCount - 1);
        HashSet<string> mappingKeys = new HashSet<string>(StringComparer.Ordinal);
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

            string macrolanguageId = ParseIdentifier(sourcePath, lineNumber, fields[0], "macrolanguage identifier");
            string individualLanguageId = ParseIdentifier(
                sourcePath,
                lineNumber,
                fields[1],
                "individual-language identifier");
            Iso639Set3MappingStatus status = ParseStatus(sourcePath, lineNumber, fields[2]);

            ValidateReferences(
                sourcePath,
                lineNumber,
                macrolanguageId,
                individualLanguageId,
                status,
                activeById,
                retiredIds);

            string mappingKey = macrolanguageId + "\0" + individualLanguageId;
            if (!mappingKeys.Add(mappingKey))
            {
                throw LineError(
                    sourcePath,
                    lineNumber,
                    $"duplicate macrolanguage mapping '{macrolanguageId}' to '{individualLanguageId}'.");
            }

            mappings.Add(new Iso639Set3MacrolanguageMapping(
                macrolanguageId,
                individualLanguageId,
                status));
        }

        if (mappings.Count == 0)
        {
            throw new InvalidDataException($"{sourcePath}: expected at least one macrolanguage mapping.");
        }

        return mappings.ToArray();
    }

    private static Dictionary<string, Iso639Set3Record> CreateActiveIndex(Iso639Set3Record[] records)
    {
        Dictionary<string, Iso639Set3Record> recordsById =
            new Dictionary<string, Iso639Set3Record>(StringComparer.Ordinal);
        foreach (Iso639Set3Record record in records)
        {
            if (!recordsById.TryAdd(record.Id, record))
            {
                throw new InvalidDataException($"Active ISO 639-3 identifier '{record.Id}' is duplicated.");
            }
        }

        return recordsById;
    }

    private static HashSet<string> CreateRetiredIndex(
        Iso639Set3RetirementRecord[] records,
        Dictionary<string, Iso639Set3Record> activeById)
    {
        HashSet<string> retiredIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (Iso639Set3RetirementRecord record in records)
        {
            string identifier = record.Id;
            if (activeById.ContainsKey(identifier))
            {
                throw new InvalidDataException(
                    $"ISO 639-3 identifier '{identifier}' is both active and retired.");
            }

            if (!retiredIds.Add(identifier))
            {
                throw new InvalidDataException($"Retired ISO 639-3 identifier '{identifier}' is duplicated.");
            }
        }

        return retiredIds;
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

    private static string ParseIdentifier(
        string sourcePath,
        int lineNumber,
        string value,
        string fieldName)
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

    private static Iso639Set3MappingStatus ParseStatus(string sourcePath, int lineNumber, string value)
    {
        switch (value)
        {
            case "A":
                return Iso639Set3MappingStatus.Active;
            case "R":
                return Iso639Set3MappingStatus.Retired;
            default:
                throw LineError(sourcePath, lineNumber, $"unknown mapping status marker '{value}'.");
        }
    }

    private static void ValidateReferences(
        string sourcePath,
        int lineNumber,
        string macrolanguageId,
        string individualLanguageId,
        Iso639Set3MappingStatus status,
        Dictionary<string, Iso639Set3Record> activeById,
        HashSet<string> retiredIds)
    {
        if (!activeById.TryGetValue(macrolanguageId, out Iso639Set3Record? macrolanguage))
        {
            throw LineError(
                sourcePath,
                lineNumber,
                $"macrolanguage identifier '{macrolanguageId}' does not reference an active Set 3 record.");
        }

        if (macrolanguage.Scope != Iso639Set3Scope.Macrolanguage)
        {
            throw LineError(
                sourcePath,
                lineNumber,
                $"identifier '{macrolanguageId}' does not have macrolanguage scope.");
        }

        if (status == Iso639Set3MappingStatus.Active)
        {
            if (!activeById.TryGetValue(individualLanguageId, out Iso639Set3Record? member))
            {
                throw LineError(
                    sourcePath,
                    lineNumber,
                    $"active member '{individualLanguageId}' does not reference an active Set 3 record.");
            }

            if (member.Scope != Iso639Set3Scope.Individual)
            {
                throw LineError(
                    sourcePath,
                    lineNumber,
                    $"active member '{individualLanguageId}' does not have individual-language scope.");
            }

            return;
        }

        if (!retiredIds.Contains(individualLanguageId))
        {
            throw LineError(
                sourcePath,
                lineNumber,
                $"retired member '{individualLanguageId}' does not reference a retired Set 3 record.");
        }
    }

    private static InvalidDataException LineError(string sourcePath, int lineNumber, string message)
    {
        return new InvalidDataException($"{sourcePath}({lineNumber}): {message}");
    }
}
