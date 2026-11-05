using System.Collections.Generic;
using System.Security.Cryptography;

namespace gltf;

internal class ResourceResolverDispatcher
{
    internal IGltfResourceResolver? Resolver;
    internal FileGltfResourceResolver BuiltInResolver = new FileGltfResourceResolver();
    internal UriReference UriReference = new UriReference();
    internal Dictionary<string, GltfResource> RequestCache = new Dictionary<string, GltfResource>();
    internal Dictionary<string, GltfResource> IdentityCache = new Dictionary<string, GltfResource>();
    internal Dictionary<string, GltfResource> ContentCache = new Dictionary<string, GltfResource>();

    internal ResourceDispatchData DispatchResource(GltfResourceRequest request)
    {
        ValidateRequest(request);
        string requestKey = CreateRequestKey(request);
        if (RequestCache.ContainsKey(requestKey))
        {
            GltfResource cachedResource = RequestCache[requestKey];
            ValidateCachedLength(cachedResource, request);
            ResourceDispatchData cachedResult = new ResourceDispatchData();
            cachedResult.Resource = CreateContextResource(cachedResource, request);
            cachedResult.CacheHit = true;
            return cachedResult;
        }

        IGltfResourceResolver selectedResolver = BuiltInResolver;
        if (Resolver != null)
        {
            selectedResolver = Resolver;
        }

        GltfResource resolvedResource = selectedResolver.ResolveResource(request);
        if (resolvedResource == null)
        {
            throw new GltfResourceException("The resource resolver returned a null resource.");
        }

        if (resolvedResource.Status != GltfOperationStatus.Success)
        {
            ResourceDispatchData unsuccessfulResult = new ResourceDispatchData();
            unsuccessfulResult.Resource = CreateContextResource(resolvedResource, request);
            return unsuccessfulResult;
        }

        if (resolvedResource.Bytes == null)
        {
            throw new GltfResourceException("The resource resolver returned null resource bytes.");
        }

        if (resolvedResource.Bytes.Length > request.MaximumByteLength)
        {
            throw new GltfResourceException(
                $"Resolved resource length {resolvedResource.Bytes.Length} exceeds the configured maximum {request.MaximumByteLength}.");
        }

        string contentIdentity = CreateContentIdentity(resolvedResource.Bytes);
        string cacheIdentity = SelectCacheIdentity(resolvedResource, contentIdentity);
        GltfResource cacheEntry;
        GltfResource requestEntry;
        bool newContent = false;
        if (IdentityCache.ContainsKey(cacheIdentity))
        {
            cacheEntry = IdentityCache[cacheIdentity];
            if (!BytesEqual(cacheEntry.Bytes, resolvedResource.Bytes))
            {
                throw new GltfResourceException(
                    $"Resource cache identity '{cacheIdentity}' was returned with different content.");
            }

            requestEntry = CreateCacheEntry(
                resolvedResource,
                cacheIdentity,
                cacheEntry.Bytes);
        }
        else
        {
            byte[] cacheBytes;
            if (ContentCache.ContainsKey(contentIdentity))
            {
                GltfResource contentEntry = ContentCache[contentIdentity];
                if (!BytesEqual(contentEntry.Bytes, resolvedResource.Bytes))
                {
                    throw new GltfResourceException(
                        $"Resource content identity '{contentIdentity}' matched different content.");
                }

                cacheBytes = contentEntry.Bytes;
            }
            else
            {
                cacheBytes = OwnedBytes.CopyArray(resolvedResource.Bytes);
                newContent = true;
            }

            cacheEntry = CreateCacheEntry(resolvedResource, cacheIdentity, cacheBytes);
            IdentityCache[cacheIdentity] = cacheEntry;
            requestEntry = cacheEntry;
            if (!ContentCache.ContainsKey(contentIdentity))
            {
                ContentCache[contentIdentity] = cacheEntry;
            }
        }

        RequestCache[requestKey] = requestEntry;
        ResourceDispatchData result = new ResourceDispatchData();
        result.Resource = CreateContextResource(requestEntry, request);
        result.NewContent = newContent;
        return result;
    }

