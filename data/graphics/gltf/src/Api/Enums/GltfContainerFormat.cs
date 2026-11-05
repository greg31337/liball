namespace gltf;

/// <summary>Identifies the supported glTF container formats.</summary>
public enum GltfContainerFormat
{
    /// <summary>Detect the container format from its content.</summary>
    Auto = 0,

    /// <summary>A UTF-8 JSON glTF document.</summary>
    Json = 1,

    /// <summary>A binary glTF version 2 container.</summary>
    Glb = 2
}
