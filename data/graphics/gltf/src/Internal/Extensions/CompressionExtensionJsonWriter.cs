using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class CompressionExtensionJsonWriter
{
    internal SceneExtensionPropertyJson PropertyJson = new SceneExtensionPropertyJson();

    internal JsonNode? WriteCompressionExtension(
        GltfExtensionContext context,
        GltfTypedExtensionData typed)
    {
        if (context.Name == GltfExtensionNames.KhrDracoMeshCompression
            && typed.DracoMeshCompression != null)
        {
            return WriteDraco(typed.DracoMeshCompression);
        }

        if (context.Name == GltfExtensionNames.KhrMeshoptCompression)
        {
            if (context.ObjectLocation == "buffer"
                && typed.KhrMeshoptCompressionBuffer != null)
            {
                return WriteMeshoptBuffer(typed.KhrMeshoptCompressionBuffer);
            }

            if (context.ObjectLocation == "bufferView"
                && typed.KhrMeshoptCompressionBufferView != null)
            {
                return WriteMeshoptBufferView(typed.KhrMeshoptCompressionBufferView);
            }
        }

        if (context.Name == GltfExtensionNames.ExtMeshoptCompression)
        {
            if (context.ObjectLocation == "buffer"
                && typed.ExtMeshoptCompressionBuffer != null)
            {
                return WriteMeshoptBuffer(typed.ExtMeshoptCompressionBuffer);
            }

            if (context.ObjectLocation == "bufferView"
                && typed.ExtMeshoptCompressionBufferView != null)
            {
                return WriteMeshoptBufferView(typed.ExtMeshoptCompressionBufferView);
            }
        }

        return null;
    }

    internal JsonObject WriteDraco(GltfDracoMeshCompressionExtension source)
    {
        JsonObject result = new JsonObject();
        result["bufferView"] = JsonValue.Create(source.BufferView);
        JsonObject attributes = new JsonObject();
        List<string> names = PropertyJson.GetSortedIndexKeys(source.Attributes);
        int index = 0;
        while (index < names.Count)
        {
            string name = names[index];
            attributes[name] = JsonValue.Create(source.Attributes[name]);
            index++;
        }

        result["attributes"] = attributes;
        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteMeshoptBuffer(GltfMeshoptCompressionBufferExtension source)
    {
        JsonObject result = new JsonObject();
        if (source.HasFallback)
        {
            result["fallback"] = JsonValue.Create(source.Fallback);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }

    internal JsonObject WriteMeshoptBufferView(
        GltfMeshoptCompressionBufferViewExtension source)
    {
        JsonObject result = new JsonObject();
        result["buffer"] = JsonValue.Create(source.Buffer);
        if (source.HasByteOffset)
        {
            result["byteOffset"] = JsonValue.Create(source.ByteOffset);
        }

        result["byteLength"] = JsonValue.Create(source.ByteLength);
        result["byteStride"] = JsonValue.Create(source.ByteStride);
        result["count"] = JsonValue.Create(source.Count);
        result["mode"] = JsonValue.Create(source.Mode);
        if (source.HasFilter)
        {
            result["filter"] = JsonValue.Create(source.Filter);
        }

        PropertyJson.AppendPropertyData(result, source.PropertyJson);
        return result;
    }
}
