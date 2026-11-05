using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal static class JsonValueCloner
{
    internal static JsonNode? CloneNode(JsonNode? source)
    {
        if (source == null)
        {
            return null;
        }

        return source.DeepClone();
    }

    internal static GltfPropertyJson ClonePropertyJson(GltfPropertyJson source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        GltfPropertyJson result = new GltfPropertyJson();
        foreach (KeyValuePair<string, JsonNode?> extension in source.Extensions)
        {
            result.Extensions[extension.Key] = CloneNode(extension.Value);
        }

        result.Extras = CloneNode(source.Extras);
        result.HasExtras = source.HasExtras;
        foreach (KeyValuePair<string, JsonNode?> member in source.UnknownMembers)
        {
            result.UnknownMembers[member.Key] = CloneNode(member.Value);
        }

        return result;
    }
}
