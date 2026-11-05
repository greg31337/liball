using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class GltfResourceSchemaReader
{
    internal GltfBuffer ReadBuffer(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfBuffer result = new GltfBuffer();
        if (source.ContainsKey("uri"))
        {
            result.Uri = values.ReadStringValue(
                source["uri"],
                values.PointerBuilder.AppendProperty(jsonPointer, "uri"));
        }

        result.ByteLength = values.ReadInt32Value(
            values.ReadRequiredNode(source, "byteLength", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "byteLength"));
        if (source.ContainsKey("name"))
        {
            result.Name = values.ReadStringValue(
                source["name"],
                values.PointerBuilder.AppendProperty(jsonPointer, "name"));
        }

        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "uri", "byteLength", "name" },
            jsonPointer);
        return result;
    }

    internal GltfBufferView ReadBufferView(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfBufferView result = new GltfBufferView();
        result.Buffer = values.ReadInt32Value(
            values.ReadRequiredNode(source, "buffer", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "buffer"));
        if (source.ContainsKey("byteOffset"))
        {
            result.HasByteOffset = true;
            result.ByteOffset = values.ReadInt32Value(
                source["byteOffset"],
                values.PointerBuilder.AppendProperty(jsonPointer, "byteOffset"));
        }

        result.ByteLength = values.ReadInt32Value(
            values.ReadRequiredNode(source, "byteLength", jsonPointer),
            values.PointerBuilder.AppendProperty(jsonPointer, "byteLength"));
        if (source.ContainsKey("byteStride"))
        {
            result.HasByteStride = true;
            result.ByteStride = values.ReadInt32Value(
                source["byteStride"],
                values.PointerBuilder.AppendProperty(jsonPointer, "byteStride"));
        }

        if (source.ContainsKey("target"))
        {
            result.HasTarget = true;
            result.Target = ReadBufferViewTarget(
                source["target"],
                values.PointerBuilder.AppendProperty(jsonPointer, "target"),
                values);
        }

        if (source.ContainsKey("name"))
        {
            result.Name = values.ReadStringValue(
                source["name"],
                values.PointerBuilder.AppendProperty(jsonPointer, "name"));
        }

        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "buffer", "byteOffset", "byteLength", "byteStride", "target", "name" },
            jsonPointer);
        return result;
    }

    internal GltfTexture ReadTexture(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfTexture result = new GltfTexture();
        if (source.ContainsKey("sampler"))
        {
            result.HasSampler = true;
            result.Sampler = values.ReadInt32Value(
                source["sampler"],
                values.PointerBuilder.AppendProperty(jsonPointer, "sampler"));
        }

        if (source.ContainsKey("source"))
        {
            result.HasSource = true;
            result.Source = values.ReadInt32Value(
                source["source"],
                values.PointerBuilder.AppendProperty(jsonPointer, "source"));
        }

        if (source.ContainsKey("name"))
        {
            result.Name = values.ReadStringValue(
                source["name"],
                values.PointerBuilder.AppendProperty(jsonPointer, "name"));
        }

        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "sampler", "source", "name" },
            jsonPointer);
        return result;
    }

    internal GltfSampler ReadSampler(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfSampler result = new GltfSampler();
        if (source.ContainsKey("magFilter"))
        {
            result.HasMagnificationFilter = true;
            result.MagnificationFilter = ReadMagnificationFilter(
                source["magFilter"],
                values.PointerBuilder.AppendProperty(jsonPointer, "magFilter"),
                values);
        }

        if (source.ContainsKey("minFilter"))
        {
            result.HasMinificationFilter = true;
            result.MinificationFilter = ReadMinificationFilter(
                source["minFilter"],
                values.PointerBuilder.AppendProperty(jsonPointer, "minFilter"),
                values);
        }

        if (source.ContainsKey("wrapS"))
        {
            result.HasWrapS = true;
            result.WrapS = ReadSamplerWrap(
                source["wrapS"],
                values.PointerBuilder.AppendProperty(jsonPointer, "wrapS"),
                values);
        }

        if (source.ContainsKey("wrapT"))
        {
            result.HasWrapT = true;
            result.WrapT = ReadSamplerWrap(
                source["wrapT"],
                values.PointerBuilder.AppendProperty(jsonPointer, "wrapT"),
                values);
        }

        if (source.ContainsKey("name"))
        {
            result.Name = values.ReadStringValue(
                source["name"],
                values.PointerBuilder.AppendProperty(jsonPointer, "name"));
        }

        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "magFilter", "minFilter", "wrapS", "wrapT", "name" },
            jsonPointer);
        return result;
    }

    internal GltfImage ReadImage(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        JsonObject source = values.ReadObjectValue(node, jsonPointer);
        GltfImage result = new GltfImage();
        if (source.ContainsKey("uri"))
        {
            result.Uri = values.ReadStringValue(
                source["uri"],
                values.PointerBuilder.AppendProperty(jsonPointer, "uri"));
        }

        if (source.ContainsKey("mimeType"))
        {
            result.MimeType = values.ReadStringValue(
                source["mimeType"],
                values.PointerBuilder.AppendProperty(jsonPointer, "mimeType"));
        }

        if (source.ContainsKey("bufferView"))
        {
            result.HasBufferView = true;
            result.BufferView = values.ReadInt32Value(
                source["bufferView"],
                values.PointerBuilder.AppendProperty(jsonPointer, "bufferView"));
        }

        if (source.ContainsKey("name"))
        {
            result.Name = values.ReadStringValue(
                source["name"],
                values.PointerBuilder.AppendProperty(jsonPointer, "name"));
        }

        result.PropertyJson = values.ReadPropertyJson(
            source,
            new HashSet<string> { "uri", "mimeType", "bufferView", "name" },
            jsonPointer);
        return result;
    }

    internal GltfBufferViewTarget ReadBufferViewTarget(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        int value = values.ReadInt32Value(node, jsonPointer);
        switch (value)
        {
            case 34962:
                return GltfBufferViewTarget.ArrayBuffer;
            case 34963:
                return GltfBufferViewTarget.ElementArrayBuffer;
            default:
                throw values.CreateSchemaError($"Unknown buffer view target {value}", jsonPointer);
        }
    }

    internal GltfSamplerFilter ReadMagnificationFilter(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        int value = values.ReadInt32Value(node, jsonPointer);
        if (value == 9728)
        {
            return GltfSamplerFilter.Nearest;
        }

        if (value == 9729)
        {
            return GltfSamplerFilter.Linear;
        }

        throw values.CreateSchemaError($"Unknown magnification filter {value}", jsonPointer);
    }

    internal GltfSamplerFilter ReadMinificationFilter(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        int value = values.ReadInt32Value(node, jsonPointer);
        switch (value)
        {
            case 9728:
                return GltfSamplerFilter.Nearest;
            case 9729:
                return GltfSamplerFilter.Linear;
            case 9984:
                return GltfSamplerFilter.NearestMipmapNearest;
            case 9985:
                return GltfSamplerFilter.LinearMipmapNearest;
            case 9986:
                return GltfSamplerFilter.NearestMipmapLinear;
            case 9987:
                return GltfSamplerFilter.LinearMipmapLinear;
            default:
                throw values.CreateSchemaError($"Unknown minification filter {value}", jsonPointer);
        }
    }

    internal GltfSamplerWrap ReadSamplerWrap(
        JsonNode? node,
        string jsonPointer,
        GltfSchemaValueReader values)
    {
        int value = values.ReadInt32Value(node, jsonPointer);
        switch (value)
        {
            case 33071:
                return GltfSamplerWrap.ClampToEdge;
            case 33648:
                return GltfSamplerWrap.MirroredRepeat;
            case 10497:
                return GltfSamplerWrap.Repeat;
            default:
                throw values.CreateSchemaError($"Unknown sampler wrap mode {value}", jsonPointer);
        }
    }
}
