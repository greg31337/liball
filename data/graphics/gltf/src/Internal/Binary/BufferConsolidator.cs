using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

internal class BufferConsolidationOptions
{
    internal int MaximumBufferByteLength = int.MaxValue;
    internal bool Partition;
}

internal class BufferConsolidationResult
{
    internal int OriginalBufferCount;
    internal int ResultBufferCount;
    internal List<int> BufferViews = new List<int>();
}

internal class ExtensionBufferViewReference
{
    internal JsonObject Payload = new JsonObject();
    internal int BufferView;
    internal int RelatedBufferView = GltfConstants.AbsentIndex;
}

internal class MeshoptBufferReference
{
    internal JsonObject Payload = new JsonObject();
    internal int PackItem;
}

internal class BufferConsolidator
{
    internal BufferPacker Packer = new BufferPacker();

    internal BufferConsolidationResult ConsolidateBuffers(
        GltfDocument document,
        BufferConsolidationOptions options)
    {
        ValidateDocumentCollections(document, options);
        int originalBufferCount = document.Buffers.Count;
        int originalBufferViewCount = document.BufferViews.Count;
        ValidateCoreBufferViewReferences(document);
        List<BufferPackItem> items = CreateBufferViewItems(document);
        List<MeshoptBufferReference> meshoptReferences = new List<MeshoptBufferReference>();
        CollectMeshoptReferences(document, items, meshoptReferences);
        List<ExtensionBufferViewReference> extensionBufferViewReferences = new List<ExtensionBufferViewReference>();
        CollectExtensionBufferViewReferences(document, extensionBufferViewReferences);

        BufferPackOptions packOptions = new BufferPackOptions();
        packOptions.MaximumBufferByteLength = options.MaximumBufferByteLength;
        packOptions.Partition = options.Partition;
        packOptions.Interleave = false;
        BufferPackResult packed = Packer.PackBuffers(items, packOptions);
        ValidateExtensionBufferRelationships(
            extensionBufferViewReferences,
            packed);
        List<int> bufferViewMap = CreateBufferViewMap(
            originalBufferViewCount,
            packed);
        List<GltfBuffer> buffers = CreateBuffers(packed);
        List<GltfBufferView> bufferViews = CreateBufferViews(
            document.BufferViews,
            originalBufferViewCount,
            packed,
            bufferViewMap);

        ApplyCoreBufferViewRemapping(document, bufferViewMap);
        ApplyExtensionBufferViewRemapping(
            extensionBufferViewReferences,
            bufferViewMap);
        ApplyMeshoptRemapping(meshoptReferences, packed);
        document.Buffers = buffers;
        document.BufferViews = bufferViews;

        BufferConsolidationResult result = new BufferConsolidationResult();
        result.OriginalBufferCount = originalBufferCount;
        result.ResultBufferCount = buffers.Count;
        result.BufferViews = bufferViewMap;
        return result;
    }

    internal void ValidateDocumentCollections(
        GltfDocument document,
        BufferConsolidationOptions options)
    {
        if (document == null
            || document.Buffers == null
            || document.BufferViews == null
            || document.Accessors == null
            || document.Images == null
            || document.Meshes == null
            || document.Nodes == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null || options.MaximumBufferByteLength < 1)
        {
            throw new ArgumentOutOfRangeException("options");
        }
    }

    internal List<BufferPackItem> CreateBufferViewItems(GltfDocument document)
    {
        List<BufferPackItem> result = new List<BufferPackItem>();
        int index = 0;
        while (index < document.BufferViews.Count)
        {
            GltfBufferView bufferView = document.BufferViews[index];
            if (bufferView == null)
            {
                throw new ArgumentException(
                    $"Buffer view {index} is null.",
                    "document");
            }

            int byteOffset = 0;
            if (bufferView.HasByteOffset)
            {
                byteOffset = bufferView.ByteOffset;
            }

            byte[] bytes = CopyBufferRange(
                document,
                bufferView.Buffer,
                byteOffset,
                bufferView.ByteLength,
                $"buffer view {index}");
            BufferPackItem item = new BufferPackItem();
            item.Bytes = bytes;
            item.ElementCount = 1;
            item.ElementByteLength = bytes.Length;
            item.ElementAlignment = 1;
            if (bufferView.HasTarget)
            {
                item.Target = bufferView.Target;
            }

            item.PreserveLayout = true;
            item.HasByteStride = bufferView.HasByteStride;
            item.ByteStride = bufferView.ByteStride;
            result.Add(item);
            index++;
        }

        return result;
    }

