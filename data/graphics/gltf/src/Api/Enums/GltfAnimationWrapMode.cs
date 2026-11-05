namespace gltf;

/// <summary>Identifies how sampling maps time outside an animation's key range.</summary>
public enum GltfAnimationWrapMode
{
    /// <summary>Clamp time to the animation's key range.</summary>
    Clamp = 0,

    /// <summary>Loop time through the animation's key range.</summary>
    Loop = 1
}
