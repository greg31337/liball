using System.Collections.Generic;

namespace gltf;

/// <summary>Represents an owned resolved or generated glTF resource.</summary>
public class GltfResource
{
    /// <summary>The operation status associated with the resource.</summary>
    public GltfOperationStatus Status = GltfOperationStatus.Success;

    /// <summary>The resource purpose.</summary>
    public GltfResourceKind Kind = GltfResourceKind.Other;

    /// <summary>The source URI classification.</summary>
    public GltfUriKind UriKind = GltfUriKind.Absent;

    /// <summary>The original URI reference.</summary>
    public string? SourceUri;

    /// <summary>The effective resolved URI.</summary>
    public string? EffectiveUri;

    /// <summary>The declared or detected media type.</summary>
    public string? MimeType;

    /// <summary>The media type declared by the resource request.</summary>
    public string? DeclaredMimeType;

    /// <summary>The media type declared or detected by the underlying resource source.</summary>
    public string? SourceMimeType;

    /// <summary>The recognized image format when the resource is an image.</summary>
    public GltfImageFormat ImageFormat = GltfImageFormat.Unknown;

    /// <summary>The owned resource bytes.</summary>
    public byte[] Bytes = [];

    /// <summary>A deterministic resolver cache identity.</summary>
    public string? CacheKey;

    /// <summary>Whether the declared byte length was present.</summary>
    public bool HasDeclaredByteLength;

    /// <summary>The declared byte length.</summary>
    public int DeclaredByteLength;

    /// <summary>Whether the resource is embedded in the primary output.</summary>
    public bool Embedded;
}

/// <summary>Describes one resource resolution request.</summary>
public class GltfResourceRequest
{
    /// <summary>The optional base URI used to resolve a relative reference.</summary>
    public string? BaseUri;

    /// <summary>The source URI reference to resolve.</summary>
    public string? SourceUri;

    /// <summary>The resource purpose.</summary>
    public GltfResourceKind Kind = GltfResourceKind.Other;

    /// <summary>The optional declared media type.</summary>
    public string? DeclaredMimeType;

    /// <summary>Whether the declared byte length was provided.</summary>
    public bool HasDeclaredByteLength;

    /// <summary>The declared byte length.</summary>
    public int DeclaredByteLength;

    /// <summary>The largest resource byte length permitted for this request.</summary>
    public int MaximumByteLength = GltfConstants.DefaultMaximumResourceByteLength;

    /// <summary>The file-system access policy applied while resolving this request.</summary>
    public GltfResourcePolicy Policy = new GltfResourcePolicy();
}

/// <summary>Controls the file-system boundary used by built-in resource resolution.</summary>
public class GltfResourcePolicy
{
    /// <summary>File-system directories or file URIs that may contain resolved resources.</summary>
    public List<string> FileRoots = new List<string>();

    /// <summary>Whether an absolute path or file URI may be supplied as the source reference.</summary>
    public bool AllowAbsolutePaths;

    /// <summary>Whether a relative source reference may contain parent path segments.</summary>
    public bool AllowParentPathSegments;
}

/// <summary>Owns resolved resources and deterministic cache entries for a document.</summary>
public class GltfResourceSet
{
    /// <summary>The resolved or generated resources.</summary>
    public List<GltfResource> Resources = new List<GltfResource>();

    /// <summary>Cache entries keyed by deterministic cache identity.</summary>
    public Dictionary<string, GltfResource> Cache = new Dictionary<string, GltfResource>();
}

/// <summary>Describes one resource emitted or retained by a write or conversion operation.</summary>
public class GltfResourceManifestEntry
{
    /// <summary>The resource purpose.</summary>
    public GltfResourceKind Kind = GltfResourceKind.Other;

    /// <summary>The output URI or relative path.</summary>
    public string? Uri;

    /// <summary>The effective URI used by the converted document.</summary>
    public string? EffectiveUri;

    /// <summary>The resource media type.</summary>
    public string? MimeType;

    /// <summary>The output byte length.</summary>
    public int ByteLength;

    /// <summary>A deterministic hexadecimal content hash.</summary>
    public string? ContentHash;

    /// <summary>Whether the resource is embedded in the primary output.</summary>
    public bool Embedded;

    /// <summary>Whether the resource is emitted outside the primary output.</summary>
    public bool External;

    /// <summary>The selected output representation.</summary>
    public GltfResourceMode Mode = GltfResourceMode.Preserve;
}
