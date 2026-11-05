using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using LanguageCodes.DataGenerator.Model;

namespace LanguageCodes.DataGenerator.SourceReaders;

internal static class DataManifestReader
{
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    internal static DataManifest Read(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        string content;
        try
        {
            content = StrictUtf8.GetString(File.ReadAllBytes(sourcePath));
        }
        catch (DecoderFallbackException exception)
        {
            throw new InvalidDataException($"{sourcePath}: manifest is not valid UTF-8.", exception);
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(content);
            JsonElement root = document.RootElement;
            int manifestVersion = GetRequiredInt(sourcePath, root, "manifestVersion");
            int generatorSchemaVersion = GetRequiredInt(sourcePath, root, "generatorSchemaVersion");
            JsonElement sourcesElement = GetRequiredProperty(sourcePath, root, "sources");
            if (sourcesElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException($"{sourcePath}: manifest sources must be an array.");
            }

            List<DataManifestSource> sources = new List<DataManifestSource>();
            HashSet<string> sourceIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> sourcePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            DateOnly retrievalDate = default(DateOnly);
            foreach (JsonElement sourceElement in sourcesElement.EnumerateArray())
            {
                string id = GetRequiredString(sourcePath, sourceElement, "id");
                if (!sourceIds.Add(id))
                {
                    throw new InvalidDataException($"{sourcePath}: manifest source id '{id}' is duplicated.");
                }

                string url = GetRequiredString(sourcePath, sourceElement, "url");
                ValidateUrl(sourcePath, id, url);
                string path = GetRequiredString(sourcePath, sourceElement, "path");
                ValidateRelativePath(sourcePath, id, path);
                if (!sourcePaths.Add(path))
                {
                    throw new InvalidDataException(
                        $"{sourcePath}: manifest source path '{path}' is duplicated.");
                }

                string retrievedAtText = GetRequiredString(sourcePath, sourceElement, "retrievedAt");
                if (!DateTimeOffset.TryParseExact(
                        retrievedAtText,
                        "yyyy-MM-dd'T'HH:mm:sszzz",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTimeOffset retrievedAt))
                {
                    throw new InvalidDataException(
                        $"{sourcePath}: source '{id}' has invalid retrieval timestamp '{retrievedAtText}'.");
                }

                DateOnly sourceRetrievalDate = DateOnly.FromDateTime(retrievedAt.Date);
                if (retrievalDate == default(DateOnly))
                {
                    retrievalDate = sourceRetrievalDate;
                }
                else if (retrievalDate != sourceRetrievalDate)
                {
                    throw new InvalidDataException(
                        $"{sourcePath}: source '{id}' has a retrieval date inconsistent with the manifest.");
                }

                string upstreamVersion = GetRequiredString(
                    sourcePath,
                    sourceElement,
                    "upstreamVersion");
                string? upstreamDate = GetOptionalString(sourcePath, sourceElement, "upstreamDate");
                ValidateUpstreamDate(sourcePath, id, upstreamDate);
                string upstreamDateBasis = GetRequiredString(
                    sourcePath,
                    sourceElement,
                    "upstreamDateBasis");
                string sha256 = GetRequiredString(sourcePath, sourceElement, "sha256");
                ValidateSha256(sourcePath, id, sha256);
                long byteLength = GetRequiredLong(sourcePath, sourceElement, "byteLength");
                string encoding = GetRequiredString(sourcePath, sourceElement, "encoding");
                ValidateEncoding(sourcePath, id, encoding);
                string mediaType = GetRequiredString(sourcePath, sourceElement, "mediaType");
                ValidateMediaType(sourcePath, id, mediaType);
                string expectedHeader = GetRequiredString(
                    sourcePath,
                    sourceElement,
                    "expectedHeader");
                sources.Add(new DataManifestSource(
                    id,
                    url,
                    path,
                    retrievedAt,
                    upstreamVersion,
                    upstreamDate,
                    upstreamDateBasis,
                    sha256,
                    byteLength,
                    encoding,
                    mediaType,
                    expectedHeader));
            }

            if (sources.Count == 0)
            {
                throw new InvalidDataException($"{sourcePath}: manifest must contain at least one source.");
            }

            return new DataManifest(
                manifestVersion,
                generatorSchemaVersion,
                retrievalDate,
                sources.ToArray());
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"{sourcePath}: manifest is not valid JSON.", exception);
        }
    }

    private static JsonElement GetRequiredProperty(string sourcePath, JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement value))
        {
            throw new InvalidDataException($"{sourcePath}: manifest property '{name}' is missing.");
        }

        return value;
    }

    private static int GetRequiredInt(string sourcePath, JsonElement element, string name)
    {
        JsonElement value = GetRequiredProperty(sourcePath, element, name);
        if (!value.TryGetInt32(out int result) || result <= 0)
        {
            throw new InvalidDataException($"{sourcePath}: manifest property '{name}' must be a positive integer.");
        }

        return result;
    }

    private static long GetRequiredLong(string sourcePath, JsonElement element, string name)
    {
        JsonElement value = GetRequiredProperty(sourcePath, element, name);
        if (!value.TryGetInt64(out long result) || result <= 0)
        {
            throw new InvalidDataException(
                $"{sourcePath}: manifest property '{name}' must be a positive integer.");
        }

        return result;
    }

    private static string GetRequiredString(string sourcePath, JsonElement element, string name)
    {
        string? value = GetOptionalString(sourcePath, element, name);
        if (value == null || value.Length == 0)
        {
            throw new InvalidDataException($"{sourcePath}: manifest property '{name}' must be a non-empty string.");
        }

        return value;
    }

    private static string? GetOptionalString(string sourcePath, JsonElement element, string name)
    {
        JsonElement value = GetRequiredProperty(sourcePath, element, name);
        if (value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException($"{sourcePath}: manifest property '{name}' must be a string or null.");
        }

        return value.GetString();
    }

    private static void ValidateUpstreamDate(string sourcePath, string id, string? value)
    {
        if (value == null)
        {
            return;
        }

        bool validFullDate = DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateOnly unusedDate);
        bool validMonth = DateOnly.TryParseExact(
            value + "-01",
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out unusedDate);
        if (!validFullDate && !validMonth)
        {
            throw new InvalidDataException(
                $"{sourcePath}: source '{id}' has invalid upstream date '{value}'.");
        }
    }

    private static void ValidateUrl(string sourcePath, string id, string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidDataException(
                $"{sourcePath}: source '{id}' URL must be an absolute HTTP or HTTPS URL.");
        }
    }

    private static void ValidateRelativePath(string sourcePath, string id, string value)
    {
        if (Path.IsPathRooted(value) || value.IndexOf('\\') >= 0)
        {
            throw new InvalidDataException(
                $"{sourcePath}: source '{id}' path must be a portable relative path.");
        }

        string[] segments = value.Split('/');
        foreach (string segment in segments)
        {
            if (segment.Length == 0 || segment == "." || segment == "..")
            {
                throw new InvalidDataException(
                    $"{sourcePath}: source '{id}' path contains an invalid segment.");
            }
        }
    }

    private static void ValidateSha256(string sourcePath, string id, string value)
    {
        if (value.Length != 64)
        {
            throw new InvalidDataException(
                $"{sourcePath}: source '{id}' SHA-256 must contain 64 lowercase hexadecimal characters.");
        }

        foreach (char character in value)
        {
            bool hexadecimal = character >= '0' && character <= '9' ||
                character >= 'a' && character <= 'f';
            if (!hexadecimal)
            {
                throw new InvalidDataException(
                    $"{sourcePath}: source '{id}' SHA-256 must contain 64 lowercase hexadecimal characters.");
            }
        }
    }

    private static void ValidateEncoding(string sourcePath, string id, string value)
    {
        if (value != "binary" && value != "us-ascii" &&
            value != "utf-8" && value != "utf-8-bom")
        {
            throw new InvalidDataException(
                $"{sourcePath}: source '{id}' has unsupported encoding '{value}'.");
        }
    }

    private static void ValidateMediaType(string sourcePath, string id, string value)
    {
        int separator = value.IndexOf('/');
        if (separator <= 0 || separator == value.Length - 1 ||
            value.IndexOf(';') >= 0 || value.IndexOf(' ') >= 0)
        {
            throw new InvalidDataException(
                $"{sourcePath}: source '{id}' has invalid media type '{value}'.");
        }
    }
}
