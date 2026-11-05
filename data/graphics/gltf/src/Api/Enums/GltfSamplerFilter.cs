namespace gltf;

/// <summary>Identifies a glTF texture sampling filter.</summary>
public enum GltfSamplerFilter
{
    /// <summary>Nearest sampling, corresponding to glTF constant <c>9728</c>.</summary>
    Nearest = 9728,

    /// <summary>Linear sampling, corresponding to glTF constant <c>9729</c>.</summary>
    Linear = 9729,

    /// <summary>Nearest sampling from the nearest mip level, corresponding to glTF constant <c>9984</c>.</summary>
    NearestMipmapNearest = 9984,

    /// <summary>Linear sampling from the nearest mip level, corresponding to glTF constant <c>9985</c>.</summary>
    LinearMipmapNearest = 9985,

    /// <summary>Nearest sampling interpolated between mip levels, corresponding to glTF constant <c>9986</c>.</summary>
    NearestMipmapLinear = 9986,

    /// <summary>Linear sampling interpolated between mip levels, corresponding to glTF constant <c>9987</c>.</summary>
    LinearMipmapLinear = 9987
}
