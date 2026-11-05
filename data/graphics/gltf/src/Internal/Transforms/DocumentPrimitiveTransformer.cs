using System;
using System.Collections.Generic;

namespace gltf;

internal class DocumentPrimitiveTransformer
{
    internal DocumentVertexTransformer Vertices = new DocumentVertexTransformer();
    internal DocumentGpuInstancer GpuInstancer = new DocumentGpuInstancer();
    internal PrimitiveTransformSettingsReader Settings =
        new PrimitiveTransformSettingsReader();
    internal DeduplicationValueKeyWriter Keys = new DeduplicationValueKeyWriter();
    internal DocumentCloner Cloner = new DocumentCloner();
    internal DocumentCompactor Mapping = new DocumentCompactor();
    internal BufferViewTargetCleaner Targets = new BufferViewTargetCleaner();
    internal List<int> SourceBufferViews = new List<int>();

    internal TransformStepExecution TransformPrimitiveDocument(
        GltfDocument document,
        GltfTransformStep step)
    {
        TransformStepExecution execution = new TransformStepExecution();
        execution.Document = document;
        execution.Result.Step = step;
        execution.Result.Remap = Mapping.CreateIdentityRemap(document);
        SourceBufferViews.Clear();
        bool changed = false;
        if (step.Kind == GltfTransformKind.JoinPrimitives)
        {
            changed = JoinDocumentPrimitives(document, execution.Result);
        }
        else if (step.Kind == GltfTransformKind.SplitPrimitives)
        {
            PrimitiveTransformSettings settings = Settings.ReadPrimitiveSettings(step);
            changed = SplitDocumentPrimitives(
                document,
                settings.MaxVertices,
                execution.Result);
        }
        else if (step.Kind == GltfTransformKind.CreateGpuInstances)
        {
            changed = GpuInstancer.CreateDocumentInstances(document, execution.Result);
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

    internal bool JoinDocumentPrimitives(
        GltfDocument document,
        GltfTransformStepResult result)
    {
        bool changed = false;
        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            if (MeshCanChangeGeometry(document, meshIndex))
            {
                GltfMesh mesh = document.Meshes[meshIndex];
                int primitiveIndex = 0;
                while (primitiveIndex < mesh.Primitives.Count)
                {
                    GltfMeshPrimitive first = mesh.Primitives[primitiveIndex];
                    PrimitiveVertexData? firstData = Vertices.ReadPrimitiveData(
                        document,
                        first);
                    if (firstData == null || !IsIndependentTopology(first.Mode))
                    {
                        primitiveIndex++;
                        continue;
                    }

                    List<PrimitiveVertexData> data = new List<PrimitiveVertexData>();
                    data.Add(firstData);
                    int runLength = 1;
                    while (primitiveIndex + runLength < mesh.Primitives.Count)
                    {
                        GltfMeshPrimitive candidate =
                            mesh.Primitives[primitiveIndex + runLength];
                        if (!PrimitivesHaveJoinLayout(document, first, candidate))
                        {
                            break;
                        }

                        PrimitiveVertexData? candidateData = Vertices.ReadPrimitiveData(
                            document,
                            candidate);
                        if (candidateData == null)
                        {
                            break;
                        }

                        data.Add(candidateData);
                        runLength++;
                    }

                    if (runLength > 1
                        && JoinPrimitiveRun(
                            document,
                            mesh,
                            primitiveIndex,
                            runLength,
                            data))
                    {
                        int changedIndex = 0;
                        while (changedIndex < runLength)
                        {
                            result.ChangedObjects.Add(
                                $"meshes/{meshIndex}/primitives/{primitiveIndex + changedIndex}");
                            changedIndex++;
                        }

                        changed = true;
                    }

                    primitiveIndex++;
                }
            }

            meshIndex++;
        }

        return changed;
    }

    internal bool SplitDocumentPrimitives(
        GltfDocument document,
        int maxVertices,
        GltfTransformStepResult result)
    {
        bool changed = false;
        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            if (MeshCanChangeGeometry(document, meshIndex))
            {
                GltfMesh mesh = document.Meshes[meshIndex];
                int primitiveIndex = 0;
                while (primitiveIndex < mesh.Primitives.Count)
                {
                    GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
                    PrimitiveVertexData? data = Vertices.ReadPrimitiveData(
                        document,
                        primitive);
                    List<PrimitiveSplitChunk>? chunks = CreateSplitChunks(
                        data,
                        primitive.Mode,
                        maxVertices);
                    if (data == null || chunks == null || chunks.Count <= 1)
                    {
                        primitiveIndex++;
                        continue;
                    }

                    RecordPrimitiveBufferViews(document, data);
                    List<GltfMeshPrimitive> replacements =
                        CreateSplitPrimitives(document, primitive, data, chunks);
                    mesh.Primitives.RemoveAt(primitiveIndex);
                    int chunkIndex = 0;
                    while (chunkIndex < replacements.Count)
                    {
                        mesh.Primitives.Insert(
                            primitiveIndex + chunkIndex,
                            replacements[chunkIndex]);
                        result.ChangedObjects.Add(
                            $"meshes/{meshIndex}/primitives/{primitiveIndex + chunkIndex}");
                        chunkIndex++;
                    }

                    changed = true;
                    primitiveIndex += replacements.Count;
                }
            }

            meshIndex++;
        }

        return changed;
    }

