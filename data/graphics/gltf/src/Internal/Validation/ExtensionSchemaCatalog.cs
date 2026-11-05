using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal class ExtensionSchemaCatalog
{
    internal Dictionary<string, Dictionary<string, JsonObject>> Schemas =
        new Dictionary<string, Dictionary<string, JsonObject>>();
    internal List<string> ExtensionNames = new List<string>();

    internal ExtensionSchemaCatalog()
    {
        LoadEmbeddedSchemas();
    }

    internal List<string> GetSchemaNames(string extensionName)
    {
        if (extensionName == null)
        {
            throw new ArgumentNullException("extensionName");
        }

        List<string> result = new List<string>();
        if (!Schemas.ContainsKey(extensionName))
        {
            return result;
        }

        foreach (KeyValuePair<string, JsonObject> member in Schemas[extensionName])
        {
            result.Add(member.Key);
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    internal JsonObject GetExtensionSchema(string extensionName, string schemaName)
    {
        if (extensionName == null)
        {
            throw new ArgumentNullException("extensionName");
        }

        if (schemaName == null)
        {
            throw new ArgumentNullException("schemaName");
        }

        if (!Schemas.ContainsKey(extensionName)
            || !Schemas[extensionName].ContainsKey(schemaName))
        {
            throw new ArgumentException(
                $"Schema '{schemaName}' is not available for extension '{extensionName}'.",
                "schemaName");
        }

        return Schemas[extensionName][schemaName];
    }

    internal void LoadEmbeddedSchemas()
    {
        byte[] compressed = Convert.FromBase64String(ExtensionSchemaArchive.Data);
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
            throw new InvalidOperationException("The embedded extension schema catalog is invalid.");
        }

        foreach (KeyValuePair<string, JsonNode?> extensionMember in parsed.AsObject())
        {
            if (extensionMember.Value == null
                || extensionMember.Value.GetValueKind() != JsonValueKind.Object)
            {
                throw new InvalidOperationException(
                    $"The embedded schema family '{extensionMember.Key}' is invalid.");
            }

            Dictionary<string, JsonObject> family =
                new Dictionary<string, JsonObject>();
            foreach (KeyValuePair<string, JsonNode?> schemaMember
                in extensionMember.Value.AsObject())
            {
                if (schemaMember.Value == null
                    || schemaMember.Value.GetValueKind() != JsonValueKind.Object)
                {
                    throw new InvalidOperationException(
                        $"The embedded extension schema '{schemaMember.Key}' is invalid.");
                }

                family.Add(schemaMember.Key, schemaMember.Value.AsObject());
            }

            Schemas.Add(extensionMember.Key, family);
            ExtensionNames.Add(extensionMember.Key);
        }

        ExtensionNames.Sort(StringComparer.Ordinal);
    }
}
