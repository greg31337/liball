using System.IO;
using System.Collections.Generic;

namespace gltf;

/// <summary>Provides glTF and GLB reading operations.</summary>
public class GltfReader : IGltfReader
{
    internal ContainerDetector Detector;
    internal GlbReader GlbReader;
    internal BoundedStreamReader StreamReader;
    internal GltfJsonReader JsonReader;
    internal GltfSchemaReader SchemaReader;
    internal UriReference UriReference;
    internal IGltfValidator Validator;
    internal CompressionReadProcessor CompressionProcessor;
    internal ImageExtensionReadProcessor ImageProcessor;
    internal RequiredExtensionCapabilityChecker RequiredExtensions;
    internal AssetVersionCapabilityChecker AssetVersions;

    /// <summary>Initializes a glTF reader.</summary>
    public GltfReader()
    {
        Detector = new ContainerDetector();
        GlbReader = new GlbReader();
        StreamReader = new BoundedStreamReader();
        JsonReader = new GltfJsonReader();
        SchemaReader = new GltfSchemaReader();
        UriReference = new UriReference();
        Validator = new GltfValidator();
        CompressionProcessor = new CompressionReadProcessor();
        ImageProcessor = new ImageExtensionReadProcessor();
        RequiredExtensions = new RequiredExtensionCapabilityChecker();
        AssetVersions = new AssetVersionCapabilityChecker();
    }