    internal bool MeshCanChangeGeometry(GltfDocument document, int meshIndex)
    {
        GltfMesh mesh = document.Meshes[meshIndex];
        if (mesh.Weights != null || HasExtensionPayload(mesh.PropertyJson))
        {
            return false;
        }

        int primitiveIndex = 0;
        while (primitiveIndex < mesh.Primitives.Count)
        {
            GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
            if (primitive.Targets.Count > 0 || !Vertices.IsCompatiblePrimitive(primitive))
            {
                return false;
            }

            primitiveIndex++;
        }

        int nodeIndex = 0;
        while (nodeIndex < document.Nodes.Count)
        {
            GltfNode node = document.Nodes[nodeIndex];
            if (node.HasMesh && node.Mesh == meshIndex)
            {
                if (node.HasSkin
                    || node.Weights != null
                    || HasExtensionPayload(node.PropertyJson)
                    || IsNodeAnimationTarget(document, nodeIndex)
                    || IsSkinNode(document, nodeIndex))
                {
                    return false;
                }
            }

            nodeIndex++;
        }

        return true;
    }

    internal bool IsNodeAnimationTarget(GltfDocument document, int nodeIndex)
    {
        int animationIndex = 0;
        while (animationIndex < document.Animations.Count)
        {
            GltfAnimation animation = document.Animations[animationIndex];
            int channelIndex = 0;
            while (channelIndex < animation.Channels.Count)
            {
                GltfAnimationTarget target = animation.Channels[channelIndex].Target;
                if (target.HasNode && target.Node == nodeIndex)
                {
                    return true;
                }

                string? pointer = new AnimationPointerResolver().GetTargetPointer(target);
                if (pointer != null)
                {
                    AnimationPointerTarget resolved =
                        new AnimationPointerResolver().ResolvePointer(document, pointer);
                    if (resolved.TargetCollection == "nodes"
                        && resolved.TargetIndex == nodeIndex)
                    {
                        return true;
                    }
                }

                channelIndex++;
            }

            animationIndex++;
        }

        return false;
    }

    internal bool IsSkinNode(GltfDocument document, int nodeIndex)
    {
        int skinIndex = 0;
        while (skinIndex < document.Skins.Count)
        {
            GltfSkin skin = document.Skins[skinIndex];
            if ((skin.HasSkeleton && skin.Skeleton == nodeIndex)
                || skin.Joints.Contains(nodeIndex))
            {
                return true;
            }

            skinIndex++;
        }

        return false;
    }

    internal bool HasExtensionPayload(GltfPropertyJson property)
    {
        return property.Extensions.Count > 0 || property.TypedExtensions.Count > 0;
    }

    internal bool IsIndependentTopology(GltfPrimitiveMode mode)
    {
        return mode == GltfPrimitiveMode.Points
            || mode == GltfPrimitiveMode.Lines
            || mode == GltfPrimitiveMode.Triangles;
    }

