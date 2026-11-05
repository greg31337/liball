using System;
using System.Collections.Generic;

namespace gltf;

internal class SameContainerConverter
{
    internal DocumentCloner Cloner = new DocumentCloner();
    internal GltfReader Reader = new GltfReader();
    internal GlbReader GlbReader = new GlbReader();
    internal CompressionReadProcessor Compression =
        new CompressionReadProcessor();
    internal DocumentStorageTransformer Storage =
        new DocumentStorageTransformer();
    internal BufferConsolidator Consolidator = new BufferConsolidator();
    internal CompressionMetadataSynchronizer CompressionMetadata =
        new CompressionMetadataSynchronizer();
    internal RequiredExtensionCapabilityChecker RequiredExtensions =
        new RequiredExtensionCapabilityChecker();
    internal ResourceLayoutPlanner Layout = new ResourceLayoutPlanner();
    internal ResourcePlanner Resources = new ResourcePlanner();
    internal GlbToGltfConverter GltfOutput = new GlbToGltfConverter();
    internal GltfToGlbConverter GlbOutput = new GltfToGlbConverter();
    internal GltfWriter Writer = new GltfWriter();

    internal GltfConversionResult ConvertDocument(
        GltfDocument document,
        GltfConversionOptions options,
        GltfContainerFormat format)
    {
        ValidateInputs(document, options, format);
        ValidateExtensionCapabilities(document, options);
        GltfDocument working = CloneSource(document);
        List<GltfValidationIssue> diagnostics =
            new List<GltfValidationIssue>();

        if (CanPreserveDirectly(working, options, format))
        {
            return WriteResult(
                working,
                options,
                format,
                diagnostics,
                null);
        }

        ResolveRawResources(working, options, format, diagnostics);
        if (options.ResourceMode == GltfResourceMode.GlbBin)
        {
            return RepackGlbBin(
                working,
                options,
                diagnostics);
        }

        if (options.PartitionBuffers)
        {
            RepackBuffers(working, options);
        }

        if (options.ResourceMode == GltfResourceMode.BufferView)
        {
            PrepareCanonicalResources(working, true);
            ResourceWritePlan bufferViewPlan = ApplyBufferViewLayout(
                working,
                options);
            return WriteResult(
                working,
                options,
                format,
                diagnostics,
                bufferViewPlan);
        }

        bool includeBufferViewImages =
            options.ResourceMode != GltfResourceMode.Preserve;
        PrepareCanonicalResources(working, includeBufferViewImages);
        ResourceWritePlan standardPlan = ApplyStandardLayout(
            working,
            options);
        PrepareContainerForStandardLayout(
            working,
            format);
        return WriteResult(
            working,
            options,
            format,
            diagnostics,
            standardPlan);
    }

    internal void ValidateInputs(
        GltfDocument document,
        GltfConversionOptions options,
        GltfContainerFormat format)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (format != GltfContainerFormat.Json
            && format != GltfContainerFormat.Glb)
        {
            throw new ArgumentOutOfRangeException("format");
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

        if (options.ResourceMode != GltfResourceMode.Preserve
            && options.ResourceMode != GltfResourceMode.External
            && options.ResourceMode != GltfResourceMode.DataUri
            && options.ResourceMode != GltfResourceMode.BufferView
            && options.ResourceMode != GltfResourceMode.GlbBin)
        {
            throw new ArgumentOutOfRangeException("options");
        }

        if (format == GltfContainerFormat.Json
            && options.ResourceMode == GltfResourceMode.GlbBin)
        {
            throw new GltfWriteException(
                "GLB BIN resource layout requires GLB output.");
        }

        if (options.MaximumBufferByteLength < 1)
        {
            throw new ArgumentOutOfRangeException("options");
        }
    }

    internal void ValidateExtensionCapabilities(
        GltfDocument document,
        GltfConversionOptions options)
    {
        List<GltfValidationIssue> issues =
            RequiredExtensions.CheckConversionCapabilities(
                document,
                options);
        if (issues.Count > 0)
        {
            throw new GltfUnsupportedFeatureException(issues[0].Message);
        }
    }

    internal GltfDocument CloneSource(GltfDocument document)
    {
        GltfEditOptions options = new GltfEditOptions();
        options.CloneJson = true;
        options.CloneResourceBytes = true;
        options.ValidateResult = false;
        return Cloner.CloneDocumentData(document, options);
    }

