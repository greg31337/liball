using System;
using System.Text.Json.Nodes;

namespace gltf;

internal class ExtensionContextLocator
{
    internal GltfExtensionContext CreateContext(
        GltfDocument? document,
        string name,
        JsonNode? json,
        string jsonPointer,
        GltfResourceSet resources)
    {
        GltfExtensionContext result = new GltfExtensionContext();
        result.Document = document;
        result.Name = name;
        result.Json = JsonValueCloner.CloneNode(json);
        result.Resources = resources;
        result.ObjectJsonPointer = jsonPointer;
        PopulateLocation(result, jsonPointer);
        return result;
    }

    internal void PopulateLocation(GltfExtensionContext context, string jsonPointer)
    {
        if (jsonPointer == null || jsonPointer.Length == 0)
        {
            context.ObjectLocation = "glTF";
            return;
        }

        string[] segments = jsonPointer.Split('/');
        if (segments.Length < 2)
        {
            context.ObjectLocation = "glTFProperty";
            return;
        }

        string collection = segments[1];
        context.ObjectLocation = GetObjectLocation(collection, segments);
        if (segments.Length > 2 && IsDecimalIndex(segments[2]))
        {
            context.HasObjectIndex = true;
            context.ObjectIndex = ParseDecimalIndex(segments[2]);
        }

        int primitiveSegment = FindSegment(segments, "primitives");
        if (primitiveSegment >= 0
            && primitiveSegment + 1 < segments.Length
            && IsDecimalIndex(segments[primitiveSegment + 1]))
        {
            context.HasSubObjectIndex = true;
            context.SubObjectIndex = ParseDecimalIndex(segments[primitiveSegment + 1]);
        }
    }

    internal string GetObjectLocation(string collection, string[] segments)
    {
        if (collection == "accessors")
        {
            return ContainsSegment(segments, "sparse") ? "accessor.sparse" : "accessor";
        }

        if (collection == "animations")
        {
            if (ContainsSegment(segments, "target"))
            {
                return "animation.channel.target";
            }

            if (ContainsSegment(segments, "channels"))
            {
                return "animation.channel";
            }

            if (ContainsSegment(segments, "samplers"))
            {
                return "animation.sampler";
            }

            return "animation";
        }

        if (collection == "bufferViews")
        {
            return "bufferView";
        }

        if (collection == "buffers")
        {
            return "buffer";
        }

        if (collection == "cameras")
        {
            return "camera";
        }

        if (collection == "images")
        {
            return "image";
        }

        if (collection == "materials")
        {
            if (ContainsTextureInfoSegment(segments))
            {
                return "textureInfo";
            }

            return "material";
        }

        if (collection == "meshes")
        {
            return ContainsSegment(segments, "primitives") ? "mesh.primitive" : "mesh";
        }

        if (collection == "nodes")
        {
            return "node";
        }

        if (collection == "samplers")
        {
            return "sampler";
        }

        if (collection == "scenes")
        {
            return "scene";
        }

        if (collection == "skins")
        {
            return "skin";
        }

        if (collection == "textures")
        {
            return "texture";
        }

        if (collection == "asset")
        {
            return "asset";
        }

        return "glTFProperty";
    }

    internal bool ContainsTextureInfoSegment(string[] segments)
    {
        return ContainsSegment(segments, "baseColorTexture")
            || ContainsSegment(segments, "metallicRoughnessTexture")
            || ContainsSegment(segments, "normalTexture")
            || ContainsSegment(segments, "occlusionTexture")
            || ContainsSegment(segments, "emissiveTexture");
    }

    internal bool ContainsSegment(string[] segments, string value)
    {
        return FindSegment(segments, value) >= 0;
    }

    internal int FindSegment(string[] segments, string value)
    {
        int index = 0;
        while (index < segments.Length)
        {
            if (string.Equals(segments[index], value, StringComparison.Ordinal))
            {
                return index;
            }

            index++;
        }

        return GltfConstants.AbsentIndex;
    }

    internal bool IsDecimalIndex(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal int ParseDecimalIndex(string value)
    {
        int result = 0;
        int index = 0;
        while (index < value.Length)
        {
            int digit = value[index] - '0';
            if (result > (int.MaxValue - digit) / 10)
            {
                throw new GltfParseException($"Object index '{value}' exceeds Int32 range.");
            }

            result = result * 10 + digit;
            index++;
        }

        return result;
    }
}
