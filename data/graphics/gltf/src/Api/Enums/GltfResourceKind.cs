namespace gltf;

/// <summary>Identifies the purpose of a glTF-related resource.</summary>
public enum GltfResourceKind
{
    /// <summary>A glTF binary buffer.</summary>
    Buffer = 0,

    /// <summary>An image resource.</summary>
    Image = 1,

    /// <summary>A JSON Schema resource.</summary>
    Schema = 2,

    /// <summary>An extension-owned resource.</summary>
    Extension = 3,

    /// <summary>Another application-defined resource.</summary>
    Other = 4
}
