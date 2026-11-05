namespace gltf;

/// <summary>Identifies how conversion handled an extension or codec capability.</summary>
public enum GltfConversionDecisionAction
{
    /// <summary>The input extension declaration was retained.</summary>
    Preserved = 0,

    /// <summary>The extension declaration was added.</summary>
    Added = 1,

    /// <summary>The extension declaration was removed.</summary>
    Removed = 2,

    /// <summary>The codec capability was available to conversion.</summary>
    Available = 3,

    /// <summary>The codec capability was unavailable to conversion.</summary>
    Unavailable = 4,

    /// <summary>The codec implementation was applied by a transform.</summary>
    Applied = 5
}
