using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace gltf;

/// <summary>Describes registry metadata and capabilities for one glTF extension.</summary>
public class GltfExtensionDescriptor
{
    /// <summary>The extension name.</summary>
    public string Name = "";

    /// <summary>The registry status.</summary>
    public GltfExtensionStatus Status = GltfExtensionStatus.Unknown;

    /// <summary>The snapshot or specification version.</summary>
    public string? Version;

    /// <summary>Core or extension object locations where the extension may occur.</summary>
    public List<string> ObjectLocations = new List<string>();

    /// <summary>Extension dependencies.</summary>
    public List<string> Dependencies = new List<string>();

    /// <summary>Known conflicting extensions.</summary>
    public List<string> Conflicts = new List<string>();

    /// <summary>Embedded schema resource names.</summary>
    public List<string> SchemaResources = new List<string>();

    /// <summary>Whether typed model support is available.</summary>
    public bool HasTypedModel;

    /// <summary>Whether semantic validation support is available.</summary>
    public bool HasSemanticValidation;

    /// <summary>Whether index references can be remapped safely.</summary>
    public bool CanRemapReferences;

    /// <summary>Whether fallback core data may make the extension optional.</summary>
    public bool AllowsFallback;

    /// <summary>The optional required codec capability name.</summary>
    public string? RequiredCodec;
}

/// <summary>Provides document, location, JSON, and resource state to an extension handler.</summary>
public class GltfExtensionContext
{
    internal string ObjectJsonPointer = "";

    /// <summary>The containing document.</summary>
    public GltfDocument? Document;

    /// <summary>The extension name.</summary>
    public string Name = "";

    /// <summary>The core object location name.</summary>
    public string ObjectLocation = "";

    /// <summary>Whether an indexed object location was supplied.</summary>
    public bool HasObjectIndex;

    /// <summary>The indexed object location.</summary>
    public int ObjectIndex = GltfConstants.AbsentIndex;

    /// <summary>Whether a nested indexed object location was supplied.</summary>
    public bool HasSubObjectIndex;

    /// <summary>The nested object index, such as a mesh-local primitive index.</summary>
    public int SubObjectIndex = GltfConstants.AbsentIndex;

    /// <summary>The owned or handler-owned extension JSON value.</summary>
    public JsonNode? Json;

    /// <summary>The location-specific typed extension payload supplied for writing or remapping.</summary>
    public GltfTypedExtensionData? TypedData;

    /// <summary>The associated resource set.</summary>
    public GltfResourceSet Resources = new GltfResourceSet();
}

/// <summary>Contains extension handler output and diagnostics.</summary>
public class GltfExtensionResult
{
    /// <summary>The overall handler status.</summary>
    public GltfOperationStatus Status = GltfOperationStatus.Success;

    /// <summary>The extension descriptor when requested.</summary>
    public GltfExtensionDescriptor? Descriptor;

    /// <summary>The owned extension JSON output when requested.</summary>
    public JsonNode? Json;

    /// <summary>The location-specific typed extension payload when available.</summary>
    public GltfTypedExtensionData? TypedData;

    /// <summary>Extension-owned resources.</summary>
    public GltfResourceSet Resources = new GltfResourceSet();

    /// <summary>Index references owned by the extension payload.</summary>
    public List<GltfDependency> References = new List<GltfDependency>();

    /// <summary>Handler diagnostics.</summary>
    public List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}
