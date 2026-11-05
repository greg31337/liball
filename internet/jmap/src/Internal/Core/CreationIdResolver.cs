using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace jmap;

internal class CreationIdResolution
{
    internal bool IsSuccess;
    internal JsonElement Data;
    internal JmapPatchObject? Patch;
    internal JmapSetError? Error;
}

internal class CreationIdResolver
{
    internal const int ValueContext = 0;
    internal const int InvocationContext = 1;
    internal const int PatchMapContext = 2;
    internal const int PatchObjectContext = 3;
    internal Dictionary<string, string> CreatedIds = new Dictionary<string, string>();
    internal bool IncludeCarryOut;
    internal ValueValidator Values = new ValueValidator();
    internal JsonPointer Pointers = new JsonPointer();
    internal JmapJsonReader Reader = new JmapJsonReader(int.MaxValue, 1000);

    internal CreationIdResolver(Dictionary<string, string>? carryIn)
    {
        IncludeCarryOut = carryIn != null;
        if (carryIn != null)
        {
            foreach (KeyValuePair<string, string> item in carryIn)
            {
                CreatedIds.Add(item.Key, item.Value);
            }
        }
    }

    internal bool AccumulateCreatedId(string? creationId, string? id)
    {
        if (!Values.IsCreationId(creationId) || !Values.IsId(id) || creationId == null || id == null)
        {
            return false;
        }

        CreatedIds[creationId] = id;
        return true;
    }

    internal Dictionary<string, string> SnapshotCreatedIds()
    {
        return new Dictionary<string, string>(CreatedIds);
    }

    internal Dictionary<string, string>? GetCarryOutCreatedIds()
    {
        if (!IncludeCarryOut)
        {
            return null;
        }

        return SnapshotCreatedIds();
    }

    internal void AccumulateResponseCreatedIds(JmapInvocation? response)
    {
        if (response == null || response.Name == ResponseMapper.ErrorResponseName ||
            response.Arguments.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        JsonElement created = UniqueProperty(response.Arguments, JmapProperties.Created);
        if (created.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (JsonProperty item in created.EnumerateObject())
        {
            if (item.Value.ValueKind == JsonValueKind.Object)
            {
                JsonElement id = UniqueProperty(item.Value, JmapProperties.Id);
                if (id.ValueKind == JsonValueKind.String)
                {
                    AccumulateCreatedId(item.Name, id.GetString());
                }
            }
        }
    }

    internal JsonElement UniqueProperty(JsonElement value, string name)
    {
        JsonElement result = default(JsonElement);
        bool found = false;
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Name == name)
            {
                if (found)
                {
                    return default(JsonElement);
                }

                result = property.Value;
                found = true;
            }
        }

        return result;
    }

    internal bool ContainsCreationIdReference(JsonElement value)
    {
        return ContainsCreationIdReferenceInContext(value, ValueContext);
    }

    internal bool ContainsInvocationCreationIdReference(JsonElement value)
    {
        return ContainsCreationIdReferenceInContext(value, InvocationContext);
    }

