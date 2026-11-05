namespace gltf;

/// <summary>Identifies the intended GPU binding target of a glTF buffer view.</summary>
public enum GltfBufferViewTarget
{
    /// <summary>No target was declared.</summary>
    None = 0,

    /// <summary>An array buffer, corresponding to glTF constant <c>34962</c>.</summary>
    ArrayBuffer = 34962,

    /// <summary>An element array buffer, corresponding to glTF constant <c>34963</c>.</summary>
    ElementArrayBuffer = 34963
}