    internal bool CanPreserveDirectly(
        GltfDocument document,
        GltfConversionOptions options,
        GltfContainerFormat format)
    {
        if (options.PartitionBuffers || options.EmbedImages)
        {
            return false;
        }

        if (options.ResourceMode == GltfResourceMode.Preserve)
        {
            Layout.ValidateLayout(
                document,
                format,
                GltfResourceMode.Preserve,
                false);
            return true;
        }

        if (options.ResourceMode != GltfResourceMode.GlbBin
            || format != GltfContainerFormat.Glb
            || document.Buffers.Count > 1)
        {
            return false;
        }

        if (document.Buffers.Count == 1
            && document.Buffers[0].Uri != null)
        {
            return false;
        }

        return document.Container != null
            && document.Container.HasBinChunk;
    }

    internal void ResolveRawResources(
        GltfDocument document,
        GltfConversionOptions options,
        GltfContainerFormat format,
        List<GltfValidationIssue> diagnostics)
    {
        GltfOutput.BindExistingResources(document);
        if (GltfOutput.NeedsResourceResolution(document))
        {
            GltfResourceSet previous = document.Resources;
            GltfReadOptions readOptions = CreateResolutionOptions(
                options.ReadOptions,
                format);
            document.Resources = Reader.ResolveDocumentResources(
                document,
                document.Container,
                readOptions);
            PreserveOtherResources(previous, document.Resources);
        }

        if (GltfOutput.NeedsFallbackDecoding(document))
        {
            DecodeRequiredFallbacks(document, options, format, diagnostics);
        }

        GltfOutput.BindExistingResources(document);
    }