    internal bool PrimitivesHaveJoinLayout(
        GltfDocument document,
        GltfMeshPrimitive first,
        GltfMeshPrimitive second)
    {
        if (first.Mode != second.Mode
            || !IsIndependentTopology(second.Mode)
            || first.HasMaterial != second.HasMaterial
            || first.Material != second.Material
            || first.HasIndices != second.HasIndices
            || first.Attributes.Count != second.Attributes.Count
            || first.Targets.Count != second.Targets.Count
            || Keys.CreateExactKey(first.PropertyJson)
                != Keys.CreateExactKey(second.PropertyJson))
        {
            return false;
        }

        int attributeIndex = 0;
        while (attributeIndex < first.Attributes.Count)
        {
            GltfAttribute left = first.Attributes[attributeIndex];
            GltfAttribute right = second.Attributes[attributeIndex];
            if (left.Semantic != right.Semantic
                || !AccessorsHaveJoinLayout(document, left.Accessor, right.Accessor))
            {
                return false;
            }

            attributeIndex++;
        }

        int targetIndex = 0;
        while (targetIndex < first.Targets.Count)
        {
            GltfMorphTarget leftTarget = first.Targets[targetIndex];
            GltfMorphTarget rightTarget = second.Targets[targetIndex];
            if (leftTarget.Attributes.Count != rightTarget.Attributes.Count)
            {
                return false;
            }

            attributeIndex = 0;
            while (attributeIndex < leftTarget.Attributes.Count)
            {
                GltfAttribute left = leftTarget.Attributes[attributeIndex];
                GltfAttribute right = rightTarget.Attributes[attributeIndex];
                if (left.Semantic != right.Semantic
                    || !AccessorsHaveJoinLayout(
                        document,
                        left.Accessor,
                        right.Accessor))
                {
                    return false;
                }

                attributeIndex++;
            }

            targetIndex++;
        }

        if (first.HasIndices
            && !AccessorsHaveJoinLayout(document, first.Indices, second.Indices))
        {
            return false;
        }

        return true;
    }

    internal bool AccessorsHaveJoinLayout(
        GltfDocument document,
        int firstIndex,
        int secondIndex)
    {
        if (firstIndex < 0
            || firstIndex >= document.Accessors.Count
            || secondIndex < 0
            || secondIndex >= document.Accessors.Count)
        {
            return false;
        }

        GltfAccessor first = document.Accessors[firstIndex];
        GltfAccessor second = document.Accessors[secondIndex];
        return first.ComponentType == second.ComponentType
            && first.Type == second.Type
            && first.HasNormalized == second.HasNormalized
            && first.Normalized == second.Normalized
            && first.Name == second.Name
            && Keys.CreateExactKey(first.PropertyJson)
                == Keys.CreateExactKey(second.PropertyJson);
    }

