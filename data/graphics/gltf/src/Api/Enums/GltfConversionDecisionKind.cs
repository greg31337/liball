namespace gltf;

/// <summary>Identifies the subject of a conversion decision.</summary>
public enum GltfConversionDecisionKind
{
    /// <summary>An extension declaration decision.</summary>
    Extension = 0,

    /// <summary>A codec capability decision.</summary>
    Codec = 1
}
