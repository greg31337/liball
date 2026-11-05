using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CountryCodes.DataGenerator.Staging;

internal sealed class SourceManifestInfo
{
    private readonly IReadOnlyDictionary<string, SourceSnapshotInfo> sources;

    internal SourceManifestInfo(
        string datasetSchemaVersion,
        string dataVersion,
        string generatorVersion,
        string effectiveDate,
        IReadOnlyDictionary<string, SourceSnapshotInfo> sources,
        IReadOnlyList<SourceSnapshotInfo> orderedSources)
    {
        DatasetSchemaVersion = datasetSchemaVersion;
        DataVersion = dataVersion;
        GeneratorVersion = generatorVersion;
        EffectiveDate = effectiveDate;
        this.sources = sources;
        Sources = orderedSources;
    }

    internal string DatasetSchemaVersion { get; }

    internal string DataVersion { get; }

    internal string GeneratorVersion { get; }

    internal string EffectiveDate { get; }

    internal IReadOnlyList<SourceSnapshotInfo> Sources { get; }

    internal SourceSnapshotInfo GetSource(string sourceId)
    {
        if (!sources.TryGetValue(sourceId, out SourceSnapshotInfo? source))
        {
            throw new StagingException($"Manifest does not declare source '{sourceId}'.");
        }

        return source;
    }

    internal static SourceManifestInfo Load(string manifestPath)
    {
        using FileStream stream = File.OpenRead(manifestPath);
        using JsonDocument document = JsonDocument.Parse(stream);
        JsonElement root = document.RootElement;
        string datasetSchemaVersion = ReadString(root, "datasetSchemaVersion", "manifest");
        string dataVersion = ReadString(root, "dataVersion", "manifest");
        string generatorVersion = ReadString(root, "generatorVersion", "manifest");
        string effectiveDate = ReadString(root, "effectiveDate", "manifest");
        if (!root.TryGetProperty("sources", out JsonElement sourcesElement) ||
            sourcesElement.ValueKind != JsonValueKind.Array)
        {
            throw new StagingException("Manifest sources must be an array.");
        }

        Dictionary<string, SourceSnapshotInfo> sources = new Dictionary<string, SourceSnapshotInfo>(
            StringComparer.Ordinal);
        int index = 0;
        foreach (JsonElement source in sourcesElement.EnumerateArray())
        {
            string context = $"sources[{index}]";
            string id = ReadString(source, "id", context);
            string revision = ReadString(source, "revision", context);
            string retrievedOn = ReadString(source, "retrievedOn", context);
            if (!sources.TryAdd(id, new SourceSnapshotInfo(id, revision, retrievedOn)))
            {
                throw new StagingException($"Manifest contains duplicate source id '{id}'.");
            }

            index += 1;
        }

        List<SourceSnapshotInfo> orderedSources = new List<SourceSnapshotInfo>(sources.Values);
        orderedSources.Sort(CompareSources);
        return new SourceManifestInfo(
            datasetSchemaVersion,
            dataVersion,
            generatorVersion,
            effectiveDate,
            sources,
            orderedSources);
    }

    private static int CompareSources(SourceSnapshotInfo left, SourceSnapshotInfo right)
    {
        return string.CompareOrdinal(left.Id, right.Id);
    }

    private static string ReadString(JsonElement parent, string propertyName, string context)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement property) ||
            property.ValueKind != JsonValueKind.String)
        {
            throw new StagingException($"{context}.{propertyName} must be a string.");
        }

        string? value = property.GetString();
        if (string.IsNullOrEmpty(value))
        {
            throw new StagingException($"{context}.{propertyName} must not be empty.");
        }

        return value;
    }
}

internal sealed class SourceSnapshotInfo
{
    internal SourceSnapshotInfo(string id, string revision, string retrievedOn)
    {
        Id = id;
        Revision = revision;
        RetrievedOn = retrievedOn;
    }

    internal string Id { get; }

    internal string Revision { get; }

    internal string RetrievedOn { get; }
}

internal sealed class StagingException : Exception
{
    internal StagingException(string message)
        : base(message)
    {
    }
}
