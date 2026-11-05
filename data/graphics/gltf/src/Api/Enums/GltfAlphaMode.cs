namespace gltf;

/// <summary>Identifies how a material interprets base-color alpha.</summary>
public enum GltfAlphaMode
{
    /// <summary>Alpha is ignored and the material is fully opaque.</summary>
    Opaque = 0,

    /// <summary>Alpha is compared with an alpha cutoff.</summary>
    Mask = 1,

    /// <summary>Alpha participates in blending.</summary>
    Blend = 2
}
