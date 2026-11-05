using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace gltf;

internal class StorageTransformSettings
{
    internal double ZeroTolerance;
    internal int MaximumBufferByteLength = int.MaxValue;
    internal bool Partition;
    internal bool UseContentHashNames;
}

internal class DocumentStorageTransformer
{
    internal AccessorSparseConverter SparseConverter = new AccessorSparseConverter();
    internal AccessorCodecEngine Accessors = new AccessorCodecEngine();
    internal BufferConsolidator Consolidator = new BufferConsolidator();
    internal DocumentCompactor Mapping = new DocumentCompactor();
    internal ResourcePlanner Resources = new ResourcePlanner();
    internal UriReference Uris = new UriReference();

    internal TransformStepExecution TransformStorage(
        GltfDocument document,
        GltfTransformStep step)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (step == null || step.Settings == null)
        {
            throw new ArgumentNullException("step");
        }

        TransformStepExecution execution = new TransformStepExecution();
        execution.Document = document;
        execution.Result.Step = step;
        execution.Result.Remap = Mapping.CreateIdentityRemap(document);
        StorageTransformSettings settings = ReadStorageSettings(step);
        bool changed = false;
        if (step.Kind == GltfTransformKind.DenseToSparse)
        {
            changed = ConvertDenseAccessors(document, settings, execution.Result);
        }
        else if (step.Kind == GltfTransformKind.SparseToDense)
        {
            changed = ConvertSparseAccessors(document, execution.Result);
        }
        else if (step.Kind == GltfTransformKind.ConsolidateBuffers
            || step.Kind == GltfTransformKind.PartitionBuffers
            || step.Kind == GltfTransformKind.PackBuffers)
        {
            changed = RepackDocumentBuffers(document, step, settings, execution.Result);
        }
        else if (step.Kind == GltfTransformKind.EmbedResources)
        {
            changed = ChangeResourceMode(
                document,
                GltfResourceMode.DataUri,
                settings,
                execution.Result);
        }
        else if (step.Kind == GltfTransformKind.ExtractResources)
        {
            changed = ChangeResourceMode(
                document,
                GltfResourceMode.External,
                settings,
                execution.Result);
        }

        if (changed && document.Container != null)
        {
            document.Container = null;
            execution.Result.ChangedObjects.Add("container");
        }

