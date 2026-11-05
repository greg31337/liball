using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal class CoreSchemaCatalog
{
    internal Dictionary<string, JsonObject> Schemas = new Dictionary<string, JsonObject>();
    internal List<string> SchemaNames = new List<string>();

    internal CoreSchemaCatalog()
    {
        LoadEmbeddedSchemas();
    }

    internal JsonObject GetCoreSchema(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        string normalizedName = GetSchemaName(name);
        if (!Schemas.ContainsKey(normalizedName))
        {
            throw new ArgumentException(
                $"Core schema '{name}' is not available in the embedded catalog.",
                "name");
        }

        return Schemas[normalizedName];
    }

    internal JsonObject ResolveCoreSchemaReference(
        string sourceSchemaName,
        string reference)
    {
        if (sourceSchemaName == null)
        {
            throw new ArgumentNullException("sourceSchemaName");
        }

        if (reference == null)
        {
            throw new ArgumentNullException("reference");
        }

        int fragmentIndex = reference.IndexOf('#');
        string documentReference = fragmentIndex < 0
            ? reference
            : reference.Substring(0, fragmentIndex);
        if (documentReference.Length == 0)
        {
            documentReference = sourceSchemaName;
        }

        return GetCoreSchema(documentReference);
    }

    internal void LoadEmbeddedSchemas()
    {
        byte[] compressed = Convert.FromBase64String(CoreSchemaArchive.Data);
        string json;
        using (MemoryStream stream = new MemoryStream(compressed, false))
        {
            using (GZipStream gzip = new GZipStream(
                stream,
                CompressionMode.Decompress,
                false))
            {
                using (StreamReader reader = new StreamReader(
                    gzip,
                    Encoding.UTF8,
                    true,
                    1024,
                    false))
                {
                    json = reader.ReadToEnd();
                }
            }
        }

        JsonNode? parsed = JsonNode.Parse(json);
        if (parsed == null || parsed.GetValueKind() != JsonValueKind.Object)
        {
            throw new InvalidOperationException("The embedded core schema catalog is invalid.");
        }

        foreach (KeyValuePair<string, JsonNode?> member in parsed.AsObject())
        {
            if (member.Value == null
                || member.Value.GetValueKind() != JsonValueKind.Object)
            {
                throw new InvalidOperationException(
                    $"The embedded core schema '{member.Key}' is invalid.");
            }

            Schemas.Add(member.Key, member.Value.AsObject());
            SchemaNames.Add(member.Key);
        }

        SchemaNames.Sort(StringComparer.Ordinal);
    }

    internal string GetSchemaName(string identifier)
    {
        int fragmentIndex = identifier.IndexOf('#');
        string withoutFragment = fragmentIndex < 0
            ? identifier
            : identifier.Substring(0, fragmentIndex);
        int queryIndex = withoutFragment.IndexOf('?');
        if (queryIndex >= 0)
        {
            withoutFragment = withoutFragment.Substring(0, queryIndex);
        }

        string normalized = withoutFragment.Replace('\\', '/');
        int separatorIndex = normalized.LastIndexOf('/');
        if (separatorIndex >= 0)
        {
            normalized = normalized.Substring(separatorIndex + 1);
        }

        return normalized;
    }
}
