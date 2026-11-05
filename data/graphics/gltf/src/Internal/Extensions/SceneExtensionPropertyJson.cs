using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class SceneExtensionPropertyJson
{
    internal GltfPropertyJson ReadPropertyData(JsonObject source, string[] knownNames)
    {
        GltfPropertyJson result = new GltfPropertyJson();
        foreach (KeyValuePair<string, JsonNode?> member in source)
        {
            if (member.Key == "extensions"
                && member.Value != null
                && member.Value.GetValueKind() == System.Text.Json.JsonValueKind.Object)
            {
                foreach (KeyValuePair<string, JsonNode?> extension in member.Value.AsObject())
                {
                    result.Extensions[extension.Key] = JsonValueCloner.CloneNode(extension.Value);
                }
            }
            else if (member.Key == "extras")
            {
                result.HasExtras = true;
                result.Extras = JsonValueCloner.CloneNode(member.Value);
            }
            else if (!ContainsName(knownNames, member.Key))
            {
                result.UnknownMembers[member.Key] = JsonValueCloner.CloneNode(member.Value);
            }
        }

        return result;
    }

    internal void AppendPropertyData(JsonObject target, GltfPropertyJson source)
    {
        if (source.Extensions.Count > 0)
        {
            JsonObject extensions = new JsonObject();
            List<string> names = GetSortedNodeKeys(source.Extensions);
            int extensionIndex = 0;
            while (extensionIndex < names.Count)
            {
                string name = names[extensionIndex];
                extensions[name] = JsonValueCloner.CloneNode(source.Extensions[name]);
                extensionIndex++;
            }

            target["extensions"] = extensions;
        }

        if (source.HasExtras || source.Extras != null)
        {
            target["extras"] = JsonValueCloner.CloneNode(source.Extras);
        }

        List<string> unknownNames = GetSortedNodeKeys(source.UnknownMembers);
        int unknownIndex = 0;
        while (unknownIndex < unknownNames.Count)
        {
            string name = unknownNames[unknownIndex];
            if (!target.ContainsKey(name) && name != "extensions" && name != "extras")
            {
                target[name] = JsonValueCloner.CloneNode(source.UnknownMembers[name]);
            }

            unknownIndex++;
        }
    }

    internal List<string> GetSortedNodeKeys(Dictionary<string, JsonNode?> source)
    {
        List<string> result = new List<string>();
        foreach (KeyValuePair<string, JsonNode?> member in source)
        {
            result.Add(member.Key);
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    internal List<string> GetSortedIndexKeys(Dictionary<string, int> source)
    {
        List<string> result = new List<string>();
        foreach (KeyValuePair<string, int> member in source)
        {
            result.Add(member.Key);
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    internal bool ContainsName(string[] names, string value)
    {
        int index = 0;
        while (index < names.Length)
        {
            if (string.Equals(names[index], value, StringComparison.Ordinal))
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