    internal GltfReadOptions CreateResolutionOptions(
        GltfReadOptions source,
        GltfContainerFormat format)
    {
        GltfReadOptions result = new GltfReadOptions();
        result.Format = format;
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

    internal void PreserveOtherResources(
        GltfResourceSet source,
        GltfResourceSet target)
    {
        int index = 0;
        while (index < source.Resources.Count)
        {
            GltfResource resource = source.Resources[index];
            if (resource.Kind != GltfResourceKind.Buffer
                && resource.Kind != GltfResourceKind.Image)
            {
                GltfOutput.AddSourceResource(
                    target,
                    Resources.CopyResource(resource));
            }

            index++;
        }
    }

    internal void DecodeRequiredFallbacks(
        GltfDocument document,
        GltfConversionOptions options,
        GltfContainerFormat format,
        List<GltfValidationIssue> diagnostics)
    {
        GltfReadOptions readOptions = CreateResolutionOptions(
            options.ReadOptions,
            format);
        GltfReadResult readResult = new GltfReadResult();
        readResult.Document = document;
        readResult.Resources = document.Resources;
        Compression.ProcessRead(readResult, readOptions);
        CopyDiagnostics(diagnostics, readResult.Diagnostics);
        if (readResult.Status != GltfOperationStatus.Success)
        {
            string message =
                "A required compression fallback could not be resolved.";
            if (readResult.Diagnostics.Count > 0)
            {
                message = readResult.Diagnostics[0].Message;
            }

            throw new GltfResourceException(message);
        }
    }

    internal void RepackBuffers(
        GltfDocument document,
        GltfConversionOptions options)
    {
        BufferConsolidationOptions consolidationOptions =
            new BufferConsolidationOptions();
        consolidationOptions.MaximumBufferByteLength =
            options.MaximumBufferByteLength;
        consolidationOptions.Partition = true;
        BufferConsolidationResult consolidation =
            Consolidator.ConsolidateBuffers(
                document,
                consolidationOptions);
        CompressionMetadata.SynchronizeDocument(
            document,
            consolidation);
    }

    internal void PrepareCanonicalResources(
        GltfDocument document,
        bool includeBufferViewImages)
    {
        GltfResourceSet source = document.Resources;
        GltfResourceSet result = new GltfResourceSet();
        int bufferIndex = 0;
        while (bufferIndex < document.Buffers.Count)
        {
            GltfBuffer buffer = document.Buffers[bufferIndex];
            if (!buffer.Resolved || buffer.ResolvedBytes == null)
            {
                throw new GltfResourceException(
                    $"Buffer {bufferIndex} is not resolved for resource repacking.");
            }

            GltfResource resource = Storage.CreateBufferResource(buffer);
            buffer.HasResource = true;
            buffer.Resource = result.Resources.Count;
            GltfOutput.AddSourceResource(result, resource);
            bufferIndex++;
        }

        int imageIndex = 0;
        while (imageIndex < document.Images.Count)
        {
            GltfImage image = document.Images[imageIndex];
            bool includeImage = image.Uri != null
                || includeBufferViewImages;
            if (includeImage)
            {
                if (!image.Resolved && !image.HasBufferView)
                {
                    throw new GltfResourceException(
                        $"Image {imageIndex} is not resolved for resource repacking.");
                }

                GltfResource resource = Storage.CreateImageResource(
                    document,
                    image);
                image.HasResource = true;
                image.Resource = result.Resources.Count;
                GltfOutput.AddSourceResource(result, resource);
            }
            else
            {
                image.HasResource = false;
                image.Resource = GltfConstants.AbsentIndex;
            }

            imageIndex++;
        }

        int sourceIndex = 0;
        while (sourceIndex < source.Resources.Count)
        {
            GltfResource resource = source.Resources[sourceIndex];
            if (resource.Kind != GltfResourceKind.Buffer
                && resource.Kind != GltfResourceKind.Image)
            {
                GltfOutput.AddSourceResource(
                    result,
                    Resources.CopyResource(resource));
            }

            sourceIndex++;
        }

        document.Resources = result;
    }

    internal ResourceWritePlan ApplyStandardLayout(
        GltfDocument document,
        GltfConversionOptions options)
    {
        GltfResourceMode bufferMode = options.ResourceMode;
        GltfResourceMode imageMode = options.ResourceMode;
        if (options.ResourceMode == GltfResourceMode.Preserve
            && options.PartitionBuffers)
        {
            bufferMode = GltfResourceMode.External;
        }

        if (options.EmbedImages)
        {
            imageMode = GltfResourceMode.DataUri;
        }

        ResourceWritePlan plan = GltfOutput.PlanOutputResources(
            document.Resources,
            bufferMode,
            imageMode,
            options.WriteOptions.UseContentHashResourceNames);
        GltfTransformStepResult changes = new GltfTransformStepResult();
        Storage.ApplyPlannedResourceBindings(
            document,
            plan,
            changes);
        document.Resources = plan.Resources;
        return plan;
    }

    internal ResourceWritePlan ApplyBufferViewLayout(
        GltfDocument document,
        GltfConversionOptions options)
    {
        ResourceWritePlan plan = Resources.PlanResources(
            document.Resources,
            GltfResourceMode.BufferView,
            options.WriteOptions.UseContentHashResourceNames);
        ImageBufferViewPackPlan? imagePack = plan.ImagePack;
        if (imagePack == null)
        {
            throw new GltfWriteException(
                "The image bufferView plan is missing.");
        }

        int bufferIndex = 0;
        while (bufferIndex < document.Buffers.Count)
        {
            GltfBuffer buffer = document.Buffers[bufferIndex];
            int sourceResource = buffer.Resource;
            buffer.Resource = plan.SourceResourceMap[sourceResource];
            buffer.Uri = plan.SourceResourceUris[sourceResource];
            buffer.PendingResourceRequest = null;
            bufferIndex++;
        }

        int firstBufferView = document.BufferViews.Count;
        int packedBuffer = GltfConstants.AbsentIndex;
        if (imagePack.Images.Count > 0)
        {
            packedBuffer = document.Buffers.Count;
            int packedResource = plan.Resources.Resources.Count - 1;
            GltfResource packed = plan.Resources.Resources[packedResource];
            GltfBuffer buffer = new GltfBuffer();
            buffer.Uri = packed.EffectiveUri;
            buffer.ByteLength = packed.Bytes.Length;
            buffer.Resolved = true;
            buffer.ResolvedBytes = OwnedBytes.CopyArray(packed.Bytes);
            buffer.HasResource = true;
            buffer.Resource = packedResource;
            document.Buffers.Add(buffer);

            int viewIndex = 0;
            while (viewIndex < imagePack.ByteOffsets.Count)
            {
                GltfBufferView view = new GltfBufferView();
                view.Buffer = packedBuffer;
                view.HasByteOffset = imagePack.ByteOffsets[viewIndex] != 0;
                view.ByteOffset = imagePack.ByteOffsets[viewIndex];
                view.ByteLength = imagePack.ByteLengths[viewIndex];
                document.BufferViews.Add(view);
                viewIndex++;
            }
        }

        int imageIndex = 0;
        while (imageIndex < document.Images.Count)
        {
            GltfImage image = document.Images[imageIndex];
            int sourceResource = image.Resource;
            int localView = imagePack.ResourceBufferViews[sourceResource];
            image.Uri = null;
            image.EffectiveUri = "bufferView:"
                + (firstBufferView + localView);
            image.MimeType = imagePack.MimeTypes[localView];
            image.HasBufferView = true;
            image.BufferView = firstBufferView + localView;
            image.HasResource = false;
            image.Resource = GltfConstants.AbsentIndex;
            image.PendingResourceRequest = null;
            imageIndex++;
        }

        document.Resources = plan.Resources;
        return plan;
    }

    internal void PrepareContainerForStandardLayout(
        GltfDocument document,
        GltfContainerFormat format)
    {
        bool hasUriLessBuffer = false;
        int bufferIndex = 0;
        while (bufferIndex < document.Buffers.Count)
        {
            if (document.Buffers[bufferIndex].Uri == null)
            {
                hasUriLessBuffer = true;
            }

            bufferIndex++;
        }

        if (format == GltfContainerFormat.Glb
            && hasUriLessBuffer
            && document.Container != null
            && document.Container.HasBinChunk)
        {
            return;
        }

        GltfContainerData container = new GltfContainerData();
        container.Format = format;
        document.Container = container;
    }

    internal GltfConversionResult RepackGlbBin(
        GltfDocument document,
        GltfConversionOptions options,
        List<GltfValidationIssue> diagnostics)
    {
        GltfContainerData? sourceContainer = document.Container;
        BufferConsolidationOptions consolidationOptions =
            new BufferConsolidationOptions();
        consolidationOptions.MaximumBufferByteLength = int.MaxValue;
        consolidationOptions.Partition = false;
        BufferConsolidationResult consolidation =
            Consolidator.ConsolidateBuffers(
                document,
                consolidationOptions);
        if (consolidation.ResultBufferCount > 1)
        {
            throw new GltfWriteException(
                "GLB repacking did not produce a single binary buffer.");
        }

        CompressionMetadata.SynchronizeDocument(
            document,
            consolidation);
        if (options.EmbedImages)
        {
            GlbOutput.EmbedUriImages(document);
        }

        GlbOutput.RebuildOutputResources(document);
        GlbOutput.SetOutputContainer(document);
        if (options.WriteOptions.PreserveUnknownChunks
            && sourceContainer != null)
        {
            CopyUnknownChunks(sourceContainer, document.Container);
        }

        GltfConversionResult result = WriteResult(
            document,
            options,
            GltfContainerFormat.Glb,
            diagnostics,
            null);
        int manifestIndex = 0;
        while (manifestIndex < result.Manifest.Count)
        {
            if (result.Manifest[manifestIndex].Kind
                == GltfResourceKind.Buffer
                && result.Manifest[manifestIndex].Embedded)
            {
                result.Manifest[manifestIndex].Mode =
                    GltfResourceMode.GlbBin;
            }

            manifestIndex++;
        }

        return result;
    }

    internal void CopyUnknownChunks(
        GltfContainerData source,
        GltfContainerData? target)
    {
        if (target == null)
        {
            return;
        }

        int index = 0;
        while (index < source.UnknownChunks.Count)
        {
            GltfGlbChunk sourceChunk = source.UnknownChunks[index];
            GltfGlbChunk targetChunk = new GltfGlbChunk();
            targetChunk.Type = sourceChunk.Type;
            targetChunk.Data = OwnedBytes.CopyArray(sourceChunk.Data);
            target.UnknownChunks.Add(targetChunk);
            index++;
        }
    }

    internal GltfConversionResult WriteResult(
        GltfDocument document,
        GltfConversionOptions options,
        GltfContainerFormat format,
        List<GltfValidationIssue> diagnostics,
        ResourceWritePlan? layoutPlan)
    {
        GltfWriteOptions writeOptions = CreateWriteOptions(
            options.WriteOptions,
            format);
        GltfWriteResult writeResult = Writer.WriteBytes(
            document,
            writeOptions);
        if (writeResult.Status == GltfOperationStatus.Success)
        {
            SetResultContainer(
                document,
                writeResult.PrimaryBytes,
                format,
                options.WriteOptions.PreserveUnknownChunks);
        }

        GltfConversionResult result = new GltfConversionResult();
        result.Status = writeResult.Status;
        result.Document = document;
        result.PrimaryBytes = OwnedBytes.CopyArray(
            writeResult.PrimaryBytes);
        result.Resources = writeResult.Resources;
        result.Manifest = writeResult.Manifest;
        if (layoutPlan != null)
        {
            result.Manifest = layoutPlan.Manifest;
        }

        CopyDiagnostics(result.Diagnostics, diagnostics);
        CopyDiagnostics(result.Diagnostics, writeResult.Diagnostics);
        return result;
    }

    internal GltfWriteOptions CreateWriteOptions(
        GltfWriteOptions source,
        GltfContainerFormat format)
    {
        GltfWriteOptions result = new GltfWriteOptions();
        result.Format = format;
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

    internal void SetResultContainer(
        GltfDocument document,
        byte[] primaryBytes,
        GltfContainerFormat format,
        bool preserveUnknownChunks)
    {
        if (format == GltfContainerFormat.Json)
        {
            GltfContainerData container = new GltfContainerData();
            container.Format = GltfContainerFormat.Json;
            container.JsonBytes = OwnedBytes.CopyArray(primaryBytes);
            container.OriginalByteLength = primaryBytes.Length;
            document.Container = container;
            return;
        }

        GlbHeader header = GlbReader.ReadHeader(
            primaryBytes,
            int.MaxValue);
        document.Container = GlbReader.ReadChunks(
            primaryBytes,
            header,
            preserveUnknownChunks);
    }

    internal void CopyDiagnostics(
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
