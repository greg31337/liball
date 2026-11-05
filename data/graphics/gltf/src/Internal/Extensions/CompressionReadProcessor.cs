using System.Collections.Generic;

namespace gltf;

internal class CompressionReadProcessor
{
    internal CompressionCodecDispatcher Dispatcher = new CompressionCodecDispatcher();
    internal AccessorLayout Layout = new AccessorLayout();

    internal void ProcessRead(GltfReadResult readResult, GltfReadOptions options)
    {
        GltfDocument? document = readResult.Document;
        if (document == null || options.CompressionCodecs.Count == 0)
        {
            return;
        }

        DecodeMeshoptViews(readResult, options, document);
        DecodeDracoPrimitives(readResult, options, document);
    }

    internal bool IsMeshoptFallbackBuffer(GltfDocument document, int bufferIndex)
    {
        if (bufferIndex < 0 || bufferIndex >= document.Buffers.Count)
        {
            return false;
        }

        GltfBuffer buffer = document.Buffers[bufferIndex];
        if (HasFallbackMarker(buffer, GltfExtensionNames.KhrMeshoptCompression)
            || HasFallbackMarker(buffer, GltfExtensionNames.ExtMeshoptCompression))
        {
            return true;
        }

        int viewIndex = 0;
        while (viewIndex < document.BufferViews.Count)
        {
            GltfBufferView view = document.BufferViews[viewIndex];
            if (view.Buffer == bufferIndex
                && (GetMeshoptView(view, GltfExtensionNames.KhrMeshoptCompression) != null
                    || GetMeshoptView(view, GltfExtensionNames.ExtMeshoptCompression) != null))
            {
                return true;
            }

            viewIndex++;
        }

        return false;
    }

    internal void DecodeMeshoptViews(
        GltfReadResult readResult,
        GltfReadOptions options,
        GltfDocument document)
    {
        int viewIndex = 0;
        while (viewIndex < document.BufferViews.Count)
        {
            GltfBufferView view = document.BufferViews[viewIndex];
            GltfMeshoptCompressionBufferViewExtension? metadata = GetMeshoptView(
                view,
                GltfExtensionNames.KhrMeshoptCompression);
            string extension = GltfExtensionNames.KhrMeshoptCompression;
            if (metadata == null)
            {
                metadata = GetMeshoptView(view, GltfExtensionNames.ExtMeshoptCompression);
                extension = GltfExtensionNames.ExtMeshoptCompression;
            }

            if (metadata != null)
            {
                DecodeMeshoptView(
                    readResult,
                    options,
                    document,
                    viewIndex,
                    view,
                    extension,
                    metadata);
            }

            viewIndex++;
        }
    }

    internal void DecodeMeshoptView(
        GltfReadResult readResult,
        GltfReadOptions options,
        GltfDocument document,
        int viewIndex,
        GltfBufferView view,
        string extension,
        GltfMeshoptCompressionBufferViewExtension metadata)
    {
        string pointer = $"/bufferViews/{viewIndex}/extensions/{extension}";
        if (metadata.Buffer < 0
            || metadata.Buffer >= document.Buffers.Count
            || view.Buffer < 0
            || view.Buffer >= document.Buffers.Count)
        {
            AddIssue(readResult, GltfOperationStatus.Invalid, GltfIssueCodes.UnresolvedReference,
                "Meshopt compression references an invalid buffer.", pointer);
            return;
        }

        GltfBuffer source = document.Buffers[metadata.Buffer];
        GltfBuffer target = document.Buffers[view.Buffer];
        int compressedOffset = metadata.HasByteOffset ? metadata.ByteOffset : 0;
        long compressedEnd = (long)compressedOffset + metadata.ByteLength;
        int targetOffset = view.HasByteOffset ? view.ByteOffset : 0;
        long targetEnd = (long)targetOffset + view.ByteLength;
        if (!source.Resolved
            || source.ResolvedBytes == null
            || compressedOffset < 0
            || metadata.ByteLength < 0
            || compressedEnd > source.ResolvedBytes.Length
            || target.ByteLength < 0
            || targetOffset < 0
            || targetEnd > target.ByteLength)
        {
            AddIssue(readResult, GltfOperationStatus.Invalid, GltfIssueCodes.UnresolvedReference,
                "Meshopt compressed or fallback storage is unavailable.", pointer);
            return;
        }

        byte[] compressed = new byte[metadata.ByteLength];
        System.Array.Copy(source.ResolvedBytes, compressedOffset, compressed, 0, compressed.Length);
        GltfCompressionRequest request = new GltfCompressionRequest();
        request.Codec = extension;
        request.Document = document;
        request.Bytes = compressed;
        request.Meshopt = metadata;
        request.MaximumOutputByteLength = options.Limits.MaximumResourceByteLength;
        GltfCompressionResult decoded = Dispatcher.DecodeMeshopt(
            request,
            options.CompressionCodecs);
        if (decoded.Status == GltfOperationStatus.Unavailable
            && target.Resolved
            && !document.ExtensionsRequired.Contains(extension))
        {
            return;
        }

        AppendResult(readResult, decoded, pointer);
        if (decoded.Status != GltfOperationStatus.Success)
        {
            return;
        }

        if (decoded.Bytes.Length != view.ByteLength)
        {
            AddIssue(readResult, GltfOperationStatus.Invalid, GltfIssueCodes.BufferViewTooLong,
                "Meshopt decoded output must exactly match the parent bufferView byteLength.", pointer);
            return;
        }

        if (!target.Resolved || target.ResolvedBytes == null)
        {
            target.ResolvedBytes = new byte[target.ByteLength];
            target.Resolved = true;
        }

        if (target.ResolvedBytes.Length < target.ByteLength)
        {
            AddIssue(readResult, GltfOperationStatus.Invalid, GltfIssueCodes.BufferViewTooLong,
                "The resolved Meshopt fallback buffer is shorter than its declared byteLength.", pointer);
            return;
        }

        System.Array.Copy(decoded.Bytes, 0, target.ResolvedBytes, targetOffset, decoded.Bytes.Length);
    }

