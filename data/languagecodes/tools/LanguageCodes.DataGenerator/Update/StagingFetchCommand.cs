using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LanguageCodes.DataGenerator.Model;
using LanguageCodes.DataGenerator.SourceReaders;

namespace LanguageCodes.DataGenerator.Update;

internal sealed class StagingFetchResult
{
    internal StagingFetchResult(string stagingPath, int sourceCount, long byteCount)
    {
        StagingPath = stagingPath;
        SourceCount = sourceCount;
        ByteCount = byteCount;
    }

    internal string StagingPath { get; }

    internal int SourceCount { get; }

    internal long ByteCount { get; }
}

internal sealed class StagedSourceResponse
{
    internal StagedSourceResponse(
        DataManifestSource source,
        string stagedPath,
        DateTimeOffset requestedAt,
        DateTimeOffset retrievedAt,
        int statusCode,
        string finalUrl,
        string mediaType,
        long? contentLengthHeader,
        string? entityTag,
        DateTimeOffset? lastModified,
        DateTimeOffset? responseDate,
        string sha256,
        long byteLength)
    {
        Source = source;
        StagedPath = stagedPath;
        RequestedAt = requestedAt;
        RetrievedAt = retrievedAt;
        StatusCode = statusCode;
        FinalUrl = finalUrl;
        MediaType = mediaType;
        ContentLengthHeader = contentLengthHeader;
        EntityTag = entityTag;
        LastModified = lastModified;
        ResponseDate = responseDate;
        Sha256 = sha256;
        ByteLength = byteLength;
    }

    internal DataManifestSource Source { get; }

    internal string StagedPath { get; }

    internal DateTimeOffset RequestedAt { get; }

    internal DateTimeOffset RetrievedAt { get; }

    internal int StatusCode { get; }

    internal string FinalUrl { get; }

    internal string MediaType { get; }

    internal long? ContentLengthHeader { get; }

    internal string? EntityTag { get; }

    internal DateTimeOffset? LastModified { get; }

    internal DateTimeOffset? ResponseDate { get; }

    internal string Sha256 { get; }

    internal long ByteLength { get; }
}

internal static class StagingFetchCommand
{
    private const int BufferSize = 81920;
    private const long MaximumSourceBytes = 67108864;
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
    private static readonly byte[] Utf8Preamble = new byte[] { 239, 187, 191 };

    internal static StagingFetchResult Execute(string manifestPath, string stagingPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingPath);

        string manifestFullPath = Path.GetFullPath(manifestPath);
        if (!File.Exists(manifestFullPath))
        {
            throw new FileNotFoundException("The data manifest was not found.", manifestFullPath);
        }

        string stagingFullPath = Path.GetFullPath(stagingPath);
        if (File.Exists(stagingFullPath) || Directory.Exists(stagingFullPath))
        {
            throw new IOException($"The staging destination already exists: {stagingFullPath}");
        }

        string? parentPath = Path.GetDirectoryName(stagingFullPath);
        string stagingName = Path.GetFileName(stagingFullPath);
        if (parentPath == null || stagingName.Length == 0 || !Directory.Exists(parentPath))
        {
            throw new DirectoryNotFoundException(
                $"The staging destination parent directory does not exist: {parentPath}");
        }

        string temporaryPath = Path.Combine(
            parentPath,
            "." + stagingName + ".fetch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryPath);
        try
        {
            string stagedManifestPath = Path.Combine(temporaryPath, "manifest.json");
            File.Copy(manifestFullPath, stagedManifestPath, false);
            DataManifest manifest = DataManifestReader.Read(stagedManifestPath);
            string filesPath = Path.Combine(temporaryPath, "files");
            Directory.CreateDirectory(filesPath);

            List<StagedSourceResponse> responses = new List<StagedSourceResponse>();
            long totalByteCount = 0;
            using HttpClientHandler handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All
            };
            using HttpClient client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "languagecodes-data-generator/1.0");

            foreach (DataManifestSource source in manifest.Sources)
            {
                StagedSourceResponse response = FetchSource(client, source, filesPath);
                responses.Add(response);
                totalByteCount = checked(totalByteCount + response.ByteLength);
            }

