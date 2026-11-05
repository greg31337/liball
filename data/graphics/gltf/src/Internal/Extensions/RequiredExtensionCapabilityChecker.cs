using System;
using System.Collections.Generic;

namespace gltf;

internal enum RequiredExtensionOperation
{
    Read = 0,
    Evaluation = 1,
    Conversion = 2,
    Mutation = 3
}

internal class RequiredExtensionCapabilityChecker
{
    internal ExtensionRegistry Registry = new ExtensionRegistry();

    internal List<GltfValidationIssue> CheckReadCapabilities(
        GltfDocument document,
        List<IGltfExtensionHandler> handlers,
        List<IGltfCompressionCodec> compressionCodecs,
        List<IGltfImageCodec> imageCodecs)
    {
        return CheckRequiredExtensions(
            document,
            handlers,
            compressionCodecs,
            imageCodecs,
            RequiredExtensionOperation.Read);
    }

    internal List<GltfValidationIssue> CheckEvaluationCapabilities(GltfDocument document)
    {
        return CheckRequiredExtensions(
            document,
            new List<IGltfExtensionHandler>(),
            new List<IGltfCompressionCodec>(),
            new List<IGltfImageCodec>(),
            RequiredExtensionOperation.Evaluation);
    }

    internal List<GltfValidationIssue> CheckConversionCapabilities(
        GltfDocument document,
        GltfConversionOptions options)
    {
        List<IGltfExtensionHandler> handlers = new List<IGltfExtensionHandler>();
        List<IGltfCompressionCodec> compressionCodecs =
            new List<IGltfCompressionCodec>();
        List<IGltfImageCodec> imageCodecs = new List<IGltfImageCodec>();
        if (options != null)
        {
            AddReadCapabilities(options.ReadOptions, handlers, compressionCodecs, imageCodecs);
            AddWriteCapabilities(options.WriteOptions, handlers, compressionCodecs, imageCodecs);
            AddTransformCapabilities(
                options.TransformOptions,
                handlers,
                compressionCodecs,
                imageCodecs);
        }

        List<GltfValidationIssue> result = CheckRequiredExtensions(
            document,
            handlers,
            compressionCodecs,
            imageCodecs,
            RequiredExtensionOperation.Conversion);
        if (result.Count == 0
            && options != null
            && options.TransformPlan != null
            && options.TransformOptions != null
            && options.TransformOptions.RequireSafeUnknownExtensions)
        {
            result = CheckRequiredExtensions(
                document,
                handlers,
                compressionCodecs,
                imageCodecs,
                RequiredExtensionOperation.Mutation);
        }

        return result;
    }

    internal List<GltfValidationIssue> CheckMutationCapabilities(GltfDocument document)
    {
        return CheckRequiredExtensions(
            document,
            new List<IGltfExtensionHandler>(),
            new List<IGltfCompressionCodec>(),
            new List<IGltfImageCodec>(),
            RequiredExtensionOperation.Mutation);
    }

    internal List<GltfValidationIssue> CheckTransformCapabilities(
        GltfDocument document,
        GltfTransformOptions options)
    {
        List<IGltfExtensionHandler> handlers = new List<IGltfExtensionHandler>();
        List<IGltfCompressionCodec> compressionCodecs =
            new List<IGltfCompressionCodec>();
        List<IGltfImageCodec> imageCodecs = new List<IGltfImageCodec>();
        AddTransformCapabilities(options, handlers, compressionCodecs, imageCodecs);
        return CheckRequiredExtensions(
            document,
            handlers,
            compressionCodecs,
            imageCodecs,
            RequiredExtensionOperation.Mutation);
    }

    internal List<GltfValidationIssue> CheckRequiredExtensions(
        GltfDocument document,
        List<IGltfExtensionHandler> handlers,
        List<IGltfCompressionCodec> compressionCodecs,
        List<IGltfImageCodec> imageCodecs,
        RequiredExtensionOperation operation)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (handlers == null)
        {
            throw new ArgumentNullException("handlers");
        }

        if (compressionCodecs == null)
        {
            throw new ArgumentNullException("compressionCodecs");
        }

        if (imageCodecs == null)
        {
            throw new ArgumentNullException("imageCodecs");
        }

        List<GltfValidationIssue> result = new List<GltfValidationIssue>();
        int index = 0;
        while (document.ExtensionsRequired != null
            && index < document.ExtensionsRequired.Count)
        {
            string name = document.ExtensionsRequired[index];
            GltfExtensionDescriptor? descriptor = FindDescriptor(name, handlers);
            string pointer = $"/extensionsRequired/{index}";
            if (descriptor == null)
            {
                result.Add(CreateIssue(
                    GltfIssueCodes.RequiredExtensionMetadataUnavailable,
                    $"Required extension '{name}' has no registered metadata support.",
                    pointer));
            }
            else if (RequiresSemantics(operation) && !descriptor.HasSemanticValidation)
            {
                result.Add(CreateIssue(
                    GltfIssueCodes.RequiredExtensionSemanticsUnavailable,
                    $"Required extension '{name}' has no semantic implementation for this operation.",
                    pointer));
            }
            else if (RequiresResourceDecode(operation)
                && descriptor.RequiredCodec != null
                && !HasCodec(descriptor.RequiredCodec, compressionCodecs, imageCodecs))
            {
                result.Add(CreateIssue(
                    GltfIssueCodes.RequiredExtensionResourceDecodeUnavailable,
                    $"Required extension '{name}' needs unavailable codec '{descriptor.RequiredCodec}'.",
                    pointer));
            }
            else if (operation == RequiredExtensionOperation.Mutation
                && !descriptor.CanRemapReferences)
            {
                result.Add(CreateIssue(
                    GltfIssueCodes.RequiredExtensionRemapUnavailable,
                    $"Required extension '{name}' cannot safely remap index references.",
                    pointer));
            }

            index++;
        }