    internal void ValidateRequest(GltfResourceRequest request)
    {
        if (request == null)
        {
            throw new System.ArgumentNullException("request");
        }

        if (request.Policy == null || request.Policy.FileRoots == null)
        {
            throw new System.ArgumentException(
                "The resource policy and file roots must not be null.",
                "request");
        }

        if (request.MaximumByteLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("request");
        }
    }

    internal void ValidateCachedLength(GltfResource resource, GltfResourceRequest request)
    {
        if (resource.Bytes.Length > request.MaximumByteLength)
        {
            throw new GltfResourceException(
                $"Resolved resource length {resource.Bytes.Length} exceeds the configured maximum {request.MaximumByteLength}.");
        }
    }

    internal string CreateRequestKey(GltfResourceRequest request)
    {
        string result = AppendKeyPart("base", request.BaseUri);
        result += AppendKeyPart("source", request.SourceUri);
        result += request.Policy.AllowAbsolutePaths ? "absolute:1;" : "absolute:0;";
        result += request.Policy.AllowParentPathSegments ? "parent:1;" : "parent:0;";
        int rootIndex = 0;
        while (rootIndex < request.Policy.FileRoots.Count)
        {
            result += AppendKeyPart("root", request.Policy.FileRoots[rootIndex]);
            rootIndex++;
        }

        return result;
    }

    internal string AppendKeyPart(string name, string? value)
    {
        if (value == null)
        {
            return name + ":-1:;";
        }

        return name + ":" + value.Length + ":" + value + ";";
    }

    internal string CreateContentIdentity(byte[] bytes)
    {
        byte[] hash = SHA256.HashData(bytes);
        return "content:sha256:" + System.Convert.ToHexString(hash).ToLowerInvariant();
    }

    internal string SelectCacheIdentity(GltfResource resource, string contentIdentity)
    {
        if (resource.CacheKey != null && resource.CacheKey.Length > 0)
        {
            return resource.CacheKey;
        }

        if (resource.EffectiveUri != null && resource.EffectiveUri.Length > 0)
        {
            return "uri:" + UriReference.NormalizeReference(resource.EffectiveUri);
        }

        return contentIdentity;
    }

    internal GltfResource CreateCacheEntry(
        GltfResource source,
        string cacheIdentity,
        byte[] cacheBytes)
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
        result.Bytes = cacheBytes;
        result.CacheKey = cacheIdentity;
        result.HasDeclaredByteLength = source.HasDeclaredByteLength;
        result.DeclaredByteLength = source.DeclaredByteLength;
        result.Embedded = source.Embedded;
        return result;
    }

    internal GltfResource CreateContextResource(
        GltfResource cached,
        GltfResourceRequest request)
    {
        GltfResource result = new GltfResource();
        result.Status = cached.Status;
        result.Kind = request.Kind;
        result.UriKind = UriReference.ClassifyReference(request.SourceUri);
        result.SourceUri = request.SourceUri;
        result.EffectiveUri = cached.EffectiveUri;
        result.MimeType = cached.MimeType;
        result.DeclaredMimeType = request.DeclaredMimeType;
        result.SourceMimeType = cached.SourceMimeType;
        if (request.DeclaredMimeType != null)
        {
            result.MimeType = request.DeclaredMimeType;
        }

        result.ImageFormat = cached.ImageFormat;
        if (cached.Bytes == null)
        {
            result.Bytes = [];
        }
        else
        {
            result.Bytes = OwnedBytes.CopyArray(cached.Bytes);
        }
        result.CacheKey = cached.CacheKey;
        result.HasDeclaredByteLength = request.HasDeclaredByteLength;
        result.DeclaredByteLength = request.DeclaredByteLength;
        result.Embedded = cached.Embedded;
        return result;
    }

    internal bool BytesEqual(byte[] first, byte[] second)
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
}

internal class ResourceDispatchData
{
    internal GltfResource Resource = new GltfResource();
    internal bool CacheHit;
    internal bool NewContent;
}
