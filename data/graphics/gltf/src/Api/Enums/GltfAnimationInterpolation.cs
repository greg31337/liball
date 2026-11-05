namespace gltf;

/// <summary>Identifies the interpolation algorithm of an animation sampler.</summary>
public enum GltfAnimationInterpolation
{
    /// <summary>Linear interpolation, the glTF default.</summary>
    Linear = 0,

    /// <summary>Use the preceding keyframe value.</summary>
    Step = 1,

    /// <summary>Cubic Hermite spline interpolation.</summary>
    CubicSpline = 2
}
