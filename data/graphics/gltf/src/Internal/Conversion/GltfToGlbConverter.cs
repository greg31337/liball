using System;
using System.Collections.Generic;

namespace gltf;

internal class GlbImagePlacement
{
    internal int Image;
    internal int BufferView = GltfConstants.AbsentIndex;
    internal int ByteOffset;
    internal byte[] Bytes = [];
    internal string MimeType = "";
}

internal class GltfToGlbConverter
{
    internal DocumentCloner Cloner = new DocumentCloner();
    internal GltfReader Reader = new GltfReader();
    internal CompressionReadProcessor Compression =
        new CompressionReadProcessor();
    internal DocumentStorageTransformer Storage =
        new DocumentStorageTransformer();
    internal BufferConsolidator Consolidator = new BufferConsolidator();
    internal CompressionMetadataSynchronizer CompressionMetadata =
        new CompressionMetadataSynchronizer();
    internal ResourceLayoutPlanner Layout = new ResourceLayoutPlanner();
    internal RequiredExtensionCapabilityChecker RequiredExtensions =
        new RequiredExtensionCapabilityChecker();
    internal GltfWriter Writer = new GltfWriter();
    internal ResourcePlanner Resources = new ResourcePlanner();

    internal GltfConversionResult ConvertDocumentToGlb(
        GltfDocument document,
        GltfConversionOptions options)
    {
        ValidateInputs(document, options);
        ValidateExtensionCapabilities(document, options);
        GltfDocument working = CloneDocument(document);
        List<GltfValidationIssue> diagnostics =
            new List<GltfValidationIssue>();
        ResolveResources(working, options, diagnostics);
        Storage.PrepareDocumentResources(working);
        Layout.ValidateLayout(
            working,
            GltfContainerFormat.Glb,
            GltfResourceMode.GlbBin,
            options.EmbedImages);

        BufferConsolidationOptions consolidationOptions =
            new BufferConsolidationOptions();
        consolidationOptions.MaximumBufferByteLength = int.MaxValue;
        consolidationOptions.Partition = false;
        BufferConsolidationResult consolidation =
            Consolidator.ConsolidateBuffers(
                working,
                consolidationOptions);
        if (consolidation.ResultBufferCount > 1)
        {
            throw new GltfWriteException(
                "GLB conversion did not produce a single binary buffer.");
        }

        CompressionMetadata.SynchronizeDocument(working, consolidation);
        if (options.EmbedImages)
        {
            EmbedUriImages(working);
        }

        RebuildOutputResources(working);
        SetOutputContainer(working);
        GltfWriteOptions writeOptions = CreateGlbWriteOptions(
            options.WriteOptions);
        GltfWriteResult writeResult = Writer.WriteBytes(
            working,
            writeOptions);
        GltfConversionResult result = new GltfConversionResult();
        result.Status = writeResult.Status;
        result.Document = working;
        result.PrimaryBytes = OwnedBytes.CopyArray(writeResult.PrimaryBytes);
        result.Resources = writeResult.Resources;
        result.Manifest = writeResult.Manifest;
        AddDiagnostics(result.Diagnostics, diagnostics);
        AddDiagnostics(result.Diagnostics, writeResult.Diagnostics);
        return result;
    }

    internal void ValidateInputs(
        GltfDocument document,
        GltfConversionOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (options.ReadOptions == null
            || options.ReadOptions.Limits == null
            || options.ReadOptions.ResourcePolicy == null)
        {
            throw new ArgumentException(
                "The conversion read options, limits, and resource policy must not be null.",
                "options");
        }

        if (options.WriteOptions == null
            || options.WriteOptions.ValidationOptions == null)
        {
            throw new ArgumentException(
                "The conversion write options and validation options must not be null.",
                "options");
        }

        if (options.InputFormat != GltfContainerFormat.Auto
            && options.InputFormat != GltfContainerFormat.Json)
        {
            throw new GltfWriteException(
                "The glTF-to-GLB stage requires JSON glTF input.");
        }

        if (document.Container != null
            && document.Container.Format == GltfContainerFormat.Glb
            && options.InputFormat == GltfContainerFormat.Auto)
        {
            throw new GltfWriteException(
                "The glTF-to-GLB stage cannot consume an existing GLB document.");
        }

        if (options.OutputFormat != GltfContainerFormat.Auto
            && options.OutputFormat != GltfContainerFormat.Glb)
        {
            throw new GltfWriteException(
                "The glTF-to-GLB stage requires GLB output.");
        }

        if (options.ResourceMode != GltfResourceMode.Preserve
            && options.ResourceMode != GltfResourceMode.GlbBin)
        {
            throw new GltfWriteException(
                "The glTF-to-GLB stage requires Preserve or GlbBin resource mode.");
        }
    }

