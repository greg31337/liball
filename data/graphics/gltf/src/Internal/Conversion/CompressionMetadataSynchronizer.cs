using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class CompressionMetadataSynchronizer
{
    internal void SynchronizeDocument(
        GltfDocument document,
        BufferConsolidationResult consolidation)
    {
        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            GltfMesh mesh = document.Meshes[meshIndex];
            int primitiveIndex = 0;
            while (primitiveIndex < mesh.Primitives.Count)
            {
                SynchronizeDracoMetadata(
                    mesh.Primitives[primitiveIndex].PropertyJson,
                    consolidation.BufferViews);
                primitiveIndex++;
            }

            meshIndex++;
        }

        int viewIndex = 0;
        while (viewIndex < document.BufferViews.Count)
        {
            GltfPropertyJson propertyJson =
                document.BufferViews[viewIndex].PropertyJson;
            SynchronizeMeshoptMetadata(
                propertyJson,
                GltfExtensionNames.KhrMeshoptCompression,
                true);
            SynchronizeMeshoptMetadata(
                propertyJson,
                GltfExtensionNames.ExtMeshoptCompression,
                false);
            viewIndex++;
        }
    }

    internal void SynchronizeDracoMetadata(
        GltfPropertyJson propertyJson,
        List<int> bufferViewMap)
    {
        if (!propertyJson.TypedExtensions.ContainsKey(
            GltfExtensionNames.KhrDracoMeshCompression))
        {
            return;
        }

        GltfTypedExtensionData typed = propertyJson.TypedExtensions[
            GltfExtensionNames.KhrDracoMeshCompression];
        GltfDracoMeshCompressionExtension? draco =
            typed.DracoMeshCompression;
        if (draco != null
            && draco.BufferView >= 0
            && draco.BufferView < bufferViewMap.Count)
        {
            draco.BufferView = bufferViewMap[draco.BufferView];
        }
    }

    internal void SynchronizeMeshoptMetadata(
        GltfPropertyJson propertyJson,
        string extensionName,
        bool khr)
    {
        if (!propertyJson.Extensions.ContainsKey(extensionName)
            || !propertyJson.TypedExtensions.ContainsKey(extensionName))
        {
            return;
        }

        JsonObject? payload = propertyJson.Extensions[extensionName]
            as JsonObject;
        if (payload == null)
        {
            return;
        }

        GltfTypedExtensionData typed =
            propertyJson.TypedExtensions[extensionName];
        GltfMeshoptCompressionBufferViewExtension? metadata =
            typed.ExtMeshoptCompressionBufferView;
        if (khr)
        {
            metadata = typed.KhrMeshoptCompressionBufferView;
        }

        if (metadata == null
            || !payload.ContainsKey("buffer")
            || payload["buffer"] == null)
        {
            return;
        }

        JsonNode? bufferNode = payload["buffer"];
        if (bufferNode == null)
        {
            return;
        }

        metadata.Buffer = bufferNode.GetValue<int>();
        metadata.HasByteOffset = payload.ContainsKey("byteOffset");
        if (metadata.HasByteOffset && payload["byteOffset"] != null)
        {
            JsonNode? byteOffsetNode = payload["byteOffset"];
            if (byteOffsetNode != null)
            {
                metadata.ByteOffset = byteOffsetNode.GetValue<int>();
            }
        }
        else
        {
            metadata.ByteOffset = 0;
        }
    }
}
