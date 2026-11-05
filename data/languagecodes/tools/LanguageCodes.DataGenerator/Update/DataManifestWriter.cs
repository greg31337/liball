using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using LanguageCodes.DataGenerator.Model;

namespace LanguageCodes.DataGenerator.Update;

internal static class DataManifestWriter
{
    internal static byte[] Render(DataManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        using MemoryStream stream = new MemoryStream();
        using Utf8JsonWriter writer = new Utf8JsonWriter(
            stream,
            new JsonWriterOptions
            {
                Indented = true
            });
        writer.WriteStartObject();
        writer.WriteNumber("manifestVersion", manifest.ManifestVersion);
        writer.WriteNumber("generatorSchemaVersion", manifest.GeneratorSchemaVersion);
        writer.WriteStartArray("sources");
        foreach (DataManifestSource source in manifest.Sources)
        {
            WriteSource(writer, source);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();
        stream.WriteByte((byte)'\n');
        return stream.ToArray();
    }

    private static void WriteSource(Utf8JsonWriter writer, DataManifestSource source)
    {
        writer.WriteStartObject();
        writer.WriteString("id", source.Id);
        writer.WriteString("url", source.Url);
        writer.WriteString("path", source.Path);
        writer.WriteString(
            "retrievedAt",
            source.RetrievedAt.ToString(
                "yyyy-MM-dd'T'HH:mm:sszzz",
                CultureInfo.InvariantCulture));
        writer.WriteString("upstreamVersion", source.UpstreamVersion);
        if (source.UpstreamDate == null)
        {
            writer.WriteNull("upstreamDate");
        }
        else
        {
            writer.WriteString("upstreamDate", source.UpstreamDate);
        }

        writer.WriteString("upstreamDateBasis", source.UpstreamDateBasis);
        writer.WriteString("sha256", source.Sha256);
        writer.WriteNumber("byteLength", source.ByteLength);
        writer.WriteString("encoding", source.Encoding);
        writer.WriteString("mediaType", source.MediaType);
        writer.WriteString("expectedHeader", source.ExpectedHeader);
        writer.WriteEndObject();
    }
}