    internal bool ContainsCreationIdReferenceInContext(JsonElement value, int context)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            return Values.IsCreationIdReference(value.GetString());
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in value.EnumerateObject())
            {
                if (Values.IsCreationIdReference(property.Name) ||
                    context == PatchObjectContext &&
                        PatchPathContainsCreationIdReference(property.Name) ||
                    ContainsCreationIdReferenceInContext(
                        property.Value, ChildContext(context, property.Name)))
                {
                    return true;
                }
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in value.EnumerateArray())
            {
                if (ContainsCreationIdReferenceInContext(item, ValueContext))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal bool PatchPathContainsCreationIdReference(string path)
    {
        string pointer = "/" + path;
        if (!Pointers.IsPointer(pointer))
        {
            return false;
        }

        foreach (string token in Pointers.ParsePointer(pointer))
        {
            if (Values.IsCreationIdReference(token))
            {
                return true;
            }
        }

        return false;
    }

    internal int ChildContext(int context, string propertyName)
    {
        if (context == InvocationContext &&
            (propertyName == JmapProperties.Update ||
                propertyName == JmapProperties.OnSuccessUpdateEmail))
        {
            return PatchMapContext;
        }

        if (context == PatchMapContext)
        {
            return PatchObjectContext;
        }

        return ValueContext;
    }

    internal CreationIdResolution ResolveCreateData(JsonElement data)
    {
        return ResolveDataInContext(data, ValueContext);
    }

    internal CreationIdResolution ResolveInvocationData(JsonElement data)
    {
        return ResolveDataInContext(data, InvocationContext);
    }

    internal CreationIdResolution ResolveDataInContext(JsonElement data, int context)
    {
        if (data.ValueKind != JsonValueKind.Object)
        {
            return InvalidDataResolution("");
        }

        List<string> unresolvedPaths = new List<string>();
        CollectUnresolvedPathsInContext(data, "", unresolvedPaths, context);
        if (unresolvedPaths.Count != 0)
        {
            return UnresolvedResolution(unresolvedPaths);
        }

        CreationIdResolution resolution = new CreationIdResolution();
        resolution.IsSuccess = true;
        resolution.Data = BuildResolvedElementInContext(data, context);
        return resolution;
    }

    internal CreationIdResolution ResolveUpdateData(JmapPatchObject? patch)
    {
        if (patch == null)
        {
            return InvalidDataResolution("");
        }

        List<string> unresolvedPaths = new List<string>();
        HashSet<string> resolvedPaths = new HashSet<string>();
        foreach (KeyValuePair<string, JsonElement> item in patch.Values)
        {
            CollectUnresolvedPatchPath(item.Key, unresolvedPaths);
            if (item.Value.ValueKind == JsonValueKind.Undefined)
            {
                unresolvedPaths.Add(item.Key);
            }
            else
            {
                CollectUnresolvedPaths(item.Value, item.Key, unresolvedPaths);
            }

            string resolvedPath = ResolvePatchPath(item.Key);
            if (!resolvedPaths.Add(resolvedPath))
            {
                unresolvedPaths.Add(item.Key);
            }
        }

        if (unresolvedPaths.Count != 0)
        {
            return UnresolvedResolution(unresolvedPaths);
        }

        JmapPatchObject resolvedPatch = new JmapPatchObject();
        foreach (KeyValuePair<string, JsonElement> item in patch.Values)
        {
            resolvedPatch.Values.Add(ResolvePatchPath(item.Key), BuildResolvedElement(item.Value));
        }

        CreationIdResolution resolution = new CreationIdResolution();
        resolution.IsSuccess = true;
        resolution.Patch = resolvedPatch;
        return resolution;
    }

    internal void CollectUnresolvedPaths(JsonElement value, string path, List<string> paths)
    {
        CollectUnresolvedPathsInContext(value, path, paths, ValueContext);
    }

    internal void CollectUnresolvedPathsInContext(
        JsonElement value,
        string path,
        List<string> paths,
        int context)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            string? text = value.GetString();
            if (Values.IsCreationIdReference(text) && text != null && !CreatedIds.ContainsKey(text.Substring(1)))
            {
                paths.Add(path);
            }

            return;
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            HashSet<string> resolvedNames = new HashSet<string>();
            foreach (JsonProperty property in value.EnumerateObject())
            {
                string propertyPath = AppendPropertyPath(path, property.Name);
                if (Values.IsCreationIdReference(property.Name) && !CreatedIds.ContainsKey(property.Name.Substring(1)))
                {
                    paths.Add(propertyPath);
                }

                if (context == PatchObjectContext)
                {
                    CollectUnresolvedPatchPathAt(property.Name, propertyPath, paths);
                }

                string resolvedName = ResolveContextPropertyName(property.Name, context);
                if (!resolvedNames.Add(resolvedName))
                {
                    paths.Add(propertyPath);
                }

                CollectUnresolvedPathsInContext(
                    property.Value,
                    propertyPath,
                    paths,
                    ChildContext(context, property.Name));
            }

            return;
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in value.EnumerateArray())
            {
                CollectUnresolvedPathsInContext(
                    item, AppendArrayPath(path, index), paths, ValueContext);
                index = index + 1;
            }
        }
    }

    internal void CollectUnresolvedPatchPath(string path, List<string> paths)
    {
        CollectUnresolvedPatchPathAt(path, path, paths);
    }

    internal void CollectUnresolvedPatchPathAt(
        string path,
        string errorPath,
        List<string> paths)
    {
        string pointer = "/" + path;
        if (!Pointers.IsPointer(pointer))
        {
            return;
        }

        List<string> tokens = Pointers.ParsePointer(pointer);
        foreach (string token in tokens)
        {
            if (Values.IsCreationIdReference(token) && !CreatedIds.ContainsKey(token.Substring(1)))
            {
                if (!paths.Contains(errorPath))
                {
                    paths.Add(errorPath);
                }

                return;
            }
        }
    }

    internal string ResolvePropertyName(string propertyName)
    {
        if (Values.IsCreationIdReference(propertyName) && CreatedIds.ContainsKey(propertyName.Substring(1)))
        {
            return CreatedIds[propertyName.Substring(1)];
        }

        return propertyName;
    }

    internal string ResolvePatchPath(string path)
    {
        string pointer = "/" + path;
        if (!Pointers.IsPointer(pointer))
        {
            return path;
        }

        List<string> tokens = Pointers.ParsePointer(pointer);
        List<string> resolvedTokens = new List<string>();
        foreach (string token in tokens)
        {
            resolvedTokens.Add(ResolvePropertyName(token));
        }

        string resolvedPointer = Pointers.FormatPointer(resolvedTokens);
        return resolvedPointer.Substring(1);
    }

    internal string ResolveContextPropertyName(string propertyName, int context)
    {
        if (context == PatchObjectContext)
        {
            return ResolvePatchPath(propertyName);
        }

        return ResolvePropertyName(propertyName);
    }

    internal string AppendPropertyPath(string path, string propertyName)
    {
        string token = Pointers.EscapeToken(propertyName);
        if (path.Length == 0)
        {
            return token;
        }

        return path + "/" + token;
    }

    internal string AppendArrayPath(string path, int index)
    {
        string token = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (path.Length == 0)
        {
            return token;
        }

        return path + "/" + token;
    }

    internal JsonElement BuildResolvedElement(JsonElement value)
    {
        return BuildResolvedElementInContext(value, ValueContext);
    }

    internal JsonElement BuildResolvedElementInContext(JsonElement value, int context)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        WriteResolvedElementInContext(value, writer, context);
        writer.DisposeWriter();
        return Reader.ReadUtf8Memory(stream.ToArray());
    }

    internal void WriteResolvedElement(JsonElement value, JmapJsonWriter writer)
    {
        WriteResolvedElementInContext(value, writer, ValueContext);
    }

    internal void WriteResolvedElementInContext(
        JsonElement value,
        JmapJsonWriter writer,
        int context)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            string? text = value.GetString();
            if (Values.IsCreationIdReference(text) && text != null)
            {
                writer.WriteStringValue(CreatedIds[text.Substring(1)]);
            }
            else
            {
                writer.WriteStringValue(text);
            }

            return;
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            List<JsonProperty> properties = new List<JsonProperty>();
            foreach (JsonProperty property in value.EnumerateObject())
            {
                properties.Add(property);
            }

            properties.Sort((left, right) => StringComparer.Ordinal.Compare(
                ResolveContextPropertyName(left.Name, context),
                ResolveContextPropertyName(right.Name, context)));
            writer.WriteObjectStart();
            foreach (JsonProperty property in properties)
            {
                writer.WriteMemberName(ResolveContextPropertyName(property.Name, context));
                WriteResolvedElementInContext(
                    property.Value,
                    writer,
                    ChildContext(context, property.Name));
            }

            writer.WriteObjectEnd();
            return;
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            writer.WriteArrayStart();
            foreach (JsonElement item in value.EnumerateArray())
            {
                WriteResolvedElementInContext(item, writer, ValueContext);
            }

            writer.WriteArrayEnd();
            return;
        }

        writer.WriteElementValue(value);
    }

    internal CreationIdResolution UnresolvedResolution(List<string> paths)
    {
        JmapSetError error = new JmapSetError();
        error.Type = JmapErrorCodes.InvalidProperties;
        error.Kind = JmapSetErrorKind.InvalidProperties;
        error.Description = "Creation-id substitution failed for one or more properties.";
        error.Properties = new List<string>(paths);
        CreationIdResolution resolution = new CreationIdResolution();
        resolution.Error = error;
        return resolution;
    }

    internal CreationIdResolution InvalidDataResolution(string path)
    {
        List<string> paths = new List<string>();
        paths.Add(path);
        JmapSetError error = new JmapSetError();
        error.Type = JmapErrorCodes.InvalidProperties;
        error.Kind = JmapSetErrorKind.InvalidProperties;
        error.Description = "Creation-id substitution requires valid create or update data.";
        error.Properties = paths;
        CreationIdResolution resolution = new CreationIdResolution();
        resolution.Error = error;
        return resolution;
    }
}