    internal bool JoinPrimitiveRun(
        GltfDocument document,
        GltfMesh mesh,
        int primitiveIndex,
        int runLength,
        List<PrimitiveVertexData> data)
    {
        long totalVertexCount = 0;
        long totalIndexCount = 0;
        int dataIndex = 0;
        while (dataIndex < data.Count)
        {
            totalVertexCount += data[dataIndex].VertexCount;
            totalIndexCount += data[dataIndex].Indices.Length;
            dataIndex++;
        }

        if (totalVertexCount > int.MaxValue || totalIndexCount > int.MaxValue)
        {
            return false;
        }

        GltfMeshPrimitive target = mesh.Primitives[primitiveIndex];
        dataIndex = 0;
        while (dataIndex < data.Count)
        {
            RecordPrimitiveBufferViews(document, data[dataIndex]);
            dataIndex++;
        }

        int streamIndex = 0;
        while (streamIndex < data[0].Streams.Count)
        {
            byte[] combined = ConcatenateStreamData(data, streamIndex);
            PrimitiveVertexStream stream = data[0].Streams[streamIndex];
            AccessorEncodingResult encoding = Vertices.Encoder.EncodeRawAccessor(
                combined,
                stream.Accessor.ComponentType,
                stream.Accessor.Type,
                stream.Accessor.Normalized,
                false);
            AccessorCreationResult created = Vertices.Encoder.CreateAccessorStorage(
                document,
                encoding,
                true);
            Vertices.CopyAccessorMetadata(
                stream.Accessor,
                document.Accessors[created.Accessor]);
            Vertices.SetPrimitiveAccessor(target, stream, created.Accessor);
            streamIndex++;
        }

        if (target.HasIndices)
        {
            uint[] indices = new uint[(int)totalIndexCount];
            int outputIndex = 0;
            int vertexOffset = 0;
            dataIndex = 0;
            while (dataIndex < data.Count)
            {
                int index = 0;
                while (index < data[dataIndex].Indices.Length)
                {
                    indices[outputIndex] = data[dataIndex].Indices[index]
                        + (uint)vertexOffset;
                    outputIndex++;
                    index++;
                }

                vertexOffset += data[dataIndex].VertexCount;
                dataIndex++;
            }

            GltfComponentType componentType = SelectJoinedIndexType(
                data[0].IndexComponentType,
                (int)totalVertexCount);
            target.Indices = Vertices.WriteIndexAccessor(
                document,
                indices,
                componentType,
                data[0].IndexAccessor);
        }

        int removeIndex = 1;
        while (removeIndex < runLength)
        {
            mesh.Primitives.RemoveAt(primitiveIndex + 1);
            removeIndex++;
        }

        return true;
    }

    internal byte[] ConcatenateStreamData(
        List<PrimitiveVertexData> data,
        int streamIndex)
    {
        long byteLength = 0;
        int dataIndex = 0;
        while (dataIndex < data.Count)
        {
            byteLength += data[dataIndex].Streams[streamIndex].RawBytes.Length;
            dataIndex++;
        }

        if (byteLength > int.MaxValue)
        {
            throw new GltfUnsupportedFeatureException(
                "Joined primitive attribute storage exceeds the Int32 byte limit.");
        }

        byte[] result = new byte[(int)byteLength];
        int offset = 0;
        dataIndex = 0;
        while (dataIndex < data.Count)
        {
            byte[] source = data[dataIndex].Streams[streamIndex].RawBytes;
            source.CopyTo(result, offset);
            offset += source.Length;
            dataIndex++;
        }

        return result;
    }

    internal GltfComponentType SelectJoinedIndexType(
        GltfComponentType source,
        int vertexCount)
    {
        GltfComponentType required = Vertices.SelectIndexComponentType(vertexCount);
        if (source == GltfComponentType.UnsignedInt
            || (source == GltfComponentType.UnsignedShort
                && required == GltfComponentType.UnsignedByte))
        {
            return source;
        }

        return required;
    }

    internal List<PrimitiveSplitChunk>? CreateSplitChunks(
        PrimitiveVertexData? data,
        GltfPrimitiveMode mode,
        int maxVertices)
    {
        if (data == null || !IsIndependentTopology(mode))
        {
            return null;
        }

        int unitSize = 1;
        if (mode == GltfPrimitiveMode.Lines)
        {
            unitSize = 2;
        }
        else if (mode == GltfPrimitiveMode.Triangles)
        {
            unitSize = 3;
        }

        if (data.Indices.Length % unitSize != 0)
        {
            return null;
        }

        if (data.HasIndices)
        {
            return CreateIndexedSplitChunks(data.Indices, unitSize, maxVertices);
        }

        return CreateUnindexedSplitChunks(data.VertexCount, unitSize, maxVertices);
    }

    internal List<PrimitiveSplitChunk> CreateIndexedSplitChunks(
        uint[] indices,
        int unitSize,
        int maxVertices)
    {
        List<PrimitiveSplitChunk> result = new List<PrimitiveSplitChunk>();
        PrimitiveSplitChunk current = new PrimitiveSplitChunk();
        int index = 0;
        while (index < indices.Length)
        {
            int additional = CountUnitVertices(current, indices, index, unitSize);
            if (current.Indices.Count > 0
                && current.Vertices.Count + additional > maxVertices)
            {
                result.Add(current);
                current = new PrimitiveSplitChunk();
            }

            AddUnitToSplitChunk(current, indices, index, unitSize);
            index += unitSize;
        }

        if (current.Indices.Count > 0)
        {
            result.Add(current);
        }

        return result;
    }