    internal void ValidateExtensionCapabilities(
        GltfDocument document,
        GltfConversionOptions options)
    {
        List<GltfValidationIssue> issues =
            RequiredExtensions.CheckConversionCapabilities(document, options);
        if (issues.Count > 0)
        {
            throw new GltfUnsupportedFeatureException(issues[0].Message);
        }
    }

    internal GltfDocument CloneDocument(GltfDocument document)
    {
        GltfEditOptions options = new GltfEditOptions();
        options.CloneJson = true;
        options.CloneResourceBytes = true;
        options.ValidateResult = false;
        return Cloner.CloneDocumentData(document, options);
    }

    internal void ResolveResources(
        GltfDocument document,
        GltfConversionOptions options,
        List<GltfValidationIssue> diagnostics)
    {
        BindExistingResources(document);
        if (NeedsResourceResolution(document))
        {
            GltfReadOptions readOptions = CreateResolutionOptions(
                options.ReadOptions);
            document.Resources = Reader.ResolveDocumentResources(
                document,
                null,
                readOptions);
        }

        if (NeedsFallbackDecoding(document))
        {
            GltfReadOptions readOptions = CreateResolutionOptions(
                options.ReadOptions);
            GltfReadResult readResult = new GltfReadResult();
            readResult.Document = document;
            readResult.Resources = document.Resources;
            Compression.ProcessRead(readResult, readOptions);
            AddDiagnostics(diagnostics, readResult.Diagnostics);
            if (readResult.Status != GltfOperationStatus.Success)
            {
                string message = "A required compression fallback could not be resolved.";
                if (readResult.Diagnostics.Count > 0)
                {
                    message = readResult.Diagnostics[0].Message;
                }

                throw new GltfResourceException(message);
            }
        }

        BindExistingResources(document);
    }

    internal void BindExistingResources(GltfDocument document)
    {
        int resourceCount = document.Resources.Resources.Count;
        int bufferIndex = 0;
        while (bufferIndex < document.Buffers.Count)
        {
            GltfBuffer buffer = document.Buffers[bufferIndex];
            if (!buffer.Resolved
                && buffer.HasResource
                && buffer.Resource >= 0
                && buffer.Resource < resourceCount)
            {
                GltfResource resource = document.Resources.Resources[
                    buffer.Resource];
                if (resource.Kind == GltfResourceKind.Buffer
                    && resource.Bytes != null
                    && buffer.ByteLength >= 0
                    && resource.Bytes.Length >= buffer.ByteLength)
                {
                    buffer.ResolvedBytes = OwnedBytes.CopyRange(
                        resource.Bytes,
                        0,
                        buffer.ByteLength);
                    buffer.Resolved = true;
                }
            }

            bufferIndex++;
        }

        int imageIndex = 0;
        while (imageIndex < document.Images.Count)
        {
            GltfImage image = document.Images[imageIndex];
            if (!image.Resolved
                && image.HasResource
                && image.Resource >= 0
                && image.Resource < resourceCount)
            {
                GltfResource resource = document.Resources.Resources[
                    image.Resource];
                if (resource.Kind == GltfResourceKind.Image
                    && resource.Bytes != null)
                {
                    image.ResolvedBytes = OwnedBytes.CopyArray(resource.Bytes);
                    image.Resolved = true;
                    image.ResolvedMimeType = resource.SourceMimeType;
                    if (image.ResolvedMimeType == null)
                    {
                        image.ResolvedMimeType = resource.MimeType;
                    }
                }
            }

            imageIndex++;
        }
    }

    internal bool NeedsResourceResolution(GltfDocument document)
    {
        int bufferIndex = 0;
        while (bufferIndex < document.Buffers.Count)
        {
            GltfBuffer buffer = document.Buffers[bufferIndex];
            if (!buffer.Resolved
                && !Compression.IsMeshoptFallbackBuffer(document, bufferIndex))
            {
                return true;
            }

            bufferIndex++;
        }

        int imageIndex = 0;
        while (imageIndex < document.Images.Count)
        {
            GltfImage image = document.Images[imageIndex];
            if (!image.Resolved)
            {
                return true;
            }

            imageIndex++;
        }

        return false;
    }

