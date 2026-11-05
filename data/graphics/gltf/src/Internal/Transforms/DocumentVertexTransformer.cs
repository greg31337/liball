using System;
using System.Collections.Generic;

namespace gltf;

internal class DocumentVertexTransformer
{
    internal AccessorCodecEngine Accessors = new AccessorCodecEngine();
    internal AccessorEncoder Encoder = new AccessorEncoder();
    internal DocumentCloner Cloner = new DocumentCloner();
    internal DocumentCompactor Mapping = new DocumentCompactor();
    internal BufferViewTargetCleaner Targets = new BufferViewTargetCleaner();
    internal List<int> SourceBufferViews = new List<int>();

    internal TransformStepExecution TransformVertices(
        GltfDocument document,
        GltfTransformStep step,
        bool weld)
    {
        TransformStepExecution execution = new TransformStepExecution();
        execution.Document = document;
        execution.Result.Step = step;
        execution.Result.Remap = Mapping.CreateIdentityRemap(document);
        SourceBufferViews.Clear();
        bool changed = false;
        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            GltfMesh mesh = document.Meshes[meshIndex];
            int primitiveIndex = 0;
            while (primitiveIndex < mesh.Primitives.Count)
            {
                GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
                bool primitiveChanged;
                if (weld)
                {
                    primitiveChanged = WeldPrimitive(document, primitive);
                }
                else
                {
                    primitiveChanged = UnweldPrimitive(document, primitive);
                }

                if (primitiveChanged)
                {
                    execution.Result.ChangedObjects.Add(
                        $"meshes/{meshIndex}/primitives/{primitiveIndex}");
                    changed = true;
                }

                primitiveIndex++;
            }

            meshIndex++;
        }

        if (changed && document.Container != null)
        {
            document.Container = null;
            execution.Result.ChangedObjects.Add("container");
        }

        if (changed)
        {
            Targets.ClearUnusedTargets(
                document,
                execution.Result,
                SourceBufferViews);
        }

