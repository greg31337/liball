using System;
using System.Collections.Generic;

namespace gltf;

internal class DocumentInspector
{
    internal DependencyGraph Dependencies = new DependencyGraph();
    internal ExtensionUsageCollector Extensions = new ExtensionUsageCollector();
    internal AccessorCodecEngine AccessorCodec = new AccessorCodecEngine();
    internal TransformMath Transforms = new TransformMath();
    internal BoundsCalculator Bounds = new BoundsCalculator();

    internal GltfInspectionResult InspectDocumentData(
        GltfDocument document,
        GltfInspectionOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        GltfInspectionResult result = new GltfInspectionResult();
        PopulateCollectionCounts(document, result.Statistics);
        PopulateGeometryStatistics(document, result.Statistics);
        PopulateDrawCallStatistics(document, result.Statistics);
        PopulateAnimationStatistics(document, options, result);
        PopulateResourceStatistics(document, result.Statistics);
        result.Statistics.Extensions = Extensions.CollectDocumentExtensions(document);

        if (options.IncludeDependencies || options.IncludeReverseDependencies)
        {
            List<GltfDependency> forward = Dependencies.EnumerateDocumentDependencies(document);
            if (options.IncludeDependencies)
            {
                result.Dependencies = forward;
            }

            if (options.IncludeReverseDependencies)
            {
                result.ReverseDependencies = Dependencies.ReverseDocumentDependencies(forward);
            }
        }

        if (options.CalculateBounds)
        {
            PopulateDocumentBounds(document, result);
        }

        return result;
    }

    internal List<GltfDependency> EnumerateConfiguredDependencies(
        GltfDocument document,
        GltfInspectionOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (!options.IncludeDependencies)
        {
            return new List<GltfDependency>();
        }

        return Dependencies.EnumerateDocumentDependencies(document);
    }

    internal void PopulateCollectionCounts(
        GltfDocument document,
        GltfDocumentStatistics statistics)
    {
        statistics.AccessorCount = document.Accessors.Count;
        statistics.AnimationCount = document.Animations.Count;
        statistics.BufferCount = document.Buffers.Count;
        statistics.BufferViewCount = document.BufferViews.Count;
        statistics.CameraCount = document.Cameras.Count;
        statistics.ImageCount = document.Images.Count;
        statistics.MaterialCount = document.Materials.Count;
        statistics.MeshCount = document.Meshes.Count;
        statistics.NodeCount = document.Nodes.Count;
        statistics.SamplerCount = document.Samplers.Count;
        statistics.SceneCount = document.Scenes.Count;
        statistics.SkinCount = document.Skins.Count;
        statistics.TextureCount = document.Textures.Count;
    }

