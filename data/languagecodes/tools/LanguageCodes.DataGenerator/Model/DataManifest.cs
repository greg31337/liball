using System;
using System.Collections.Generic;
using System.IO;

namespace LanguageCodes.DataGenerator.Model;

internal sealed class DataManifest
{
    private readonly Dictionary<string, DataManifestSource> sourcesById;

    internal DataManifest(
        int manifestVersion,
        int generatorSchemaVersion,
        DateOnly retrievalDate,
        DataManifestSource[] sources)
    {
        ManifestVersion = manifestVersion;
        GeneratorSchemaVersion = generatorSchemaVersion;
        RetrievalDate = retrievalDate;
        Sources = sources;
        sourcesById = new Dictionary<string, DataManifestSource>(StringComparer.Ordinal);
        foreach (DataManifestSource source in sources)
        {
            sourcesById.Add(source.Id, source);
        }
    }

    internal int ManifestVersion { get; }

    internal int GeneratorSchemaVersion { get; }

    internal DateOnly RetrievalDate { get; }

    internal DataManifestSource[] Sources { get; }

    internal DataManifestSource GetSource(string id)
    {
        if (!sourcesById.TryGetValue(id, out DataManifestSource? source))
        {
            throw new InvalidDataException($"Data manifest source '{id}' is missing.");
        }

        return source;
    }
}
