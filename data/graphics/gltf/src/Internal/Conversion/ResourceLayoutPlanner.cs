using System;
using System.Collections.Generic;

namespace gltf;

internal class ResourceLayoutPlacement
{
    internal int SourceResource = GltfConstants.AbsentIndex;
    internal int OutputResource = GltfConstants.AbsentIndex;
    internal GltfResourceMode Mode = GltfResourceMode.Preserve;
    internal string? Uri;
    internal bool Embedded;
    internal int BinByteOffset = GltfConstants.AbsentIndex;
    internal int BufferView = GltfConstants.AbsentIndex;
    internal int ByteLength;
    internal string? MimeType;
}

internal class ResourceLayoutPlan
{
    internal GltfContainerFormat Format = GltfContainerFormat.Auto;
    internal GltfResourceMode Mode = GltfResourceMode.Preserve;
    internal ResourceWritePlan WritePlan = new ResourceWritePlan();
    internal List<ResourceLayoutPlacement> Placements =
        new List<ResourceLayoutPlacement>();
    internal bool HasBinChunk;
    internal byte[] BinBytes = [];
}

internal class ResourceLayoutPlanner
{
    internal ResourcePlanner Resources = new ResourcePlanner();

    internal ResourceLayoutPlan PlanLayout(
        GltfDocument document,
        GltfContainerFormat outputFormat,
        GltfResourceMode mode,
        bool embedImages,
        bool useContentHashNames)
    {
        GltfContainerFormat format = ResolveOutputFormat(
            document,
            outputFormat,
            mode);
        ValidateLayout(document, format, mode, embedImages);
        if (mode == GltfResourceMode.GlbBin)
        {
            return PlanGlbBinLayout(document, format, embedImages);
        }

        ResourceWritePlan writePlan = Resources.PlanResources(
            document.Resources,
            mode,
            useContentHashNames);
        ResourceLayoutPlan result = new ResourceLayoutPlan();
        result.Format = format;
        result.Mode = mode;
        result.WritePlan = writePlan;
        AddStandardPlacements(result, document.Resources, writePlan, mode);
        AddPreservedBinChunk(result, document, mode, format);
        return result;
    }

    internal GltfContainerFormat ResolveOutputFormat(
        GltfDocument document,
        GltfContainerFormat outputFormat,
        GltfResourceMode mode)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        GltfContainerFormat result = outputFormat;
        if (result == GltfContainerFormat.Auto)
        {
            if (mode == GltfResourceMode.GlbBin)
            {
                result = GltfContainerFormat.Glb;
            }
            else if (document.Container != null
                && (document.Container.Format == GltfContainerFormat.Json
                    || document.Container.Format == GltfContainerFormat.Glb))
            {
                result = document.Container.Format;
            }
            else
            {
                result = GltfContainerFormat.Json;
            }
        }

        if (result != GltfContainerFormat.Json
            && result != GltfContainerFormat.Glb)
        {
            throw new GltfWriteException(
                "The resource layout output format is not specified.");
        }