    internal void PopulateGeometryStatistics(
        GltfDocument document,
        GltfDocumentStatistics statistics)
    {
        int accessorIndex = 0;
        while (accessorIndex < document.Accessors.Count)
        {
            GltfAccessor accessor = document.Accessors[accessorIndex];
            if (accessor != null && accessor.Sparse != null)
            {
                statistics.SparseAccessorCount++;
            }

            accessorIndex++;
        }

        int viewIndex = 0;
        while (viewIndex < document.BufferViews.Count)
        {
            GltfBufferView view = document.BufferViews[viewIndex];
            if (view != null && view.HasByteStride)
            {
                statistics.InterleavedBufferViewCount++;
            }

            viewIndex++;
        }

        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            GltfMesh mesh = document.Meshes[meshIndex];
            if (mesh != null && mesh.Primitives != null)
            {
                int primitiveIndex = 0;
                while (primitiveIndex < mesh.Primitives.Count)
                {
                    GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
                    if (primitive != null)
                    {
                        long vertexCount = GetPrimitiveVertexCount(document, primitive);
                        long elementCount = GetPrimitiveElementCount(document, primitive, vertexCount);
                        statistics.PrimitiveCount++;
                        statistics.VertexCount += vertexCount;
                        statistics.TriangleCount += GetTriangleCount(primitive.Mode, elementCount);
                    }

                    primitiveIndex++;
                }
            }

            meshIndex++;
        }
    }

    internal long GetPrimitiveVertexCount(GltfDocument document, GltfMeshPrimitive primitive)
    {
        int accessorIndex = GltfConstants.AbsentIndex;
        int attributeIndex = 0;
        while (primitive.Attributes != null && attributeIndex < primitive.Attributes.Count)
        {
            GltfAttribute attribute = primitive.Attributes[attributeIndex];
            if (attribute != null && attribute.Semantic == "POSITION")
            {
                accessorIndex = attribute.Accessor;
                break;
            }

            if (attribute != null && accessorIndex == GltfConstants.AbsentIndex)
            {
                accessorIndex = attribute.Accessor;
            }

            attributeIndex++;
        }

        if (accessorIndex < 0 || accessorIndex >= document.Accessors.Count
            || document.Accessors[accessorIndex] == null)
        {
            return 0;
        }

        int count = document.Accessors[accessorIndex].Count;
        return count > 0 ? count : 0;
    }

    internal long GetPrimitiveElementCount(
        GltfDocument document,
        GltfMeshPrimitive primitive,
        long vertexCount)
    {
        if (!primitive.HasIndices)
        {
            return vertexCount;
        }

        if (primitive.Indices < 0 || primitive.Indices >= document.Accessors.Count
            || document.Accessors[primitive.Indices] == null)
        {
            return 0;
        }

        int count = document.Accessors[primitive.Indices].Count;
        return count > 0 ? count : 0;
    }

    internal long GetTriangleCount(GltfPrimitiveMode mode, long elementCount)
    {
        if (mode == GltfPrimitiveMode.Triangles)
        {
            return elementCount / 3;
        }

        if (mode == GltfPrimitiveMode.TriangleStrip
            || mode == GltfPrimitiveMode.TriangleFan)
        {
            return elementCount > 2 ? elementCount - 2 : 0;
        }

        return 0;
    }

    internal void PopulateDrawCallStatistics(
        GltfDocument document,
        GltfDocumentStatistics statistics)
    {
        int nodeIndex = 0;
        while (nodeIndex < document.Nodes.Count)
        {
            GltfNode node = document.Nodes[nodeIndex];
            if (node != null && node.HasMesh && node.Mesh >= 0
                && node.Mesh < document.Meshes.Count
                && document.Meshes[node.Mesh] != null
                && document.Meshes[node.Mesh].Primitives != null)
            {
                statistics.DrawCallCount += document.Meshes[node.Mesh].Primitives.Count;
            }

            nodeIndex++;
        }
    }

    internal void PopulateAnimationStatistics(
        GltfDocument document,
        GltfInspectionOptions options,
        GltfInspectionResult result)
    {
        int animationIndex = 0;
        while (animationIndex < document.Animations.Count)
        {
            GltfAnimation animation = document.Animations[animationIndex];
            int samplerIndex = 0;
            while (animation != null && animation.Samplers != null
                && samplerIndex < animation.Samplers.Count)
            {
                GltfAnimationSampler sampler = animation.Samplers[samplerIndex];
                if (sampler != null && sampler.Input >= 0
                    && sampler.Input < document.Accessors.Count
                    && document.Accessors[sampler.Input] != null)
                {
                    GltfAccessor input = document.Accessors[sampler.Input];
                    if (input.Count > 0)
                    {
                        result.Statistics.AnimationKeyframeCount += input.Count;
                    }

                    double duration = GetAnimationSamplerDuration(
                        document,
                        sampler.Input,
                        options.DecodeAccessors,
                        result);
                    if (duration > result.Statistics.AnimationDuration)
                    {
                        result.Statistics.AnimationDuration = duration;
                    }
                }

                samplerIndex++;
            }

            animationIndex++;
        }
    }

    internal double GetAnimationSamplerDuration(
        GltfDocument document,
        int accessorIndex,
        bool decodeAccessor,
        GltfInspectionResult result)
    {
        GltfAccessor accessor = document.Accessors[accessorIndex];
        if (!decodeAccessor)
        {
            if (accessor.Maximum != null && accessor.Maximum.Length > 0
                && double.IsFinite(accessor.Maximum[0]))
            {
                return accessor.Maximum[0];
            }

            return 0.0;
        }

        GltfAccessorReadRequest request = new GltfAccessorReadRequest();
        request.Document = document;
        request.Accessor = accessorIndex;
        request.ApplyNormalization = false;
        GltfAccessorData data = AccessorCodec.ReadFloatingData(request);
        if (data.Status != GltfOperationStatus.Success)
        {
            MergeInspectionStatus(result, data.Status);
            if (data.Diagnostic != null)
            {
                result.Diagnostics.Add(data.Diagnostic);
            }

            return 0.0;
        }

        double maximum = 0.0;
        int valueIndex = 0;
        while (valueIndex < data.FloatingValues.Length)
        {
            double value = data.FloatingValues[valueIndex];
            if (double.IsFinite(value) && value > maximum)
            {
                maximum = value;
            }

            valueIndex++;
        }

        return maximum;
    }

    internal void PopulateResourceStatistics(
        GltfDocument document,
        GltfDocumentStatistics statistics)
    {
        int bufferIndex = 0;
        while (bufferIndex < document.Buffers.Count)
        {
            GltfBuffer buffer = document.Buffers[bufferIndex];
            if (buffer != null)
            {
                if (buffer.ByteLength > 0)
                {
                    statistics.BufferByteLength += buffer.ByteLength;
                }

                if (!buffer.Resolved)
                {
                    statistics.UnresolvedResourceCount++;
                }
            }

            bufferIndex++;
        }

        int imageIndex = 0;
        while (imageIndex < document.Images.Count)
        {
            GltfImage image = document.Images[imageIndex];
            if (image != null)
            {
                if (image.Resolved && image.ResolvedBytes != null)
                {
                    statistics.ImageByteLength += image.ResolvedBytes.Length;
                }

                if (!image.Resolved)
                {
                    statistics.UnresolvedResourceCount++;
                }
            }

            imageIndex++;
        }
    }

    internal void PopulateDocumentBounds(
        GltfDocument document,
        GltfInspectionResult result)
    {
        SceneTransformAnalysis transforms = Transforms.EvaluateDocumentTransforms(document);
        if (!transforms.IsValid)
        {
            CopyTransformDiagnostics(transforms, result);
            MergeInspectionStatus(result, GltfOperationStatus.Invalid);
            return;
        }

        SceneBoundsEvaluation bounds = Bounds.CalculateSceneBounds(
            document,
            transforms.Graph.TraversalOrder,
            transforms,
            false);
        int issueIndex = 0;
        while (issueIndex < bounds.Diagnostics.Count)
        {
            result.Diagnostics.Add(bounds.Diagnostics[issueIndex]);
            issueIndex++;
        }

        MergeInspectionStatus(result, bounds.Status);
        if (bounds.Status == GltfOperationStatus.Success)
        {
            result.Statistics.Bounds = bounds.Bounds;
        }
    }

    internal void CopyTransformDiagnostics(
        SceneTransformAnalysis transforms,
        GltfInspectionResult result)
    {
        int issueIndex = 0;
        while (issueIndex < transforms.Diagnostics.Count)
        {
            result.Diagnostics.Add(transforms.Diagnostics[issueIndex]);
            issueIndex++;
        }
    }

    internal void MergeInspectionStatus(
        GltfInspectionResult result,
        GltfOperationStatus status)
    {
        if (status == GltfOperationStatus.Success)
        {
            return;
        }

        if (result.Status == GltfOperationStatus.Success
            || status == GltfOperationStatus.Failed
            || (status == GltfOperationStatus.Invalid
                && result.Status != GltfOperationStatus.Failed))
        {
            result.Status = status;
        }
    }
}