    internal void DecodeDracoPrimitives(
        GltfReadResult readResult,
        GltfReadOptions options,
        GltfDocument document)
    {
        int meshIndex = 0;
        while (meshIndex < document.Meshes.Count)
        {
            GltfMesh mesh = document.Meshes[meshIndex];
            int primitiveIndex = 0;
            while (primitiveIndex < mesh.Primitives.Count)
            {
                GltfMeshPrimitive primitive = mesh.Primitives[primitiveIndex];
                GltfDracoMeshCompressionExtension? metadata = GetDraco(primitive);
                if (metadata != null)
                {
                    DecodeDracoPrimitive(
                        readResult,
                        options,
                        document,
                        meshIndex,
                        primitiveIndex,
                        primitive,
                        metadata);
                }

                primitiveIndex++;
            }

            meshIndex++;
        }
    }

    internal void DecodeDracoPrimitive(
        GltfReadResult readResult,
        GltfReadOptions options,
        GltfDocument document,
        int meshIndex,
        int primitiveIndex,
        GltfMeshPrimitive primitive,
        GltfDracoMeshCompressionExtension metadata)
    {
        string pointer = $"/meshes/{meshIndex}/primitives/{primitiveIndex}/extensions/{GltfExtensionNames.KhrDracoMeshCompression}";
        byte[]? compressed = ReadBufferViewBytes(document, metadata.BufferView);
        if (compressed == null)
        {
            AddIssue(readResult, GltfOperationStatus.Invalid, GltfIssueCodes.UnresolvedReference,
                "The Draco compressed bufferView is unavailable.", pointer + "/bufferView");
            return;
        }

        GltfCompressionRequest request = new GltfCompressionRequest();
        request.Codec = GltfExtensionNames.KhrDracoMeshCompression;
        request.Document = document;
        request.HasMesh = true;
        request.Mesh = meshIndex;
        request.HasPrimitive = true;
        request.Primitive = primitiveIndex;
        request.Bytes = compressed;
        request.Draco = metadata;
        request.MaximumOutputByteLength = options.Limits.MaximumResourceByteLength;
        GltfCompressionResult decoded = Dispatcher.DecodeGeometry(
            request,
            options.CompressionCodecs);
        AppendResult(readResult, decoded, pointer);
        if (decoded.Status != GltfOperationStatus.Success)
        {
            return;
        }

        foreach (KeyValuePair<string, int> mapping in metadata.Attributes)
        {
            int accessorIndex = FindAttributeAccessor(primitive, mapping.Key);
            if (accessorIndex < 0 || !decoded.Attributes.ContainsKey(mapping.Key))
            {
                AddIssue(readResult, GltfOperationStatus.Invalid, GltfIssueCodes.UnresolvedReference,
                    $"The Draco codec did not return the '{mapping.Key}' attribute stream.", pointer + "/attributes");
            }
            else
            {
                StoreAccessorBytes(
                    readResult,
                    document,
                    accessorIndex,
                    decoded.Attributes[mapping.Key],
                    pointer);
            }
        }

        if (primitive.HasIndices)
        {
            StoreAccessorBytes(readResult, document, primitive.Indices, decoded.Bytes, pointer);
        }
    }

    internal byte[]? ReadBufferViewBytes(GltfDocument document, int viewIndex)
    {
        if (viewIndex < 0 || viewIndex >= document.BufferViews.Count)
        {
            return null;
        }

        GltfBufferView view = document.BufferViews[viewIndex];
        if (view.Buffer < 0 || view.Buffer >= document.Buffers.Count)
        {
            return null;
        }

        GltfBuffer buffer = document.Buffers[view.Buffer];
        int offset = view.HasByteOffset ? view.ByteOffset : 0;
        long end = (long)offset + view.ByteLength;
        if (!buffer.Resolved
            || buffer.ResolvedBytes == null
            || offset < 0
            || view.ByteLength < 0
            || end > buffer.ResolvedBytes.Length)
        {
            return null;
        }

        byte[] result = new byte[view.ByteLength];
        System.Array.Copy(buffer.ResolvedBytes, offset, result, 0, result.Length);
        return result;
    }

