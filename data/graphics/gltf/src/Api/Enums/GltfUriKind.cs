namespace gltf;

/// <summary>Classifies a resource URI reference.</summary>
public enum GltfUriKind
{
    /// <summary>No URI was supplied.</summary>
    Absent = 0,

    /// <summary>A relative URI reference.</summary>
    Relative = 1,

    /// <summary>An absolute URI not assigned a more specific kind.</summary>
    Absolute = 2,

    /// <summary>A file URI.</summary>
    File = 3,

    /// <summary>A data URI.</summary>
    Data = 4,

    /// <summary>A URI using a recognized network scheme.</summary>
    Network = 5,

    /// <summary>A URI using an application-defined scheme.</summary>
    Custom = 6
}