        return result;
    }

    internal void ValidateLayout(
        GltfDocument document,
        GltfContainerFormat format,
        GltfResourceMode mode,
        bool embedImages)
    {
        if (document.Resources == null
            || document.Resources.Resources == null
            || document.Buffers == null
            || document.Images == null)
        {
            throw new GltfWriteException(
                "The document resource layout collections must not be null.");
        }

        if (mode != GltfResourceMode.Preserve
            && mode != GltfResourceMode.External
            && mode != GltfResourceMode.DataUri
            && mode != GltfResourceMode.BufferView
            && mode != GltfResourceMode.GlbBin)
        {
            throw new ArgumentOutOfRangeException("mode");
        }

        if (mode == GltfResourceMode.GlbBin
            && format != GltfContainerFormat.Glb)
        {
            throw new GltfWriteException(
                "GLB BIN resource layout requires GLB output.");
        }

        if (embedImages
            && mode != GltfResourceMode.BufferView
            && mode != GltfResourceMode.GlbBin)
        {
            throw new GltfWriteException(
                "Image embedding requires BufferView or GlbBin resource mode.");
        }

        ValidateResources(document.Resources);
        ValidateBindings(document, mode, embedImages);
        if (mode == GltfResourceMode.Preserve)
        {
            ValidatePreservedLayout(document, format);
        }

        if (mode == GltfResourceMode.BufferView
            || (mode == GltfResourceMode.GlbBin && embedImages))
        {
            Resources.ImagePacker.ValidateImageResources(document.Resources);
        }
    }

    internal void ValidateResources(GltfResourceSet resourceSet)
    {
        int index = 0;
        while (index < resourceSet.Resources.Count)
        {
            GltfResource resource = resourceSet.Resources[index];
            if (resource == null)
            {
                throw new GltfWriteException(
                    $"Document resource {index} is null.");
            }

            if (resource.Bytes == null)
            {
                throw new GltfWriteException(
                    $"Document resource {index} bytes are null.");
            }

            index++;
        }
    }

    internal void ValidateBindings(
        GltfDocument document,
        GltfResourceMode mode,
        bool embedImages)
    {
        int resourceCount = document.Resources.Resources.Count;
        int bufferIndex = 0;
        while (bufferIndex < document.Buffers.Count)
        {
            GltfBuffer buffer = document.Buffers[bufferIndex];
            if (buffer == null)
            {
                throw new GltfWriteException(
                    $"Document buffer {bufferIndex} is null.");
            }

            ValidateBinding(
                buffer.HasResource,
                buffer.Resource,
                resourceCount,
                GltfResourceKind.Buffer,
                document.Resources,
                $"Buffer {bufferIndex}");
            if (mode == GltfResourceMode.GlbBin
                && !buffer.HasResource)
            {
                throw new GltfWriteException(
                    $"Buffer {bufferIndex} has no resolved resource for GLB BIN layout.");
            }

            bufferIndex++;
        }

        int imageIndex = 0;
        while (imageIndex < document.Images.Count)
        {
            GltfImage image = document.Images[imageIndex];
            if (image == null)
            {
                throw new GltfWriteException(
                    $"Document image {imageIndex} is null.");
            }

            ValidateBinding(
                image.HasResource,
                image.Resource,
                resourceCount,
                GltfResourceKind.Image,
                document.Resources,
                $"Image {imageIndex}");
            if (mode == GltfResourceMode.GlbBin
                && embedImages
                && image.Uri != null
                && !image.HasResource)
            {
                throw new GltfWriteException(
                    $"Image {imageIndex} has no resolved resource for GLB BIN embedding.");
            }

            imageIndex++;
        }
    }

    internal void ValidateBinding(
        bool hasResource,
        int resourceIndex,
        int resourceCount,
        GltfResourceKind expectedKind,
        GltfResourceSet resources,
        string subject)
    {
        if (!hasResource)
        {
            return;
        }

        if (resourceIndex < 0 || resourceIndex >= resourceCount)
        {
            throw new GltfWriteException(
                $"{subject} resource index {resourceIndex} is outside the document resource collection.");
        }

        if (resources.Resources[resourceIndex].Kind != expectedKind)
        {
            throw new GltfWriteException(
                $"{subject} resource {resourceIndex} has an incompatible resource kind.");
        }
    }

    internal void ValidatePreservedLayout(
        GltfDocument document,
        GltfContainerFormat format)
    {
        int embeddedBufferCount = 0;
        int index = 0;
        while (index < document.Buffers.Count)
        {
            GltfBuffer buffer = document.Buffers[index];
            if (buffer.Uri == null)
            {
                if (format == GltfContainerFormat.Json)
                {
                    throw new GltfWriteException(
                        $"JSON output cannot preserve URI-less buffer {index}.");
                }

                embeddedBufferCount++;
                if (index != 0)
                {
                    throw new GltfWriteException(
                        "GLB output can preserve only buffer 0 without a URI.");
                }

                if (!HasPreservedBufferSource(document, buffer))
                {
                    throw new GltfWriteException(
                        "GLB output cannot preserve buffer 0 without resolved BIN bytes.");
                }
            }

            index++;
        }

        if (embeddedBufferCount > 1)
        {
            throw new GltfWriteException(
                "GLB output cannot preserve more than one URI-less buffer.");
        }
    }

    internal bool HasPreservedBufferSource(
        GltfDocument document,
        GltfBuffer buffer)
    {
        if (document.Container != null
            && document.Container.HasBinChunk
            && document.Container.BinBytes != null)
        {
            return true;
        }

        if (buffer.HasResource)
        {
            return true;
        }

        return buffer.Resolved && buffer.ResolvedBytes != null;
    }

    internal void AddStandardPlacements(
        ResourceLayoutPlan target,
        GltfResourceSet source,
        ResourceWritePlan writePlan,
        GltfResourceMode mode)
    {
        int sourceIndex = 0;
        while (sourceIndex < source.Resources.Count)
        {
            int outputIndex = writePlan.SourceResourceMap[sourceIndex];
            GltfResource sourceResource = source.Resources[sourceIndex];
            ResourceLayoutPlacement placement = new ResourceLayoutPlacement();
            placement.SourceResource = sourceIndex;
            placement.OutputResource = outputIndex;
            placement.Mode = mode;
            placement.Uri = writePlan.SourceResourceUris[sourceIndex];
            placement.ByteLength = sourceResource.Bytes.Length;
            placement.MimeType = sourceResource.MimeType;
            placement.Embedded = outputIndex >= 0
                && writePlan.Resources.Resources[outputIndex].Embedded;
            if (outputIndex >= 0)
            {
                placement.MimeType =
                    writePlan.Resources.Resources[outputIndex].MimeType;
            }
            if (mode == GltfResourceMode.BufferView
                && sourceResource.Kind == GltfResourceKind.Image)
            {
                ImageBufferViewPackPlan? imagePack = writePlan.ImagePack;
                if (imagePack == null)
                {
                    throw new GltfWriteException(
                        "The image bufferView plan is missing.");
                }

                placement.Uri = null;
                placement.Embedded = true;
                placement.BufferView = imagePack.ResourceBufferViews[sourceIndex];
                placement.MimeType = imagePack.MimeTypes[placement.BufferView];
            }

            target.Placements.Add(placement);
            sourceIndex++;
        }
    }

    internal void AddPreservedBinChunk(
        ResourceLayoutPlan target,
        GltfDocument document,
        GltfResourceMode mode,
        GltfContainerFormat format)
    {
        if (mode != GltfResourceMode.Preserve
            || format != GltfContainerFormat.Glb
            || document.Buffers.Count == 0
            || document.Buffers[0].Uri != null)
        {
            return;
        }

        target.HasBinChunk = true;
        if (document.Container != null
            && document.Container.HasBinChunk
            && document.Container.BinBytes != null)
        {
            target.BinBytes = OwnedBytes.CopyArray(document.Container.BinBytes);
            return;
        }

        GltfBuffer buffer = document.Buffers[0];
        if (buffer.HasResource)
        {
            target.BinBytes = OwnedBytes.CopyArray(
                document.Resources.Resources[buffer.Resource].Bytes);
        }
        else
        {
            target.BinBytes = OwnedBytes.CopyArray(buffer.ResolvedBytes);
        }
    }

    internal ResourceLayoutPlan PlanGlbBinLayout(
        GltfDocument document,
        GltfContainerFormat format,
        bool embedImages)
    {
        ResourceLayoutPlan result = new ResourceLayoutPlan();
        result.Format = format;
        result.Mode = GltfResourceMode.GlbBin;
        result.WritePlan.Mode = GltfResourceMode.GlbBin;
        List<int> offsets = CalculateBinOffsets(
            document.Resources,
            embedImages);
        int binByteLength = CalculateBinByteLength(
            document.Resources,
            offsets);
        if (HasBinResources(document.Resources, embedImages))
        {
            result.HasBinChunk = true;
            result.BinBytes = new byte[binByteLength];
            CopyBinResources(document.Resources, offsets, result.BinBytes);
            AddBinResource(result);
        }

        AddGlbResourcesAndPlacements(
            result,
            document.Resources,
            offsets,
            embedImages);
        return result;
    }

    internal bool HasBinResources(
        GltfResourceSet source,
        bool embedImages)
    {
        int index = 0;
        while (index < source.Resources.Count)
        {
            if (ShouldStoreInBin(source.Resources[index], embedImages))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal List<int> CalculateBinOffsets(
        GltfResourceSet source,
        bool embedImages)
    {
        List<int> result = new List<int>();
        long totalByteLength = 0;
        int index = 0;
        while (index < source.Resources.Count)
        {
            GltfResource resource = source.Resources[index];
            int offset = GltfConstants.AbsentIndex;
            if (ShouldStoreInBin(resource, embedImages))
            {
                long aligned = (totalByteLength + 3L) & -4L;
                long end = aligned + resource.Bytes.Length;
                if (end > int.MaxValue)
                {
                    throw new GltfWriteException(
                        "The planned GLB BIN chunk exceeds the supported signed byte length.");
                }

                offset = (int)aligned;
                totalByteLength = end;
            }

            result.Add(offset);
            index++;
        }

        return result;
    }

    internal int CalculateBinByteLength(
        GltfResourceSet source,
        List<int> offsets)
    {
        int result = 0;
        int index = 0;
        while (index < offsets.Count)
        {
            if (offsets[index] >= 0)
            {
                long end = (long)offsets[index]
                    + source.Resources[index].Bytes.Length;
                if (end > result)
                {
                    result = (int)end;
                }
            }

            index++;
        }

        return result;
    }

    internal bool ShouldStoreInBin(
        GltfResource resource,
        bool embedImages)
    {
        return resource.Kind == GltfResourceKind.Buffer
            || (embedImages && resource.Kind == GltfResourceKind.Image);
    }

    internal void CopyBinResources(
        GltfResourceSet source,
        List<int> offsets,
        byte[] target)
    {
        int index = 0;
        while (index < offsets.Count)
        {
            int offset = offsets[index];
            if (offset >= 0)
            {
                byte[] bytes = source.Resources[index].Bytes;
                Array.Copy(bytes, 0, target, offset, bytes.Length);
            }

            index++;
        }
    }

    internal void AddBinResource(ResourceLayoutPlan target)
    {
        GltfResource resource = new GltfResource();
        resource.Kind = GltfResourceKind.Buffer;
        resource.UriKind = GltfUriKind.Absent;
        resource.MimeType = GltfMimeTypes.GltfBuffer;
        resource.Bytes = OwnedBytes.CopyArray(target.BinBytes);
        resource.Embedded = true;
        string hash = Resources.ComputeContentHash(resource.Bytes);
        target.WritePlan.Resources.Resources.Add(resource);
        target.WritePlan.OutputHashes.Add(hash);
        Resources.AddCacheEntry(target.WritePlan.Resources, resource, hash);
        target.WritePlan.Manifest.Add(Resources.CreateManifestEntry(
            resource,
            GltfResourceMode.GlbBin,
            hash));
    }

    internal void AddGlbResourcesAndPlacements(
        ResourceLayoutPlan target,
        GltfResourceSet source,
        List<int> offsets,
        bool embedImages)
    {
        int bufferViewIndex = 0;
        int sourceIndex = 0;
        while (sourceIndex < source.Resources.Count)
        {
            GltfResource sourceResource = source.Resources[sourceIndex];
            int offset = offsets[sourceIndex];
            ResourceLayoutPlacement placement = new ResourceLayoutPlacement();
            placement.SourceResource = sourceIndex;
            placement.ByteLength = sourceResource.Bytes.Length;
            placement.MimeType = sourceResource.MimeType;
            if (offset >= 0)
            {
                placement.OutputResource = 0;
                placement.Mode = GltfResourceMode.GlbBin;
                placement.Embedded = true;
                placement.BinByteOffset = offset;
                if (embedImages
                    && sourceResource.Kind == GltfResourceKind.Image)
                {
                    placement.BufferView = bufferViewIndex;
                    placement.MimeType = Resources.ImagePacker.GetImageMimeType(
                        sourceResource);
                    bufferViewIndex++;
                }
            }
            else
            {
                GltfResource outputResource = Resources.CopyResource(sourceResource);
                int outputIndex = target.WritePlan.Resources.Resources.Count;
                string hash = Resources.ComputeContentHash(outputResource.Bytes);
                target.WritePlan.Resources.Resources.Add(outputResource);
                target.WritePlan.OutputHashes.Add(hash);
                Resources.AddCacheEntry(
                    target.WritePlan.Resources,
                    outputResource,
                    hash);
                target.WritePlan.Manifest.Add(Resources.CreateManifestEntry(
                    outputResource,
                    GltfResourceMode.Preserve,
                    hash));
                placement.OutputResource = outputIndex;
                placement.Mode = GltfResourceMode.Preserve;
                placement.Uri = Resources.GetOutputUri(outputResource);
                placement.Embedded = outputResource.Embedded;
            }

            target.WritePlan.SourceResourceMap.Add(placement.OutputResource);
            target.WritePlan.SourceResourceUris.Add(placement.Uri);
            target.Placements.Add(placement);
            sourceIndex++;
        }
    }
}
