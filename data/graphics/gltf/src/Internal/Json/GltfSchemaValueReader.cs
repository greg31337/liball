using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal class GltfSchemaValueReader
{
    internal GltfReadLimits Limits = new GltfReadLimits();
    internal bool PreserveUnknownMembers;
    internal Dictionary<string, int> ByteOffsets = new Dictionary<string, int>();
    internal JsonPointerBuilder PointerBuilder = new JsonPointerBuilder();
    internal ExtensionHandlerDispatcher ExtensionDispatcher = new ExtensionHandlerDispatcher();
    internal ExtensionContextLocator ExtensionLocations = new ExtensionContextLocator();
    internal List<IGltfExtensionHandler> ExtensionHandlers = new List<IGltfExtensionHandler>();
    internal GltfResourceSet ExtensionResources = new GltfResourceSet();
    internal List<GltfValidationIssue> ExtensionDiagnostics = new List<GltfValidationIssue>();
    internal List<GltfDependency> ExtensionReferences = new List<GltfDependency>();
    internal GltfOperationStatus ExtensionStatus = GltfOperationStatus.Success;

    internal void Configure(
        GltfJsonReadResult json,
        GltfReadLimits limits,
        bool preserveUnknownMembers,
        List<IGltfExtensionHandler> extensionHandlers)
    {
        if (json == null)
        {
            throw new ArgumentNullException("json");
        }

        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        if (extensionHandlers == null)
        {
            throw new ArgumentNullException("extensionHandlers");
        }

        if (limits.MaximumCollectionCount < 0)
        {
            throw new ArgumentOutOfRangeException("limits");
        }

        Limits = limits;
        PreserveUnknownMembers = preserveUnknownMembers;
        ExtensionHandlers = extensionHandlers;
        ExtensionResources = new GltfResourceSet();
        ExtensionDiagnostics = new List<GltfValidationIssue>();
        ExtensionReferences = new List<GltfDependency>();
        ExtensionStatus = GltfOperationStatus.Success;
        ByteOffsets = new Dictionary<string, int>();
        int index = 0;
        while (index < json.Tokens.Count)
        {
            GltfJsonToken token = json.Tokens[index];
            if (!ByteOffsets.ContainsKey(token.JsonPointer))
            {
                ByteOffsets[token.JsonPointer] = token.ByteOffset;
            }

            index++;
        }
    }

    internal JsonObject ReadObjectValue(JsonNode? node, string jsonPointer)
    {
        if (node == null || node.GetValueKind() != JsonValueKind.Object)
        {
            throw CreateSchemaError("Expected a JSON object", jsonPointer);
        }

        return node.AsObject();
    }

    internal JsonArray ReadArrayValue(JsonNode? node, string jsonPointer)
    {
        if (node == null || node.GetValueKind() != JsonValueKind.Array)
        {
            throw CreateSchemaError("Expected a JSON array", jsonPointer);
        }

        JsonArray array = node.AsArray();
        EnsureCollectionCount(array.Count, jsonPointer);
        return array;
    }

    internal JsonNode ReadRequiredNode(JsonObject value, string memberName, string jsonPointer)
    {
        JsonNode? node = value[memberName];
        if (!value.ContainsKey(memberName) || node == null)
        {
            throw CreateSchemaError($"Required JSON member '{memberName}' is missing", jsonPointer);
        }

        return node;
    }

    internal string ReadStringValue(JsonNode? node, string jsonPointer)
    {
        if (node == null || node.GetValueKind() != JsonValueKind.String)
        {
            throw CreateSchemaError("Expected a JSON string", jsonPointer);
        }

        return node.GetValue<string>();
    }

    internal bool ReadBooleanValue(JsonNode? node, string jsonPointer)
    {
        if (node == null
            || (node.GetValueKind() != JsonValueKind.True
                && node.GetValueKind() != JsonValueKind.False))
        {
            throw CreateSchemaError("Expected a JSON boolean", jsonPointer);
        }

        return node.GetValue<bool>();
    }

    internal double ReadDoubleValue(JsonNode? node, string jsonPointer)
    {
        if (node == null || node.GetValueKind() != JsonValueKind.Number)
        {
            throw CreateSchemaError("Expected a JSON number", jsonPointer);
        }

        double value = node.GetValue<double>();
        if (!FiniteNumber.IsFiniteDouble(value))
        {
            throw CreateSchemaError("Expected a finite JSON number", jsonPointer);
        }

        return value;
    }

    internal int ReadInt32Value(JsonNode? node, string jsonPointer)
    {
        double value = ReadDoubleValue(node, jsonPointer);
        if (value < int.MinValue || value > int.MaxValue || value != Math.Truncate(value))
        {
            throw CreateSchemaError("Expected a JSON 32-bit integer", jsonPointer);
        }

        return (int)value;
    }

    internal double[] ReadDoubleArrayValue(
        JsonNode? node,
        string jsonPointer,
        int requiredLength)
    {
        JsonArray array = ReadArrayValue(node, jsonPointer);
        if (requiredLength >= 0 && array.Count != requiredLength)
        {
            throw CreateSchemaError(
                $"Expected a JSON array with exactly {requiredLength} elements",
                jsonPointer);
        }

        double[] values = new double[array.Count];
        int index = 0;
        while (index < array.Count)
        {
            string itemPointer = PointerBuilder.AppendIndex(jsonPointer, index);
            values[index] = ReadDoubleValue(array[index], itemPointer);
            index++;
        }

        return values;
    }

    internal List<int> ReadInt32ListValue(JsonNode? node, string jsonPointer)
    {
        JsonArray array = ReadArrayValue(node, jsonPointer);
        List<int> values = new List<int>();
        int index = 0;
        while (index < array.Count)
        {
            string itemPointer = PointerBuilder.AppendIndex(jsonPointer, index);
            values.Add(ReadInt32Value(array[index], itemPointer));
            index++;
        }

        return values;
    }

    internal List<string> ReadStringListValue(JsonNode? node, string jsonPointer)
    {
        JsonArray array = ReadArrayValue(node, jsonPointer);
        List<string> values = new List<string>();
        int index = 0;
        while (index < array.Count)
        {
            string itemPointer = PointerBuilder.AppendIndex(jsonPointer, index);
            values.Add(ReadStringValue(array[index], itemPointer));
            index++;
        }

        return values;
    }

    internal GltfPropertyJson ReadPropertyJson(
        JsonObject value,
        HashSet<string> knownMemberNames,
        string jsonPointer)
    {
        GltfPropertyJson propertyJson = new GltfPropertyJson();
        if (value.ContainsKey("extensions"))
        {
            string extensionsPointer = PointerBuilder.AppendProperty(jsonPointer, "extensions");
            JsonObject extensions = ReadObjectValue(value["extensions"], extensionsPointer);
            foreach (KeyValuePair<string, JsonNode?> extension in extensions)
            {
                string extensionPointer = PointerBuilder.AppendProperty(extensionsPointer, extension.Key);
                if (extension.Value == null || extension.Value.GetValueKind() != JsonValueKind.Object)
                {
                    throw CreateSchemaError("Expected an extension value to be a JSON object", extensionPointer);
                }

                propertyJson.Extensions[extension.Key] = JsonValueCloner.CloneNode(extension.Value);
                ReadTypedExtension(
                    propertyJson,
                    extension.Key,
                    extension.Value,
                    jsonPointer);
            }
        }

        if (value.ContainsKey("extras"))
        {
            propertyJson.HasExtras = true;
            propertyJson.Extras = JsonValueCloner.CloneNode(value["extras"]);
        }

        if (PreserveUnknownMembers)
        {
            foreach (KeyValuePair<string, JsonNode?> member in value)
            {
                if (!knownMemberNames.Contains(member.Key)
                    && member.Key != "extensions"
                    && member.Key != "extras")
                {
                    propertyJson.UnknownMembers[member.Key] = JsonValueCloner.CloneNode(member.Value);
                }
            }
        }

        return propertyJson;
    }

    internal void ReadTypedExtension(
        GltfPropertyJson propertyJson,
        string name,
        JsonNode json,
        string jsonPointer)
    {
        GltfExtensionContext context = ExtensionLocations.CreateContext(
            null,
            name,
            json,
            jsonPointer,
            new GltfResourceSet());
        GltfExtensionResult result = ExtensionDispatcher.DispatchReadExtension(
            context,
            ExtensionHandlers);
        if (result.TypedData != null)
        {
            propertyJson.TypedExtensions[name] = result.TypedData;
        }

        AppendExtensionResources(result.Resources);
        AppendExtensionDiagnostics(result.Diagnostics);
        AppendExtensionReferences(result.References);
        if ((int)result.Status > (int)ExtensionStatus)
        {
            ExtensionStatus = result.Status;
        }
    }

    internal void AppendExtensionResources(GltfResourceSet source)
    {
        int index = 0;
        while (index < source.Resources.Count)
        {
            GltfResource resource = source.Resources[index];
            ExtensionResources.Resources.Add(resource);
            if (resource.CacheKey != null
                && !ExtensionResources.Cache.ContainsKey(resource.CacheKey))
            {
                ExtensionResources.Cache[resource.CacheKey] = resource;
            }

            index++;
        }
    }

    internal void AppendExtensionDiagnostics(List<GltfValidationIssue> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            ExtensionDiagnostics.Add(source[index]);
            index++;
        }
    }

    internal void AppendExtensionReferences(List<GltfDependency> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            ExtensionReferences.Add(source[index]);
            index++;
        }
    }

    internal void RequireNonEmptyCollection(int count, string jsonPointer)
    {
        if (count == 0)
        {
            throw CreateSchemaError("Expected a non-empty JSON collection", jsonPointer);
        }
    }

    internal void EnsureCollectionCount(int count, string jsonPointer)
    {
        if (count > Limits.MaximumCollectionCount)
        {
            throw CreateSchemaError(
                $"JSON collection count {count} exceeds the configured maximum {Limits.MaximumCollectionCount}",
                jsonPointer);
        }
    }

    internal GltfParseException CreateSchemaError(string message, string jsonPointer)
    {
        int byteOffset = 0;
        bool hasByteOffset = ByteOffsets.ContainsKey(jsonPointer);
        if (hasByteOffset)
        {
            byteOffset = ByteOffsets[jsonPointer];
        }
        else
        {
            string currentPointer = jsonPointer;
            while (!hasByteOffset && currentPointer.Length > 0)
            {
                int slashIndex = currentPointer.LastIndexOf('/');
                if (slashIndex < 0)
                {
                    currentPointer = "";
                }
                else
                {
                    currentPointer = currentPointer.Substring(0, slashIndex);
                }

                if (ByteOffsets.ContainsKey(currentPointer))
                {
                    byteOffset = ByteOffsets[currentPointer];
                    hasByteOffset = true;
                }
            }
        }

        return new GltfParseException($"{message} at byte {byteOffset} and JSON pointer '{jsonPointer}'.");
    }
}