    internal void StoreAccessorBytes(
        GltfReadResult readResult,
        GltfDocument document,
        int accessorIndex,
        byte[] bytes,
        string pointer)
    {
        if (accessorIndex < 0 || accessorIndex >= document.Accessors.Count || bytes == null)
        {
            AddIssue(readResult, GltfOperationStatus.Invalid, GltfIssueCodes.UnresolvedReference,
                "The Draco codec returned data for an invalid accessor.", pointer);
            return;
        }

        GltfAccessor accessor = document.Accessors[accessorIndex];
        AccessorElementLayout element = Layout.CalculateElementLayout(
            accessor.ComponentType,
            accessor.Type);
        long expected = (long)element.ElementByteLength * accessor.Count;
        if (expected != bytes.Length)
        {
            AddIssue(readResult, GltfOperationStatus.Invalid, GltfIssueCodes.AccessorTooLong,
                "The Draco decoded stream length does not match its accessor layout.", pointer);
            return;
        }

        GltfResource resource = new GltfResource();
        resource.Kind = GltfResourceKind.Buffer;
        resource.Bytes = OwnedBytes.CopyArray(bytes);
        resource.CacheKey = CreateAccessorCacheKey(accessorIndex);
        readResult.Resources.Cache[resource.CacheKey] = resource;
    }

    internal int FindAttributeAccessor(GltfMeshPrimitive primitive, string semantic)
    {
        int index = 0;
        while (index < primitive.Attributes.Count)
        {
            GltfAttribute attribute = primitive.Attributes[index];
            if (attribute.Semantic == semantic)
            {
                return attribute.Accessor;
            }

            index++;
        }

        return GltfConstants.AbsentIndex;
    }

    internal string CreateAccessorCacheKey(int accessorIndex)
    {
        return "compression:accessor:" + accessorIndex;
    }

    internal GltfDracoMeshCompressionExtension? GetDraco(GltfMeshPrimitive primitive)
    {
        if (primitive.PropertyJson.TypedExtensions.ContainsKey(
            GltfExtensionNames.KhrDracoMeshCompression))
        {
            return primitive.PropertyJson.TypedExtensions[
                GltfExtensionNames.KhrDracoMeshCompression].DracoMeshCompression;
        }

        return null;
    }

    internal GltfMeshoptCompressionBufferViewExtension? GetMeshoptView(
        GltfBufferView view,
        string extension)
    {
        if (!view.PropertyJson.TypedExtensions.ContainsKey(extension))
        {
            return null;
        }

        GltfTypedExtensionData typed = view.PropertyJson.TypedExtensions[extension];
        if (extension == GltfExtensionNames.KhrMeshoptCompression)
        {
            return typed.KhrMeshoptCompressionBufferView;
        }

        return typed.ExtMeshoptCompressionBufferView;
    }

    internal bool HasFallbackMarker(GltfBuffer buffer, string extension)
    {
        if (!buffer.PropertyJson.TypedExtensions.ContainsKey(extension))
        {
            return false;
        }

        GltfTypedExtensionData typed = buffer.PropertyJson.TypedExtensions[extension];
        GltfMeshoptCompressionBufferExtension? metadata = typed.KhrMeshoptCompressionBuffer;
        if (extension == GltfExtensionNames.ExtMeshoptCompression)
        {
            metadata = typed.ExtMeshoptCompressionBuffer;
        }

        return metadata != null && metadata.HasFallback && metadata.Fallback;
    }

    internal void AppendResult(
        GltfReadResult readResult,
        GltfCompressionResult result,
        string pointer)
    {
        if ((int)result.Status > (int)readResult.Status)
        {
            readResult.Status = result.Status;
        }

        int index = 0;
        while (index < result.Diagnostics.Count)
        {
            GltfValidationIssue issue = CopyDiagnostic(result.Diagnostics[index]);
            if (issue.JsonPointer == null || issue.JsonPointer.Length == 0)
            {
                issue.JsonPointer = pointer;
            }

            readResult.Diagnostics.Add(issue);
            index++;
        }
    }

    internal GltfValidationIssue CopyDiagnostic(GltfValidationIssue source)
    {
        GltfValidationIssue result = new GltfValidationIssue();
        result.Code = source.Code;
        result.Severity = source.Severity;
        result.Message = source.Message;
        result.JsonPointer = source.JsonPointer;
        result.HasByteOffset = source.HasByteOffset;
        result.ByteOffset = source.ByteOffset;
        result.ResourceUri = source.ResourceUri;
        int index = 0;
        while (index < source.RelatedJsonPointers.Count)
        {
            result.RelatedJsonPointers.Add(source.RelatedJsonPointers[index]);
            index++;
        }

        return result;
    }

    internal void AddIssue(
        GltfReadResult readResult,
        GltfOperationStatus status,
        string code,
        string message,
        string pointer)
    {
        if ((int)status > (int)readResult.Status)
        {
            readResult.Status = status;
        }

        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = code;
        issue.Severity = GltfValidationSeverity.Error;
        issue.Message = message;
        issue.JsonPointer = pointer;
        readResult.Diagnostics.Add(issue);
    }
}
