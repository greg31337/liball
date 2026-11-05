using System.Collections.Generic;
using System.Security.Cryptography;

namespace gltf;

internal class ResourceWritePlan
{
    internal GltfResourceMode Mode = GltfResourceMode.Preserve;
    internal GltfResourceSet Resources = new GltfResourceSet();
    internal List<GltfResourceManifestEntry> Manifest = new List<GltfResourceManifestEntry>();
    internal List<int> SourceResourceMap = new List<int>();
    internal List<string?> SourceResourceUris = new List<string?>();
    internal List<string> OutputHashes = new List<string>();
    internal ImageBufferViewPackPlan? ImagePack;
}

internal class ResourcePlanner
{
    internal ResourceNameAllocator NameAllocator;
    internal Base64Codec Base64Codec;
    internal ImageBufferViewPacker ImagePacker;

    internal ResourcePlanner()
    {
        NameAllocator = new ResourceNameAllocator();
        Base64Codec = new Base64Codec();
        ImagePacker = new ImageBufferViewPacker();
    }

    internal ResourceWritePlan PlanResources(
        GltfResourceSet source,
        GltfResourceMode mode,
        bool useContentHashNames)
    {
        ValidatePlanInputs(source, mode);
        NameAllocator = new ResourceNameAllocator();
        if (mode == GltfResourceMode.BufferView)
        {
            return PlanBufferViewResources(source);
        }

        ResourceWritePlan result = new ResourceWritePlan();
        result.Mode = mode;
        int sourceIndex = 0;
        while (sourceIndex < source.Resources.Count)
        {
            GltfResource sourceResource = source.Resources[sourceIndex];
            string hash = ComputeContentHash(sourceResource.Bytes);
            int outputIndex = -1;
            if (mode != GltfResourceMode.Preserve)
            {
                outputIndex = FindDuplicateResource(result, sourceResource, hash);
            }

            if (outputIndex < 0)
            {
                GltfResource outputResource = CopyResource(sourceResource);
                ApplyResourceMode(outputResource, mode, useContentHashNames);
                outputIndex = result.Resources.Resources.Count;
                result.Resources.Resources.Add(outputResource);
                result.OutputHashes.Add(hash);
                AddCacheEntry(result.Resources, outputResource, hash);
                result.Manifest.Add(CreateManifestEntry(outputResource, mode, hash));
            }

            GltfResource selectedResource = result.Resources.Resources[outputIndex];
            result.SourceResourceMap.Add(outputIndex);
            result.SourceResourceUris.Add(GetOutputUri(selectedResource));
            sourceIndex++;
        }

        return result;
    }

