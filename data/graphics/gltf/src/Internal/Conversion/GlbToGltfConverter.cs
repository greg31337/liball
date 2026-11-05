using System;
using System.Collections.Generic;

namespace gltf;

internal class GlbToGltfConverter
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
    internal RequiredExtensionCapabilityChecker RequiredExtensions =
        new RequiredExtensionCapabilityChecker();
    internal ResourcePlanner Resources = new ResourcePlanner();
    internal GltfWriter Writer = new GltfWriter();

    internal GltfConversionResult ConvertDocumentToGltf(
        GltfDocument document,
        GltfConversionOptions options)
    {
        ValidateInputs(document, options);
        ValidateExtensionCapabilities(document, options);
        GltfDocument working = CloneDocument(document);
        List<GltfValidationIssue> diagnostics =
            new List<GltfValidationIssue>();
        ResolveResources(working, options, diagnostics);
        if (options.PartitionBuffers)
        {
            PartitionDocumentBuffers(working, options);
        }

        PrepareOutputSourceResources(working);
        GltfResourceMode bufferMode = GetBufferMode(options.ResourceMode);
        GltfResourceMode imageMode = GetImageMode(options);
        ResourceWritePlan resourcePlan = PlanOutputResources(
            working.Resources,
            bufferMode,
            imageMode,
            options.WriteOptions.UseContentHashResourceNames);
        GltfTransformStepResult bindingChanges =
            new GltfTransformStepResult();
        Storage.ApplyPlannedResourceBindings(
            working,
            resourcePlan,
            bindingChanges);
        working.Resources = resourcePlan.Resources;
        SetJsonContainer(working);

        GltfWriteOptions writeOptions = CreateJsonWriteOptions(
            options.WriteOptions);
        GltfWriteResult writeResult = Writer.WriteBytes(
            working,
            writeOptions);
        GltfContainerData? outputContainer = working.Container;
        if (writeResult.Status == GltfOperationStatus.Success
            && outputContainer != null)
        {
            outputContainer.JsonBytes = OwnedBytes.CopyArray(
                writeResult.PrimaryBytes);
        }

        GltfConversionResult result = new GltfConversionResult();
        result.Status = writeResult.Status;
        result.Document = working;
        result.PrimaryBytes = OwnedBytes.CopyArray(writeResult.PrimaryBytes);
        result.Resources = writeResult.Resources;
        result.Manifest = resourcePlan.Manifest;
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

        if (document.Container == null
            || document.Container.Format != GltfContainerFormat.Glb)
        {
            throw new GltfWriteException(
                "The GLB-to-glTF stage requires a GLB source container.");
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
            && options.InputFormat != GltfContainerFormat.Glb)
        {
            throw new GltfWriteException(
                "The GLB-to-glTF stage requires GLB input.");
        }

        if (options.OutputFormat != GltfContainerFormat.Auto
            && options.OutputFormat != GltfContainerFormat.Json)
        {
            throw new GltfWriteException(
                "The GLB-to-glTF stage requires JSON glTF output.");
        }

        if (options.ResourceMode != GltfResourceMode.Preserve
            && options.ResourceMode != GltfResourceMode.External
            && options.ResourceMode != GltfResourceMode.DataUri)
        {
            throw new GltfWriteException(
                "The GLB-to-glTF stage requires Preserve, External, or DataUri resource mode.");
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
                document.Container,
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
                string message =
                    "A required compression fallback could not be resolved.";
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
            if (!document.Buffers[bufferIndex].Resolved
                && !Compression.IsMeshoptFallbackBuffer(
                    document,
                    bufferIndex))
            {
                return true;
            }

            bufferIndex++;
        }

        int imageIndex = 0;
        while (imageIndex < document.Images.Count)
        {
            if (!document.Images[imageIndex].Resolved)
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
        result.Format = GltfContainerFormat.Glb;
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

    internal void PartitionDocumentBuffers(
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
        CompressionMetadata.SynchronizeDocument(document, consolidation);
    }

    internal void PrepareOutputSourceResources(GltfDocument document)
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
                    $"Buffer {bufferIndex} is not resolved for glTF extraction.");
            }

            GltfResource resource = Storage.CreateBufferResource(buffer);
            buffer.HasResource = true;
            buffer.Resource = result.Resources.Count;
            AddSourceResource(result, resource);
            bufferIndex++;
        }

        int imageIndex = 0;
        while (imageIndex < document.Images.Count)
        {
            GltfImage image = document.Images[imageIndex];
            if (!image.Resolved && !image.HasBufferView)
            {
                throw new GltfResourceException(
                    $"Image {imageIndex} is not resolved for glTF extraction.");
            }

            GltfResource resource = Storage.CreateImageResource(
                document,
                image);
            image.HasResource = true;
            image.Resource = result.Resources.Count;
            AddSourceResource(result, resource);
            imageIndex++;
        }

        int sourceIndex = 0;
        while (sourceIndex < source.Resources.Count)
        {
            GltfResource resource = source.Resources[sourceIndex];
            if (resource.Kind != GltfResourceKind.Buffer
                && resource.Kind != GltfResourceKind.Image)
            {
                AddSourceResource(
                    result,
                    Resources.CopyResource(resource));
            }

            sourceIndex++;
        }

        document.Resources = result;
    }

    internal int AddSourceResource(
        GltfResourceSet target,
        GltfResource resource)
    {
        int result = target.Resources.Count;
        target.Resources.Add(resource);
        string hash = Resources.ComputeContentHash(resource.Bytes);
        Resources.AddCacheEntry(target, resource, hash);
        return result;
    }

    internal GltfResourceMode GetBufferMode(GltfResourceMode requested)
    {
        if (requested == GltfResourceMode.DataUri)
        {
            return GltfResourceMode.DataUri;
        }

        return GltfResourceMode.External;
    }

    internal GltfResourceMode GetImageMode(GltfConversionOptions options)
    {
        if (options.ResourceMode == GltfResourceMode.DataUri
            || options.EmbedImages)
        {
            return GltfResourceMode.DataUri;
        }

        return GltfResourceMode.External;
    }

    internal ResourceWritePlan PlanOutputResources(
        GltfResourceSet source,
        GltfResourceMode bufferMode,
        GltfResourceMode imageMode,
        bool useContentHashNames)
    {
        Resources.NameAllocator = new ResourceNameAllocator();
        ResourceWritePlan result = new ResourceWritePlan();
        result.Mode = bufferMode;
        int sourceIndex = 0;
        while (sourceIndex < source.Resources.Count)
        {
            GltfResource sourceResource = source.Resources[sourceIndex];
            GltfResourceMode mode = bufferMode;
            if (sourceResource.Kind == GltfResourceKind.Image)
            {
                mode = imageMode;
            }

            string hash = Resources.ComputeContentHash(
                sourceResource.Bytes);
            int outputIndex = FindDuplicateOutput(
                result,
                sourceResource,
                hash,
                mode);
            if (outputIndex < 0)
            {
                GltfResource outputResource =
                    Resources.CopyResource(sourceResource);
                Resources.ApplyResourceMode(
                    outputResource,
                    mode,
                    useContentHashNames);
                outputIndex = result.Resources.Resources.Count;
                result.Resources.Resources.Add(outputResource);
                result.OutputHashes.Add(hash);
                Resources.AddCacheEntry(
                    result.Resources,
                    outputResource,
                    hash);
                result.Manifest.Add(Resources.CreateManifestEntry(
                    outputResource,
                    mode,
                    hash));
            }

            result.SourceResourceMap.Add(outputIndex);
            result.SourceResourceUris.Add(Resources.GetOutputUri(
                result.Resources.Resources[outputIndex]));
            sourceIndex++;
        }

        return result;
    }

    internal int FindDuplicateOutput(
        ResourceWritePlan plan,
        GltfResource source,
        string hash,
        GltfResourceMode mode)
    {
        int outputIndex = 0;
        while (outputIndex < plan.Resources.Resources.Count)
        {
            if (plan.Manifest[outputIndex].Mode == mode
                && plan.OutputHashes[outputIndex] == hash
                && Resources.AreResourcesCompatible(
                    source,
                    plan.Resources.Resources[outputIndex])
                && Resources.HaveEqualBytes(
                    source.Bytes,
                    plan.Resources.Resources[outputIndex].Bytes))
            {
                return outputIndex;
            }

            outputIndex++;
        }

        return GltfConstants.AbsentIndex;
    }

    internal void SetJsonContainer(GltfDocument document)
    {
        GltfContainerData container = new GltfContainerData();
        container.Format = GltfContainerFormat.Json;
        document.Container = container;
    }

    internal GltfWriteOptions CreateJsonWriteOptions(GltfWriteOptions source)
    {
        GltfWriteOptions result = new GltfWriteOptions();
        result.Format = GltfContainerFormat.Json;
        result.ResourceMode = GltfResourceMode.Preserve;
        result.Indented = source.Indented;
        result.Deterministic = source.Deterministic;
        result.UseContentHashResourceNames =
            source.UseContentHashResourceNames;
        result.PreserveExplicitDefaults = source.PreserveExplicitDefaults;
        result.PreserveUnknownMembers = source.PreserveUnknownMembers;
        result.PreserveUnknownChunks = false;
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
