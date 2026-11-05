using System.Collections.Generic;

namespace gltf;

/// <summary>Controls deterministic document serialization and resource layout.</summary>
public class GltfWriteOptions
{
    /// <summary>The output container format.</summary>
    public GltfContainerFormat Format = GltfContainerFormat.Json;

    /// <summary>The resource output representation.</summary>
    public GltfResourceMode ResourceMode = GltfResourceMode.Preserve;

    /// <summary>Whether JSON output is indented.</summary>
    public bool Indented;

    /// <summary>Whether deterministic member and resource ordering is enforced.</summary>
    /// <remarks>Deterministic glTF JSON normalizes negative floating-point zero to the JSON number <c>0</c>.</remarks>
    public bool Deterministic = true;

    /// <summary>Whether external resource file names use lowercase SHA-256 content hashes.</summary>
    public bool UseContentHashResourceNames;

    /// <summary>Whether default-valued optional members are emitted when explicitly present.</summary>
    public bool PreserveExplicitDefaults = true;

    /// <summary>Whether unknown JSON members are emitted.</summary>
    public bool PreserveUnknownMembers = true;

    /// <summary>Whether preserved unknown GLB chunks are emitted.</summary>
    public bool PreserveUnknownChunks = true;

    /// <summary>Whether validation runs before output is modified.</summary>
    public bool Validate = true;

    /// <summary>Whether a caller-owned output stream remains open.</summary>
    public bool LeaveStreamOpen = true;

    /// <summary>Validation options used when validation is enabled.</summary>
    public GltfValidationOptions ValidationOptions = new GltfValidationOptions();

    /// <summary>Registered extension handlers.</summary>
    public List<IGltfExtensionHandler> ExtensionHandlers = new List<IGltfExtensionHandler>();

    /// <summary>Registered geometry compression codecs.</summary>
    public List<IGltfCompressionCodec> CompressionCodecs = new List<IGltfCompressionCodec>();

    /// <summary>Registered image codecs.</summary>
    public List<IGltfImageCodec> ImageCodecs = new List<IGltfImageCodec>();
}

/// <summary>Contains owned primary and resource outputs from a write operation.</summary>
public class GltfWriteResult
{
    /// <summary>The overall operation status.</summary>
    public GltfOperationStatus Status = GltfOperationStatus.Success;

    /// <summary>The owned primary output bytes.</summary>
    public byte[] PrimaryBytes = [];

    /// <summary>The optional JSON text corresponding exactly to the UTF-8 output.</summary>
    public string? JsonText;

    /// <summary>Generated or retained output resources.</summary>
    public GltfResourceSet Resources = new GltfResourceSet();

    /// <summary>The deterministic resource manifest.</summary>
    public List<GltfResourceManifestEntry> Manifest = new List<GltfResourceManifestEntry>();

    /// <summary>The preflight validation report.</summary>
    public GltfValidationReport Validation = new GltfValidationReport();

    /// <summary>Write diagnostics.</summary>
    public List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}
