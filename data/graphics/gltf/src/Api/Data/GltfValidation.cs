using System.Collections.Generic;

namespace gltf;

/// <summary>Controls structural, semantic, resource, and extension validation.</summary>
public class GltfValidationOptions
{
    /// <summary>Whether structural schema rules are evaluated.</summary>
    public bool ValidateStructure = true;

    /// <summary>Whether semantic and cross-reference rules are evaluated.</summary>
    public bool ValidateSemantics = true;

    /// <summary>Whether resolved resource content is evaluated.</summary>
    public bool ValidateResources = true;

    /// <summary>Whether registered extension rules are evaluated.</summary>
    public bool ValidateExtensions = true;

    /// <summary>Registered extension handlers used for semantic extension validation.</summary>
    public List<IGltfExtensionHandler> ExtensionHandlers = new List<IGltfExtensionHandler>();

    /// <summary>Image codecs available while validating non-core encoded image formats.</summary>
    public List<IGltfImageCodec> ImageCodecs = new List<IGltfImageCodec>();

    /// <summary>Whether validation stops after the first reportable issue.</summary>
    public bool FailFast;

    /// <summary>Whether an error report is converted to an exception by strict entry points.</summary>
    public bool Strict;

    /// <summary>The minimum severity included in the report.</summary>
    public GltfValidationSeverity MinimumSeverity = GltfValidationSeverity.Information;

    /// <summary>The maximum number of collected issues.</summary>
    public int MaximumIssueCount = GltfConstants.DefaultMaximumIssueCount;

    /// <summary>Issue codes omitted from the report.</summary>
    public List<string> SuppressedIssueCodes = new List<string>();

    /// <summary>Per-code severity replacements.</summary>
    public Dictionary<string, GltfValidationSeverity> SeverityOverrides = new Dictionary<string, GltfValidationSeverity>();
}

/// <summary>Represents one stable validation or operation diagnostic.</summary>
public class GltfValidationIssue
{
    /// <summary>The stable issue code.</summary>
    public string Code = "";

    /// <summary>The issue severity.</summary>
    public GltfValidationSeverity Severity = GltfValidationSeverity.Error;

    /// <summary>The human-readable diagnostic message.</summary>
    public string Message = "";

    /// <summary>The RFC 6901 JSON Pointer of the primary location.</summary>
    public string JsonPointer = "";

    /// <summary>Whether a primary byte offset was recorded.</summary>
    public bool HasByteOffset;

    /// <summary>The primary byte offset.</summary>
    public int ByteOffset;

    /// <summary>The related resource URI, when applicable.</summary>
    public string? ResourceUri;

    /// <summary>Related RFC 6901 JSON Pointer locations.</summary>
    public List<string> RelatedJsonPointers = new List<string>();
}

/// <summary>Contains collected issues and statistics from validation.</summary>
public class GltfValidationReport
{
    /// <summary>Whether no error-severity issue was reported.</summary>
    public bool IsValid = true;

    /// <summary>Whether issue collection stopped at its configured cap.</summary>
    public bool IssueLimitReached;

    /// <summary>Issues in deterministic report order.</summary>
    public List<GltfValidationIssue> Issues = new List<GltfValidationIssue>();

    /// <summary>Statistics collected while traversing the document.</summary>
    public GltfDocumentStatistics Statistics = new GltfDocumentStatistics();
}

/// <summary>Contains document, geometry, animation, and resource statistics.</summary>
public class GltfDocumentStatistics
{
    /// <summary>The number of accessors.</summary>
    public int AccessorCount;

    /// <summary>The number of animations.</summary>
    public int AnimationCount;

    /// <summary>The number of buffers.</summary>
    public int BufferCount;

    /// <summary>The number of buffer views.</summary>
    public int BufferViewCount;

    /// <summary>The number of cameras.</summary>
    public int CameraCount;

    /// <summary>The number of images.</summary>
    public int ImageCount;

    /// <summary>The number of materials.</summary>
    public int MaterialCount;

    /// <summary>The number of meshes.</summary>
    public int MeshCount;

    /// <summary>The number of nodes.</summary>
    public int NodeCount;

    /// <summary>The number of samplers.</summary>
    public int SamplerCount;

    /// <summary>The number of scenes.</summary>
    public int SceneCount;

    /// <summary>The number of skins.</summary>
    public int SkinCount;

    /// <summary>The number of textures.</summary>
    public int TextureCount;

    /// <summary>The number of mesh primitives.</summary>
    public int PrimitiveCount;

    /// <summary>The estimated number of referenced vertices.</summary>
    public long VertexCount;

    /// <summary>The estimated number of rendered triangles.</summary>
    public long TriangleCount;

    /// <summary>The estimated number of draw calls.</summary>
    public long DrawCallCount;

    /// <summary>The total number of animation keyframes.</summary>
    public long AnimationKeyframeCount;

    /// <summary>The greatest animation duration in seconds.</summary>
    public double AnimationDuration;

    /// <summary>The total declared buffer bytes.</summary>
    public long BufferByteLength;

    /// <summary>The total resolved image bytes.</summary>
    public long ImageByteLength;

    /// <summary>The number of sparse accessors.</summary>
    public int SparseAccessorCount;

    /// <summary>The number of interleaved buffer views.</summary>
    public int InterleavedBufferViewCount;

    /// <summary>The number of unresolved resources.</summary>
    public int UnresolvedResourceCount;

    /// <summary>The encountered extension names in deterministic order.</summary>
    public List<string> Extensions = new List<string>();

    /// <summary>The optional calculated document bounds.</summary>
    public GltfBounds? Bounds;
}