        return execution;
    }

    internal bool WeldPrimitive(GltfDocument document, GltfMeshPrimitive primitive)
    {
        PrimitiveVertexData? data = ReadPrimitiveData(document, primitive);
        if (data == null)
        {
            return false;
        }

        Dictionary<string, int> unique = new Dictionary<string, int>();
        List<int> uniqueVertices = new List<int>();
        int[] vertexRemap = new int[data.VertexCount];
        int vertexIndex = 0;
        while (vertexIndex < data.VertexCount)
        {
            string key = CreateVertexKey(data.Streams, vertexIndex);
            if (unique.ContainsKey(key))
            {
                vertexRemap[vertexIndex] = unique[key];
            }
            else
            {
                int newIndex = uniqueVertices.Count;
                unique[key] = newIndex;
                vertexRemap[vertexIndex] = newIndex;
                uniqueVertices.Add(vertexIndex);
            }

            vertexIndex++;
        }

        if (uniqueVertices.Count == data.VertexCount)
        {
            return false;
        }

        uint[] indices = new uint[data.Indices.Length];
        int index = 0;
        while (index < data.Indices.Length)
        {
            indices[index] = (uint)vertexRemap[(int)data.Indices[index]];
            index++;
        }

        RecordSourceBufferViews(document, data);
        WriteVertexStreams(document, primitive, data.Streams, uniqueVertices);
        GltfComponentType componentType = data.HasIndices
            ? data.IndexComponentType
            : SelectIndexComponentType(uniqueVertices.Count);
        int indexAccessor = WriteIndexAccessor(
            document,
            indices,
            componentType,
            data.HasIndices ? data.IndexAccessor : GltfConstants.AbsentIndex);
        primitive.HasIndices = true;
        primitive.Indices = indexAccessor;
        return true;
    }

    internal bool UnweldPrimitive(GltfDocument document, GltfMeshPrimitive primitive)
    {
        if (!primitive.HasIndices)
        {
            return false;
        }

        PrimitiveVertexData? data = ReadPrimitiveData(document, primitive);
        if (data == null)
        {
            return false;
        }

        List<int> vertices = new List<int>();
        int index = 0;
        while (index < data.Indices.Length)
        {
            vertices.Add((int)data.Indices[index]);
            index++;
        }

        RecordSourceBufferViews(document, data);
        WriteVertexStreams(document, primitive, data.Streams, vertices);
        primitive.HasIndices = false;
        primitive.Indices = GltfConstants.AbsentIndex;
        return true;
    }

    internal PrimitiveVertexData? ReadPrimitiveData(
        GltfDocument document,
        GltfMeshPrimitive primitive)
    {
        if (!IsCompatiblePrimitive(primitive) || primitive.Attributes.Count == 0)
        {
            return null;
        }

        PrimitiveVertexData result = new PrimitiveVertexData();
        int attributeIndex = 0;
        while (attributeIndex < primitive.Attributes.Count)
        {
            if (!AddVertexStream(
                document,
                result,
                0,
                0,
                attributeIndex,
                primitive.Attributes[attributeIndex].Accessor))
            {
                return null;
            }

            attributeIndex++;
        }

        int targetIndex = 0;
        while (targetIndex < primitive.Targets.Count)
        {
            GltfMorphTarget target = primitive.Targets[targetIndex];
            attributeIndex = 0;
            while (attributeIndex < target.Attributes.Count)
            {
                if (!AddVertexStream(
                    document,
                    result,
                    1,
                    targetIndex,
                    attributeIndex,
                    target.Attributes[attributeIndex].Accessor))
                {
                    return null;
                }

                attributeIndex++;
            }

            targetIndex++;
        }

        if (result.VertexCount <= 0)
        {
            return null;
        }

        if (primitive.HasIndices)
        {
            if (primitive.Indices < 0 || primitive.Indices >= document.Accessors.Count)
            {
                return null;
            }

            GltfAccessor indexAccessor = document.Accessors[primitive.Indices];
            if (!IsAccessorStorageCompatible(document, indexAccessor))
            {
                return null;
            }

            GltfAccessorReadRequest request = new GltfAccessorReadRequest();
            request.Document = document;
            request.Accessor = primitive.Indices;
            GltfAccessorData indices = Accessors.ReadIndexData(request);
            if (indices.Status != GltfOperationStatus.Success
                || indices.Indices.Length == 0
                || !IndicesResolve(indices.Indices, result.VertexCount))
            {
                return null;
            }

            result.HasIndices = true;
            result.IndexAccessor = primitive.Indices;
            result.IndexComponentType = indexAccessor.ComponentType;
            result.Indices = indices.Indices;
        }
        else
        {
            result.Indices = CreateSequentialIndices(result.VertexCount);
        }

        return result;
    }

    internal bool AddVertexStream(
        GltfDocument document,
        PrimitiveVertexData data,
        int kind,
        int outerIndex,
        int innerIndex,
        int accessorIndex)
    {
        if (accessorIndex < 0 || accessorIndex >= document.Accessors.Count)
        {
            return false;
        }

        GltfAccessor accessor = document.Accessors[accessorIndex];
        if (!IsAccessorStorageCompatible(document, accessor))
        {
            return false;
        }

        GltfAccessorReadRequest request = new GltfAccessorReadRequest();
        request.Document = document;
        request.Accessor = accessorIndex;
        GltfAccessorData decoded = Accessors.ReadRawData(request);
        if (decoded.Status != GltfOperationStatus.Success
            || accessor.Count <= 0
            || decoded.RawBytes.Length == 0
            || decoded.RawBytes.Length % accessor.Count != 0)
        {
            return false;
        }

        if (data.Streams.Count == 0)
        {
            data.VertexCount = accessor.Count;
        }
        else if (accessor.Count != data.VertexCount)
        {
            return false;
        }

        PrimitiveVertexStream stream = new PrimitiveVertexStream();
        stream.Kind = kind;
        stream.OuterIndex = outerIndex;
        stream.InnerIndex = innerIndex;
        stream.SourceAccessor = accessorIndex;
        stream.Accessor = accessor;
        stream.RawBytes = decoded.RawBytes;
        stream.ElementByteLength = decoded.RawBytes.Length / accessor.Count;
        data.Streams.Add(stream);
        return true;
    }

    internal bool IsAccessorStorageCompatible(
        GltfDocument document,
        GltfAccessor accessor)
    {
        if (accessor.PropertyJson.Extensions.Count > 0
            || accessor.PropertyJson.TypedExtensions.Count > 0)
        {
            return false;
        }

        if (!accessor.HasBufferView)
        {
            return true;
        }

        if (accessor.BufferView < 0 || accessor.BufferView >= document.BufferViews.Count)
        {
            return false;
        }

        GltfBufferView view = document.BufferViews[accessor.BufferView];
        if (view.PropertyJson.Extensions.Count > 0
            || view.PropertyJson.TypedExtensions.Count > 0
            || view.Buffer < 0
            || view.Buffer >= document.Buffers.Count)
        {
            return false;
        }

        GltfBuffer buffer = document.Buffers[view.Buffer];
        return buffer.PropertyJson.Extensions.Count == 0
            && buffer.PropertyJson.TypedExtensions.Count == 0;
    }

    internal void RecordSourceBufferViews(
        GltfDocument document,
        PrimitiveVertexData data)
    {
        int streamIndex = 0;
        while (streamIndex < data.Streams.Count)
        {
            RecordAccessorBufferView(
                document,
                data.Streams[streamIndex].SourceAccessor);
            streamIndex++;
        }

        if (data.HasIndices)
        {
            RecordAccessorBufferView(document, data.IndexAccessor);
        }
    }

    internal void RecordAccessorBufferView(GltfDocument document, int accessorIndex)
    {
        if (accessorIndex < 0 || accessorIndex >= document.Accessors.Count)
        {
            return;
        }

        GltfAccessor accessor = document.Accessors[accessorIndex];
        if (accessor.HasBufferView
            && accessor.BufferView >= 0
            && accessor.BufferView < document.BufferViews.Count
            && !SourceBufferViews.Contains(accessor.BufferView))
        {
            SourceBufferViews.Add(accessor.BufferView);
        }
    }

    internal bool IsCompatiblePrimitive(GltfMeshPrimitive primitive)
    {
        foreach (KeyValuePair<string, System.Text.Json.Nodes.JsonNode?> extension
            in primitive.PropertyJson.Extensions)
        {
            if (extension.Key != GltfExtensionNames.KhrMaterialsVariants)
            {
                return false;
            }
        }

        foreach (KeyValuePair<string, GltfTypedExtensionData> extension
            in primitive.PropertyJson.TypedExtensions)
        {
            if (extension.Key != GltfExtensionNames.KhrMaterialsVariants)
            {
                return false;
            }
        }

        return true;
    }

    internal bool IndicesResolve(uint[] indices, int vertexCount)
    {
        int index = 0;
        while (index < indices.Length)
        {
            if (indices[index] >= (uint)vertexCount)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal uint[] CreateSequentialIndices(int count)
    {
        uint[] result = new uint[count];
        int index = 0;
        while (index < count)
        {
            result[index] = (uint)index;
            index++;
        }

        return result;
    }

    internal string CreateVertexKey(
        List<PrimitiveVertexStream> streams,
        int vertexIndex)
    {
        int byteLength = 0;
        int streamIndex = 0;
        while (streamIndex < streams.Count)
        {
            byteLength += streams[streamIndex].ElementByteLength;
            streamIndex++;
        }

        byte[] value = new byte[byteLength];
        int targetOffset = 0;
        streamIndex = 0;
        while (streamIndex < streams.Count)
        {
            PrimitiveVertexStream stream = streams[streamIndex];
            int sourceOffset = vertexIndex * stream.ElementByteLength;
            int byteIndex = 0;
            while (byteIndex < stream.ElementByteLength)
            {
                value[targetOffset + byteIndex] = stream.RawBytes[sourceOffset + byteIndex];
                byteIndex++;
            }

            targetOffset += stream.ElementByteLength;
            streamIndex++;
        }

        return Convert.ToBase64String(value);
    }

    internal void WriteVertexStreams(
        GltfDocument document,
        GltfMeshPrimitive primitive,
        List<PrimitiveVertexStream> streams,
        List<int> vertices)
    {
        int streamIndex = 0;
        while (streamIndex < streams.Count)
        {
            PrimitiveVertexStream stream = streams[streamIndex];
            byte[] rawBytes = SelectRawElements(stream, vertices);
            AccessorEncodingResult encoding = Encoder.EncodeRawAccessor(
                rawBytes,
                stream.Accessor.ComponentType,
                stream.Accessor.Type,
                stream.Accessor.Normalized,
                false);
            AccessorCreationResult created = Encoder.CreateAccessorStorage(
                document,
                encoding,
                true);
            CopyAccessorMetadata(stream.Accessor, document.Accessors[created.Accessor]);
            SetPrimitiveAccessor(primitive, stream, created.Accessor);
            streamIndex++;
        }
    }

    internal byte[] SelectRawElements(
        PrimitiveVertexStream stream,
        List<int> vertices)
    {
        byte[] result = new byte[vertices.Count * stream.ElementByteLength];
        int outputIndex = 0;
        while (outputIndex < vertices.Count)
        {
            int sourceOffset = vertices[outputIndex] * stream.ElementByteLength;
            int targetOffset = outputIndex * stream.ElementByteLength;
            int byteIndex = 0;
            while (byteIndex < stream.ElementByteLength)
            {
                result[targetOffset + byteIndex] = stream.RawBytes[sourceOffset + byteIndex];
                byteIndex++;
            }

            outputIndex++;
        }

        return result;
    }

    internal void SetPrimitiveAccessor(
        GltfMeshPrimitive primitive,
        PrimitiveVertexStream stream,
        int accessor)
    {
        if (stream.Kind == 0)
        {
            primitive.Attributes[stream.InnerIndex].Accessor = accessor;
        }
        else
        {
            primitive.Targets[stream.OuterIndex]
                .Attributes[stream.InnerIndex].Accessor = accessor;
        }
    }

    internal int WriteIndexAccessor(
        GltfDocument document,
        uint[] indices,
        GltfComponentType componentType,
        int sourceAccessor)
    {
        AccessorEncodingResult encoding = Encoder.EncodeIndexAccessor(
            indices,
            componentType);
        AccessorCreationResult created = Encoder.CreateAccessorStorage(
            document,
            encoding,
            true);
        if (sourceAccessor >= 0 && sourceAccessor < document.Accessors.Count)
        {
            CopyAccessorMetadata(
                document.Accessors[sourceAccessor],
                document.Accessors[created.Accessor]);
        }

        return created.Accessor;
    }

    internal GltfComponentType SelectIndexComponentType(int vertexCount)
    {
        if (vertexCount <= 255)
        {
            return GltfComponentType.UnsignedByte;
        }

        if (vertexCount <= 65535)
        {
            return GltfComponentType.UnsignedShort;
        }

        return GltfComponentType.UnsignedInt;
    }

    internal void CopyAccessorMetadata(GltfAccessor source, GltfAccessor target)
    {
        target.Name = source.Name;
        Cloner.Clones.Clear();
        Cloner.CloneJson = true;
        Cloner.CloneResourceBytes = true;
        target.PropertyJson = (GltfPropertyJson)(Cloner.CloneValue(source.PropertyJson)
            ?? new GltfPropertyJson());
    }
}