    internal void ValidatePlanInputs(GltfResourceSet source, GltfResourceMode mode)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }

        if (source.Resources == null)
        {
            throw new GltfWriteException("The document resource collection is null.");
        }

        if (mode != GltfResourceMode.Preserve
            && mode != GltfResourceMode.External
            && mode != GltfResourceMode.DataUri
            && mode != GltfResourceMode.BufferView)
        {
            if (source.Resources.Count > 0)
            {
                throw new GltfUnsupportedFeatureException(
                    $"Resource layout mode {mode} is not implemented.");
            }

            return;
        }

        int resourceIndex = 0;
        while (resourceIndex < source.Resources.Count)
        {
            GltfResource resource = source.Resources[resourceIndex];
            if (resource == null)
            {
                throw new GltfWriteException($"Document resource {resourceIndex} is null.");
            }

            if (resource.Bytes == null)
            {
                throw new GltfWriteException($"Document resource {resourceIndex} bytes are null.");
            }

            resourceIndex++;
        }
    }

    internal ResourceWritePlan PlanBufferViewResources(GltfResourceSet source)
    {
        ImageBufferViewPackPlan imagePack = ImagePacker.PackImages(source);
        ResourceWritePlan result = new ResourceWritePlan();
        result.Mode = GltfResourceMode.BufferView;
        result.ImagePack = imagePack;
        int sourceIndex = 0;
        while (sourceIndex < source.Resources.Count)
        {
            GltfResource sourceResource = source.Resources[sourceIndex];
            if (sourceResource.Kind == GltfResourceKind.Image)
            {
                result.SourceResourceMap.Add(-1);
                result.SourceResourceUris.Add(null);
            }
            else
            {
                GltfResource outputResource = CopyResource(sourceResource);
                string hash = ComputeContentHash(outputResource.Bytes);
                int outputIndex = result.Resources.Resources.Count;
                result.Resources.Resources.Add(outputResource);
                result.OutputHashes.Add(hash);
                AddCacheEntry(result.Resources, outputResource, hash);
                result.Manifest.Add(CreateManifestEntry(
                    outputResource,
                    GltfResourceMode.BufferView,
                    hash));
                result.SourceResourceMap.Add(outputIndex);
                result.SourceResourceUris.Add(GetOutputUri(outputResource));
            }

            sourceIndex++;
        }

        if (imagePack.Images.Count > 0)
        {
            GltfResource packedBuffer = new GltfResource();
            packedBuffer.Kind = GltfResourceKind.Buffer;
            packedBuffer.UriKind = GltfUriKind.Data;
            packedBuffer.MimeType = GltfMimeTypes.GltfBuffer;
            packedBuffer.Bytes = OwnedBytes.CopyArray(imagePack.BufferBytes);
            packedBuffer.EffectiveUri = "data:"
                + GltfMimeTypes.GltfBuffer
                + ";base64,"
                + Base64Codec.EncodeBase64Bytes(packedBuffer.Bytes);
            packedBuffer.Embedded = true;
            string hash = ComputeContentHash(packedBuffer.Bytes);
            int packedResourceIndex = result.Resources.Resources.Count;
            result.Resources.Resources.Add(packedBuffer);
            result.OutputHashes.Add(hash);
            AddCacheEntry(result.Resources, packedBuffer, hash);
            result.Manifest.Add(CreateManifestEntry(
                packedBuffer,
                GltfResourceMode.BufferView,
                hash));
            sourceIndex = 0;
            while (sourceIndex < result.SourceResourceMap.Count)
            {
                if (result.SourceResourceMap[sourceIndex] < 0)
                {
                    result.SourceResourceMap[sourceIndex] = packedResourceIndex;
                }

                sourceIndex++;
            }
        }

        return result;
    }

    internal int FindDuplicateResource(
        ResourceWritePlan plan,
        GltfResource source,
        string hash)
    {
        int outputIndex = 0;
        while (outputIndex < plan.Resources.Resources.Count)
        {
            GltfResource candidate = plan.Resources.Resources[outputIndex];
            if (plan.OutputHashes[outputIndex] == hash
                && AreResourcesCompatible(source, candidate)
                && HaveEqualBytes(source.Bytes, candidate.Bytes))
            {
                return outputIndex;
            }

            outputIndex++;
        }

        return -1;
    }

    internal bool AreResourcesCompatible(GltfResource first, GltfResource second)
    {
        return first.Kind == second.Kind
            && first.ImageFormat == second.ImageFormat
            && string.Equals(
                GetEmbeddedMimeType(first),
                GetEmbeddedMimeType(second),
                System.StringComparison.OrdinalIgnoreCase);
    }

    internal bool HaveEqualBytes(byte[] first, byte[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }

        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }

            index++;
        }

        return true;
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

    internal void ApplyResourceMode(
        GltfResource resource,
        GltfResourceMode mode,
        bool useContentHashNames)
    {
        if (mode == GltfResourceMode.External)
        {
            resource.EffectiveUri = NameAllocator.AllocateResourceName(
                resource,
                null,
                useContentHashNames);
            resource.UriKind = GltfUriKind.Relative;
            resource.Embedded = false;
        }
        else if (mode == GltfResourceMode.DataUri)
        {
            string mimeType = GetEmbeddedMimeType(resource);
            resource.EffectiveUri = "data:"
                + mimeType
                + ";base64,"
                + Base64Codec.EncodeBase64Bytes(resource.Bytes);
            resource.UriKind = GltfUriKind.Data;
            resource.Embedded = true;
            if (resource.MimeType == null)
            {
                resource.MimeType = mimeType;
            }
        }
    }

    internal string GetEmbeddedMimeType(GltfResource resource)
    {
        if (resource.MimeType != null && resource.MimeType.Length > 0)
        {
            return resource.MimeType.ToLowerInvariant();
        }

        switch (resource.ImageFormat)
        {
            case GltfImageFormat.Png:
                return GltfMimeTypes.Png;
            case GltfImageFormat.Jpeg:
                return GltfMimeTypes.Jpeg;
            case GltfImageFormat.Ktx2:
                return GltfMimeTypes.Ktx2;
            case GltfImageFormat.WebP:
                return GltfMimeTypes.WebP;
            case GltfImageFormat.Avif:
                return GltfMimeTypes.Avif;
            case GltfImageFormat.Dds:
                return GltfMimeTypes.Dds;
            default:
                return GltfMimeTypes.OctetStream;
        }
    }

    internal void AddCacheEntry(
        GltfResourceSet resources,
        GltfResource resource,
        string hash)
    {
        string key = resource.CacheKey ?? "content:sha256:" + hash;
        if (!resources.Cache.ContainsKey(key))
        {
            resources.Cache[key] = resource;
        }
    }

    internal GltfResourceManifestEntry CreateManifestEntry(
        GltfResource resource,
        GltfResourceMode mode,
        string hash)
    {
        GltfResourceManifestEntry result = new GltfResourceManifestEntry();
        result.Kind = resource.Kind;
        result.Uri = GetOutputUri(resource);
        result.EffectiveUri = result.Uri;
        result.MimeType = resource.MimeType;
        result.ByteLength = resource.Bytes.Length;
        result.ContentHash = hash;
        result.Embedded = resource.Embedded;
        result.External = !resource.Embedded;
        result.Mode = mode;
        return result;
    }

    internal string? GetOutputUri(GltfResource resource)
    {
        if (resource.EffectiveUri != null)
        {
            return resource.EffectiveUri;
        }

        return resource.SourceUri;
    }

    internal string ComputeContentHash(byte[] bytes)
    {
        byte[] hash = SHA256.HashData(bytes);
        return System.Convert.ToHexString(hash).ToLowerInvariant();
    }
}