    internal bool NeedsFallbackDecoding(GltfDocument document)
    {
        int index = 0;
        while (index < document.Buffers.Count)
        {
            if (!document.Buffers[index].Resolved
                && Compression.IsMeshoptFallbackBuffer(document, index))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal GltfReadOptions CreateResolutionOptions(GltfReadOptions source)
    {
        GltfReadOptions result = new GltfReadOptions();
        result.Format = GltfContainerFormat.Json;
        result.DuplicateMemberPolicy = source.DuplicateMemberPolicy;
        result.PreserveUnknownMembers = source.PreserveUnknownMembers;
        result.PreserveUnknownChunks = source.PreserveUnknownChunks;
        result.ResolveResources = true;
        result.Validate = false;
        result.LeaveStreamOpen = true;
        result.BaseUri = source.BaseUri;
        result.Limits = source.Limits;
        result.ResourceResolver = source.ResourceResolver;
        result.ResourcePolicy = source.ResourcePolicy;
        result.ExtensionHandlers = source.ExtensionHandlers;
        result.CompressionCodecs = source.CompressionCodecs;
        result.ImageCodecs = source.ImageCodecs;
        result.ValidationOptions = source.ValidationOptions;
        return result;
    }

    internal void EmbedUriImages(GltfDocument document)
    {
        List<GlbImagePlacement> placements =
            CreateImagePlacements(document);
        if (placements.Count == 0)
        {
            return;
        }

        byte[] originalBytes = [];
        if (document.Buffers.Count == 0)
        {
            document.Buffers.Add(new GltfBuffer());
        }
        else
        {
            originalBytes = document.Buffers[0].ResolvedBytes;
        }

        int byteLength = CalculateEmbeddedByteLength(
            originalBytes.Length,
            placements);
        byte[] combined = new byte[byteLength];
        Array.Copy(originalBytes, 0, combined, 0, originalBytes.Length);
        int placementIndex = 0;
        while (placementIndex < placements.Count)
        {
            GlbImagePlacement placement = placements[placementIndex];
            if (placement.BufferView == GltfConstants.AbsentIndex)
            {
                Array.Copy(
                    placement.Bytes,
                    0,
                    combined,
                    placement.ByteOffset,
                    placement.Bytes.Length);
                GltfBufferView view = new GltfBufferView();
                view.Buffer = 0;
                view.HasByteOffset = placement.ByteOffset != 0;
                view.ByteOffset = placement.ByteOffset;
                view.ByteLength = placement.Bytes.Length;
                placement.BufferView = document.BufferViews.Count;
                document.BufferViews.Add(view);
                ShareDuplicateImagePlacements(placements, placementIndex);
            }

            ApplyImagePlacement(document, placement);
            placementIndex++;
        }

        GltfBuffer buffer = document.Buffers[0];
        buffer.Uri = null;
        buffer.ByteLength = combined.Length;
        buffer.Resolved = true;
        buffer.ResolvedBytes = combined;
        buffer.PendingResourceRequest = null;
    }

    internal List<GlbImagePlacement> CreateImagePlacements(
        GltfDocument document)
    {
        List<GlbImagePlacement> result = new List<GlbImagePlacement>();
        int imageIndex = 0;
        while (imageIndex < document.Images.Count)
        {
            GltfImage image = document.Images[imageIndex];
            if (image.Uri != null)
            {
                if (!image.Resolved || image.ResolvedBytes == null)
                {
                    throw new GltfResourceException(
                        $"Image {imageIndex} is not resolved for GLB embedding.");
                }

                GltfResource resource = CreateResolvedImageResource(image);
                GlbImagePlacement placement = new GlbImagePlacement();
                placement.Image = imageIndex;
                placement.Bytes = OwnedBytes.CopyArray(image.ResolvedBytes);
                placement.MimeType = Resources.ImagePacker.GetImageMimeType(
                    resource);
                result.Add(placement);
            }

            imageIndex++;
        }

        return result;
    }

    internal GltfResource CreateResolvedImageResource(GltfImage image)
    {
        GltfResource result = new GltfResource();
        result.Kind = GltfResourceKind.Image;
        result.MimeType = image.ResolvedMimeType;
        if (result.MimeType == null)
        {
            result.MimeType = image.MimeType;
        }

        result.ImageFormat = InferImageFormat(result.MimeType);
        result.Bytes = image.ResolvedBytes;
        return result;
    }

    internal GltfImageFormat InferImageFormat(string? mimeType)
    {
        if (string.Equals(
            mimeType,
            GltfMimeTypes.Png,
            StringComparison.OrdinalIgnoreCase))
        {
            return GltfImageFormat.Png;
        }

        if (string.Equals(
            mimeType,
            GltfMimeTypes.Jpeg,
            StringComparison.OrdinalIgnoreCase))
        {
            return GltfImageFormat.Jpeg;
        }

        if (string.Equals(
            mimeType,
            GltfMimeTypes.Ktx2,
            StringComparison.OrdinalIgnoreCase))
        {
            return GltfImageFormat.Ktx2;
        }

        if (string.Equals(
            mimeType,
            GltfMimeTypes.WebP,
            StringComparison.OrdinalIgnoreCase))
        {
            return GltfImageFormat.WebP;
        }

        if (string.Equals(
            mimeType,
            GltfMimeTypes.Avif,
            StringComparison.OrdinalIgnoreCase))
        {
            return GltfImageFormat.Avif;
        }

        if (string.Equals(
            mimeType,
            GltfMimeTypes.Dds,
            StringComparison.OrdinalIgnoreCase))
        {
            return GltfImageFormat.Dds;
        }

        return GltfImageFormat.Unknown;
    }

    internal int CalculateEmbeddedByteLength(
        int originalByteLength,
        List<GlbImagePlacement> placements)
    {
        long byteLength = originalByteLength;
        int index = 0;
        while (index < placements.Count)
        {
            GlbImagePlacement placement = placements[index];
            int duplicate = FindDuplicateImagePlacement(placements, index);
            if (duplicate < 0)
            {
                byteLength = (byteLength + 3L) & -4L;
                if (byteLength > int.MaxValue)
                {
                    throw new GltfWriteException(
                        "Embedded GLB image data exceeds the supported signed byte length.");
                }

                placement.ByteOffset = (int)byteLength;
                byteLength += placement.Bytes.Length;
                if (byteLength > int.MaxValue)
                {
                    throw new GltfWriteException(
                        "Embedded GLB image data exceeds the supported signed byte length.");
                }
            }

            index++;
        }

        return (int)byteLength;
    }

    internal int FindDuplicateImagePlacement(
        List<GlbImagePlacement> placements,
        int placementIndex)
    {
        int candidateIndex = 0;
        while (candidateIndex < placementIndex)
        {
            GlbImagePlacement candidate = placements[candidateIndex];
            GlbImagePlacement placement = placements[placementIndex];
            if (candidate.MimeType == placement.MimeType
                && Resources.HaveEqualBytes(
                    candidate.Bytes,
                    placement.Bytes))
            {
                return candidateIndex;
            }

            candidateIndex++;
        }

        return GltfConstants.AbsentIndex;
    }

    internal void ShareDuplicateImagePlacements(
        List<GlbImagePlacement> placements,
        int sourceIndex)
    {
        GlbImagePlacement source = placements[sourceIndex];
        int index = sourceIndex + 1;
        while (index < placements.Count)
        {
            GlbImagePlacement candidate = placements[index];
            if (candidate.MimeType == source.MimeType
                && Resources.HaveEqualBytes(candidate.Bytes, source.Bytes))
            {
                candidate.ByteOffset = source.ByteOffset;
                candidate.BufferView = source.BufferView;
            }

            index++;
        }
    }

    internal void ApplyImagePlacement(
        GltfDocument document,
        GlbImagePlacement placement)
    {
        GltfImage image = document.Images[placement.Image];
        image.Uri = null;
        image.MimeType = placement.MimeType;
        image.HasBufferView = true;
        image.BufferView = placement.BufferView;
        image.EffectiveUri = "bufferView:" + placement.BufferView;
        image.PendingResourceRequest = null;
    }

    internal void RebuildOutputResources(GltfDocument document)
    {
        GltfResourceSet source = document.Resources;
        GltfResourceSet result = new GltfResourceSet();
        List<int> resourceMap = CreateAbsentResourceMap(
            source.Resources.Count);
        if (document.Buffers.Count > 0)
        {
            GltfBuffer buffer = document.Buffers[0];
            buffer.Uri = null;
            buffer.HasResource = true;
            buffer.Resource = 0;
            buffer.PendingResourceRequest = null;
            GltfResource binary = new GltfResource();
            binary.Kind = GltfResourceKind.Buffer;
            binary.UriKind = GltfUriKind.Absent;
            binary.MimeType = GltfMimeTypes.GltfBuffer;
            binary.Bytes = OwnedBytes.CopyArray(buffer.ResolvedBytes);
            binary.Embedded = true;
            AddOutputResource(result, binary);
        }

        int imageIndex = 0;
        while (imageIndex < document.Images.Count)
        {
            GltfImage image = document.Images[imageIndex];
            if (image.Uri == null)
            {
                image.HasResource = false;
                image.Resource = GltfConstants.AbsentIndex;
            }
            else
            {
                if (!image.HasResource
                    || image.Resource < 0
                    || image.Resource >= source.Resources.Count)
                {
                    throw new GltfResourceException(
                        $"External image {imageIndex} has no resolved output resource.");
                }

                int outputResource = resourceMap[image.Resource];
                if (outputResource == GltfConstants.AbsentIndex)
                {
                    outputResource = AddOutputResource(
                        result,
                        Resources.CopyResource(
                            source.Resources[image.Resource]));
                    resourceMap[image.Resource] = outputResource;
                }

                image.Resource = outputResource;
            }

            imageIndex++;
        }

        int sourceIndex = 0;
        while (sourceIndex < source.Resources.Count)
        {
            GltfResource resource = source.Resources[sourceIndex];
            if (resource.Kind != GltfResourceKind.Buffer
                && resource.Kind != GltfResourceKind.Image
                && resourceMap[sourceIndex] == GltfConstants.AbsentIndex)
            {
                resourceMap[sourceIndex] = AddOutputResource(
                    result,
                    Resources.CopyResource(resource));
            }

            sourceIndex++;
        }

        document.Resources = result;
    }

    internal List<int> CreateAbsentResourceMap(int count)
    {
        List<int> result = new List<int>();
        int index = 0;
        while (index < count)
        {
            result.Add(GltfConstants.AbsentIndex);
            index++;
        }

        return result;
    }

    internal int AddOutputResource(
        GltfResourceSet target,
        GltfResource resource)
    {
        int result = target.Resources.Count;
        target.Resources.Add(resource);
        string hash = Resources.ComputeContentHash(resource.Bytes);
        Resources.AddCacheEntry(target, resource, hash);
        return result;
    }

    internal void SetOutputContainer(GltfDocument document)
    {
        GltfContainerData container = new GltfContainerData();
        container.Format = GltfContainerFormat.Glb;
        if (document.Buffers.Count > 0)
        {
            container.HasBinChunk = true;
            container.BinBytes = OwnedBytes.CopyArray(
                document.Buffers[0].ResolvedBytes);
        }

        document.Container = container;
    }

    internal GltfWriteOptions CreateGlbWriteOptions(GltfWriteOptions source)
    {
        GltfWriteOptions result = new GltfWriteOptions();
        result.Format = GltfContainerFormat.Glb;
        result.ResourceMode = GltfResourceMode.Preserve;
        result.Indented = source.Indented;
        result.Deterministic = source.Deterministic;
        result.UseContentHashResourceNames =
            source.UseContentHashResourceNames;
        result.PreserveExplicitDefaults = source.PreserveExplicitDefaults;
        result.PreserveUnknownMembers = source.PreserveUnknownMembers;
        result.PreserveUnknownChunks = source.PreserveUnknownChunks;
        result.Validate = source.Validate;
        result.LeaveStreamOpen = true;
        result.ValidationOptions = source.ValidationOptions;
        result.ExtensionHandlers = source.ExtensionHandlers;
        result.CompressionCodecs = source.CompressionCodecs;
        result.ImageCodecs = source.ImageCodecs;
        return result;
    }

    internal void AddDiagnostics(
        List<GltfValidationIssue> target,
        List<GltfValidationIssue> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index++;
        }
    }
}
