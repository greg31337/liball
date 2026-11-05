namespace gltf;

/// <summary>Identifies the property targeted by an animation channel.</summary>
public enum GltfAnimationPath
{
    /// <summary>Node translation.</summary>
    Translation = 0,

    /// <summary>Node rotation.</summary>
    Rotation = 1,

    /// <summary>Node scale.</summary>
    Scale = 2,

    /// <summary>Morph target weights.</summary>
    Weights = 3,

    /// <summary>A JSON Pointer target supplied by <c>KHR_animation_pointer</c>.</summary>
    Pointer = 4
}
