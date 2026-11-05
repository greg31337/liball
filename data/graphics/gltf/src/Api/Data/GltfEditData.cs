using System.Collections.Generic;

namespace gltf;

/// <summary>Controls document cloning, merging, compaction, and reference rebuilding.</summary>
public class GltfEditOptions
{
    /// <summary>Whether resource bytes are copied into independent arrays.</summary>
    public bool CloneResourceBytes = true;

    /// <summary>Whether extension, extras, and unknown JSON values are deep-cloned.</summary>
    public bool CloneJson = true;

    /// <summary>Whether unknown extensions without remap handlers prevent index-changing edits.</summary>
    public bool RequireSafeUnknownExtensionRemap = true;

    /// <summary>Whether the edited document is validated before it is returned.</summary>
    public bool ValidateResult = true;
}

/// <summary>Contains old-to-new index mappings for core and supported extension collections.</summary>
public class GltfIndexRemap
{
    /// <summary>Accessor index mappings.</summary>
    public Dictionary<int, int> Accessors = new Dictionary<int, int>();

    /// <summary>Animation index mappings.</summary>
    public Dictionary<int, int> Animations = new Dictionary<int, int>();

    /// <summary>Buffer index mappings.</summary>
    public Dictionary<int, int> Buffers = new Dictionary<int, int>();

    /// <summary>Buffer view index mappings.</summary>
    public Dictionary<int, int> BufferViews = new Dictionary<int, int>();

    /// <summary>Camera index mappings.</summary>
    public Dictionary<int, int> Cameras = new Dictionary<int, int>();

    /// <summary>Image index mappings.</summary>
    public Dictionary<int, int> Images = new Dictionary<int, int>();

    /// <summary>Material index mappings.</summary>
    public Dictionary<int, int> Materials = new Dictionary<int, int>();

    /// <summary>Mesh index mappings.</summary>
    public Dictionary<int, int> Meshes = new Dictionary<int, int>();

    /// <summary>Node index mappings.</summary>
    public Dictionary<int, int> Nodes = new Dictionary<int, int>();

    /// <summary>Owned resource index mappings.</summary>
    public Dictionary<int, int> Resources = new Dictionary<int, int>();

    /// <summary>Sampler index mappings.</summary>
    public Dictionary<int, int> Samplers = new Dictionary<int, int>();

    /// <summary>Scene index mappings.</summary>
    public Dictionary<int, int> Scenes = new Dictionary<int, int>();

    /// <summary>Skin index mappings.</summary>
    public Dictionary<int, int> Skins = new Dictionary<int, int>();

    /// <summary>Texture index mappings.</summary>
    public Dictionary<int, int> Textures = new Dictionary<int, int>();

    /// <summary><c>KHR_lights_punctual</c> light index mappings.</summary>
    public Dictionary<int, int> PunctualLights = new Dictionary<int, int>();

    /// <summary><c>KHR_materials_variants</c> variant index mappings.</summary>
    public Dictionary<int, int> MaterialVariants = new Dictionary<int, int>();

    /// <summary><c>KHR_xmp_json_ld</c> packet index mappings.</summary>
    public Dictionary<int, int> XmpJsonLdPackets = new Dictionary<int, int>();
}

/// <summary>Contains a merged document, source remaps, and diagnostics.</summary>
public class GltfMergeResult
{
    /// <summary>The overall edit status.</summary>
    public GltfOperationStatus Status = GltfOperationStatus.Success;

    /// <summary>The independently owned merged document.</summary>
    public GltfDocument? Document;

    /// <summary>One complete remap for each source document.</summary>
    public List<GltfIndexRemap> SourceRemaps = new List<GltfIndexRemap>();

    /// <summary>Edit diagnostics.</summary>
    public List<GltfValidationIssue> Diagnostics = new List<GltfValidationIssue>();
}
