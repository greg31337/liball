namespace gltf;

/// <summary>Identifies the severity of a validation issue.</summary>
public enum GltfValidationSeverity
{
    /// <summary>Informational output that does not indicate invalid content.</summary>
    Information = 0,

    /// <summary>A warning about suspicious or discouraged content.</summary>
    Warning = 1,

    /// <summary>An error indicating invalid or unusable content.</summary>
    Error = 2
}
