using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal class EncounteredExtension
{
    internal string Name = "";
    internal string JsonPointer = "";
    internal JsonNode? Json;
}

internal class ExtensionUsageCollector
{
    internal JsonPointerBuilder Pointers = new JsonPointerBuilder();

    internal List<string> CollectEncounteredDocumentExtensions(GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        List<string> result = new List<string>();
        List<EncounteredExtension> encountered = CollectEncounteredExtensionLocations(document);
        int index = 0;
        while (index < encountered.Count)
        {
            string name = encountered[index].Name;
            if (!result.Contains(name))
            {
                result.Add(name);
            }

            index++;
        }

        result.Sort(StringComparer.Ordinal);
        return result;
    }

    internal List<string> CollectObjectGraphExtensions(GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        List<string> result = new List<string>();
        AddDeclaredExtensions(result, document.ExtensionsUsed);
        AddDeclaredExtensions(result, document.ExtensionsRequired);
        HashSet<object> visited = new HashSet<object>(
            ReferenceEqualityComparer.Instance);
        CollectObjectExtensions(document, result, visited);
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    internal void CollectObjectExtensions(
        object? value,
        List<string> result,
        HashSet<object> visited)
    {
        if (value == null || value as string != null || value.GetType().IsValueType)
        {
            return;
        }

        JsonNode? json = value as JsonNode;
        if (json != null)
        {
            CollectJsonExtensions(result, json);
            return;
        }

        if (visited.Contains(value))
        {
            return;
        }

        visited.Add(value);
        GltfPropertyJson? propertyJson = value as GltfPropertyJson;
        if (propertyJson != null)
        {
            AddPropertyExtensions(result, propertyJson);
        }

        IDictionary? dictionary = value as IDictionary;
        if (dictionary != null)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                CollectObjectExtensions(entry.Value, result, visited);
            }

            return;
        }

        IList? list = value as IList;
        if (list != null)
        {
            int itemIndex = 0;
            while (itemIndex < list.Count)
            {
                CollectObjectExtensions(list[itemIndex], result, visited);
                itemIndex++;
            }

            return;
        }

        FieldInfo[] fields = value.GetType().GetFields(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        int fieldIndex = 0;
        while (fieldIndex < fields.Length)
        {
            FieldInfo field = fields[fieldIndex];
            if (!field.IsStatic)
            {
                CollectObjectExtensions(field.GetValue(value), result, visited);
            }

            fieldIndex++;
        }
    }

    internal void AddPropertyExtensions(
        List<string> result,
        GltfPropertyJson propertyJson)
    {
        foreach (KeyValuePair<string, JsonNode?> entry in propertyJson.Extensions)
        {
            if (!result.Contains(entry.Key))
            {
                result.Add(entry.Key);
            }

            CollectJsonExtensions(result, entry.Value);
        }

        foreach (KeyValuePair<string, GltfTypedExtensionData> entry
            in propertyJson.TypedExtensions)
        {
            if (!result.Contains(entry.Key))
            {
                result.Add(entry.Key);
            }
        }
    }

    internal List<EncounteredExtension> CollectEncounteredExtensionLocations(
        GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        List<EncounteredExtension> result = new List<EncounteredExtension>();
        GltfWriteOptions options = new GltfWriteOptions();
        options.PreserveUnknownMembers = true;
        JsonObject serialized = new GltfJsonWriter().WriteDocument(document, options);
        CollectJsonExtensionLocations(result, serialized, "");
        SortEncounteredExtensions(result);
        return result;
    }

    internal List<string> CollectDocumentExtensions(GltfDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        List<string> result = new List<string>();
        AddDeclaredExtensions(result, document.ExtensionsUsed);
        AddDeclaredExtensions(result, document.ExtensionsRequired);
        GltfWriteOptions options = new GltfWriteOptions();
        options.PreserveUnknownMembers = true;
        JsonObject serialized = new GltfJsonWriter().WriteDocument(document, options);
        CollectJsonExtensions(result, serialized);
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    internal void AddDeclaredExtensions(List<string> result, List<string> names)
    {
        if (names == null)
        {
            return;
        }

        int index = 0;
        while (index < names.Count)
        {
            string name = names[index];
            if (name != null && !result.Contains(name))
            {
                result.Add(name);
            }

            index++;
        }
    }

    internal void CollectJsonExtensions(List<string> result, JsonNode? node)
    {
        if (node == null)
        {
            return;
        }

        JsonValueKind kind = node.GetValueKind();
        if (kind == JsonValueKind.Object)
        {
            JsonObject objectValue = node.AsObject();
            foreach (KeyValuePair<string, JsonNode?> member in objectValue)
            {
                if (member.Key == "extensions" && member.Value != null
                    && member.Value.GetValueKind() == JsonValueKind.Object)
                {
                    foreach (KeyValuePair<string, JsonNode?> extension in member.Value.AsObject())
                    {
                        if (!result.Contains(extension.Key))
                        {
                            result.Add(extension.Key);
                        }

                        CollectJsonExtensions(result, extension.Value);
                    }
                }
                else if (member.Key != "extras")
                {
                    CollectJsonExtensions(result, member.Value);
                }
            }
        }
        else if (kind == JsonValueKind.Array)
        {
            JsonArray arrayValue = node.AsArray();
            int index = 0;
            while (index < arrayValue.Count)
            {
                CollectJsonExtensions(result, arrayValue[index]);
                index++;
            }
        }
    }

    internal void CollectJsonExtensionLocations(
        List<EncounteredExtension> result,
        JsonNode? node,
        string jsonPointer)
    {
        if (node == null)
        {
            return;
        }

        JsonValueKind kind = node.GetValueKind();
        if (kind == JsonValueKind.Object)
        {
            JsonObject objectValue = node.AsObject();
            foreach (KeyValuePair<string, JsonNode?> member in objectValue)
            {
                string memberPointer = Pointers.AppendProperty(jsonPointer, member.Key);
                if (member.Key == "extensions" && member.Value != null
                    && member.Value.GetValueKind() == JsonValueKind.Object)
                {
                    foreach (KeyValuePair<string, JsonNode?> extension in member.Value.AsObject())
                    {
                        EncounteredExtension encountered = new EncounteredExtension();
                        encountered.Name = extension.Key;
                        encountered.JsonPointer = Pointers.AppendProperty(
                            memberPointer,
                            extension.Key);
                        encountered.Json = JsonValueCloner.CloneNode(extension.Value);
                        result.Add(encountered);
                        CollectJsonExtensionLocations(
                            result,
                            extension.Value,
                            encountered.JsonPointer);
                    }
                }
                else if (member.Key != "extras")
                {
                    CollectJsonExtensionLocations(result, member.Value, memberPointer);
                }
            }
        }
        else if (kind == JsonValueKind.Array)
        {
            JsonArray arrayValue = node.AsArray();
            int index = 0;
            while (index < arrayValue.Count)
            {
                CollectJsonExtensionLocations(
                    result,
                    arrayValue[index],
                    Pointers.AppendIndex(jsonPointer, index));
                index++;
            }
        }
    }

    internal void SortEncounteredExtensions(List<EncounteredExtension> values)
    {
        int index = 1;
        while (index < values.Count)
        {
            EncounteredExtension value = values[index];
            int insertion = index;
            while (insertion > 0
                && string.CompareOrdinal(
                    values[insertion - 1].JsonPointer,
                    value.JsonPointer) > 0)
            {
                values[insertion] = values[insertion - 1];
                insertion--;
            }

            values[insertion] = value;
            index++;
        }
    }
}
