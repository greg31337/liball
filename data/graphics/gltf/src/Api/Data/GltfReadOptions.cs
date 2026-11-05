using System.Collections.Generic;

namespace gltf;

/// <summary>Controls container parsing, model construction, resource resolution, and validation.</summary>
public class GltfReadOptions
{
    /// <summary>The requested container format or automatic detection.</summary>
    public GltfContainerFormat Format = GltfContainerFormat.Auto;

    /// <summary>The policy applied when a JSON object repeats a member name.</summary>
    public GltfDuplicateMemberPolicy DuplicateMemberPolicy = GltfDuplicateMemberPolicy.Reject;

    /// <summary>Whether unknown JSON object members are retained.</summary>
    public bool PreserveUnknownMembers = true;

    /// <summary>Whether unknown GLB chunks are retained.</summary>
    public bool PreserveUnknownChunks = true;

    /// <summary>Whether referenced resources are resolved while reading.</summary>
    public bool ResolveResources;

    /// <summary>Whether validation runs after model construction.</summary>
    public bool Validate;

    /// <summary>Whether a caller-owned input stream remains open.</summary>
    public bool LeaveStreamOpen = true;

    /// <summary>The optional base URI for resource resolution.</summary>
    public string? BaseUri;

    /// <summary>The input limits.</summary>
    public GltfReadLimits Limits = new GltfReadLimits();

    /// <summary>The optional resource resolver.</summary>
    public IGltfResourceResolver? ResourceResolver;

    /// <summary>The file-system policy supplied to resource resolution requests.</summary>
    public GltfResourcePolicy ResourcePolicy = new GltfResourcePolicy();

    /// <summary>Registered extension handlers.</summary>
    public List<IGltfExtensionHandler> ExtensionHandlers = new List<IGltfExtensionHandler>();

    /// <summary>Registered geometry compression codecs.</summary>
    public List<IGltfCompressionCodec> CompressionCodecs = new List<IGltfCompressionCodec>();

    /// <summary>Registered image codecs.</summary>
    public List<IGltfImageCodec> ImageCodecs = new List<IGltfImageCodec>();

    /// <summary>Validation options used when validation is enabled.</summary>
    public GltfValidationOptions ValidationOptions = new GltfValidationOptions();
}

/// <summary>Defines bounded input limits applied by read and resource operations.</summary>
public class GltfReadLimits
{
    /// <summary>The maximum primary input length in bytes.</summary>
    public int MaximumInputByteLength = GltfConstants.DefaultMaximumInputByteLength;

    /// <summary>The maximum JSON content length in bytes.</summary>
    public int MaximumJsonByteLength = GltfConstants.DefaultMaximumJsonByteLength;

    /// <summary>The maximum JSON nesting depth.</summary>
    public int MaximumJsonDepth = GltfConstants.DefaultMaximumJsonDepth;

    /// <summary>The maximum JSON token count.</summary>
    public int MaximumJsonTokenCount = GltfConstants.DefaultMaximumJsonTokenCount;

    /// <summary>The maximum UTF-8 byte length of one JSON string.</summary>
    public int MaximumJsonStringByteLength = GltfConstants.DefaultMaximumJsonStringByteLength;

    /// <summary>The maximum UTF-8 source byte length of one JSON number.</summary>
    public int MaximumJsonNumberByteLength = GltfConstants.DefaultMaximumJsonNumberByteLength;

    /// <summary>The maximum number of elements in one parsed collection.</summary>
    public int MaximumCollectionCount = GltfConstants.DefaultMaximumCollectionCount;

    /// <summary>The maximum decoded byte length of one data URI.</summary>
    public int MaximumDataUriByteLength = GltfConstants.DefaultMaximumDataUriByteLength;

    /// <summary>The maximum byte length of one resolved resource.</summary>
    public int MaximumResourceByteLength = GltfConstants.DefaultMaximumResourceByteLength;

    /// <summary>The maximum total byte length of resolved resources.</summary>
    public int MaximumTotalResourceByteLength = GltfConstants.DefaultMaximumTotalResourceByteLength;
}

/// <summary>Contains the owned outputs and diagnostics of a read operation.</summary>
public class GltfReadResult
{
    /// <summary>The overall operation status.</summary>
    public GltfOperationStatus Status = GltfOperationStatus.Success;

    /// <summary>The parsed document when model construction succeeded.</summary>
    public GltfDocument? Document;

    /// <summary>The raw parsed container data when available.</summary>
    public GltfContainerData? Container;

    /// <summary>The resolved resource set.</summary>
    public GltfResourceSet Resources = new GltfResourceSet();

    /// <summary>The validation report.</summary>
    public GltfValidationReport Validation = new GltfValidationReport();

    /// <summary>Operation diagnostics not represented by validation issues.</summary>
    public List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}