    internal int CountUnitVertices(
        PrimitiveSplitChunk chunk,
        uint[] indices,
        int start,
        int count)
    {
        int additional = 0;
        int index = 0;
        while (index < count)
        {
            int vertex = (int)indices[start + index];
            if (!chunk.VertexRemap.ContainsKey(vertex))
            {
                bool repeatedInUnit = false;
                int prior = 0;
                while (prior < index)
                {
                    if (indices[start + prior] == indices[start + index])
                    {
                        repeatedInUnit = true;
                    }

                    prior++;
                }

                if (!repeatedInUnit)
                {
                    additional++;
                }
            }

            index++;
        }

        return additional;
    }

    internal void AddUnitToSplitChunk(
        PrimitiveSplitChunk chunk,
        uint[] indices,
        int start,
        int count)
    {
        int index = 0;
        while (index < count)
        {
            int vertex = (int)indices[start + index];
            if (!chunk.VertexRemap.ContainsKey(vertex))
            {
                chunk.VertexRemap[vertex] = chunk.Vertices.Count;
                chunk.Vertices.Add(vertex);
            }

            chunk.Indices.Add((uint)chunk.VertexRemap[vertex]);
            index++;
        }
    }

    internal List<PrimitiveSplitChunk> CreateUnindexedSplitChunks(
        int vertexCount,
        int unitSize,
        int maxVertices)
    {
        List<PrimitiveSplitChunk> result = new List<PrimitiveSplitChunk>();
        int capacity = maxVertices - maxVertices % unitSize;
        int vertex = 0;
        while (vertex < vertexCount)
        {
            PrimitiveSplitChunk chunk = new PrimitiveSplitChunk();
            int count = Math.Min(capacity, vertexCount - vertex);
            int index = 0;
            while (index < count)
            {
                chunk.Vertices.Add(vertex + index);
                index++;
            }

            result.Add(chunk);
            vertex += count;
        }

        return result;
    }

    internal List<GltfMeshPrimitive> CreateSplitPrimitives(
        GltfDocument document,
        GltfMeshPrimitive source,
        PrimitiveVertexData data,
        List<PrimitiveSplitChunk> chunks)
    {
        List<GltfMeshPrimitive> result = new List<GltfMeshPrimitive>();
        int chunkIndex = 0;
        while (chunkIndex < chunks.Count)
        {
            PrimitiveSplitChunk chunk = chunks[chunkIndex];
            GltfMeshPrimitive primitive = ClonePrimitiveValue(source);
            Vertices.WriteVertexStreams(document, primitive, data.Streams, chunk.Vertices);
            if (data.HasIndices)
            {
                primitive.Indices = Vertices.WriteIndexAccessor(
                    document,
                    chunk.Indices.ToArray(),
                    data.IndexComponentType,
                    data.IndexAccessor);
            }

            result.Add(primitive);
            chunkIndex++;
        }

        return result;
    }

    internal void RecordPrimitiveBufferViews(
        GltfDocument document,
        PrimitiveVertexData data)
    {
        int streamIndex = 0;
        while (streamIndex < data.Streams.Count)
        {
            RecordPrimitiveAccessorView(
                document,
                data.Streams[streamIndex].SourceAccessor);
            streamIndex++;
        }

        if (data.HasIndices)
        {
            RecordPrimitiveAccessorView(document, data.IndexAccessor);
        }
    }

    internal void RecordPrimitiveAccessorView(
        GltfDocument document,
        int accessorIndex)
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

    internal GltfMeshPrimitive ClonePrimitiveValue(GltfMeshPrimitive source)
    {
        Cloner.Clones.Clear();
        Cloner.CloneJson = true;
        Cloner.CloneResourceBytes = true;
        return (GltfMeshPrimitive)(Cloner.CloneValue(source)
            ?? new GltfMeshPrimitive());
    }
}