    internal void CollectMeshoptReferences(
        GltfDocument document,
        List<BufferPackItem> items,
        List<MeshoptBufferReference> references)
    {
        int index = 0;
        while (index < document.BufferViews.Count)
        {
            GltfBufferView bufferView = document.BufferViews[index];
            CollectMeshoptReference(
                document,
                bufferView.PropertyJson,
                GltfExtensionNames.KhrMeshoptCompression,
                items,
                references);
            CollectMeshoptReference(
                document,
                bufferView.PropertyJson,
                GltfExtensionNames.ExtMeshoptCompression,
                items,
                references);
            index++;
        }
    }

    internal void CollectMeshoptReference(
        GltfDocument document,
        GltfPropertyJson propertyJson,
        string extensionName,
        List<BufferPackItem> items,
        List<MeshoptBufferReference> references)
    {
        JsonObject? payload = GetExtensionObject(propertyJson, extensionName);
        if (payload == null)
        {
            return;
        }

        int bufferIndex = ReadRequiredInteger(payload, "buffer", extensionName);
        int byteOffset = ReadOptionalInteger(payload, "byteOffset", extensionName);
        int byteLength = ReadRequiredInteger(payload, "byteLength", extensionName);
        byte[] bytes = CopyBufferRange(
            document,
            bufferIndex,
            byteOffset,
            byteLength,
            extensionName);
        BufferPackItem item = new BufferPackItem();
        item.Bytes = bytes;
        item.ElementCount = 1;
        item.ElementByteLength = bytes.Length;
        item.ElementAlignment = 1;
        item.PreserveLayout = true;
        MeshoptBufferReference reference = new MeshoptBufferReference();
        reference.Payload = payload;
        reference.PackItem = items.Count;
        items.Add(item);
        references.Add(reference);
    }

