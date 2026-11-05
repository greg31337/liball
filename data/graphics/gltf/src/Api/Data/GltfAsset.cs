namespace gltf;

/// <summary>Represents required glTF asset metadata.</summary>
public class GltfAsset
{
    /// <summary>The glTF version targeted by the asset.</summary>
    public string Version = GltfConstants.CoreVersion;

    /// <summary>The optional minimum glTF version required to read the asset.</summary>
    public string? MinimumVersion;

    /// <summary>The optional tool name that generated the asset.</summary>
    public string? Generator;

    /// <summary>The optional copyright message.</summary>
    public string? Copyright;

    /// <summary>Extension, extras, and unknown JSON state.</summary>
    public GltfPropertyJson PropertyJson = new GltfPropertyJson();
}
