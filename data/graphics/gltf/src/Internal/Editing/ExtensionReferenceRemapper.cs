using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Nodes;

namespace gltf;

internal class ExtensionReferenceRemapper
{
    internal GltfExtensionHandler Handler = new GltfExtensionHandler();
    internal HashSet<object> Visited = new HashSet<object>(ReferenceEqualityComparer.Instance);

    internal void PrepareRootExtensions(
        GltfDocument document,
        List<GltfValidationIssue> diagnostics)
    {
        List<string> names = new List<string>();
        foreach (KeyValuePair<string, JsonNode?> entry in document.PropertyJson.Extensions)
        {
            if (!document.PropertyJson.TypedExtensions.ContainsKey(entry.Key))
            {
                names.Add(entry.Key);
            }
        }

        names.Sort(StringComparer.Ordinal);
        int index = 0;
        while (index < names.Count)
        {
            string name = names[index];
            GltfExtensionContext context = new GltfExtensionContext();
            context.Document = document;
            context.Name = name;
            context.ObjectLocation = "glTF";
            context.Json = document.PropertyJson.Extensions[name];
            context.Resources = document.Resources;
            GltfExtensionResult parsed = Handler.ReadExtension(context);
            AppendDiagnostics(parsed.Diagnostics, diagnostics);
            if (parsed.TypedData != null)
            {
                document.PropertyJson.TypedExtensions[name] = parsed.TypedData;
            }

            index++;
        }
    }

    internal void RemapDocumentExtensions(
        GltfDocument document,
        GltfIndexRemap remap,
        List<GltfValidationIssue> diagnostics)
    {
        Visited.Clear();
        RemapObjectGraph(document, document, remap, diagnostics);
    }

    internal void RemapObjectGraph(
        object? value,
        GltfDocument document,
        GltfIndexRemap remap,
        List<GltfValidationIssue> diagnostics)
    {
        if (value == null)
        {
            return;
        }

        string? text = value as string;
        if (text != null || value.GetType().IsValueType)
        {
            return;
        }

        JsonNode? json = value as JsonNode;
        if (json != null || Visited.Contains(value))
        {
            return;
        }

        Visited.Add(value);
        IDictionary? dictionary = value as IDictionary;
        if (dictionary != null)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                RemapObjectGraph(entry.Value, document, remap, diagnostics);
            }

            return;
        }

        IList? list = value as IList;
        if (list != null)
        {
            int itemIndex = 0;
            while (itemIndex < list.Count)
            {
                RemapObjectGraph(list[itemIndex], document, remap, diagnostics);
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
                object? fieldValue = field.GetValue(value);
                GltfPropertyJson? propertyJson = fieldValue as GltfPropertyJson;
                if (propertyJson != null)
                {
                    RemapPropertyExtensions(
                        propertyJson,
                        GetObjectLocation(value),
                        document,
                        remap,
                        diagnostics);
                }

                RemapObjectGraph(fieldValue, document, remap, diagnostics);
            }

            fieldIndex++;
        }
    }

    internal void RemapPropertyExtensions(
        GltfPropertyJson propertyJson,
        string objectLocation,
        GltfDocument document,
        GltfIndexRemap remap,
        List<GltfValidationIssue> diagnostics)
    {
        List<string> names = new List<string>();
        foreach (KeyValuePair<string, JsonNode?> entry in propertyJson.Extensions)
        {
            names.Add(entry.Key);
        }

        foreach (KeyValuePair<string, GltfTypedExtensionData> entry
            in propertyJson.TypedExtensions)
        {
            if (!names.Contains(entry.Key))
            {
                names.Add(entry.Key);
            }
        }

        names.Sort(StringComparer.Ordinal);
        int index = 0;
        while (index < names.Count)
        {
            string name = names[index];
            GltfExtensionContext context = new GltfExtensionContext();
            context.Document = document;
            context.Name = name;
            context.ObjectLocation = objectLocation;
            context.Resources = document.Resources;
            if (propertyJson.Extensions.ContainsKey(name))
            {
                context.Json = propertyJson.Extensions[name];
            }

            if (propertyJson.TypedExtensions.ContainsKey(name))
            {
                context.TypedData = propertyJson.TypedExtensions[name];
            }

            GltfExtensionResult result = Handler.RemapExtensionReferences(context, remap);
            AppendDiagnostics(result.Diagnostics, diagnostics);
            if (result.TypedData != null)
            {
                propertyJson.TypedExtensions[name] = result.TypedData;
            }

            if (result.Json != null)
            {
                propertyJson.Extensions[name] = result.Json;
            }

            index++;
        }
    }

    internal string GetObjectLocation(object owner)
    {
        if (owner as GltfDocument != null)
        {
            return "glTF";
        }

        if (owner as GltfNode != null)
        {
            return "node";
        }

        if (owner as GltfMeshPrimitive != null)
        {
            return "mesh.primitive";
        }

        if (owner as GltfBuffer != null)
        {
            return "buffer";
        }

        if (owner as GltfBufferView != null)
        {
            return "bufferView";
        }

        if (owner as GltfMaterial != null)
        {
            return "material";
        }

        if (owner as GltfTextureInfo != null
            || owner as GltfNormalTextureInfo != null
            || owner as GltfOcclusionTextureInfo != null)
        {
            return "textureInfo";
        }

        if (owner as GltfAnimationTarget != null)
        {
            return "animation.channel.target";
        }

        if (owner as GltfAccessor != null)
        {
            return "accessor";
        }

        if (owner as GltfAnimation != null)
        {
            return "animation";
        }

        if (owner as GltfImage != null)
        {
            return "image";
        }

        if (owner as GltfTexture != null)
        {
            return "texture";
        }

        if (owner as GltfScene != null)
        {
            return "scene";
        }

        if (owner as GltfSkin != null)
        {
            return "skin";
        }

        return "glTFProperty";
    }

    internal void AppendDiagnostics(
        List<GltfValidationIssue> source,
        List<GltfValidationIssue> target)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index++;
        }
    }
}
