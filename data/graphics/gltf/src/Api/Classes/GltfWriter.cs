using System.IO;

namespace gltf;

/// <summary>Provides deterministic glTF and GLB writing operations.</summary>
public class GltfWriter : IGltfWriter
{
    internal GlbWriter GlbWriter;
    internal GltfJsonWriter JsonWriter;
    internal GltfWriterPreflight Preflight;
    internal ResourcePlanner ResourcePlanner;
    internal IGltfValidator Validator;
    internal ValidationIssueAggregator IssueAggregator;

    /// <summary>Initializes a glTF writer.</summary>
    public GltfWriter()
    {
        GlbWriter = new GlbWriter();
        JsonWriter = new GltfJsonWriter();
        Preflight = new GltfWriterPreflight();
        ResourcePlanner = new ResourcePlanner();
        Validator = new GltfValidator();
        IssueAggregator = new ValidationIssueAggregator();
    }

    /// <summary>Writes a document to a file.</summary>
    /// <param name="document">The document to write.</param>
    /// <param name="path">The output file path.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write result.</returns>
    public GltfWriteResult WriteFile(GltfDocument document, string path, GltfWriteOptions options)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        ValidateWriteInputs(document, options);
        GltfWriteOptions effectiveOptions = CreateFileWriteOptions(document, path, options);
        GltfWriteResult result = WriteBytes(document, effectiveOptions);
        if (result.Status == GltfOperationStatus.Success)
        {
            File.WriteAllBytes(path, result.PrimaryBytes);
            result.PrimaryBytes = [];
        }

