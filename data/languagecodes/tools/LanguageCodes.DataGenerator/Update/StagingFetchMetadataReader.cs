using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace LanguageCodes.DataGenerator.Update;

internal sealed class StagingFetchMetadata
{
    internal StagingFetchMetadata(
        DateTimeOffset completedAt,
        long totalByteLength,
        StagingFetchSourceMetadata[] sources)
    {
        CompletedAt = completedAt;
        TotalByteLength = totalByteLength;
        Sources = sources;
    }

    internal DateTimeOffset CompletedAt { get; }

    internal long TotalByteLength { get; }

    internal StagingFetchSourceMetadata[] Sources { get; }
}

internal sealed class StagingFetchSourceMetadata
{
    internal StagingFetchSourceMetadata(
        string id,
        string requestedUrl,
        string manifestPath,
        string stagedPath,
        DateTimeOffset retrievedAt,
        int statusCode,
        string mediaType,
        DateTimeOffset? lastModified,
        string previousSha256,
        long previousByteLength,
        string sha256,
        long byteLength,
        bool changed)
    {
        Id = id;
        RequestedUrl = requestedUrl;
        ManifestPath = manifestPath;
        StagedPath = stagedPath;
        RetrievedAt = retrievedAt;
        StatusCode = statusCode;
        MediaType = mediaType;
        LastModified = lastModified;
        PreviousSha256 = previousSha256;
        PreviousByteLength = previousByteLength;
        Sha256 = sha256;
        ByteLength = byteLength;
        Changed = changed;
    }

    internal string Id { get; }

    internal string RequestedUrl { get; }

    internal string ManifestPath { get; }

    internal string StagedPath { get; }

    internal DateTimeOffset RetrievedAt { get; }

    internal int StatusCode { get; }

    internal string MediaType { get; }

    internal DateTimeOffset? LastModified { get; }

    internal string PreviousSha256 { get; }

    internal long PreviousByteLength { get; }

    internal string Sha256 { get; }

    internal long ByteLength { get; }

    internal bool Changed { get; }
}

internal static class StagingFetchMetadataReader
{
    internal static StagingFetchMetadata Read(string sourcePath)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(sourcePath));
            JsonElement root = document.RootElement;
            int schemaVersion = GetInt(sourcePath, root, "schemaVersion");
            if (schemaVersion != 1)
            {
                throw new InvalidDataException(
                    $"{sourcePath}: unsupported staging metadata schema {schemaVersion}.");
            }

            if (GetString(sourcePath, root, "manifestFile") != "manifest.json")
            {
                throw new InvalidDataException(
                    $"{sourcePath}: staging metadata manifestFile must be 'manifest.json'.");
            }

            DateTimeOffset completedAt = GetDateTimeOffset(sourcePath, root, "completedAt");
            int sourceCount = GetInt(sourcePath, root, "sourceCount");
            long totalByteLength = GetLong(sourcePath, root, "totalByteLength");
            JsonElement sourcesElement = GetProperty(sourcePath, root, "sources");
            if (sourcesElement.ValueKind != JsonValueKind.Array ||
                sourcesElement.GetArrayLength() != sourceCount)
            {
                throw new InvalidDataException(
                    $"{sourcePath}: staging metadata source count does not match its sources array.");
            }

            List<StagingFetchSourceMetadata> sources = new List<StagingFetchSourceMetadata>();
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            long calculatedByteLength = 0;
            foreach (JsonElement element in sourcesElement.EnumerateArray())
            {
                string id = GetString(sourcePath, element, "id");
                if (!ids.Add(id))
                {
                    throw new InvalidDataException(
                        $"{sourcePath}: staging metadata source '{id}' is duplicated.");
                }

                long byteLength = GetLong(sourcePath, element, "byteLength");
                calculatedByteLength = checked(calculatedByteLength + byteLength);
                sources.Add(new StagingFetchSourceMetadata(
                    id,
                    GetString(sourcePath, element, "requestedUrl"),
                    GetString(sourcePath, element, "manifestPath"),
                    GetString(sourcePath, element, "stagedPath"),
                    GetDateTimeOffset(sourcePath, element, "retrievedAt"),
                    GetInt(sourcePath, element, "statusCode"),
                    GetString(sourcePath, element, "mediaType"),
                    GetOptionalDateTimeOffset(sourcePath, element, "lastModified"),
                    GetSha256(sourcePath, element, "previousSha256"),
                    GetLong(sourcePath, element, "previousByteLength"),
                    GetSha256(sourcePath, element, "sha256"),
                    byteLength,
                    GetBoolean(sourcePath, element, "changed")));
            }

            if (calculatedByteLength != totalByteLength)
            {
                throw new InvalidDataException(
                    $"{sourcePath}: staging metadata total byte length is inconsistent.");
            }

            return new StagingFetchMetadata(completedAt, totalByteLength, sources.ToArray());
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"{sourcePath}: staging fetch metadata is not valid JSON.",
                exception);
        }
    }

    private static JsonElement GetProperty(string path, JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value))
        {
            throw new InvalidDataException($"{path}: staging metadata property '{name}' is missing.");
        }

        return value;
    }

    private static string GetString(string path, JsonElement element, string name)
    {
        JsonElement value = GetProperty(path, element, name);
        if (value.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(value.GetString()))
        {
            throw new InvalidDataException(
                $"{path}: staging metadata property '{name}' must be a non-empty string.");
        }

        return value.GetString()!;
    }

    private static string GetSha256(string path, JsonElement element, string name)
    {
        string value = GetString(path, element, name);
        if (value.Length != 64)
        {
            throw new InvalidDataException(
                $"{path}: staging metadata property '{name}' is not a lowercase SHA-256 value.");
        }

        foreach (char character in value)
        {
            if (!(character >= '0' && character <= '9') &&
                !(character >= 'a' && character <= 'f'))
            {
                throw new InvalidDataException(
                    $"{path}: staging metadata property '{name}' is not a lowercase SHA-256 value.");
            }
        }

        return value;
    }

    private static int GetInt(string path, JsonElement element, string name)
    {
        JsonElement value = GetProperty(path, element, name);
        if (!value.TryGetInt32(out int result) || result < 0)
        {
            throw new InvalidDataException(
                $"{path}: staging metadata property '{name}' must be a nonnegative integer.");
        }

        return result;
    }

    private static long GetLong(string path, JsonElement element, string name)
    {
        JsonElement value = GetProperty(path, element, name);
        if (!value.TryGetInt64(out long result) || result < 0)
        {
            throw new InvalidDataException(
                $"{path}: staging metadata property '{name}' must be a nonnegative integer.");
        }

        return result;
    }

    private static bool GetBoolean(string path, JsonElement element, string name)
    {
        JsonElement value = GetProperty(path, element, name);
        if (value.ValueKind != JsonValueKind.True && value.ValueKind != JsonValueKind.False)
        {
            throw new InvalidDataException(
                $"{path}: staging metadata property '{name}' must be boolean.");
        }

        return value.GetBoolean();
    }

    private static DateTimeOffset GetDateTimeOffset(
        string path,
        JsonElement element,
        string name)
    {
        string value = GetString(path, element, name);
        if (!DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTimeOffset result))
        {
            throw new InvalidDataException(
                $"{path}: staging metadata property '{name}' is not a timestamp.");
        }

        return result;
    }

    private static DateTimeOffset? GetOptionalDateTimeOffset(
        string path,
        JsonElement element,
        string name)
    {
        JsonElement value = GetProperty(path, element, name);
        if (value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return GetDateTimeOffset(path, element, name);
    }
}