    /// <summary>Reads a glTF or GLB file.</summary>
    /// <param name="path">The input file path.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The read result.</returns>
    public GltfReadResult ReadFile(string path, GltfReadOptions options)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        ValidateReadOptions(options);
        GltfReadOptions effectiveOptions = CreateFileReadOptions(path, options);
        using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            return ReadStream(stream, effectiveOptions);
        }
    }

    /// <summary>Reads glTF or GLB data from a stream.</summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The read result.</returns>
    public GltfReadResult ReadStream(Stream stream, GltfReadOptions options)
    {
        GltfReadResult result = ReadContainerStream(stream, options);
        return CompleteRead(result, options);
    }

    /// <summary>Reads raw container data from a stream without constructing a document.</summary>
    /// <param name="stream">The input stream.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The read result containing raw container data.</returns>
    public GltfReadResult ReadContainerStream(Stream stream, GltfReadOptions options)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        Stream? ownedStream = null;
        if (!options.LeaveStreamOpen)
        {
            ownedStream = stream;
        }

        using (ownedStream)
        {
            ValidateReadOptions(options);
            byte[] bytes = StreamReader.ReadAllBytes(stream, options.Limits.MaximumInputByteLength);
            return ReadContainerBytes(bytes, options);
        }
    }

    /// <summary>Reads glTF or GLB data from owned bytes.</summary>
    /// <param name="bytes">The input bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The read result.</returns>
    public GltfReadResult ReadBytes(byte[] bytes, GltfReadOptions options)
    {
        GltfReadResult result = ReadContainerBytes(bytes, options);
        return CompleteRead(result, options);
    }

    /// <summary>Reads owned raw container data without constructing a document.</summary>
    /// <param name="bytes">The input bytes.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The read result containing raw container data.</returns>
    public GltfReadResult ReadContainerBytes(byte[] bytes, GltfReadOptions options)
    {
        if (bytes == null)
        {
            throw new System.ArgumentNullException("bytes");
        }

        ValidateReadOptions(options);
        if (bytes.Length > options.Limits.MaximumInputByteLength)
        {
            throw new GltfParseException(
                $"Input length {bytes.Length} exceeds the configured maximum {options.Limits.MaximumInputByteLength} at byte {options.Limits.MaximumInputByteLength}.");
        }

        GltfContainerFormat format = Detector.Detect(bytes, options.Format, null);
        GltfContainerData container;
        if (format == GltfContainerFormat.Glb)
        {
            GlbHeader header = GlbReader.ReadHeader(bytes, options.Limits.MaximumInputByteLength);
            container = GlbReader.ReadChunks(bytes, header, options.PreserveUnknownChunks);
        }
        else
        {
            container = new GltfContainerData();
            container.Format = GltfContainerFormat.Json;
            container.JsonBytes = OwnedBytes.CopyArray(bytes);
            container.OriginalByteLength = bytes.Length;
        }

        if (container.JsonBytes.Length > options.Limits.MaximumJsonByteLength)
        {
            throw new GltfParseException(
                $"JSON content length {container.JsonBytes.Length} exceeds the configured maximum {options.Limits.MaximumJsonByteLength}.");
        }

        GltfReadResult result = new GltfReadResult();
        result.Container = container;
        return result;
    }

    /// <summary>Constructs a document from parsed container data.</summary>
    /// <param name="container">The parsed container data.</param>
    /// <param name="options">The read options.</param>
    /// <returns>The read result.</returns>
    public GltfReadResult ReadContainer(GltfContainerData container, GltfReadOptions options)
    {
        if (container == null)
        {
            throw new System.ArgumentNullException("container");
        }

        ValidateReadOptions(options);
        ValidateContainerLimits(container, options);
        GltfReadResult result = new GltfReadResult();
        result.Container = container;
        return CompleteRead(result, options);
    }

    internal GltfReadResult ReadConversionFile(
        string path,
        GltfReadOptions options)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        ValidateReadOptions(options);
        GltfReadOptions effectiveOptions = CreateFileReadOptions(path, options);
        using (FileStream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read))
        {
            byte[] bytes = StreamReader.ReadAllBytes(
                stream,
                effectiveOptions.Limits.MaximumInputByteLength);
            return ReadConversionBytes(bytes, effectiveOptions);
        }
    }

    internal GltfReadResult ReadConversionBytes(
        byte[] bytes,
        GltfReadOptions options)
    {
        GltfReadOptions parseOptions = CreateConversionParseOptions(options);
        GltfReadResult result = ReadContainerBytes(bytes, parseOptions);
        GltfDocument document = ParseContainerJson(result.Container, parseOptions);
        document.Container = result.Container;
        document.Resources = result.Resources;
        result.Document = document;
        List<GltfValidationIssue> capabilityIssues =
            CreateReadCapabilityIssues(document, options);
        if (capabilityIssues.Count > 0)
        {
            AppendExtensionReadOutputs(result, document, options);
            AppendUnavailableCapabilityIssues(result, capabilityIssues);
            return result;
        }

        GltfReadOptions resolutionOptions =
            CreateConversionResolutionOptions(options);
        GltfResourceSet resources = ResolveDocumentResources(
            document,
            result.Container,
            resolutionOptions);
        document.Resources = resources;
        result.Resources = resources;
        AppendExtensionReadOutputs(result, document, options);
        if (options.Validate)
        {
            GltfValidationOptions validationOptions = CreateValidationOptions(
                options.ValidationOptions,
                options.ExtensionHandlers,
                options.ImageCodecs);
            GltfValidationReport validation = Validator.ValidateDocument(
                document,
                validationOptions);
            if (validation == null)
            {
                throw new GltfValidationException(
                    "The validator returned a null validation report.");
            }

            result.Validation = validation;
            if (!validation.IsValid)
            {
                result.Status = GltfOperationStatus.Invalid;
            }
        }

        return result;
    }

    internal GltfReadOptions CreateConversionParseOptions(
        GltfReadOptions source)
    {
        GltfReadOptions result = CopyConversionReadOptions(source);
        result.ResolveResources = false;
        result.Validate = false;
        result.LeaveStreamOpen = true;
        return result;
    }

    internal GltfReadOptions CreateConversionResolutionOptions(
        GltfReadOptions source)
    {
        GltfReadOptions result = CopyConversionReadOptions(source);
        result.ResolveResources = true;
        result.Validate = false;
        result.LeaveStreamOpen = true;
        return result;
    }

    internal GltfReadOptions CopyConversionReadOptions(GltfReadOptions source)
    {
        ValidateReadOptions(source);
        GltfReadOptions result = new GltfReadOptions();
        result.Format = source.Format;
        result.DuplicateMemberPolicy = source.DuplicateMemberPolicy;
        result.PreserveUnknownMembers = source.PreserveUnknownMembers;
        result.PreserveUnknownChunks = source.PreserveUnknownChunks;
        result.ResolveResources = source.ResolveResources;
        result.Validate = source.Validate;
        result.LeaveStreamOpen = source.LeaveStreamOpen;
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

    internal GltfReadResult CompleteRead(GltfReadResult result, GltfReadOptions options)
    {
        GltfDocument document = ParseContainerJson(result.Container, options);
        document.Container = result.Container;
        document.Resources = result.Resources;
        result.Document = document;
        List<GltfValidationIssue> capabilityIssues =
            CreateReadCapabilityIssues(document, options);
        if (capabilityIssues.Count > 0)
        {
            AppendExtensionReadOutputs(result, document, options);
            AppendUnavailableCapabilityIssues(result, capabilityIssues);
            return result;
        }

        GltfResourceSet resources = ResolveDocumentResources(document, result.Container, options);
        document.Resources = resources;
        result.Resources = resources;
        AppendExtensionReadOutputs(result, document, options);
        CompressionProcessor.ProcessRead(result, options);
        ImageProcessor.ProcessRead(result, options);

        if (options.Validate)
        {
            GltfValidationOptions validationOptions = CreateValidationOptions(
                options.ValidationOptions,
                options.ExtensionHandlers,
                options.ImageCodecs);
            GltfValidationReport validation = Validator.ValidateDocument(document, validationOptions);
            if (validation == null)
            {
                throw new GltfValidationException("The validator returned a null validation report.");
            }

            result.Validation = validation;
            if (!validation.IsValid)
            {
                result.Status = GltfOperationStatus.Invalid;
            }
        }

        return result;
    }

    internal List<GltfValidationIssue> CreateReadCapabilityIssues(
        GltfDocument document,
        GltfReadOptions options)
    {
        List<GltfValidationIssue> result =
            AssetVersions.CheckReadCapabilities(document);
        List<GltfValidationIssue> extensionIssues =
            RequiredExtensions.CheckReadCapabilities(
                document,
                options.ExtensionHandlers,
                options.CompressionCodecs,
                options.ImageCodecs);
        int issueIndex = 0;
        while (issueIndex < extensionIssues.Count)
        {
            result.Add(extensionIssues[issueIndex]);
            issueIndex++;
        }

        return result;
    }

    internal void AppendUnavailableCapabilityIssues(
        GltfReadResult result,
        List<GltfValidationIssue> issues)
    {
        if ((int)result.Status < (int)GltfOperationStatus.Unavailable)
        {
            result.Status = GltfOperationStatus.Unavailable;
        }

        int index = 0;
        while (index < issues.Count)
        {
            result.Diagnostics.Add(issues[index]);
            index++;
        }
    }

    internal void AppendExtensionReadOutputs(
        GltfReadResult result,
        GltfDocument document,
        GltfReadOptions options)
    {
        int diagnosticIndex = 0;
        while (diagnosticIndex < SchemaReader.Values.ExtensionDiagnostics.Count)
        {
            result.Diagnostics.Add(SchemaReader.Values.ExtensionDiagnostics[diagnosticIndex]);
            diagnosticIndex++;
        }

        if ((int)SchemaReader.Values.ExtensionStatus > (int)result.Status)
        {
            result.Status = SchemaReader.Values.ExtensionStatus;
        }

        long totalByteLength = 0;
        int existingIndex = 0;
        while (existingIndex < result.Resources.Resources.Count)
        {
            totalByteLength += result.Resources.Resources[existingIndex].Bytes.Length;
            existingIndex++;
        }

        int resourceIndex = 0;
        while (resourceIndex < SchemaReader.Values.ExtensionResources.Resources.Count)
        {
            GltfResource resource = SchemaReader.Values.ExtensionResources.Resources[resourceIndex];
            totalByteLength = AddResolvedResource(
                result.Resources,
                resource,
                totalByteLength,
                options,
                true);
            resourceIndex++;
        }

        document.Resources = result.Resources;
    }

    internal GltfValidationOptions CreateValidationOptions(
        GltfValidationOptions source,
        System.Collections.Generic.List<IGltfExtensionHandler> handlers,
        System.Collections.Generic.List<IGltfImageCodec> imageCodecs)
    {
        bool needsCopy = false;
        int candidateIndex = 0;
        while (candidateIndex < handlers.Count)
        {
            if (!source.ExtensionHandlers.Contains(handlers[candidateIndex]))
            {
                needsCopy = true;
            }

            candidateIndex++;
        }

        candidateIndex = 0;
        while (candidateIndex < imageCodecs.Count)
        {
            if (!source.ImageCodecs.Contains(imageCodecs[candidateIndex]))
            {
                needsCopy = true;
            }

            candidateIndex++;
        }

        if (!needsCopy)
        {
            return source;
        }

        GltfValidationOptions result = new GltfValidationOptions();
        result.ValidateStructure = source.ValidateStructure;
        result.ValidateSemantics = source.ValidateSemantics;
        result.ValidateResources = source.ValidateResources;
        result.ValidateExtensions = source.ValidateExtensions;
        result.FailFast = source.FailFast;
        result.Strict = source.Strict;
        result.MinimumSeverity = source.MinimumSeverity;
        result.MaximumIssueCount = source.MaximumIssueCount;
        result.ImageCodecs = source.ImageCodecs;
        result.SuppressedIssueCodes = source.SuppressedIssueCodes;
        result.SeverityOverrides = source.SeverityOverrides;
        int sourceIndex = 0;
        while (sourceIndex < source.ExtensionHandlers.Count)
        {
            result.ExtensionHandlers.Add(source.ExtensionHandlers[sourceIndex]);
            sourceIndex++;
        }

        int handlerIndex = 0;
        while (handlerIndex < handlers.Count)
        {
            IGltfExtensionHandler handler = handlers[handlerIndex];
            if (!result.ExtensionHandlers.Contains(handler))
            {
                result.ExtensionHandlers.Add(handler);
            }

            handlerIndex++;
        }

        int codecIndex = 0;
        while (codecIndex < imageCodecs.Count)
        {
            IGltfImageCodec codec = imageCodecs[codecIndex];
            if (!result.ImageCodecs.Contains(codec))
            {
                result.ImageCodecs.Add(codec);
            }

            codecIndex++;
        }

        return result;
    }

    internal GltfResourceSet ResolveDocumentResources(
        GltfDocument document,
        GltfContainerData? container,
        GltfReadOptions options)
    {
        GltfResourceSet result = new GltfResourceSet();
        long resolvedResourceCount = (long)document.Buffers.Count + document.Images.Count;
        if (options.ResolveResources
            && resolvedResourceCount > options.Limits.MaximumCollectionCount)
        {
            throw new GltfResourceException(
                $"Resolved resource count {resolvedResourceCount} exceeds the configured maximum {options.Limits.MaximumCollectionCount}.");
        }

        ResourceResolverDispatcher dispatcher = new ResourceResolverDispatcher();
        dispatcher.Resolver = options.ResourceResolver;
        long totalByteLength = 0;
        int bufferIndex = 0;
        while (bufferIndex < document.Buffers.Count)
        {
            GltfBuffer buffer = document.Buffers[bufferIndex];
            ValidateDeclaredBufferLength(
                buffer,
                bufferIndex,
                options.Limits.MaximumResourceByteLength,
                !options.ResolveResources);
            if (bufferIndex == 0
                && buffer.Uri == null
                && container != null
                && container.Format == GltfContainerFormat.Glb)
            {
                totalByteLength = BindGlbBuffer(
                    buffer,
                    bufferIndex,
                    container,
                    result,
                    totalByteLength,
                    options);
            }
            else if (buffer.Uri != null)
            {
                GltfResourceRequest request = new GltfResourceRequest();
                request.BaseUri = options.BaseUri;
                request.SourceUri = buffer.Uri;
                request.Kind = GltfResourceKind.Buffer;
                request.HasDeclaredByteLength = true;
                request.DeclaredByteLength = buffer.ByteLength;
                request.MaximumByteLength = GetResourceByteLimit(buffer.Uri, options);
                request.Policy = CopyResourcePolicy(options.ResourcePolicy);
                if (options.ResolveResources)
                {
                    ResourceDispatchData dispatched = dispatcher.DispatchResource(request);
                    ValidateExternalBufferResource(dispatched.Resource, bufferIndex, buffer.ByteLength);
                    totalByteLength = AddResolvedResource(
                        result,
                        dispatched.Resource,
                        totalByteLength,
                        options,
                        dispatched.NewContent);
                    BindResolvedBuffer(
                        buffer,
                        dispatched.Resource,
                        result.Resources.Count - 1);
                }
                else
                {
                    buffer.PendingResourceRequest = request;
                }
            }
            else if (options.ResolveResources
                && !CompressionProcessor.IsMeshoptFallbackBuffer(document, bufferIndex))
            {
                throw new GltfResourceException(
                    $"Buffer {bufferIndex} has no URI and is not bound to a GLB BIN chunk.");
            }

            bufferIndex++;
        }

        int imageIndex = 0;
        while (imageIndex < document.Images.Count)
        {
            GltfImage image = document.Images[imageIndex];
            ValidateImageSource(
                document,
                image,
                imageIndex,
                options.Limits.MaximumResourceByteLength,
                !options.ResolveResources);
            if (image.Uri != null)
            {
                GltfResourceRequest request = new GltfResourceRequest();
                request.BaseUri = options.BaseUri;
                request.SourceUri = image.Uri;
                request.Kind = GltfResourceKind.Image;
                request.DeclaredMimeType = image.MimeType;
                request.MaximumByteLength = GetResourceByteLimit(image.Uri, options);
                request.Policy = CopyResourcePolicy(options.ResourcePolicy);
                if (options.ResolveResources)
                {
                    ResourceDispatchData dispatched = dispatcher.DispatchResource(request);
                    ValidateUriImageResource(dispatched.Resource, image, imageIndex);
                    totalByteLength = AddResolvedResource(
                        result,
                        dispatched.Resource,
                        totalByteLength,
                        options,
                        dispatched.NewContent);
                    BindResolvedImage(
                        image,
                        dispatched.Resource,
                        result.Resources.Count - 1);
                }
                else
                {
                    image.PendingResourceRequest = request;
                }
            }
            else if (options.ResolveResources)
            {
                GltfResource resource = ResolveBufferViewImage(document, image, imageIndex);
                bool newContent = resource.CacheKey == null
                    || !result.Cache.ContainsKey(resource.CacheKey);
                totalByteLength = AddResolvedResource(
                    result,
                    resource,
                    totalByteLength,
                    options,
                    newContent);
                BindResolvedImage(image, resource, result.Resources.Count - 1);
            }

            imageIndex++;
        }

        return result;
    }

    internal void ValidateImageSource(
        GltfDocument document,
        GltfImage image,
        int imageIndex,
        int maximumResourceByteLength,
        bool validateMaximum)
    {
        if (image.Uri != null && image.HasBufferView)
        {
            throw new GltfResourceException(
                $"Image {imageIndex} defines both URI and bufferView sources.");
        }

        if (image.Uri == null && !image.HasBufferView)
        {
            throw new GltfResourceException(
                $"Image {imageIndex} does not define a URI or bufferView source.");
        }

        if (image.MimeType != null && !IsImageMimeType(image.MimeType))
        {
            throw new GltfResourceException(
                $"Image {imageIndex} media type '{image.MimeType}' is not an image media type.");
        }

        if (image.HasBufferView)
        {
            if (image.MimeType == null)
            {
                throw new GltfResourceException(
                    $"Image {imageIndex} uses a bufferView and must declare a media type.");
            }

            if (image.BufferView < 0 || image.BufferView >= document.BufferViews.Count)
            {
                throw new GltfResourceException(
                    $"Image {imageIndex} references bufferView {image.BufferView} outside the document bufferView collection.");
            }

            int byteLength = document.BufferViews[image.BufferView].ByteLength;
            if (validateMaximum && byteLength > maximumResourceByteLength)
            {
                throw new GltfResourceException(
                    $"Image {imageIndex} bufferView {image.BufferView} byteLength {byteLength} exceeds the configured maximum resource length {maximumResourceByteLength}.");
            }
        }
    }

    internal int GetResourceByteLimit(string uri, GltfReadOptions options)
    {
        int result = options.Limits.MaximumResourceByteLength;
        if (UriReference.ClassifyReference(uri) == GltfUriKind.Data
            && options.Limits.MaximumDataUriByteLength < result)
        {
            result = options.Limits.MaximumDataUriByteLength;
        }

        return result;
    }

    internal void ValidateUriImageResource(
        GltfResource resource,
        GltfImage image,
        int imageIndex)
    {
        if (resource.Status != GltfOperationStatus.Success)
        {
            return;
        }

        string? sourceMimeType = resource.SourceMimeType;
        if (sourceMimeType == null)
        {
            sourceMimeType = resource.MimeType;
        }

        if (sourceMimeType != null && !IsImageMimeType(sourceMimeType))
        {
            throw new GltfResourceException(
                $"Image {imageIndex} resolved media type '{sourceMimeType}' is not an image media type.");
        }

        if (resource.UriKind == GltfUriKind.Data && sourceMimeType == null)
        {
            throw new GltfResourceException(
                $"Image {imageIndex} data URI does not declare an image media type.");
        }

        if (image.MimeType != null
            && resource.SourceMimeType != null
            && !string.Equals(
                image.MimeType,
                resource.SourceMimeType,
                System.StringComparison.OrdinalIgnoreCase))
        {
            throw new GltfResourceException(
                $"Image {imageIndex} declares media type '{image.MimeType}' but its URI source declares '{resource.SourceMimeType}'.");
        }
    }

    internal GltfResource ResolveBufferViewImage(
        GltfDocument document,
        GltfImage image,
        int imageIndex)
    {
        GltfBufferView bufferView = document.BufferViews[image.BufferView];
        if (bufferView.Buffer < 0 || bufferView.Buffer >= document.Buffers.Count)
        {
            throw new GltfResourceException(
                $"Image {imageIndex} bufferView {image.BufferView} references buffer {bufferView.Buffer} outside the document buffer collection.");
        }

        if (bufferView.ByteOffset < 0 || bufferView.ByteLength < 0)
        {
            throw new GltfResourceException(
                $"Image {imageIndex} bufferView {image.BufferView} has a negative byte range.");
        }

        GltfBuffer buffer = document.Buffers[bufferView.Buffer];
        if (!buffer.Resolved)
        {
            throw new GltfResourceException(
                $"Image {imageIndex} bufferView {image.BufferView} requires unresolved buffer {bufferView.Buffer}.");
        }

        if (bufferView.ByteLength > buffer.ResolvedBytes.Length
            || bufferView.ByteOffset > buffer.ResolvedBytes.Length - bufferView.ByteLength)
        {
            throw new GltfResourceException(
                $"Image {imageIndex} bufferView {image.BufferView} range exceeds resolved buffer {bufferView.Buffer} length {buffer.ResolvedBytes.Length}.");
        }

        GltfResource result = new GltfResource();
        result.Kind = GltfResourceKind.Image;
        result.UriKind = GltfUriKind.Absent;
        result.EffectiveUri = "bufferView:" + image.BufferView;
        result.MimeType = image.MimeType;
        result.DeclaredMimeType = image.MimeType;
        result.SourceMimeType = image.MimeType;
        result.Bytes = OwnedBytes.CopyRange(
            buffer.ResolvedBytes,
            bufferView.ByteOffset,
            bufferView.ByteLength);
        result.CacheKey = result.EffectiveUri;
        result.HasDeclaredByteLength = true;
        result.DeclaredByteLength = bufferView.ByteLength;
        result.Embedded = true;
        return result;
    }

    internal void BindResolvedImage(
        GltfImage image,
        GltfResource resource,
        int resourceIndex)
    {
        image.ResolvedBytes = OwnedBytes.CopyArray(resource.Bytes);
        image.Resolved = true;
        image.HasResource = true;
        image.Resource = resourceIndex;
        image.EffectiveUri = resource.EffectiveUri;
        image.ResolvedMimeType = resource.SourceMimeType;
        if (image.ResolvedMimeType == null)
        {
            image.ResolvedMimeType = resource.MimeType;
        }

        image.PendingResourceRequest = null;
    }

    internal bool IsImageMimeType(string mimeType)
    {
        return mimeType.Length > 6
            && mimeType.StartsWith("image/", System.StringComparison.OrdinalIgnoreCase);
    }

    internal long BindGlbBuffer(
        GltfBuffer buffer,
        int bufferIndex,
        GltfContainerData container,
        GltfResourceSet resources,
        long currentTotalByteLength,
        GltfReadOptions options)
    {
        if (!container.HasBinChunk)
        {
            throw new GltfResourceException(
                $"GLB buffer {bufferIndex} has no URI and requires a BIN chunk.");
        }

        if (container.BinBytes == null)
        {
            throw new GltfResourceException("The GLB BIN chunk bytes are null.");
        }

        if (container.BinBytes.Length < buffer.ByteLength)
        {
            throw new GltfResourceException(
                $"GLB buffer {bufferIndex} declares byteLength {buffer.ByteLength} but the BIN chunk contains {container.BinBytes.Length} bytes.");
        }

        int paddingByteLength = container.BinBytes.Length - buffer.ByteLength;
        if (paddingByteLength > GltfConstants.MaximumPaddingByteLength)
        {
            throw new GltfResourceException(
                $"GLB BIN chunk length {container.BinBytes.Length} exceeds buffer {bufferIndex} declared byteLength {buffer.ByteLength} by more than {GltfConstants.MaximumPaddingByteLength} padding bytes.");
        }

        int paddingIndex = buffer.ByteLength;
        while (paddingIndex < container.BinBytes.Length)
        {
            if (container.BinBytes[paddingIndex] != GltfConstants.GlbBinPaddingByte)
            {
                throw new GltfParseException(
                    $"Invalid GLB BIN padding byte at BIN chunk offset {paddingIndex}; expected 0x00.");
            }

            paddingIndex++;
        }

        GltfResource binaryResource = new GltfResource();
        binaryResource.Kind = GltfResourceKind.Buffer;
        binaryResource.UriKind = GltfUriKind.Absent;
        binaryResource.MimeType = GltfMimeTypes.GltfBuffer;
        binaryResource.Bytes = OwnedBytes.CopyArray(container.BinBytes);
        binaryResource.Embedded = true;
        binaryResource.CacheKey = "glb:bin";
        binaryResource.HasDeclaredByteLength = true;
        binaryResource.DeclaredByteLength = buffer.ByteLength;
        long totalByteLength = AddResolvedResource(
            resources,
            binaryResource,
            currentTotalByteLength,
            options,
            true);
        BindResolvedBuffer(buffer, binaryResource, resources.Resources.Count - 1);
        return totalByteLength;
    }

    internal void ValidateDeclaredBufferLength(
        GltfBuffer buffer,
        int bufferIndex,
        int maximumResourceByteLength,
        bool validateMaximum)
    {
        if (buffer.ByteLength < 0)
        {
            throw new GltfResourceException(
                $"Buffer {bufferIndex} declares a negative byteLength {buffer.ByteLength}.");
        }

        if (validateMaximum && buffer.ByteLength > maximumResourceByteLength)
        {
            throw new GltfResourceException(
                $"Buffer {bufferIndex} declared byteLength {buffer.ByteLength} exceeds the configured maximum resource length {maximumResourceByteLength}.");
        }
    }

    internal void ValidateExternalBufferResource(
        GltfResource resource,
        int bufferIndex,
        int declaredByteLength)
    {
        if (resource.Status != GltfOperationStatus.Success)
        {
            return;
        }

        if (resource.Bytes == null)
        {
            throw new GltfResourceException("The resource resolver returned null resource bytes.");
        }

        if (resource.Bytes.Length < declaredByteLength)
        {
            throw new GltfResourceException(
                $"Buffer {bufferIndex} declares byteLength {declaredByteLength} but its resolved resource contains {resource.Bytes.Length} bytes.");
        }

        if (resource.UriKind == GltfUriKind.Data
            && !string.Equals(resource.MimeType, GltfMimeTypes.OctetStream, System.StringComparison.OrdinalIgnoreCase)
            && !string.Equals(resource.MimeType, GltfMimeTypes.GltfBuffer, System.StringComparison.OrdinalIgnoreCase))
        {
            throw new GltfResourceException(
                $"Buffer {bufferIndex} data URI media type '{resource.MimeType}' is not a supported glTF buffer media type.");
        }
    }

    internal void BindResolvedBuffer(
        GltfBuffer buffer,
        GltfResource resource,
        int resourceIndex)
    {
        buffer.ResolvedBytes = OwnedBytes.CopyRange(
            resource.Bytes,
            0,
            buffer.ByteLength);
        buffer.Resolved = true;
        buffer.HasResource = true;
        buffer.Resource = resourceIndex;
        buffer.PendingResourceRequest = null;
    }

    internal long AddResolvedResource(
        GltfResourceSet resources,
        GltfResource resource,
        long currentTotalByteLength,
        GltfReadOptions options,
        bool addByteLength)
    {
        if (resource == null)
        {
            throw new GltfResourceException("The resource resolver returned a null resource.");
        }

        if (resource.Status != GltfOperationStatus.Success)
        {
            throw new GltfResourceException(
                $"Resource resolution failed for '{resource.SourceUri}' with status {resource.Status}.");
        }

        if (resource.Bytes == null)
        {
            throw new GltfResourceException("The resource resolver returned null resource bytes.");
        }

        if (resource.Bytes.Length > options.Limits.MaximumResourceByteLength)
        {
            throw new GltfResourceException(
                $"Resolved resource length {resource.Bytes.Length} exceeds the configured maximum {options.Limits.MaximumResourceByteLength}.");
        }

        long newTotalByteLength = currentTotalByteLength;
        if (addByteLength)
        {
            newTotalByteLength += resource.Bytes.Length;
        }
        if (newTotalByteLength > options.Limits.MaximumTotalResourceByteLength)
        {
            throw new GltfResourceException(
                $"Total resolved resource length {newTotalByteLength} exceeds the configured maximum {options.Limits.MaximumTotalResourceByteLength}.");
        }

        int newResourceCount = resources.Resources.Count + 1;
        if (newResourceCount > options.Limits.MaximumCollectionCount)
        {
            throw new GltfResourceException(
                $"Resolved resource count {newResourceCount} exceeds the configured maximum {options.Limits.MaximumCollectionCount}.");
        }

        GltfResource ownedResource = CopyResource(resource);
        resources.Resources.Add(ownedResource);
        if (ownedResource.CacheKey != null && !resources.Cache.ContainsKey(ownedResource.CacheKey))
        {
            resources.Cache[ownedResource.CacheKey] = ownedResource;
        }

        return newTotalByteLength;
    }

    internal GltfResource CopyResource(GltfResource source)
    {
        GltfResource result = new GltfResource();
        result.Status = source.Status;
        result.Kind = source.Kind;
        result.UriKind = source.UriKind;
        result.SourceUri = source.SourceUri;
        result.EffectiveUri = source.EffectiveUri;
        result.MimeType = source.MimeType;
        result.DeclaredMimeType = source.DeclaredMimeType;
        result.SourceMimeType = source.SourceMimeType;
        result.ImageFormat = source.ImageFormat;
        result.Bytes = OwnedBytes.CopyArray(source.Bytes);
        result.CacheKey = source.CacheKey;
        result.HasDeclaredByteLength = source.HasDeclaredByteLength;
        result.DeclaredByteLength = source.DeclaredByteLength;
        result.Embedded = source.Embedded;
        return result;
    }

    internal GltfReadOptions CreateFileReadOptions(string path, GltfReadOptions source)
    {
        GltfReadOptions result = new GltfReadOptions();
        result.Format = source.Format;
        result.DuplicateMemberPolicy = source.DuplicateMemberPolicy;
        result.PreserveUnknownMembers = source.PreserveUnknownMembers;
        result.PreserveUnknownChunks = source.PreserveUnknownChunks;
        result.ResolveResources = source.ResolveResources;
        result.Validate = source.Validate;
        result.LeaveStreamOpen = source.LeaveStreamOpen;
        result.BaseUri = source.BaseUri;
        if (result.BaseUri == null)
        {
            result.BaseUri = new System.Uri(Path.GetFullPath(path)).AbsoluteUri;
        }

        result.Limits = source.Limits;
        result.ResourceResolver = source.ResourceResolver;
        result.ResourcePolicy = source.ResourcePolicy;
        result.ExtensionHandlers = source.ExtensionHandlers;
        result.CompressionCodecs = source.CompressionCodecs;
        result.ImageCodecs = source.ImageCodecs;
        result.ValidationOptions = source.ValidationOptions;
        return result;
    }

    internal GltfResourcePolicy CopyResourcePolicy(GltfResourcePolicy source)
    {
        if (source == null)
        {
            throw new System.ArgumentException("The resource policy must not be null.", "options");
        }

        GltfResourcePolicy result = new GltfResourcePolicy();
        result.AllowAbsolutePaths = source.AllowAbsolutePaths;
        result.AllowParentPathSegments = source.AllowParentPathSegments;
        int rootIndex = 0;
        while (rootIndex < source.FileRoots.Count)
        {
            result.FileRoots.Add(source.FileRoots[rootIndex]);
            rootIndex++;
        }

        return result;
    }

    internal void ValidateContainerLimits(GltfContainerData container, GltfReadOptions options)
    {
        if (container.JsonBytes == null)
        {
            throw new GltfParseException("The parsed container JSON bytes are null.");
        }

        if (container.JsonBytes.Length > options.Limits.MaximumJsonByteLength)
        {
            throw new GltfParseException(
                $"JSON content length {container.JsonBytes.Length} exceeds the configured maximum {options.Limits.MaximumJsonByteLength}.");
        }

        long storedByteLength = GetStoredContainerByteLength(container);
        long inputByteLength = container.OriginalByteLength;
        if (storedByteLength > inputByteLength)
        {
            inputByteLength = storedByteLength;
        }

        if (inputByteLength > options.Limits.MaximumInputByteLength)
        {
            throw new GltfParseException(
                $"Input length {inputByteLength} exceeds the configured maximum {options.Limits.MaximumInputByteLength} at byte {options.Limits.MaximumInputByteLength}.");
        }
    }

    internal long GetStoredContainerByteLength(GltfContainerData container)
    {
        if (container.BinBytes == null
            || container.TrailingBytes == null
            || container.UnknownChunks == null)
        {
            throw new GltfParseException("The parsed container byte collections must not be null.");
        }

        bool glbLayout = container.Format == GltfContainerFormat.Glb
            || container.HasBinChunk
            || container.UnknownChunks.Count > 0;
        long result = container.JsonBytes.Length;
        result += container.BinBytes.Length;
        result += container.TrailingBytes.Length;
        if (glbLayout)
        {
            result += 20L;
            if (container.HasBinChunk)
            {
                result += 8L;
            }
        }

        int chunkIndex = 0;
        while (chunkIndex < container.UnknownChunks.Count)
        {
            GltfGlbChunk chunk = container.UnknownChunks[chunkIndex];
            if (chunk == null || chunk.Data == null)
            {
                throw new GltfParseException("The parsed container contains a null unknown chunk or chunk payload.");
            }

            result += chunk.Data.Length;
            if (glbLayout)
            {
                result += 8L;
            }

            chunkIndex++;
        }

        return result;
    }

    internal GltfDocument ParseContainerJson(GltfContainerData? container, GltfReadOptions options)
    {
        if (container == null)
        {
            throw new GltfParseException("The parsed container does not contain JSON data.");
        }

        GltfJsonReadResult json = JsonReader.Read(
            container.JsonBytes,
            options.Limits,
            options.DuplicateMemberPolicy);
        return SchemaReader.ReadDocument(
            json,
            options.Limits,
            options.PreserveUnknownMembers,
            options.ExtensionHandlers);
    }

    internal void ValidateReadOptions(GltfReadOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (options.Limits == null)
        {
            throw new System.ArgumentException("The read limits must not be null.", "options");
        }

        if (options.ResourcePolicy == null || options.ResourcePolicy.FileRoots == null)
        {
            throw new System.ArgumentException("The resource policy and file roots must not be null.", "options");
        }

        if (options.Limits.MaximumInputByteLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        if (options.Limits.MaximumJsonByteLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        if (options.Limits.MaximumJsonDepth < 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        if (options.Limits.MaximumJsonTokenCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        if (options.Limits.MaximumJsonStringByteLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        if (options.Limits.MaximumJsonNumberByteLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        if (options.Limits.MaximumCollectionCount < 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        if (options.Limits.MaximumDataUriByteLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        if (options.Limits.MaximumResourceByteLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        if (options.Limits.MaximumTotalResourceByteLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        if (options.ValidationOptions == null)
        {
            throw new System.ArgumentException("The validation options must not be null.", "options");
        }

        if (options.DuplicateMemberPolicy != GltfDuplicateMemberPolicy.Reject
            && options.DuplicateMemberPolicy != GltfDuplicateMemberPolicy.KeepFirst
            && options.DuplicateMemberPolicy != GltfDuplicateMemberPolicy.KeepLast)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }
    }
}