        return execution;
    }

    internal StorageTransformSettings ReadStorageSettings(GltfTransformStep step)
    {
        StorageTransformSettings result = new StorageTransformSettings();
        if (step.Kind == GltfTransformKind.PartitionBuffers)
        {
            result.MaximumBufferByteLength =
                GltfConstants.DefaultMaximumResourceByteLength;
            result.Partition = true;
        }

        if (step.Kind == GltfTransformKind.PackBuffers)
        {
            result.Partition = ReadBooleanValue(
                step.Settings,
                "partition",
                false);
        }

        if (step.Settings.ContainsKey("zeroTolerance"))
        {
            result.ZeroTolerance = ReadNonNegativeNumber(
                step.Settings["zeroTolerance"],
                "zeroTolerance");
        }

        if (step.Settings.ContainsKey("maximumBufferByteLength"))
        {
            result.MaximumBufferByteLength = ReadPositiveInteger(
                step.Settings["maximumBufferByteLength"],
                "maximumBufferByteLength");
        }

        result.UseContentHashNames = ReadBooleanValue(
            step.Settings,
            "useContentHashNames",
            false);
        return result;
    }

    internal double ReadNonNegativeNumber(JsonNode? node, string name)
    {
        if (node == null || node.GetValueKind() != JsonValueKind.Number)
        {
            throw new ArgumentException(
                $"Storage transform setting '{name}' must be numeric.",
                "step");
        }

        double value = double.Parse(
            node.ToJsonString(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture);
        if (!double.IsFinite(value) || value < 0.0)
        {
            throw new ArgumentException(
                $"Storage transform setting '{name}' must be finite and non-negative.",
                "step");
        }

        return value;
    }

    internal int ReadPositiveInteger(JsonNode? node, string name)
    {
        if (node == null || node.GetValueKind() != JsonValueKind.Number)
        {
            throw new ArgumentException(
                $"Storage transform setting '{name}' must be a positive integer.",
                "step");
        }

        int value = node.GetValue<int>();
        if (value < 1)
        {
            throw new ArgumentException(
                $"Storage transform setting '{name}' must be a positive integer.",
                "step");
        }

        return value;
    }

    internal bool ReadBooleanValue(
        JsonObject settings,
        string name,
        bool defaultValue)
    {
        if (!settings.ContainsKey(name))
        {
            return defaultValue;
        }

        JsonNode? node = settings[name];
        if (node == null
            || (node.GetValueKind() != JsonValueKind.True
                && node.GetValueKind() != JsonValueKind.False))
        {
            throw new ArgumentException(
                $"Storage transform setting '{name}' must be Boolean.",
                "step");
        }

        return node.GetValue<bool>();
    }

    internal bool ConvertDenseAccessors(
        GltfDocument document,
        StorageTransformSettings settings,
        GltfTransformStepResult result)
    {
        bool changed = false;
        int accessorIndex = 0;
        int accessorCount = document.Accessors.Count;
        while (accessorIndex < accessorCount)
        {
            GltfAccessor accessor = document.Accessors[accessorIndex];
            if (accessor != null
                && accessor.Sparse == null
                && accessor.HasBufferView)
            {
                GltfAccessorReadRequest request = new GltfAccessorReadRequest();
                request.Document = document;
                request.Accessor = accessorIndex;
                GltfAccessorData dense = Accessors.ReadRawData(request);
                int originalBufferCount = document.Buffers.Count;
                int originalViewCount = document.BufferViews.Count;
                int oldBufferView = accessor.BufferView;
                bool oldHasByteOffset = accessor.HasByteOffset;
                int oldByteOffset = accessor.ByteOffset;
                AccessorSparseConversionResult conversion =
                    SparseConverter.GenerateSparseAccessor(
                        document,
                        accessorIndex,
                        settings.ZeroTolerance);
                int sparseByteLength = 0;
                if (conversion.Buffer != GltfConstants.AbsentIndex)
                {
                    sparseByteLength = document.Buffers[conversion.Buffer].ByteLength;
                }

                if (sparseByteLength < dense.RawBytes.Length)
                {
                    result.ChangedObjects.Add($"accessors/{accessorIndex}");
                    changed = true;
                }
                else
                {
                    accessor.HasBufferView = true;
                    accessor.BufferView = oldBufferView;
                    accessor.HasByteOffset = oldHasByteOffset;
                    accessor.ByteOffset = oldByteOffset;
                    accessor.Sparse = null;
                    RemoveGeneratedStorage(
                        document,
                        originalBufferCount,
                        originalViewCount);
                }
            }

            accessorIndex++;
        }

        return changed;
    }

    internal void RemoveGeneratedStorage(
        GltfDocument document,
        int bufferCount,
        int bufferViewCount)
    {
        while (document.BufferViews.Count > bufferViewCount)
        {
            document.BufferViews.RemoveAt(document.BufferViews.Count - 1);
        }

        while (document.Buffers.Count > bufferCount)
        {
            document.Buffers.RemoveAt(document.Buffers.Count - 1);
        }
    }

    internal bool ConvertSparseAccessors(
        GltfDocument document,
        GltfTransformStepResult result)
    {
        bool changed = false;
        int accessorIndex = 0;
        int accessorCount = document.Accessors.Count;
        while (accessorIndex < accessorCount)
        {
            GltfAccessor accessor = document.Accessors[accessorIndex];
            if (accessor != null && accessor.Sparse != null)
            {
                SparseConverter.MaterializeSparseAccessor(document, accessorIndex);
                result.ChangedObjects.Add($"accessors/{accessorIndex}");
                changed = true;
            }

            accessorIndex++;
        }

        return changed;
    }

    internal bool RepackDocumentBuffers(
        GltfDocument document,
        GltfTransformStep step,
        StorageTransformSettings settings,
        GltfTransformStepResult result)
    {
        int oldBufferCount = document.Buffers.Count;
        int oldViewCount = document.BufferViews.Count;
        int[] oldViewBuffers = new int[oldViewCount];
        int viewIndex = 0;
        while (viewIndex < oldViewCount)
        {
            oldViewBuffers[viewIndex] = document.BufferViews[viewIndex].Buffer;
            viewIndex++;
        }

        BufferConsolidationOptions options = new BufferConsolidationOptions();
        options.MaximumBufferByteLength = settings.MaximumBufferByteLength;
        options.Partition = step.Kind == GltfTransformKind.PartitionBuffers
            || settings.Partition;
        BufferConsolidationResult packed = Consolidator.ConsolidateBuffers(
            document,
            options);
        ApplyBufferPackingRemap(
            document,
            oldBufferCount,
            oldViewBuffers,
            packed,
            result.Remap);
        if (oldBufferCount == 0 && oldViewCount == 0)
        {
            return false;
        }

        result.ChangedObjects.Add("buffers");
        result.ChangedObjects.Add("bufferViews");
        return true;
    }

    internal void ApplyBufferPackingRemap(
        GltfDocument document,
        int oldBufferCount,
        int[] oldViewBuffers,
        BufferConsolidationResult packed,
        GltfIndexRemap remap)
    {
        remap.BufferViews.Clear();
        remap.Buffers.Clear();
        int oldViewIndex = 0;
        while (oldViewIndex < packed.BufferViews.Count)
        {
            int newViewIndex = packed.BufferViews[oldViewIndex];
            remap.BufferViews[oldViewIndex] = newViewIndex;
            int oldBufferIndex = oldViewBuffers[oldViewIndex];
            if (oldBufferIndex >= 0
                && oldBufferIndex < oldBufferCount
                && !remap.Buffers.ContainsKey(oldBufferIndex))
            {
                remap.Buffers[oldBufferIndex] =
                    document.BufferViews[newViewIndex].Buffer;
            }

            oldViewIndex++;
        }
    }

    internal bool ChangeResourceMode(
        GltfDocument document,
        GltfResourceMode mode,
        StorageTransformSettings settings,
        GltfTransformStepResult result)
    {
        PrepareDocumentResources(document);
        int sourceResourceCount = document.Resources.Resources.Count;
        if (sourceResourceCount == 0)
        {
            return false;
        }

        ResourceWritePlan plan = Resources.PlanResources(
            document.Resources,
            mode,
            settings.UseContentHashNames);
        ApplyPlannedResourceBindings(document, plan, result);
        document.Resources = plan.Resources;
        result.Remap.Resources.Clear();
        int resourceIndex = 0;
        while (resourceIndex < sourceResourceCount)
        {
            result.Remap.Resources[resourceIndex] =
                plan.SourceResourceMap[resourceIndex];
            result.ChangedResources.Add($"resources/{resourceIndex}");
            resourceIndex++;
        }

        return true;
    }

    internal void PrepareDocumentResources(GltfDocument document)
    {
        int bufferIndex = 0;
        while (bufferIndex < document.Buffers.Count)
        {
            GltfBuffer buffer = document.Buffers[bufferIndex];
            ValidateResourceBinding(
                buffer.HasResource,
                buffer.Resource,
                document.Resources.Resources.Count,
                $"buffer {bufferIndex}");
            if (!buffer.HasResource && buffer.Resolved)
            {
                GltfResource resource = CreateBufferResource(buffer);
                buffer.HasResource = true;
                buffer.Resource = document.Resources.Resources.Count;
                document.Resources.Resources.Add(resource);
            }

            bufferIndex++;
        }

        int imageIndex = 0;
        while (imageIndex < document.Images.Count)
        {
            GltfImage image = document.Images[imageIndex];
            ValidateResourceBinding(
                image.HasResource,
                image.Resource,
                document.Resources.Resources.Count,
                $"image {imageIndex}");
            if (!image.HasResource && (image.Resolved || image.HasBufferView))
            {
                GltfResource resource = CreateImageResource(document, image);
                image.HasResource = true;
                image.Resource = document.Resources.Resources.Count;
                document.Resources.Resources.Add(resource);
            }

            imageIndex++;
        }
    }

    internal void ValidateResourceBinding(
        bool hasResource,
        int resourceIndex,
        int resourceCount,
        string subject)
    {
        if (hasResource
            && (resourceIndex < 0 || resourceIndex >= resourceCount))
        {
            throw new ArgumentException(
                $"The {subject} resource reference is outside the document.",
                "document");
        }
    }

    internal GltfResource CreateBufferResource(GltfBuffer buffer)
    {
        GltfResource result = new GltfResource();
        result.Kind = GltfResourceKind.Buffer;
        result.SourceUri = buffer.Uri;
        result.EffectiveUri = buffer.Uri;
        result.MimeType = GltfMimeTypes.GltfBuffer;
        result.Bytes = OwnedBytes.CopyArray(buffer.ResolvedBytes);
        result.UriKind = Uris.ClassifyReference(buffer.Uri ?? "");
        return result;
    }

    internal GltfResource CreateImageResource(
        GltfDocument document,
        GltfImage image)
    {
        GltfResource result = new GltfResource();
        result.Kind = GltfResourceKind.Image;
        result.SourceUri = image.Uri;
        result.EffectiveUri = image.EffectiveUri ?? image.Uri;
        result.MimeType = image.ResolvedMimeType ?? image.MimeType;
        result.Bytes = OwnedBytes.CopyArray(image.ResolvedBytes);
        if (image.HasBufferView && result.Bytes.Length == 0)
        {
            GltfBufferView view = document.BufferViews[image.BufferView];
            int byteOffset = 0;
            if (view.HasByteOffset)
            {
                byteOffset = view.ByteOffset;
            }

            result.Bytes = Consolidator.CopyBufferRange(
                document,
                view.Buffer,
                byteOffset,
                view.ByteLength,
                "image buffer view");
        }

        result.UriKind = Uris.ClassifyReference(result.EffectiveUri ?? "");
        return result;
    }

    internal void ApplyPlannedResourceBindings(
        GltfDocument document,
        ResourceWritePlan plan,
        GltfTransformStepResult result)
    {
        int bufferIndex = 0;
        while (bufferIndex < document.Buffers.Count)
        {
            GltfBuffer buffer = document.Buffers[bufferIndex];
            if (buffer.HasResource)
            {
                int oldResource = buffer.Resource;
                buffer.Resource = plan.SourceResourceMap[oldResource];
                buffer.Uri = plan.SourceResourceUris[oldResource];
                buffer.PendingResourceRequest = null;
                result.ChangedObjects.Add($"buffers/{bufferIndex}");
            }

            bufferIndex++;
        }

        int imageIndex = 0;
        while (imageIndex < document.Images.Count)
        {
            GltfImage image = document.Images[imageIndex];
            if (image.HasResource)
            {
                int oldResource = image.Resource;
                image.Resource = plan.SourceResourceMap[oldResource];
                image.Uri = plan.SourceResourceUris[oldResource];
                image.EffectiveUri = image.Uri;
                image.HasBufferView = false;
                image.BufferView = GltfConstants.AbsentIndex;
                image.PendingResourceRequest = null;
                result.ChangedObjects.Add($"images/{imageIndex}");
            }

            imageIndex++;
        }
    }
}