            WriteMetadata(
                Path.Combine(temporaryPath, "fetch-metadata.json"),
                DateTimeOffset.UtcNow,
                totalByteCount,
                responses);
            Directory.Move(temporaryPath, stagingFullPath);
            return new StagingFetchResult(
                stagingFullPath,
                responses.Count,
                totalByteCount);
        }
        catch
        {
            if (Directory.Exists(temporaryPath))
            {
                Directory.Delete(temporaryPath, true);
            }

            throw;
        }
    }

    private static StagedSourceResponse FetchSource(
        HttpClient client,
        DataManifestSource source,
        string filesPath)
    {
        DateTimeOffset requestedAt = DateTimeOffset.UtcNow;
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, source.Url);
        request.Headers.Accept.ParseAdd(source.MediaType);
        using HttpResponseMessage response = client.Send(
            request,
            HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidDataException(
                $"Source '{source.Id}' returned HTTP {(int)response.StatusCode} " +
                $"{response.ReasonPhrase}.");
        }

        string? responseMediaType = response.Content.Headers.ContentType?.MediaType;
        if (responseMediaType == null ||
            !string.Equals(responseMediaType, source.MediaType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Source '{source.Id}' returned media type '{responseMediaType ?? "(missing)"}' " +
                $"instead of '{source.MediaType}'.");
        }

        long? contentLength = response.Content.Headers.ContentLength;
        if (contentLength > MaximumSourceBytes)
        {
            throw new InvalidDataException(
                $"Source '{source.Id}' exceeds the {MaximumSourceBytes}-byte download limit.");
        }

        byte[] content = ReadContent(response.Content, source.Id);
        ValidateContent(source, content);
        string relativePath = Path.Combine(
            "files",
            source.Path.Replace('/', Path.DirectorySeparatorChar));
        string destinationPath = Path.Combine(
            filesPath,
            source.Path.Replace('/', Path.DirectorySeparatorChar));
        string? destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (destinationDirectory == null)
        {
            throw new InvalidDataException(
                $"Source '{source.Id}' does not have a valid staging path.");
        }

        Directory.CreateDirectory(destinationDirectory);
        File.WriteAllBytes(destinationPath, content);
        DateTimeOffset retrievedAt = DateTimeOffset.UtcNow;
        string sha256 = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        string finalUrl = response.RequestMessage?.RequestUri?.AbsoluteUri ?? source.Url;
        return new StagedSourceResponse(
            source,
            relativePath.Replace(Path.DirectorySeparatorChar, '/'),
            requestedAt,
            retrievedAt,
            (int)response.StatusCode,
            finalUrl,
            responseMediaType,
            contentLength,
            response.Headers.ETag?.ToString(),
            response.Content.Headers.LastModified,
            response.Headers.Date,
            sha256,
            content.LongLength);
    }

    private static byte[] ReadContent(HttpContent content, string sourceId)
    {
        using Stream input = content.ReadAsStream();
        using MemoryStream output = new MemoryStream();
        byte[] buffer = new byte[BufferSize];
        while (true)
        {
            int read = input.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            if (output.Length + read > MaximumSourceBytes)
            {
                throw new InvalidDataException(
                    $"Source '{sourceId}' exceeds the {MaximumSourceBytes}-byte download limit.");
            }

            output.Write(buffer, 0, read);
        }

        return output.ToArray();
    }

    internal static void ValidateContent(DataManifestSource source, byte[] content)
    {
        if (content.Length == 0)
        {
            throw new InvalidDataException($"Source '{source.Id}' returned an empty file.");
        }

        if (source.Encoding == "binary")
        {
            byte[] expected = Encoding.ASCII.GetBytes(source.ExpectedHeader);
            if (!HasPrefix(content, expected))
            {
                throw new InvalidDataException(
                    $"Source '{source.Id}' does not begin with its expected header.");
            }

            return;
        }

        string text;
        if (source.Encoding == "us-ascii")
        {
            foreach (byte value in content)
            {
                if (value > 127)
                {
                    throw new InvalidDataException(
                        $"Source '{source.Id}' is not valid US-ASCII.");
                }
            }

            text = Encoding.ASCII.GetString(content);
        }
        else
        {
            if (source.Encoding == "utf-8-bom" &&
                !HasPrefix(content, Utf8Preamble))
            {
                throw new InvalidDataException(
                    $"Source '{source.Id}' does not have the required UTF-8 BOM.");
            }

            try
            {
                text = StrictUtf8.GetString(content);
            }
            catch (DecoderFallbackException exception)
            {
                throw new InvalidDataException(
                    $"Source '{source.Id}' is not valid UTF-8.",
                    exception);
            }
        }

        string expectedHeader = source.ExpectedHeader;
        if (source.Id == "iana-language-subtag-registry" &&
            expectedHeader.StartsWith("File-Date: ", StringComparison.Ordinal))
        {
            expectedHeader = "File-Date: ";
        }

        if (!text.StartsWith(expectedHeader, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Source '{source.Id}' does not begin with its expected header.");
        }
    }

    private static bool HasPrefix(byte[] content, byte[] prefix)
    {
        if (content.Length < prefix.Length)
        {
            return false;
        }

        for (int index = 0; index < prefix.Length; index++)
        {
            if (content[index] != prefix[index])
            {
                return false;
            }
        }

        return true;
    }

    private static void WriteMetadata(
        string destinationPath,
        DateTimeOffset completedAt,
        long totalByteCount,
        List<StagedSourceResponse> responses)
    {
        using FileStream stream = File.Create(destinationPath);
        using Utf8JsonWriter writer = new Utf8JsonWriter(
            stream,
            new JsonWriterOptions
            {
                Indented = true
            });
        writer.WriteStartObject();
        writer.WriteNumber("schemaVersion", 1);
        writer.WriteString("manifestFile", "manifest.json");
        writer.WriteString("completedAt", completedAt.ToString("O", CultureInfo.InvariantCulture));
        writer.WriteNumber("sourceCount", responses.Count);
        writer.WriteNumber("totalByteLength", totalByteCount);
        writer.WriteStartArray("sources");
        foreach (StagedSourceResponse response in responses)
        {
            WriteSourceMetadata(writer, response);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();
        stream.WriteByte((byte)'\n');
    }

    private static void WriteSourceMetadata(Utf8JsonWriter writer, StagedSourceResponse response)
    {
        DataManifestSource source = response.Source;
        writer.WriteStartObject();
        writer.WriteString("id", source.Id);
        writer.WriteString("requestedUrl", source.Url);
        writer.WriteString("finalUrl", response.FinalUrl);
        writer.WriteString("manifestPath", source.Path);
        writer.WriteString("stagedPath", response.StagedPath);
        writer.WriteString(
            "requestedAt",
            response.RequestedAt.ToString("O", CultureInfo.InvariantCulture));
        writer.WriteString(
            "retrievedAt",
            response.RetrievedAt.ToString("O", CultureInfo.InvariantCulture));
        writer.WriteNumber("statusCode", response.StatusCode);
        writer.WriteString("mediaType", response.MediaType);
        WriteNullableNumber(writer, "contentLengthHeader", response.ContentLengthHeader);
        WriteNullableString(writer, "entityTag", response.EntityTag);
        WriteNullableDate(writer, "lastModified", response.LastModified);
        WriteNullableDate(writer, "responseDate", response.ResponseDate);
        writer.WriteString("previousSha256", source.Sha256);
        writer.WriteNumber("previousByteLength", source.ByteLength);
        writer.WriteString("sha256", response.Sha256);
        writer.WriteNumber("byteLength", response.ByteLength);
        writer.WriteBoolean("changed", response.Sha256 != source.Sha256);
        writer.WriteEndObject();
    }

    private static void WriteNullableString(Utf8JsonWriter writer, string name, string? value)
    {
        if (value == null)
        {
            writer.WriteNull(name);
        }
        else
        {
            writer.WriteString(name, value);
        }
    }

    private static void WriteNullableNumber(Utf8JsonWriter writer, string name, long? value)
    {
        if (value == null)
        {
            writer.WriteNull(name);
        }
        else
        {
            writer.WriteNumber(name, value.Value);
        }
    }

    private static void WriteNullableDate(
        Utf8JsonWriter writer,
        string name,
        DateTimeOffset? value)
    {
        if (value == null)
        {
            writer.WriteNull(name);
        }
        else
        {
            writer.WriteString(name, value.Value.ToString("O", CultureInfo.InvariantCulture));
        }
    }
}