        return result;
    }

    internal GltfExtensionDescriptor? FindDescriptor(
        string name,
        List<IGltfExtensionHandler> handlers)
    {
        int index = 0;
        while (index < handlers.Count)
        {
            IGltfExtensionHandler handler = handlers[index];
            if (handler == null)
            {
                throw new ArgumentException(
                    $"Extension handler {index} must not be null.",
                    "handlers");
            }

            GltfExtensionDescriptor? descriptor = handler.DescribeExtension();
            if (descriptor == null)
            {
                throw new GltfUnsupportedFeatureException(
                    $"Extension handler {index} returned a null descriptor.");
            }

            if (string.Equals(descriptor.Name, name, StringComparison.Ordinal))
            {
                return descriptor;
            }

            index++;
        }

        return Registry.FindDescriptor(name);
    }

    internal bool RequiresSemantics(RequiredExtensionOperation operation)
    {
        return operation == RequiredExtensionOperation.Read
            || operation == RequiredExtensionOperation.Evaluation
            || operation == RequiredExtensionOperation.Conversion;
    }

    internal bool RequiresResourceDecode(RequiredExtensionOperation operation)
    {
        return operation == RequiredExtensionOperation.Read
            || operation == RequiredExtensionOperation.Conversion;
    }

    internal bool HasCodec(
        string codec,
        List<IGltfCompressionCodec> compressionCodecs,
        List<IGltfImageCodec> imageCodecs)
    {
        if (codec == GltfExtensionNames.KhrDracoMeshCompression
            || codec == GltfExtensionNames.KhrMeshoptCompression
            || codec == GltfExtensionNames.ExtMeshoptCompression)
        {
            return compressionCodecs.Count > 0;
        }

        if (codec == GltfExtensionNames.ExtTextureAstc
            || codec == GltfExtensionNames.ExtTextureWebp
            || codec == GltfExtensionNames.KhrTextureBasisu
            || codec == GltfExtensionNames.KhrTextureVideo
            || codec == GltfExtensionNames.MpegTextureVideo
            || codec == GltfExtensionNames.MsftTextureDds)
        {
            return imageCodecs.Count > 0;
        }

        return compressionCodecs.Count > 0 || imageCodecs.Count > 0;
    }

    internal void AddReadCapabilities(
        GltfReadOptions options,
        List<IGltfExtensionHandler> handlers,
        List<IGltfCompressionCodec> compressionCodecs,
        List<IGltfImageCodec> imageCodecs)
    {
        if (options == null)
        {
            return;
        }

        AddHandlers(options.ExtensionHandlers, handlers);
        AddCompressionCodecs(options.CompressionCodecs, compressionCodecs);
        AddImageCodecs(options.ImageCodecs, imageCodecs);
    }

    internal void AddWriteCapabilities(
        GltfWriteOptions options,
        List<IGltfExtensionHandler> handlers,
        List<IGltfCompressionCodec> compressionCodecs,
        List<IGltfImageCodec> imageCodecs)
    {
        if (options == null)
        {
            return;
        }

        AddHandlers(options.ExtensionHandlers, handlers);
        AddCompressionCodecs(options.CompressionCodecs, compressionCodecs);
        AddImageCodecs(options.ImageCodecs, imageCodecs);
    }

    internal void AddTransformCapabilities(
        GltfTransformOptions options,
        List<IGltfExtensionHandler> handlers,
        List<IGltfCompressionCodec> compressionCodecs,
        List<IGltfImageCodec> imageCodecs)
    {
        if (options == null)
        {
            return;
        }

        if (options.ValidationOptions != null)
        {
            AddHandlers(options.ValidationOptions.ExtensionHandlers, handlers);
        }

        AddCompressionCodecs(options.CompressionCodecs, compressionCodecs);
        AddImageCodecs(options.ImageCodecs, imageCodecs);
    }

    internal void AddHandlers(
        List<IGltfExtensionHandler> source,
        List<IGltfExtensionHandler> target)
    {
        int index = 0;
        while (source != null && index < source.Count)
        {
            if (!target.Contains(source[index]))
            {
                target.Add(source[index]);
            }

            index++;
        }
    }

    internal void AddCompressionCodecs(
        List<IGltfCompressionCodec> source,
        List<IGltfCompressionCodec> target)
    {
        int index = 0;
        while (source != null && index < source.Count)
        {
            if (!target.Contains(source[index]))
            {
                target.Add(source[index]);
            }

            index++;
        }
    }

    internal void AddImageCodecs(
        List<IGltfImageCodec> source,
        List<IGltfImageCodec> target)
    {
        int index = 0;
        while (source != null && index < source.Count)
        {
            if (!target.Contains(source[index]))
            {
                target.Add(source[index]);
            }

            index++;
        }
    }

    internal GltfValidationIssue CreateIssue(string code, string message, string pointer)
    {
        GltfValidationIssue result = new GltfValidationIssue();
        result.Code = code;
        result.Severity = GltfValidationSeverity.Error;
        result.Message = message;
        result.JsonPointer = pointer;
        return result;
    }
}
