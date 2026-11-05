namespace gltf;

/// <summary>Identifies a glTF texture coordinate wrapping mode.</summary>
public enum GltfSamplerWrap
{
    /// <summary>Clamp to the edge, corresponding to glTF constant <c>33071</c>.</summary>
    ClampToEdge = 33071,

    /// <summary>Mirrored repeat, corresponding to glTF constant <c>33648</c>.</summary>
    MirroredRepeat = 33648,

    /// <summary>Repeat, corresponding to glTF constant <c>10497</c>.</summary>
    Repeat = 10497
}