        return result;
    }

    /// <summary>Writes a document to a stream.</summary>
    /// <param name="document">The document to write.</param>
    /// <param name="stream">The output stream.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write result.</returns>
    public GltfWriteResult WriteStream(GltfDocument document, Stream stream, GltfWriteOptions options)
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
            if (!stream.CanWrite)
            {
                throw new System.ArgumentException("The output stream must be writable.", "stream");
            }

            GltfWriteResult result = WriteBytes(document, options);
            if (result.Status == GltfOperationStatus.Success)
            {
                stream.Write(result.PrimaryBytes, 0, result.PrimaryBytes.Length);
                result.PrimaryBytes = [];
            }

            return result;
        }
    }

    /// <summary>Writes raw container data to a stream without serializing a document.</summary>
    /// <param name="container">The raw container data to write.</param>
    /// <param name="stream">The output stream.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write result.</returns>
    public GltfWriteResult WriteContainerStream(
        GltfContainerData container,
        Stream stream,
        GltfWriteOptions options)
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
            if (container == null)
            {
                throw new System.ArgumentNullException("container");
            }

            if (!stream.CanWrite)
            {
                throw new System.ArgumentException("The output stream must be writable.", "stream");
            }

            GltfContainerFormat format = ResolveContainerOutputFormat(container, options);
            if (format == GltfContainerFormat.Glb)
            {
                GlbWriter.WriteStream(container, stream, options.PreserveUnknownChunks);
            }
            else
            {
                byte[] jsonBytes = GetJsonContainerBytes(container);
                stream.Write(jsonBytes, 0, jsonBytes.Length);
            }

            return new GltfWriteResult();
        }
    }

    /// <summary>Writes a document to owned bytes.</summary>
    /// <param name="document">The document to write.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write result.</returns>
    public GltfWriteResult WriteBytes(GltfDocument document, GltfWriteOptions options)
    {
        ValidateWriteInputs(document, options);
        GltfWriteResult result = CreateJsonResult(document, options, false);
        if (result.Status != GltfOperationStatus.Success)
        {
            return result;
        }

        GltfContainerFormat format = ResolveDocumentOutputFormat(document, options);
        if (format == GltfContainerFormat.Glb)
        {
            GltfContainerData container = CreateDocumentContainer(document, result.PrimaryBytes, options);
            result.PrimaryBytes = GlbWriter.WriteBytes(container, options.PreserveUnknownChunks);
        }

        return result;
    }

    /// <summary>Writes raw container data without serializing a document.</summary>
    /// <param name="container">The raw container data to write.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write result containing owned primary bytes.</returns>
    public GltfWriteResult WriteContainerBytes(GltfContainerData container, GltfWriteOptions options)
    {
        if (container == null)
        {
            throw new System.ArgumentNullException("container");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        GltfContainerFormat format = ResolveContainerOutputFormat(container, options);

        byte[] bytes;
        if (format == GltfContainerFormat.Glb)
        {
            bytes = GlbWriter.WriteBytes(container, options.PreserveUnknownChunks);
        }
        else
        {
            bytes = OwnedBytes.CopyArray(GetJsonContainerBytes(container));
        }

        GltfWriteResult result = new GltfWriteResult();
        result.PrimaryBytes = bytes;
        return result;
    }

    internal GltfContainerFormat ResolveContainerOutputFormat(
        GltfContainerData container,
        GltfWriteOptions options)
    {
        GltfContainerFormat format = options.Format;
        if (format == GltfContainerFormat.Auto)
        {
            format = container.Format;
        }

        if (format != GltfContainerFormat.Json && format != GltfContainerFormat.Glb)
        {
            throw new GltfWriteException("The raw container output format is not specified.");
        }

        return format;
    }

    internal byte[] GetJsonContainerBytes(GltfContainerData container)
    {
        if (container.JsonBytes == null)
        {
            throw new GltfWriteException("The JSON container bytes are null.");
        }

        if (container.JsonBytes.Length == 0)
        {
            throw new GltfWriteException("The JSON container must not be empty.");
        }

        return container.JsonBytes;
    }

    /// <summary>Writes a document as JSON text.</summary>
    /// <param name="document">The document to write.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write result containing JSON text.</returns>
    public GltfWriteResult WriteJsonText(GltfDocument document, GltfWriteOptions options)
    {
        ValidateWriteInputs(document, options);
        return CreateJsonResult(document, options, true);
    }

    internal GltfWriteResult CreateJsonResult(
        GltfDocument document,
        GltfWriteOptions options,
        bool includeJsonText)
    {
        Preflight.ValidateDocument(document);
        GltfValidationReport validation = new GltfValidationReport();
        if (options.Validate)
        {
            GltfValidationOptions validationOptions = CreateValidationOptions(
                options.ValidationOptions,
                options.ExtensionHandlers);
            validation = Validator.ValidateDocument(document, validationOptions);
            if (validation == null)
            {
                throw new GltfValidationException("The validator returned a null validation report.");
            }

            if (!validation.IsValid)
            {
                GltfWriteResult invalidResult = new GltfWriteResult();
                invalidResult.Status = GltfOperationStatus.Invalid;
                invalidResult.Validation = validation;
                return invalidResult;
            }
        }

        ResourceWritePlan resourcePlan = ResourcePlanner.PlanResources(
            document.Resources,
            options.ResourceMode,
            options.UseContentHashResourceNames);
        GltfWriteResult result = JsonWriter.WriteResourceOutput(
            document,
            options,
            includeJsonText,
            resourcePlan);
        result.Resources = resourcePlan.Resources;
        AppendExtensionWriteResources(result.Resources, JsonWriter.ExtensionResources);
        result.Validation = validation;
        result.Manifest = resourcePlan.Manifest;
        IssueAggregator.SortIssues(result.Diagnostics);
        return result;
    }

    internal void AppendExtensionWriteResources(
        GltfResourceSet target,
        GltfResourceSet source)
    {
        int index = 0;
        while (index < source.Resources.Count)
        {
            GltfResource resource = CopyResource(source.Resources[index]);
            target.Resources.Add(resource);
            if (resource.CacheKey != null && !target.Cache.ContainsKey(resource.CacheKey))
            {
                target.Cache[resource.CacheKey] = resource;
            }

            index++;
        }
    }

    internal GltfValidationOptions CreateValidationOptions(
        GltfValidationOptions source,
        System.Collections.Generic.List<IGltfExtensionHandler> handlers)
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

        return result;
    }

    internal GltfContainerFormat ResolveDocumentOutputFormat(
        GltfDocument document,
        GltfWriteOptions options)
    {
        GltfContainerFormat format = options.Format;
        if (format == GltfContainerFormat.Auto)
        {
            if (document.Container != null
                && (document.Container.Format == GltfContainerFormat.Json
                    || document.Container.Format == GltfContainerFormat.Glb))
            {
                format = document.Container.Format;
            }
            else if (options.ResourceMode == GltfResourceMode.GlbBin)
            {
                format = GltfContainerFormat.Glb;
            }
            else
            {
                format = GltfContainerFormat.Json;
            }
        }

        if (format != GltfContainerFormat.Json && format != GltfContainerFormat.Glb)
        {
            throw new GltfWriteException("The document output format is not specified.");
        }

        return format;
    }

    internal GltfContainerData CreateDocumentContainer(
        GltfDocument document,
        byte[] jsonBytes,
        GltfWriteOptions options)
    {
        GltfContainerData result = new GltfContainerData();
        result.Format = GltfContainerFormat.Glb;
        result.JsonBytes = OwnedBytes.CopyArray(jsonBytes);
        bool preserveEmbeddedBinary = options.ResourceMode != GltfResourceMode.External
            && options.ResourceMode != GltfResourceMode.DataUri;
        if (preserveEmbeddedBinary
            && document.Container != null
            && document.Container.HasBinChunk)
        {
            result.HasBinChunk = true;
            result.BinBytes = OwnedBytes.CopyArray(document.Container.BinBytes);
        }
        else if (preserveEmbeddedBinary)
        {
            int resourceIndex = 0;
            while (!result.HasBinChunk && resourceIndex < document.Resources.Resources.Count)
            {
                GltfResource resource = document.Resources.Resources[resourceIndex];
                if (resource.Kind == GltfResourceKind.Buffer
                    && resource.Embedded
                    && resource.SourceUri == null)
                {
                    result.HasBinChunk = true;
                    result.BinBytes = OwnedBytes.CopyArray(resource.Bytes);
                }

                resourceIndex++;
            }
        }

        if (options.PreserveUnknownChunks && document.Container != null)
        {
            int chunkIndex = 0;
            while (chunkIndex < document.Container.UnknownChunks.Count)
            {
                GltfGlbChunk sourceChunk = document.Container.UnknownChunks[chunkIndex];
                GltfGlbChunk resultChunk = new GltfGlbChunk();
                resultChunk.Type = sourceChunk.Type;
                resultChunk.Data = OwnedBytes.CopyArray(sourceChunk.Data);
                result.UnknownChunks.Add(resultChunk);
                chunkIndex++;
            }
        }

        return result;
    }

    internal GltfResourceSet PlanPreservedResources(
        GltfDocument document,
        GltfWriteOptions options)
    {
        if (document.Resources == null)
        {
            throw new GltfWriteException("The document resource set is null.");
        }

        if (options.ResourceMode != GltfResourceMode.Preserve
            && document.Resources.Resources.Count > 0)
        {
            throw new GltfUnsupportedFeatureException(
                $"Resource layout mode {options.ResourceMode} is not implemented.");
        }

        GltfResourceSet result = new GltfResourceSet();
        int resourceIndex = 0;
        while (resourceIndex < document.Resources.Resources.Count)
        {
            GltfResource source = document.Resources.Resources[resourceIndex];
            if (source == null)
            {
                throw new GltfWriteException($"Document resource {resourceIndex} is null.");
            }

            GltfResource resource = CopyResource(source);
            result.Resources.Add(resource);
            if (resource.CacheKey != null && !result.Cache.ContainsKey(resource.CacheKey))
            {
                result.Cache[resource.CacheKey] = resource;
            }

            resourceIndex++;
        }

        return result;
    }

    internal GltfResource CopyResource(GltfResource source)
    {
        if (source.Bytes == null)
        {
            throw new GltfWriteException("Document resource bytes are null.");
        }

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

    internal void AppendResourceManifest(
        GltfWriteResult result,
        GltfResourceSet resources,
        GltfResourceMode mode)
    {
        int resourceIndex = 0;
        while (resourceIndex < resources.Resources.Count)
        {
            GltfResource resource = resources.Resources[resourceIndex];
            GltfResourceManifestEntry entry = new GltfResourceManifestEntry();
            entry.Kind = resource.Kind;
            entry.Uri = resource.EffectiveUri;
            if (entry.Uri == null)
            {
                entry.Uri = resource.SourceUri;
            }

            entry.MimeType = resource.MimeType;
            entry.ByteLength = resource.Bytes.Length;
            entry.Embedded = resource.Embedded;
            entry.Mode = mode;
            result.Manifest.Add(entry);
            resourceIndex++;
        }
    }

    internal GltfWriteOptions CreateFileWriteOptions(
        GltfDocument document,
        string path,
        GltfWriteOptions source)
    {
        GltfWriteOptions result = new GltfWriteOptions();
        result.Format = source.Format;
        if (result.Format == GltfContainerFormat.Auto)
        {
            string extension = Path.GetExtension(path);
            if (string.Equals(extension, ".glb", System.StringComparison.OrdinalIgnoreCase))
            {
                result.Format = GltfContainerFormat.Glb;
            }
            else if (string.Equals(extension, ".gltf", System.StringComparison.OrdinalIgnoreCase))
            {
                result.Format = GltfContainerFormat.Json;
            }
            else
            {
                result.Format = ResolveDocumentOutputFormat(document, source);
            }
        }

        result.ResourceMode = source.ResourceMode;
        result.Indented = source.Indented;
        result.Deterministic = source.Deterministic;
        result.UseContentHashResourceNames = source.UseContentHashResourceNames;
        result.PreserveExplicitDefaults = source.PreserveExplicitDefaults;
        result.PreserveUnknownMembers = source.PreserveUnknownMembers;
        result.PreserveUnknownChunks = source.PreserveUnknownChunks;
        result.Validate = source.Validate;
        result.LeaveStreamOpen = source.LeaveStreamOpen;
        result.ValidationOptions = source.ValidationOptions;
        result.ExtensionHandlers = source.ExtensionHandlers;
        result.CompressionCodecs = source.CompressionCodecs;
        result.ImageCodecs = source.ImageCodecs;
        return result;
    }

    internal void ValidateWriteInputs(GltfDocument document, GltfWriteOptions options)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (options.Format != GltfContainerFormat.Auto
            && options.Format != GltfContainerFormat.Json
            && options.Format != GltfContainerFormat.Glb)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        if (options.ResourceMode != GltfResourceMode.Preserve
            && options.ResourceMode != GltfResourceMode.External
            && options.ResourceMode != GltfResourceMode.DataUri
            && options.ResourceMode != GltfResourceMode.BufferView
            && options.ResourceMode != GltfResourceMode.GlbBin)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        if (options.ValidationOptions == null)
        {
            throw new System.ArgumentException("The validation options must not be null.", "options");
        }
    }
}