    internal void CollectExtensionBufferViewReferences(
        GltfDocument document,
        List<ExtensionBufferViewReference> references)
    {
        CollectArrayBufferViewReferences(
            document.PropertyJson,
            GltfExtensionNames.ExtLightsIes,
            "lights",
            document.BufferViews.Count,
            references);
        CollectArrayBufferViewReferences(
            document.PropertyJson,
            GltfExtensionNames.NvMaterialsMdl,
            "modules",
            document.BufferViews.Count,
            references);
        CollectArrayBufferViewReferences(
            document.PropertyJson,
            GltfExtensionNames.NvMaterialsMdl,
            "bsdfMeasurements",
            document.BufferViews.Count,
            references);
        CollectArrayBufferViewReferences(
            document.PropertyJson,
            GltfExtensionNames.KhrTechniquesWebgl,
            "shaders",
            document.BufferViews.Count,
            references);

        int accessorIndex = 0;
        while (accessorIndex < document.Accessors.Count)
        {
            GltfAccessor accessor = document.Accessors[accessorIndex];
            CollectDirectBufferViewReference(
                accessor.PropertyJson,
                GltfExtensionNames.MpegAccessorTimed,
                document.BufferViews.Count,
                accessor.HasBufferView
                    ? accessor.BufferView
                    : GltfConstants.AbsentIndex,
                references);
            accessorIndex++;
        }

        int nodeIndex = 0;
        while (nodeIndex < document.Nodes.Count)
        {
            GltfNode node = document.Nodes[nodeIndex];
            if (node == null)
            {
                throw new ArgumentException(
                    $"Node {nodeIndex} is null.",
                    "document");
            }

            CollectDirectBufferViewReference(
                node.PropertyJson,
                GltfExtensionNames.GriffelBimData,
                document.BufferViews.Count,
                GltfConstants.AbsentIndex,
                references);
            nodeIndex++;
        }

        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            GltfMesh mesh = document.Meshes[meshIndex];
            if (mesh == null || mesh.Primitives == null)
            {
                throw new ArgumentException(
                    $"Mesh {meshIndex} is null or has null primitives.",
                    "document");
            }

            int primitiveIndex = 0;
            while (primitiveIndex < mesh.Primitives.Count)
            {
                GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
                if (primitive == null)
                {
                    throw new ArgumentException(
                        $"Mesh {meshIndex} primitive {primitiveIndex} is null.",
                        "document");
                }

                JsonObject? payload = GetExtensionObject(
                    primitive.PropertyJson,
                    GltfExtensionNames.KhrDracoMeshCompression);
                if (payload != null)
                {
                    ExtensionBufferViewReference reference = new ExtensionBufferViewReference();
                    reference.Payload = payload;
                    reference.BufferView = ReadRequiredInteger(
                        payload,
                        "bufferView",
                        GltfExtensionNames.KhrDracoMeshCompression);
                    ValidateBufferViewReference(
                        reference.BufferView,
                        document.BufferViews.Count,
                        "KHR_draco_mesh_compression.bufferView");
                    references.Add(reference);
                }

                primitiveIndex++;
            }

            meshIndex++;
        }
    }

    internal void CollectDirectBufferViewReference(
        GltfPropertyJson propertyJson,
        string extensionName,
        int bufferViewCount,
        int relatedBufferView,
        List<ExtensionBufferViewReference> references)
    {
        JsonObject? payload = GetExtensionObject(propertyJson, extensionName);
        if (payload == null)
        {
            return;
        }

        CollectObjectBufferViewReference(
            payload,
            extensionName,
            bufferViewCount,
            relatedBufferView,
            references);
    }

    internal void CollectArrayBufferViewReferences(
        GltfPropertyJson propertyJson,
        string extensionName,
        string arrayMember,
        int bufferViewCount,
        List<ExtensionBufferViewReference> references)
    {
        JsonObject? payload = GetExtensionObject(propertyJson, extensionName);
        if (payload == null || !payload.ContainsKey(arrayMember))
        {
            return;
        }

        JsonNode? arrayNode = payload[arrayMember];
        if (arrayNode == null)
        {
            throw new ArgumentException(
                $"Extension {extensionName} has null array member {arrayMember}.",
                "document");
        }

        JsonArray values = (JsonArray)arrayNode;
        int index = 0;
        while (index < values.Count)
        {
            JsonNode? value = values[index];
            if (value == null)
            {
                throw new ArgumentException(
                    $"Extension {extensionName} has a null {arrayMember} entry.",
                    "document");
            }

            CollectObjectBufferViewReference(
                (JsonObject)value,
                extensionName,
                bufferViewCount,
                GltfConstants.AbsentIndex,
                references);
            index++;
        }
    }

    internal void CollectObjectBufferViewReference(
        JsonObject payload,
        string extensionName,
        int bufferViewCount,
        int relatedBufferView,
        List<ExtensionBufferViewReference> references)
    {
        if (!payload.ContainsKey("bufferView"))
        {
            return;
        }

        int bufferView = ReadRequiredInteger(
            payload,
            "bufferView",
            extensionName);
        ValidateBufferViewReference(
            bufferView,
            bufferViewCount,
            $"{extensionName}.bufferView");
        ExtensionBufferViewReference reference = new ExtensionBufferViewReference();
        reference.Payload = payload;
        reference.BufferView = bufferView;
        reference.RelatedBufferView = relatedBufferView;
        references.Add(reference);
    }

    internal void ValidateExtensionBufferRelationships(
        List<ExtensionBufferViewReference> references,
        BufferPackResult packed)
    {
        int index = 0;
        while (index < references.Count)
        {
            ExtensionBufferViewReference reference = references[index];
            if (reference.RelatedBufferView != GltfConstants.AbsentIndex)
            {
                BufferPackPlacement referenced = packed.Placements[
                    reference.BufferView];
                BufferPackPlacement related = packed.Placements[
                    reference.RelatedBufferView];
                if (referenced.Buffer != related.Buffer)
                {
                    throw new ArgumentException(
                        "Partitioning would separate extension data that must share its accessor buffer.",
                        "options");
                }
            }

            index++;
        }
    }

    internal void ValidateCoreBufferViewReferences(GltfDocument document)
    {
        int accessorIndex = 0;
        while (accessorIndex < document.Accessors.Count)
        {
            GltfAccessor accessor = document.Accessors[accessorIndex];
            if (accessor == null)
            {
                throw new ArgumentException(
                    $"Accessor {accessorIndex} is null.",
                    "document");
            }

            if (accessor.HasBufferView)
            {
                ValidateBufferViewReference(
                    accessor.BufferView,
                    document.BufferViews.Count,
                    $"accessor {accessorIndex}.bufferView");
            }

            if (accessor.Sparse != null)
            {
                if (accessor.Sparse.Indices == null
                    || accessor.Sparse.Values == null)
                {
                    throw new ArgumentException(
                        $"Accessor {accessorIndex} has incomplete sparse storage.",
                        "document");
                }

                ValidateBufferViewReference(
                    accessor.Sparse.Indices.BufferView,
                    document.BufferViews.Count,
                    $"accessor {accessorIndex}.sparse.indices.bufferView");
                ValidateBufferViewReference(
                    accessor.Sparse.Values.BufferView,
                    document.BufferViews.Count,
                    $"accessor {accessorIndex}.sparse.values.bufferView");
            }

            accessorIndex++;
        }

        int imageIndex = 0;
        while (imageIndex < document.Images.Count)
        {
            GltfImage image = document.Images[imageIndex];
            if (image == null)
            {
                throw new ArgumentException(
                    $"Image {imageIndex} is null.",
                    "document");
            }

            if (image.HasBufferView)
            {
                ValidateBufferViewReference(
                    image.BufferView,
                    document.BufferViews.Count,
                    $"image {imageIndex}.bufferView");
            }

            imageIndex++;
        }
    }

    internal byte[] CopyBufferRange(
        GltfDocument document,
        int bufferIndex,
        int byteOffset,
        int byteLength,
        string subject)
    {
        if (bufferIndex < 0 || bufferIndex >= document.Buffers.Count)
        {
            throw new ArgumentException(
                $"The {subject} buffer reference is outside the document.",
                "document");
        }

        GltfBuffer buffer = document.Buffers[bufferIndex];
        if (buffer == null
            || !buffer.Resolved
            || buffer.ResolvedBytes == null)
        {
            throw new ArgumentException(
                $"The {subject} buffer is not resolved.",
                "document");
        }

        long byteEnd = (long)byteOffset + byteLength;
        if (byteOffset < 0
            || byteLength < 1
            || buffer.ByteLength < 0
            || byteEnd > buffer.ByteLength
            || byteEnd > buffer.ResolvedBytes.Length)
        {
            throw new ArgumentException(
                $"The {subject} byte range is outside its resolved buffer.",
                "document");
        }

        byte[] result = new byte[byteLength];
        Array.Copy(buffer.ResolvedBytes, byteOffset, result, 0, byteLength);
        return result;
    }

    internal JsonObject? GetExtensionObject(
        GltfPropertyJson propertyJson,
        string extensionName)
    {
        if (propertyJson == null || propertyJson.Extensions == null)
        {
            throw new ArgumentException(
                "An extension property collection is null.",
                "document");
        }

        if (!propertyJson.Extensions.ContainsKey(extensionName))
        {
            return null;
        }

        JsonNode? node = propertyJson.Extensions[extensionName];
        if (node == null)
        {
            throw new ArgumentException(
                $"Extension {extensionName} has a null payload.",
                "document");
        }

        return (JsonObject)node;
    }

    internal int ReadRequiredInteger(
        JsonObject payload,
        string memberName,
        string extensionName)
    {
        if (!payload.ContainsKey(memberName) || payload[memberName] == null)
        {
            throw new ArgumentException(
                $"Extension {extensionName} is missing integer member {memberName}.",
                "document");
        }

        JsonNode? node = payload[memberName];
        if (node == null)
        {
            throw new ArgumentException(
                $"Extension {extensionName} is missing integer member {memberName}.",
                "document");
        }

        return node.GetValue<int>();
    }

    internal int ReadOptionalInteger(
        JsonObject payload,
        string memberName,
        string extensionName)
    {
        if (!payload.ContainsKey(memberName))
        {
            return 0;
        }

        if (payload[memberName] == null)
        {
            throw new ArgumentException(
                $"Extension {extensionName} has null integer member {memberName}.",
                "document");
        }

        JsonNode? node = payload[memberName];
        if (node == null)
        {
            throw new ArgumentException(
                $"Extension {extensionName} has null integer member {memberName}.",
                "document");
        }

        return node.GetValue<int>();
    }

    internal void ValidateBufferViewReference(
        int bufferView,
        int bufferViewCount,
        string subject)
    {
        if (bufferView < 0 || bufferView >= bufferViewCount)
        {
            throw new ArgumentException(
                $"The {subject} reference is outside the document.",
                "document");
        }
    }

    internal List<int> CreateBufferViewMap(
        int originalBufferViewCount,
        BufferPackResult packed)
    {
        List<int> result = new List<int>();
        int[] packViewOwners = new int[packed.BufferViews.Count];
        int packViewIndex = 0;
        while (packViewIndex < packViewOwners.Length)
        {
            packViewOwners[packViewIndex] = GltfConstants.AbsentIndex;
            packViewIndex++;
        }

        int oldIndex = 0;
        while (oldIndex < originalBufferViewCount)
        {
            int ownerView = packed.Placements[oldIndex].BufferView;
            packViewOwners[ownerView] = oldIndex;
            result.Add(GltfConstants.AbsentIndex);
            oldIndex++;
        }

        int newIndex = 0;
        packViewIndex = 0;
        while (packViewIndex < packViewOwners.Length)
        {
            oldIndex = packViewOwners[packViewIndex];
            if (oldIndex != GltfConstants.AbsentIndex)
            {
                result[oldIndex] = newIndex;
                newIndex++;
            }

            packViewIndex++;
        }

        return result;
    }

    internal List<GltfBuffer> CreateBuffers(BufferPackResult packed)
    {
        List<GltfBuffer> result = new List<GltfBuffer>();
        int index = 0;
        while (index < packed.Buffers.Count)
        {
            byte[] packedBytes = packed.Buffers[index].Bytes;
            GltfBuffer buffer = new GltfBuffer();
            buffer.ByteLength = packedBytes.Length;
            buffer.Resolved = true;
            buffer.ResolvedBytes = packedBytes;
            result.Add(buffer);
            index++;
        }

        return result;
    }

    internal List<GltfBufferView> CreateBufferViews(
        List<GltfBufferView> originalViews,
        int originalBufferViewCount,
        BufferPackResult packed,
        List<int> bufferViewMap)
    {
        List<GltfBufferView> result = new List<GltfBufferView>();
        int newIndex = 0;
        while (newIndex < originalBufferViewCount)
        {
            result.Add(new GltfBufferView());
            newIndex++;
        }

        int oldIndex = 0;
        while (oldIndex < originalBufferViewCount)
        {
            BufferPackPlacement placement = packed.Placements[oldIndex];
            BufferPackView packedView = packed.BufferViews[placement.BufferView];
            GltfBufferView original = originalViews[oldIndex];
            original.Buffer = packedView.Buffer;
            original.HasByteOffset = packedView.ByteOffset != 0;
            original.ByteOffset = packedView.ByteOffset;
            original.ByteLength = packedView.ByteLength;
            original.HasByteStride = packedView.HasByteStride;
            original.ByteStride = packedView.ByteStride;
            original.HasTarget = packedView.Target != GltfBufferViewTarget.None;
            original.Target = packedView.Target;
            result[bufferViewMap[oldIndex]] = original;
            oldIndex++;
        }

        return result;
    }

    internal void ApplyCoreBufferViewRemapping(
        GltfDocument document,
        List<int> bufferViewMap)
    {
        int accessorIndex = 0;
        while (accessorIndex < document.Accessors.Count)
        {
            GltfAccessor accessor = document.Accessors[accessorIndex];
            if (accessor.HasBufferView)
            {
                accessor.BufferView = bufferViewMap[accessor.BufferView];
            }

            if (accessor.Sparse != null)
            {
                accessor.Sparse.Indices.BufferView = bufferViewMap[
                    accessor.Sparse.Indices.BufferView];
                accessor.Sparse.Values.BufferView = bufferViewMap[
                    accessor.Sparse.Values.BufferView];
            }

            accessorIndex++;
        }

        int imageIndex = 0;
        while (imageIndex < document.Images.Count)
        {
            GltfImage image = document.Images[imageIndex];
            if (image.HasBufferView)
            {
                image.BufferView = bufferViewMap[image.BufferView];
            }

            imageIndex++;
        }
    }

    internal void ApplyExtensionBufferViewRemapping(
        List<ExtensionBufferViewReference> references,
        List<int> bufferViewMap)
    {
        int index = 0;
        while (index < references.Count)
        {
            ExtensionBufferViewReference reference = references[index];
            reference.Payload["bufferView"] = bufferViewMap[reference.BufferView];
            index++;
        }
    }

    internal void ApplyMeshoptRemapping(
        List<MeshoptBufferReference> references,
        BufferPackResult packed)
    {
        int index = 0;
        while (index < references.Count)
        {
            MeshoptBufferReference reference = references[index];
            BufferPackPlacement placement = packed.Placements[reference.PackItem];
            BufferPackView packedView = packed.BufferViews[placement.BufferView];
            reference.Payload["buffer"] = packedView.Buffer;
            reference.Payload["byteOffset"] = packedView.ByteOffset;
            index++;
        }
    }
}
